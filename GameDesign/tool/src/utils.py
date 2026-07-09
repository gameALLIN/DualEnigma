"""工具函数"""

import os
import sys
from dataclasses import dataclass, field
from typing import Any


# 校验结果级别
LEVEL_OK = "OK"
LEVEL_WARN = "WARN"
LEVEL_ERROR = "ERROR"


@dataclass
class ValidationResult:
    level: str
    file: str
    message: str
    line: int = 0


@dataclass
class TypeDef:
    """类型定义"""
    type: str  # int, float, string, bool, enum, list
    values: list = field(default_factory=list)  # enum 的可选值
    element_type: 'TypeDef' = None  # list 的元素类型


@dataclass
class ColumnInfo:
    """列信息"""
    name: str
    marker: str  # C / S / CS
    type_def: TypeDef


@dataclass
class TableData:
    """表格数据"""
    name: str  # 文件名（不含扩展名）
    columns: list  # list[ColumnInfo]
    rows: list  # list[dict]


def load_settings(settings_path: str = None) -> dict:
    """加载配置文件"""
    import json

    if settings_path is None:
        # 默认路径：tool/config/settings.json
        tool_dir = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
        settings_path = os.path.join(tool_dir, "config", "settings.json")

    defaults = {
        "input": "../Excel",
        "output": "./output",
        "comment_prefix": "#",
        "header_row": 1,
        "marker_row": 2,
        "type_row": 3,
        "desc_row": 4,
        "data_start_row": 5,
    }

    if os.path.exists(settings_path):
        with open(settings_path, "r", encoding="utf-8") as f:
            config = json.load(f)
            defaults.update(config)

    return defaults


def scan_excel_files(input_dir: str, comment_prefix: str = "#") -> list:
    """扫描 Excel 文件，返回 [(文件名, 完整路径), ...]"""
    files = []
    if not os.path.exists(input_dir):
        return files

    for filename in sorted(os.listdir(input_dir)):
        if not filename.endswith(".xlsx"):
            continue
        if filename.startswith(comment_prefix):
            continue
        name = os.path.splitext(filename)[0]
        path = os.path.join(input_dir, filename)
        files.append((name, path))

    return files


def get_output_name(filename: str) -> str:
    """获取输出文件名（不含扩展名）"""
    return os.path.splitext(os.path.basename(filename))[0]


def print_result(result: ValidationResult):
    """格式化输出校验结果"""
    prefix = f"[{result.level}]"
    location = f"{result.file}"
    if result.line > 0:
        location += f":{result.line}"
    print(f"{prefix:8s} {location}: {result.message}")
