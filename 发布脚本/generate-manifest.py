#!/usr/bin/env python3
"""
生成更新清单 manifest.json
用于 GitHub Release 的增量更新功能

使用方法：
    python generate-manifest.py <版本号> [选项]

示例：
    # 只生成主程序清单（用于小版本更新）
    python generate-manifest.py 1.1.0

    # 生成包含指定目录的清单（用于发布新工具）
    python generate-manifest.py 1.1.0 --patches ./patches

    # 生成包含工具目录的清单（用于完整更新）
    python generate-manifest.py 1.1.0 --include-tools
"""

import os
import sys
import json
import hashlib
import argparse
from pathlib import Path
from datetime import datetime


def calculate_sha256(file_path: str) -> str:
    """计算文件的 SHA256 哈希值"""
    sha256 = hashlib.sha256()
    with open(file_path, 'rb') as f:
        while True:
            data = f.read(81920)
            if not data:
                break
            sha256.update(data)
    return sha256.hexdigest().lower()


def scan_files(base_dir: Path, files_list: list, relative_to: Path) -> dict:
    """扫描指定文件列表，生成文件清单"""
    files = {}
    for file_path in files_list:
        full_path = base_dir / file_path
        if full_path.exists() and full_path.is_file():
            relative_path = str(file_path).replace('\\', '/')
            sha256 = calculate_sha256(str(full_path))
            size = full_path.stat().st_size
            files[relative_path] = {
                "sha256": sha256,
                "size": size,
                "url": f"patches/{relative_path}"
            }
    return files


def scan_directory(base_dir: Path, relative_to: Path, exclude_dirs: set = None) -> dict:
    """扫描目录，生成文件清单"""
    files = {}
    if exclude_dirs is None:
        exclude_dirs = set()

    for file_path in base_dir.rglob('*'):
        if file_path.is_file():
            # 跳过临时文件和隐藏文件
            if file_path.name.endswith('.tmp') or file_path.name.startswith('.'):
                continue

            # 计算相对路径
            relative_path = str(file_path.relative_to(relative_to)).replace('\\', '/')

            # 检查是否在排除目录中
            parts = relative_path.split('/')
            if any(part in exclude_dirs for part in parts):
                continue

            # 计算文件信息
            sha256 = calculate_sha256(str(file_path))
            size = file_path.stat().st_size

            files[relative_path] = {
                "sha256": sha256,
                "size": size,
                "url": f"patches/{relative_path}"
            }

    return files


def main():
    parser = argparse.ArgumentParser(description='生成更新清单 manifest.json')
    parser.add_argument('version', help='版本号，如 1.1.0')
    parser.add_argument('--patches', help='指定变更文件目录')
    parser.add_argument('--include-tools', action='store_true', help='包含工具目录')
    parser.add_argument('--changelog', default='', help='更新日志')
    parser.add_argument('--min-version', default='1.0.0', help='最低可升级版本')

    args = parser.parse_args()

    # 获取项目根目录
    script_dir = Path(__file__).parent
    project_root = script_dir.parent

    # 基础清单
    manifest = {
        "version": args.version,
        "releaseDate": datetime.now().strftime("%Y-%m-%d"),
        "changelog": args.changelog,
        "minUpgradeVersion": args.min_version,
        "files": {}
    }

    # 主程序核心文件（始终包含）
    core_files = [
        '陈叔叔工具箱.exe',
        'App.xaml',
        'App.xaml.cs',
        'MainWindow.xaml',
        'MainWindow.xaml.cs',
        'AssemblyInfo.cs',
        '旧电脑拯救工具箱.csproj',
        'logo.ico',
        'logo.png',
    ]

    # Controls 目录
    controls_dir = project_root / 'Controls'
    if controls_dir.exists():
        for f in controls_dir.glob('*.xaml'):
            core_files.append(f'Controls/{f.name}')
        for f in controls_dir.glob('*.cs'):
            core_files.append(f'Controls/{f.name}')

    # Helpers 目录
    helpers_dir = project_root / 'Helpers'
    if helpers_dir.exists():
        for f in helpers_dir.glob('*.cs'):
            core_files.append(f'Helpers/{f.name}')

    # Models 目录
    models_dir = project_root / 'Models'
    if models_dir.exists():
        for f in models_dir.glob('*.cs'):
            core_files.append(f'Models/{f.name}')

    # 扫描核心文件
    print(f"[INFO] 扫描主程序核心文件...")
    manifest["files"] = scan_files(project_root, core_files, project_root)
    print(f"  找到 {len(manifest['files'])} 个核心文件")

    # 如果指定了 patches 目录，扫描变更文件
    if args.patches:
        patches_path = Path(args.patches)
        if not patches_path.exists():
            print(f"[ERROR] patches 目录不存在: {args.patches}")
            sys.exit(1)

        print(f"[INFO] 扫描 patches 目录: {args.patches}")
        patches_files = scan_directory(patches_path, patches_path)
        manifest["files"].update(patches_files)
        print(f"  找到 {len(patches_files)} 个变更文件")

    # 如果包含工具目录
    if args.include_tools:
        exclude_dirs = {'bin', 'obj', '.git', '.vs', '发布', '发布脚本', '.claude', 'Result'}

        for tool_dir in ['工具', '扩展工具']:
            tool_path = project_root / tool_dir
            if tool_path.exists():
                print(f"[INFO] 扫描 {tool_dir} 目录...")
                tool_files = scan_directory(tool_path, project_root, exclude_dirs)
                manifest["files"].update(tool_files)
                print(f"  找到 {len(tool_files)} 个文件")

    # 输出 manifest.json
    output_path = project_root / "manifest.json"
    with open(output_path, 'w', encoding='utf-8') as f:
        json.dump(manifest, f, indent=2, ensure_ascii=False)

    # 统计
    total_size = sum(f['size'] for f in manifest['files'].values())
    print(f"\n[DONE] 已生成 manifest.json")
    print(f"  版本: {args.version}")
    print(f"  文件数: {len(manifest['files'])}")
    print(f"  总大小: {total_size / 1024 / 1024:.2f} MB")
    print(f"  输出路径: {output_path}")

    # 提示
    print(f"\n[TIP] 发布步骤:")
    print(f"  1. 创建 GitHub Release，Tag 为 v{args.version}")
    print(f"  2. 上传 manifest.json")
    print(f"  3. 上传 patches 目录下的所有文件")
    print(f"  4. 上传安装包（可选）")


if __name__ == "__main__":
    main()
