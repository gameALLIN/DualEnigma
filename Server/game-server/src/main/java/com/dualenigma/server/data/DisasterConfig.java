package com.dualenigma.server.data;

import com.dualenigma.network.model.DisasterParams;
import org.springframework.stereotype.Component;

import java.util.HashMap;
import java.util.Map;

/**
 * 灾难参数配置.
 * 35 种灾难，6 大类.
 * TODO: 从配置文件或数据库加载完整参数表.
 */
@Component
public class DisasterConfig {

    private final Map<Integer, DisasterParams> disasterParams = new HashMap<>();

    public DisasterConfig() {
        // TODO: 初始化 35 种灾难参数
        // 当前为占位实现
    }

    /**
     * 获取灾难参数.
     */
    public DisasterParams getParams(int disasterId) {
        return disasterParams.get(disasterId);
    }

    /**
     * 获取灾难类别（0=元素, 1=环境, 2=时空, 3=感知, 4=物理, 5=机制）.
     */
    public int getCategory(int disasterId) {
        return disasterId / 6;
    }
}
