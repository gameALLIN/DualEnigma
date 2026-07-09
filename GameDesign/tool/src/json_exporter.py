"""客户端 JSON 导出"""

import json
import os
from .utils import TableData
from .type_parser import convert_value


def export_json(table: TableData, output_dir: str) -> str:
    """导出 JSON 文件

    Args:
        table: 表格数据
        output_dir: 输出目录

    Returns:
        输出文件路径
    """
    # 筛选 C/CS 字段
    client_columns = [col for col in table.columns if col.marker in ("C", "CS")]

    if not client_columns:
        return None

    # 构建 JSON 数据
    json_data = {}
    for row in table.rows:
        # 第一列作为 key
        key_value = row.get(client_columns[0].name)
        if key_value is None:
            continue

        key = str(key_value).strip()

        # 构建该行的数据
        row_data = {}
        for col in client_columns:
            value = row.get(col.name)
            if value is None:
                row_data[col.name] = None
            else:
                try:
                    row_data[col.name] = convert_value(value, col.type_def)
                except (ValueError, TypeError):
                    # 转换失败，保留原始字符串
                    row_data[col.name] = str(value)

        json_data[key] = row_data

    # 确保输出目录存在
    os.makedirs(output_dir, exist_ok=True)

    # 写入文件
    output_path = os.path.join(output_dir, f"{table.name}.json")
    with open(output_path, "w", encoding="utf-8") as f:
        json.dump(json_data, f, ensure_ascii=False, indent=2)

    return output_path
