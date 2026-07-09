"""导表工具入口"""

import os
import sys
import argparse

# 添加父目录到 path，以便导入模块
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from src.utils import load_settings, scan_excel_files, print_result, LEVEL_ERROR
from src.excel_reader import read_excel_sheets
from src.validator import validate
from src.json_exporter import export_json
from src.tsv_exporter import export_tsv


def main():
    parser = argparse.ArgumentParser(description="Excel 导表工具")
    subparsers = parser.add_subparsers(dest="command", help="子命令")

    # export 命令
    export_parser = subparsers.add_parser("export", help="导出数据")
    export_parser.add_argument("--table", help="指定表名（逗号分隔）")
    export_parser.add_argument("--output", help="输出目录")

    # validate 命令
    validate_parser = subparsers.add_parser("validate", help="校验数据")
    validate_parser.add_argument("--table", help="指定表名（逗号分隔）")

    args = parser.parse_args()

    if not args.command:
        parser.print_help()
        sys.exit(1)

    # 加载配置
    settings = load_settings()

    # 获取工具目录
    tool_dir = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

    # 解析输入输出目录
    input_dir = os.path.join(tool_dir, settings["input"])
    if hasattr(args, "output") and args.output:
        output_dir = os.path.join(tool_dir, args.output)
    else:
        output_dir = os.path.join(tool_dir, settings["output"])

    # 扫描 Excel 文件
    excel_files = scan_excel_files(input_dir, settings.get("comment_prefix", "#"))

    if not excel_files:
        print(f"[WARN] 在 {input_dir} 中没有找到 Excel 文件")
        sys.exit(0)

    # 过滤指定的表
    filter_names = None
    if args.table:
        filter_names = [t.strip() for t in args.table.split(",")]

    # 处理每个 Excel 文件
    all_results = []
    export_count = 0

    for file_name, file_path in excel_files:
        print(f"\n处理文件: {file_name}.xlsx")

        # 读取所有 Sheet
        try:
            tables = read_excel_sheets(file_path, settings)
        except Exception as e:
            print(f"[ERROR] {file_name}.xlsx: 读取失败 - {e}")
            continue

        if not tables:
            print(f"  没有有效的 Sheet")
            continue

        # 处理每个 Sheet
        for table in tables:
            # 过滤指定的表
            if filter_names and table.name not in filter_names:
                continue

            print(f"\n  Sheet: {table.name}")

            # 校验
            results = validate(table)
            all_results.extend(results)

            # 输出校验结果
            for result in results:
                print_result(result)

            # 检查是否有错误
            has_error = any(r.level == LEVEL_ERROR for r in results)

            if args.command == "export" and not has_error:
                # 导出 JSON
                client_dir = os.path.join(output_dir, "client")
                json_path = export_json(table, client_dir)
                if json_path:
                    print(f"  → JSON: {json_path}")

                # 导出 TSV
                server_dir = os.path.join(output_dir, "server")
                tsv_path = export_tsv(table, server_dir)
                if tsv_path:
                    print(f"  → TSV: {tsv_path}")

                export_count += 1

    # 汇总
    error_count = sum(1 for r in all_results if r.level == LEVEL_ERROR)
    print(f"\n{'='*50}")
    print(f"完成: 处理 {len(excel_files)} 个 Excel 文件")
    if args.command == "export":
        print(f"导出: {export_count} 个 Sheet")
    print(f"错误: {error_count} 个")


if __name__ == "__main__":
    main()
