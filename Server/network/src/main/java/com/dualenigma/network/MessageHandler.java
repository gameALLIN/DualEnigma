package com.dualenigma.network;

import com.dualenigma.v1.Envelope;

/**
 * 消息处理器接口.
 * 每个 Envelope.BodyCase 对应一个实现.
 */
@FunctionalInterface
public interface MessageHandler {

    void handle(ClientSession session, Envelope env);
}
