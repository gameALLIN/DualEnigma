package com.dualenigma.server.logic;

import org.springframework.stereotype.Component;

/**
 * 材料合成验证器.
 * 验证玩家提交的碎片组合是否合法.
 */
@Component
public class SynthesisValidator {

    /**
     * 验证碎片合成请求.
     *
     * @param fragmentIds 碎片 ID 列表
     * @return 合成结果（材料类型），-1 表示合成失败
     */
    public int validate(int[] fragmentIds) {
        if (fragmentIds == null || fragmentIds.length == 0) {
            return -1;
        }

        // TODO: 根据灾难环境查合成表，验证碎片组合是否匹配有效配方
        // 当前占位：返回材料类型 0（水砖）
        return 0;
    }
}
