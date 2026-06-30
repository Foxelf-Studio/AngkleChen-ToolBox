#!/usr/bin/env python3
"""
生成更新清单 manifest.json
用于 GitHub Release 的增量更新功能

使用方法：
    python generate-manifest.py <版本号> [变更目录]

示例：
    python generate-manifest.py 1.1.0
    python generate-manifest.py 1.1.0 ./patches
"""

import os
import sys
import json
import hashlib
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


def scan_directory(base_dir: str, relative_to: str) -> dict:
    """扫描目录，生成文件清单"""
    files = {}
    base_path = Path(base_dir)

    for file_path in base_path.rglob('*'):
        if file_path.is_file():
            # 跳过临时文件和隐藏文件
            if file_path.name.endswith('.tmp') or file_path.name.startswith('.'):
                continue

            # 计算相对路径
            relative_path = str(file_path.relative_to(relative_to)).replace('\\', '/')

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
    if len(sys.argv) < 2:
        print("用法: python generate-manifest.py <版本号> [变更目录]")
        print("示例: python generate-manifest.py 1.1.0")
        print("      python generate-manifest.py 1.1.0 ./patches")
        sys.exit(1)

    version = sys.argv[1]
    changes_dir = sys.argv[2] if len(sys.argv) > 2 else None

    # 获取项目根目录
    script_dir = Path(__file__).parent
    project_root = script_dir.parent

    # 基础清单
    manifest = {
        "version": version,
        "releaseDate": datetime.now().strftime("%Y-%m-%d"),
        "changelog": "",  # 手动填写
        "minUpgradeVersion": "1.0.0",  # 手动填写最低可升级版本
        "files": {}
    }

    if changes_dir:
        # 扫描指定的变更目录
        changes_path = Path(changes_dir)
        if not changes_path.exists():
            print(f"错误: 目录不存在 - {changes_dir}")
            sys.exit(1)

        manifest["files"] = scan_directory(str(changes_path), str(changes_path))
    else:
        # 扫描整个项目（排除不需要的目录）
        exclude_dirs = {'bin', 'obj', '.git', '.vs', '发布', '发布脚本', '.claude'}

        for item in project_root.iterdir():
            if item.name in exclude_dirs:
                continue

            if item.is_file():
                # 主程序文件
                if item.suffix in ['.exe', '.dll', '.json', '.ico', '.png']:
                    relative_path = item.name
                    sha256 = calculate_sha256(str(item))
                    size = item.stat().st_size
                    manifest["files"][relative_path] = {
                        "sha256": sha256,
                        "size": size,
                        "url": f"patches/{relative_path}"
                    }
            elif item.is_dir():
                # 工具目录
                for tool_file in item.rglob('*'):
                    if tool_file.is_file() and not tool_file.name.startswith('.'):
                        relative_path = str(tool_file.relative_to(project_root)).replace('\\', '/')
                        sha256 = calculate_sha256(str(tool_file))
                        size = tool_file.stat().st_size
                        manifest["files"][relative_path] = {
                            "sha256": sha256,
                            "size": size,
                            "url": f"patches/{relative_path}"
                        }

    # 输出 manifest.json
    output_path = project_root / "manifest.json"
    with open(output_path, 'w', encoding='utf-8') as f:
        json.dump(manifest, f, indent=2, ensure_ascii=False)

    print(f"✓ 已生成 manifest.json")
    print(f"  版本: {version}")
    print(f"  文件数: {len(manifest['files'])}")
    print(f"  输出路径: {output_path}")

    # 统计总大小
    total_size = sum(f['size'] for f in manifest['files'].values())
    print(f"  总大小: {total_size / 1024 / 1024:.2f} MB")


if __name__ == "__main__":
    main()
