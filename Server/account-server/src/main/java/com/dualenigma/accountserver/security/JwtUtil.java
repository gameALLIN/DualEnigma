package com.dualenigma.accountserver.security;

import io.jsonwebtoken.Claims;
import io.jsonwebtoken.Jwts;
import io.jsonwebtoken.security.Keys;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Component;

import javax.crypto.SecretKey;
import java.nio.charset.StandardCharsets;
import java.time.Instant;
import java.time.temporal.ChronoUnit;
import java.util.Date;

/**
 * JWT 工具类 — Token 签发与验证.
 */
@Component
public class JwtUtil {

    @Value("${dualenigma.jwt.secret:dualenigma_default_secret_key_change_in_production_2026}")
    private String secret;

    @Value("${dualenigma.jwt.expiration-hours:24}")
    private int expirationHours;

    private SecretKey getSigningKey() {
        return Keys.hmacShaKeyFor(secret.getBytes(StandardCharsets.UTF_8));
    }

    /**
     * 签发 JWT Token.
     */
    public String generateToken(long accountId, String username) {
        Instant now = Instant.now();
        Instant expiry = now.plus(expirationHours, ChronoUnit.HOURS);

        return Jwts.builder()
                .subject(String.valueOf(accountId))
                .claim("username", username)
                .issuedAt(Date.from(now))
                .expiration(Date.from(expiry))
                .signWith(getSigningKey())
                .compact();
    }

    /**
     * 验证 Token 并提取 Claims.
     *
     * @return Claims if valid, null if invalid/expired
     */
    public Claims validateToken(String token) {
        try {
            return Jwts.parser()
                    .verifyWith(getSigningKey())
                    .build()
                    .parseSignedClaims(token)
                    .getPayload();
        } catch (Exception e) {
            return null;
        }
    }

    /**
     * 从 Token 提取 accountId.
     */
    public Long getAccountId(String token) {
        Claims claims = validateToken(token);
        if (claims == null) return null;
        return Long.parseLong(claims.getSubject());
    }
}
