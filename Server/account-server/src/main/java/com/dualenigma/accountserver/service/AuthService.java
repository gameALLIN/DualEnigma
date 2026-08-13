package com.dualenigma.accountserver.service;

import com.dualenigma.accountserver.entity.PlayerAccount;
import com.dualenigma.accountserver.repository.PlayerAccountRepository;
import com.dualenigma.accountserver.security.JwtUtil;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.security.crypto.bcrypt.BCryptPasswordEncoder;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.time.LocalDateTime;
import java.util.Optional;

/**
 * 认证服务 — 注册、登录、Token 签发.
 */
@Service
public class AuthService {

    private static final Logger log = LoggerFactory.getLogger(AuthService.class);

    private final PlayerAccountRepository accountRepository;
    private final JwtUtil jwtUtil;
    private final BCryptPasswordEncoder passwordEncoder = new BCryptPasswordEncoder();

    public AuthService(PlayerAccountRepository accountRepository, JwtUtil jwtUtil) {
        this.accountRepository = accountRepository;
        this.jwtUtil = jwtUtil;
    }

    /**
     * 注册新账号.
     *
     * @return 创建的账号，null 表示用户名已存在
     */
    @Transactional
    public PlayerAccount register(String username, String password, String displayName) {
        if (accountRepository.existsByUsername(username)) {
            return null;
        }

        PlayerAccount account = new PlayerAccount();
        account.setUsername(username);
        account.setPasswordHash(passwordEncoder.encode(password));
        account.setDisplayName(displayName != null && !displayName.isBlank()
                ? displayName : username);

        account = accountRepository.save(account);
        log.info("Account registered: id={}, username={}", account.getId(), username);
        return account;
    }

    /**
     * 登录验证.
     *
     * @return JWT Token，null 表示验证失败
     */
    @Transactional
    public String login(String username, String password) {
        Optional<PlayerAccount> opt = accountRepository.findByUsername(username);
        if (opt.isEmpty()) {
            return null;
        }

        PlayerAccount account = opt.get();
        if (!passwordEncoder.matches(password, account.getPasswordHash())) {
            return null;
        }

        // 更新最后登录时间
        account.setLastLoginAt(LocalDateTime.now());
        accountRepository.save(account);

        String token = jwtUtil.generateToken(account.getId(), account.getUsername());
        log.info("Login success: id={}, username={}", account.getId(), username);
        return token;
    }

    /**
     * 根据 accountId 查询账号信息.
     */
    public Optional<PlayerAccount> getAccountById(Long accountId) {
        return accountRepository.findById(accountId);
    }

    /**
     * 验证 Token 有效性并返回 accountId.
     */
    public Long validateToken(String token) {
        return jwtUtil.getAccountId(token);
    }
}
