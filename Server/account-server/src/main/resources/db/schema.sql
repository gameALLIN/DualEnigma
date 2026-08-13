-- MariaDB 11.4 LTS 初始化脚本 — 账号服表（Win + Mac Linux 共用）

CREATE DATABASE IF NOT EXISTS dualenigma CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

USE dualenigma;

-- ─── 账号表 ───
CREATE TABLE IF NOT EXISTS player_account (
    id              BIGINT AUTO_INCREMENT PRIMARY KEY,
    username        VARCHAR(64) NOT NULL UNIQUE,
    password_hash   VARCHAR(255) NOT NULL,
    display_name    VARCHAR(64) NOT NULL,
    created_at      DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    last_login_at   DATETIME NULL,
    INDEX idx_username (username)
) ENGINE=InnoDB;
