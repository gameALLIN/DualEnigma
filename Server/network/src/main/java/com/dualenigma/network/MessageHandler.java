package com.dualenigma.network;

import com.dualenigma.network.protocol.Message;

/**
 * 消息处理器接口.
 * 每个 MessageType 对应一个实现.
 */
@FunctionalInterface
public interface MessageHandler {

    void handle(ClientSession session, Message msg);
}
