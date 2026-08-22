package com.dualenigma.network.protocol;

/**
 * 统一回执错误码。双端单一事实来源（客户端 C# NetErrorCode 枚举逐项镜像）。
 * 码值含义与《网络框架重构计划.md》Task 5.1 码值表一致，禁止单侧改动。
 */
public final class NetErrorCode {
    private NetErrorCode() {}

    public static final int OK = 0;                  // 成功
    public static final int TOKEN_INVALID = 1001;    // Token 校验失败（预留：当前匿名放行策略下不发）
    public static final int UNKNOWN_TYPE = 1002;     // 未支持的消息类型 / 解码失败
    public static final int ROOM_NOT_FOUND = 2001;   // 房间不存在
    public static final int ROOM_FULL = 2002;        // 房间已满
    public static final int GAME_STARTED = 2003;     // 对局已开始（拒绝进房）
    public static final int NOT_HOST = 3001;         // 非房主
    public static final int NOT_FULL = 3002;         // 未满员
    public static final int ALREADY_STARTED = 3003;  // 对局已在进行（拒绝开局）
    public static final int FRAGMENT_REJECTED = 4002;// 碎片上报被拒（不在判定半径）
}
