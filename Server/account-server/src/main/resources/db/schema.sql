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

-- ─── 好友关系表 ───
CREATE TABLE IF NOT EXISTS friendship (
    id            BIGINT AUTO_INCREMENT PRIMARY KEY,
    requester_id  BIGINT NOT NULL,
    addressee_id  BIGINT NOT NULL,
    status        VARCHAR(16) NOT NULL DEFAULT 'PENDING',
    created_at    DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at    DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE KEY uk_fs_pair (requester_id, addressee_id),
    INDEX idx_fs_requester (requester_id, status),
    INDEX idx_fs_addressee (addressee_id, status)
) ENGINE=InnoDB;

-- ─── 房间邀请表 ───
CREATE TABLE IF NOT EXISTS room_invite (
    id          BIGINT AUTO_INCREMENT PRIMARY KEY,
    inviter_id  BIGINT NOT NULL,
    invitee_id  BIGINT NOT NULL,
    room_code   VARCHAR(16) NOT NULL,
    status      VARCHAR(16) NOT NULL DEFAULT 'PENDING',
    created_at  DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_ri_invitee (invitee_id, status),
    INDEX idx_ri_inviter (inviter_id, status)
) ENGINE=InnoDB;
