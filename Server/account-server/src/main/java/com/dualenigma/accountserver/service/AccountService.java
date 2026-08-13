package com.dualenigma.accountserver.service;

import com.dualenigma.accountserver.entity.PlayerAccount;
import com.dualenigma.accountserver.repository.PlayerAccountRepository;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.util.Optional;

/**
 * 账号管理服务 — 查询、更新昵称等.
 */
@Service
public class AccountService {

    private final PlayerAccountRepository accountRepository;

    public AccountService(PlayerAccountRepository accountRepository) {
        this.accountRepository = accountRepository;
    }

    /**
     * 根据 ID 查询账号.
     */
    public Optional<PlayerAccount> findById(Long id) {
        return accountRepository.findById(id);
    }

    /**
     * 更新昵称.
     */
    @Transactional
    public Optional<PlayerAccount> updateDisplayName(Long id, String newDisplayName) {
        return accountRepository.findById(id).map(account -> {
            account.setDisplayName(newDisplayName);
            return accountRepository.save(account);
        });
    }
}
