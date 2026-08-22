package com.dualenigma.network;

import com.dualenigma.v1.Envelope;
import com.google.protobuf.InvalidProtocolBufferException;

/**
 * Envelope ⇄ bytes（解析失败返回 null，由调用方回 1002）.
 */
public final class ProtoCodec {

    private ProtoCodec() {}

    public static Envelope parse(byte[] bytes) {
        try {
            return Envelope.parseFrom(bytes);
        } catch (InvalidProtocolBufferException e) {
            return null;
        }
    }

    public static byte[] encode(Envelope env) {
        return env.toByteArray();
    }
}
