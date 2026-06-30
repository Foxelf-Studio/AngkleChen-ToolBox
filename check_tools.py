import re, os

with open('MainWindow.xaml.cs', 'r', encoding='utf-8') as f:
    content = f.read()

missing = []
found = []
for match in re.finditer(r'new\("([^"]+)".*?@"(.+?)"', content):
    name = match.group(1)
    raw_path = match.group(2)
    # 转换 Windows 路径分隔符
    path = raw_path.replace("\\", "/")
    full = os.path.join(".", path)
    if os.path.exists(full):
        found.append(name)
    else:
        missing.append((name, path))

print(f"Found: {len(found)}, Missing: {len(missing)}, Total: {len(found)+len(missing)}")
for name, path in missing:
    print(f"  MISSING: {name} -> {path}")
