package com.dualenigma.network;

import com.dualenigma.network.protocol.Message;
import com.fasterxml.jackson.core.JsonProcessingException;
import com.fasterxml.jackson.databind.DeserializationFeature;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.databind.SerializationFeature;
import org.springframework.stereotype.Component;

/**
 * JSON 消息编解码器.
 * 统一使用 Jackson 序列化/反序列化.
 */
@Component
public class MessageCodec {

    private final ObjectMapper objectMapper;

    public MessageCodec() {
        this.objectMapper = new ObjectMapper();
        this.objectMapper.configure(SerializationFeature.FAIL_ON_EMPTY_BEANS, false);
        // 客户端 data 携带服务端未定义的字段（如 playerId）时容忍而不是抛异常丢弃整条消息
        this.objectMapper.configure(DeserializationFeature.FAIL_ON_UNKNOWN_PROPERTIES, false);
    }

    /**
     * JSON 字符串 → Message 对象.
     */
    public Message decode(String json) throws JsonProcessingException {
        return objectMapper.readValue(json, Message.class);
    }

    /**
     * Message 对象 → JSON 字符串.
     */
    public String encode(Message message) throws JsonProcessingException {
        return objectMapper.writeValueAsString(message);
    }

    /**
     * 获取底层 ObjectMapper（供直接操作 JSON 时使用）.
     */
    public ObjectMapper getObjectMapper() {
        return objectMapper;
    }
}
