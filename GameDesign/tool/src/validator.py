"""数据校验"""

from .utils import TableData, ValidationResult, LEVEL_ERROR, LEVEL_WARN
from .type_parser import convert_value


def validate(table: TableData) -> list:
    """校验表格数据

    Args:
        table: 表格数据

    Returns:
        校验结果列表
    """
    results = []

    if not table.columns:
        results.append(ValidationResult(
            level=LEVEL_WARN,
            file=f"{table.name}.xlsx",
            message="没有有效列",
        ))
        return results

    if not table.rows:
        results.append(ValidationResult(
            level=LEVEL_WARN,
            file=f"{table.name}.xlsx",
            message="没有数据行",
        ))
        return results

    # 获取主键列（第一列）
    key_column = table.columns[0]
    key_values = set()

    # 逐行校验
    for row_idx, row in enumerate(table.rows, start=5):  # 数据从第5行开始
        row_file = f"{table.name}.xlsx"

        # 1. 主键校验
        key_value = row.get(key_column.name)
        if key_value is None or (isinstance(key_value, str) and key_value.strip() == ""):
            results.append(ValidationResult(
                level=LEVEL_ERROR,
                file=row_file,
                line=row_idx,
                message=f"主键 '{key_column.name}' 不能为空",
            ))
            continue

        key_str = str(key_value).strip()
        if key_str in key_values:
            results.append(ValidationResult(
                level=LEVEL_ERROR,
                file=row_file,
                line=row_idx,
                message=f"主键值 '{key_str}' 重复",
            ))
        key_values.add(key_str)

        # 2. CS 字段必填校验
        for col in table.columns[1:]:
            if col.marker == "CS":
                value = row.get(col.name)
                if value is None or (isinstance(value, str) and value.strip() == ""):
                    results.append(ValidationResult(
                        level=LEVEL_ERROR,
                        file=row_file,
                        line=row_idx,
                        message=f"CS 字段 '{col.name}' 不能为空",
                    ))

        # 3. 类型和枚举校验
        for col in table.columns:
            value = row.get(col.name)
            if value is None or (isinstance(value, str) and value.strip() == ""):
                continue

            try:
                convert_value(value, col.type_def)
            except ValueError as e:
                results.append(ValidationResult(
                    level=LEVEL_ERROR,
                    file=row_file,
                    line=row_idx,
                    message=f"字段 '{col.name}' 校验失败: {e}",
                ))

    # 汇总
    error_count = sum(1 for r in results if r.level == LEVEL_ERROR)
    if error_count == 0:
        results.append(ValidationResult(
            level="OK",
            file=f"{table.name}.xlsx",
            message=f"校验通过，共 {len(table.rows)} 条数据",
        ))

    return results
