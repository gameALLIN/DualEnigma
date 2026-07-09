"""服务器 TSV 导出"""

import csv
import os
from .utils import TableData
from .type_parser import convert_value


def export_tsv(table: TableData, output_dir: str) -> str:
    """导出 TSV 文件

    Args:
        table: 表格数据
        output_dir: 输出目录

    Returns:
        输出文件路径
    """
    # 筛选 S/CS 字段
    server_columns = [col for col in table.columns if col.marker in ("S", "CS")]

    if not server_columns:
        return None

    # 确保输出目录存在
    os.makedirs(output_dir, exist_ok=True)

    # 写入文件
    output_path = os.path.join(output_dir, f"{table.name}.tsv")
    with open(output_path, "w", encoding="utf-8", newline="") as f:
        writer = csv.writer(f, delimiter="\t")

        # 行1：字段名
        writer.writerow([col.name for col in server_columns])

        # 行2：字段类型
        writer.writerow([_type_to_string(col.type_def) for col in server_columns])

        # 数据行
        for row in table.rows:
            row_data = []
            for col in server_columns:
                value = row.get(col.name)
                if value is None:
                    row_data.append("")
                else:
                    try:
                        converted = convert_value(value, col.type_def)
                        row_data.append(_value_to_string(converted, col.type_def))
                    except (ValueError, TypeError):
                        row_data.append(str(value))
            writer.writerow(row_data)

    return output_path


def _type_to_string(type_def) -> str:
    """将 TypeDef 转换为类型字符串"""
    if type_def.type == "enum":
        return f"enum[{','.join(type_def.values)}]"
    elif type_def.type == "list":
        inner = _type_to_string(type_def.element_type) if type_def.element_type else "string"
        return f"list[{inner}]"
    else:
        return type_def.type


def _value_to_string(value, type_def) -> str:
    """将值转换为 TSV 字符串"""
    if value is None:
        return ""

    if type_def.type == "list":
        if isinstance(value, list):
            if type_def.element_type and type_def.element_type.type == "list":
                # 嵌套列表：内层用分号分隔
                inner_lists = [";".join(str(item) for item in sub_list) for sub_list in value]
                return ",".join(inner_lists)
            else:
                return ",".join(str(item) for item in value)
        return str(value)

    return str(value)
