-- MariaDB 11.4 LTS 初始化脚本 — 游戏服表（Win + Mac Linux 共用）
-- 注意：player_account 表由 account-server 模块管理，见 account-server/src/main/resources/db/schema.sql

CREATE DATABASE IF NOT EXISTS dualenigma CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

USE dualenigma;

-- ─── 房间表 ───
CREATE TABLE IF NOT EXISTS game_room (
    id              VARCHAR(16) PRIMARY KEY,          -- roomCode
    player0_id      BIGINT NULL,                      -- 水人账号 ID
    player1_id      BIGINT NULL,                      -- 火人账号 ID
    status          ENUM('waiting','playing','finished','abandoned') NOT NULL DEFAULT 'waiting',
    created_at      DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    started_at      DATETIME NULL,
    ended_at        DATETIME NULL,
    INDEX idx_status (status),
    INDEX idx_player0 (player0_id),
    INDEX idx_player1 (player1_id)
) ENGINE=InnoDB;

-- ─── 游戏进度表（每局一条，36 轮更新） ───
CREATE TABLE IF NOT EXISTS game_progress (
    id              BIGINT AUTO_INCREMENT PRIMARY KEY,
    room_id         VARCHAR(16) NOT NULL,
    chapter         INT NOT NULL DEFAULT 1,           -- 当前章节 (1-3)
    section         INT NOT NULL DEFAULT 1,           -- 当前节 (1-4)
    round           INT NOT NULL DEFAULT 1,           -- 当前轮 (1-3)
    current_phase   VARCHAR(32) NOT NULL DEFAULT 'Preview',
    phase_end_time  DATETIME NULL,                    -- 阶段结束时间
    score           INT NOT NULL DEFAULT 0,
    updated_at      DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_room (room_id)
) ENGINE=InnoDB;

-- ─── 玩家运行时状态表（每局每玩家一条） ───
CREATE TABLE IF NOT EXISTS player_state (
    id              BIGINT AUTO_INCREMENT PRIMARY KEY,
    room_id         VARCHAR(16) NOT NULL,
    player_id       TINYINT NOT NULL,                 -- 0=Aqua, 1=Ignis
    account_id      BIGINT NOT NULL,
    hp              INT NOT NULL DEFAULT 100,
    shelter_energy  FLOAT NOT NULL DEFAULT 100.0,
    pos_x           FLOAT NOT NULL DEFAULT 0.0,
    pos_y           FLOAT NOT NULL DEFAULT 0.0,
    velocity_x      FLOAT NOT NULL DEFAULT 0.0,
    velocity_y      FLOAT NOT NULL DEFAULT 0.0,
    anim_state      VARCHAR(16) NOT NULL DEFAULT 'Idle',
    facing          BOOLEAN NOT NULL DEFAULT TRUE,
    carried_fragments JSON NULL,                      -- [0,0,1] 碎片类型列表
    INDEX idx_room_player (room_id, player_id)
) ENGINE=InnoDB;

-- ─── 建筑状态表 ───
CREATE TABLE IF NOT EXISTS building_state (
    id              BIGINT AUTO_INCREMENT PRIMARY KEY,
    room_id         VARCHAR(16) NOT NULL,
    building_id     INT NOT NULL,                     -- 游戏内建筑 ID
    building_type   INT NOT NULL,
    material        INT NOT NULL,
    grid_x          INT NOT NULL,
    grid_y          INT NOT NULL,
    current_hp      FLOAT NOT NULL,
    max_hp          FLOAT NOT NULL,
    placed_by       TINYINT NOT NULL,                -- 放置者 playerId
    INDEX idx_room (room_id),
    UNIQUE KEY uk_room_building (room_id, building_id)
) ENGINE=InnoDB;

-- ─── 碎片状态表 ───
CREATE TABLE IF NOT EXISTS fragment_state (
    id              BIGINT AUTO_INCREMENT PRIMARY KEY,
    room_id         VARCHAR(16) NOT NULL,
    fragment_id     INT NOT NULL,
    fragment_type   TINYINT NOT NULL,                 -- 0=冰晶, 1=熔岩, 2=岩石
    pos_x           FLOAT NOT NULL,
    pos_y           FLOAT NOT NULL,
    state           VARCHAR(16) NOT NULL DEFAULT 'Falling',  -- Falling/Collected/Despawned
    drop_time       FLOAT NOT NULL,
    INDEX idx_room (room_id),
    INDEX idx_room_state (room_id, state)
) ENGINE=InnoDB;

-- ─── 天赋选择记录表 ───
CREATE TABLE IF NOT EXISTS talent_record (
    id              BIGINT AUTO_INCREMENT PRIMARY KEY,
    room_id         VARCHAR(16) NOT NULL,
    player_id       TINYINT NOT NULL,
    talent_id       INT NOT NULL,
    global_round    INT NOT NULL,                    -- 第几轮选择 (1-36)
    selected_at     DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_room_player (room_id, player_id)
) ENGINE=InnoDB;

-- ─── 技能状态表 ───
CREATE TABLE IF NOT EXISTS skill_state (
    id              BIGINT AUTO_INCREMENT PRIMARY KEY,
    room_id         VARCHAR(16) NOT NULL,
    player_id       TINYINT NOT NULL,
    skill_id        INT NOT NULL,
    cooldown_remaining FLOAT NOT NULL DEFAULT 0.0,
    use_count       INT NOT NULL DEFAULT 0,
    INDEX idx_room_player (room_id, player_id)
) ENGINE=InnoDB;

-- ─── 灾难状态表 ───
CREATE TABLE IF NOT EXISTS disaster_state (
    id              BIGINT AUTO_INCREMENT PRIMARY KEY,
    room_id         VARCHAR(16) NOT NULL,
    disaster_id     INT NOT NULL,
    difficulty_mult FLOAT NOT NULL,
    random_seed     BIGINT NOT NULL,
    elapsed_time    FLOAT NOT NULL DEFAULT 0.0,
    is_active       BOOLEAN NOT NULL DEFAULT FALSE,
    INDEX idx_room (room_id)
) ENGINE=InnoDB;

-- ─── 对局结算表 ───
CREATE TABLE IF NOT EXISTS game_result (
    id              BIGINT AUTO_INCREMENT PRIMARY KEY,
    room_id         VARCHAR(16) NOT NULL,
    is_victory      BOOLEAN NOT NULL,
    player0_alive   BOOLEAN NOT NULL,
    player1_alive   BOOLEAN NOT NULL,
    final_score     INT NOT NULL,
    duration_sec    INT NOT NULL,
    ended_at        DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_room (room_id)
) ENGINE=InnoDB;
