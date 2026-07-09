"""类型定义解析"""

import re
from .utils import TypeDef


def parse_marker_type(cell_value: str) -> tuple:
    """解析组合格式 "CS:enum[v1,v2]"

    Returns:
        (marker, TypeDef)
    """
    if not cell_value:
        return "CS", TypeDef(type="string")

    cell_value = cell_value.strip()

    # 解析标记部分
    if ":" in cell_value:
        parts = cell_value.split(":", 1)
        marker = parts[0].strip().upper()
        type_str = parts[1].strip()
    else:
        # 没有标记，默认 CS
        marker = "CS"
        type_str = cell_value.strip()

    # 验证标记
    if marker not in ("C", "S", "CS"):
        marker = "CS"

    type_def = parse_type(type_str)
    return marker, type_def


def parse_type(type_str: str) -> TypeDef:
    """解析类型字符串

    支持：
    - int, float, string, bool
    - enum[v1,v2,v3]
    - list[类型]
    - list[list[int]]（嵌套）
    """
    type_str = type_str.strip()

    # 枚举类型 enum[v1,v2,...]
    if type_str.startswith("enum[") and type_str.endswith("]"):
        values_str = type_str[5:-1]
        values = [v.strip() for v in values_str.split(",") if v.strip()]
        return TypeDef(type="enum", values=values)

    # 列表类型 list[类型]
    if type_str.startswith("list[") and type_str.endswith("]"):
        inner = type_str[5:-1]
        element_type = parse_type(inner)
        return TypeDef(type="list", element_type=element_type)

    # 基础类型
    if type_str in ("int", "float", "string", "bool"):
        return TypeDef(type=type_str)

    # 未知类型默认 string
    return TypeDef(type="string")


def convert_value(value, type_def: TypeDef):
    """根据类型定义转换值

    Args:
        value: 原始值（通常是字符串）
        type_def: 类型定义

    Returns:
        转换后的 Python 值

    Raises:
        ValueError: 转换失败
    """
    if value is None or (isinstance(value, str) and value.strip() == ""):
        return None

    value_str = str(value).strip()

    if type_def.type == "int":
        return int(float(value_str))

    elif type_def.type == "float":
        return float(value_str)

    elif type_def.type == "bool":
        return value_str.lower() in ("true", "1", "yes")

    elif type_def.type == "enum":
        if value_str not in type_def.values:
            raise ValueError(f"枚举值 '{value_str}' 不在允许范围 {type_def.values} 内")
        return value_str

    elif type_def.type == "list":
        # 列表值：逗号分隔，内层用分号
        if ";" in value_str and type_def.element_type and type_def.element_type.type == "list":
            # 嵌套列表：1,2;3,4
            items = value_str.split(";")
            return [_convert_list_item(item.strip(), type_def.element_type) for item in items if item.strip()]
        else:
            # 普通列表：1,2,3
            items = value_str.split(",")
            return [_convert_list_item(item.strip(), type_def.element_type) for item in items if item.strip()]

    else:  # string
        return str(value_str)


def _convert_list_item(item_str: str, element_type: TypeDef):
    """转换列表中的单个元素"""
    if element_type is None:
        return item_str

    if element_type.type == "int":
        return int(float(item_str))
    elif element_type.type == "float":
        return float(item_str)
    elif element_type.type == "bool":
        return item_str.lower() in ("true", "1", "yes")
    elif element_type.type == "enum":
        if item_str not in element_type.values:
            raise ValueError(f"枚举值 '{item_str}' 不在允许范围 {element_type.values} 内")
        return item_str
    else:
        return str(item_str)
