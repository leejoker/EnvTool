# version.json 生成工具实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 创建独立的 Python 工具，从 Adoptium API 和 GitHub API 抓取 OpenJDK、GraalVM、Liberica 的最新版本信息，生成与原 `versions.json` 格式一致的文件。

**Architecture:** 单文件 Python 程序，通过 HTTP API 获取各 JDK 发行版的最新 release 信息，解析 asset name 映射到平台/架构，输出 JSON 文件。

**Tech Stack:** Python 3.8+, requests 库

---

## 文件结构

```
tools/generate_versions/
  generate_versions.py   # 主程序（单文件，包含所有逻辑）
  requirements.txt       # requests>=2.28.0
  README.md              # 使用说明
```

---

## Task 1: 项目初始化

**Files:**
- Create: `tools/generate_versions/requirements.txt`
- Create: `tools/generate_versions/generate_versions.py` (骨架)
- Create: `tools/generate_versions/README.md`

- [ ] **Step 1: 创建 requirements.txt**

```txt
requests>=2.28.0
```

- [ ] **Step 2: 创建 generate_versions.py 骨架**

```python
#!/usr/bin/env python3
"""
version.json generator for JDK distributions.
Fetches latest versions from Adoptium API and GitHub releases.
"""
import argparse
import json
import sys
from typing import Dict, Any, Optional

import requests


def main():
    parser = argparse.ArgumentParser(description="Generate version.json for JDK distributions")
    parser.add_argument("--output", default="./version.json", help="Output file path")
    parser.add_argument("--jdk-versions", default="25,21,17", help="Comma-separated JDK versions")
    args = parser.parse_args()

    versions = [v.strip() for v in args.jdk_versions.split(",")]
    print(f"Generating version.json for versions: {versions}")


if __name__ == "__main__":
    main()
```

- [ ] **Step 3: 创建 README.md**

```markdown
# version.json Generator

从各 JDK 发行版官方数据源自动抓取最新版本信息，生成 `version.json` 文件。

## 支持的发行版

- OpenJDK (Adoptium API)
- GraalVM Community (GitHub)
- Liberica (GitHub)

## 安装

```bash
pip install -r requirements.txt
```

## 使用

```bash
python generate_versions.py
python generate_versions.py --output ./version.json --jdk-versions 25,21,17
```

## 输出

生成 `version.json` 文件，格式与 jpvm/versions.json 一致。
```

- [ ] **Step 4: 验证骨架运行**

```bash
cd tools/generate_versions && pip install -r requirements.txt && python generate_versions.py
```
Expected: 输出 "Generating version.json for versions: ['25', '21', '17']"

- [ ] **Step 5: 提交**

```bash
git add tools/generate_versions/requirements.txt tools/generate_versions/generate_versions.py tools/generate_versions/README.md
git commit -m "feat: scaffold version.json generator project"
```

---

## Task 2: 实现 Adoptium API (OpenJDK) 获取

**Files:**
- Modify: `tools/generate_versions/generate_versions.py`

- [ ] **Step 1: 编写测试用例**

```python
def test_adoptium_asset_mapping():
    """Test Adoptium API response parsing"""
    # Mock response structure
    mock_response = {
        "binaries": [
            {
                "architecture": "x64",
                "os": "windows",
                "image_type": "jdk",
                "package": {
                    "link": "https://example.com/openjdk-21.0.2_windows-x64_bin.zip"
                }
            }
        ]
    }
    # Should map to windows amd64
    url = extract_adoptium_url(mock_response, "x64", "windows")
    assert url == "https://example.com/openjdk-21.0.2_windows-x64_bin.zip"


def extract_adoptium_url(data: Dict[str, Any], arch: str, os: str) -> Optional[str]:
    """Extract download URL from Adoptium API response"""
    for binary in data.get("binaries", []):
        if binary.get("architecture") == arch and binary.get("os") == os:
            pkg = binary.get("package", {})
            if "link" in pkg:
                return pkg["link"]
    return None
```

- [ ] **Step 2: 运行测试验证失败**

```bash
cd tools/generate_versions && python -m pytest test_generate_versions.py -v
```
Expected: FAIL - function not defined

- [ ] **Step 3: 实现 fetch_adoptium_versions 函数**

```python
ADOPTIUM_API_BASE = "https://api.adoptium.net/v3/assets/latest"


def fetch_adoptium_versions(version: str) -> Dict[str, Dict[str, str]]:
    """
    Fetch OpenJDK versions from Adoptium API.

    Args:
        version: JDK version (e.g., "21")

    Returns:
        Dict mapping platform/arch to download URL.
        Example: {"windows": {"amd64": "url"}, "linux": {"amd64": "url", "aarch64": "url"}}
    """
    result: Dict[str, Dict[str, str]] = {
        "windows": {},
        "linux": {},
        "macos": {}
    }

    arch_map = {
        "x64": "amd64",
        "aarch64": "aarch64"
    }

    os_map = {
        "windows": "windows",
        "linux": "linux",
        "macos": "macos"
    }

    for arch, arch_key in arch_map.items():
        for os_name, os_key in os_map.items():
            url = f"{ADOPTIUM_API_BASE}/{version}?architecture={arch}&os={os_name}&image_type=jdk"
            try:
                resp = requests.get(url, timeout=30)
                resp.raise_for_status()
                data = resp.json()

                download_url = extract_adoptium_url(data, arch, os_name)
                if download_url:
                    result[os_key][arch_key] = download_url

            except requests.RequestException as e:
                print(f"Warning: Failed to fetch {version} {os_name} {arch}: {e}")
                continue

    return result
```

- [ ] **Step 4: 运行测试验证通过**

```bash
cd tools/generate_versions && python -m pytest test_generate_versions.py::test_adoptium_asset_mapping -v
```
Expected: PASS

- [ ] **Step 5: 提交**

```bash
git add tools/generate_versions/generate_versions.py
git commit -m "feat: implement Adoptium API fetch for OpenJDK"
```

---

## Task 3: 实现 GraalVM GitHub API 获取

**Files:**
- Modify: `tools/generate_versions/generate_versions.py`

- [ ] **Step 1: 编写测试用例**

```python
def test_graalvm_asset_parsing():
    """Test GraalVM release asset parsing"""
    mock_release = {
        "tag_name": "jdk-25.0.2",
        "assets": [
            {"name": "graalvm-community-jdk-25.0.2_linux-x64_bin.tar.gz"},
            {"name": "graalvm-community-jdk-25.0.2_linux-aarch64_bin.tar.gz"},
            {"name": "graalvm-community-jdk-25.0.2_macos-x64_bin.tar.gz"},
            {"name": "graalvm-community-jdk-25.0.2_macos-aarch64_bin.tar.gz"},
            {"name": "graalvm-community-jdk-25.0.2_windows-x64_bin.zip"},
        ]
    }
    assets = parse_graalvm_assets(mock_release)
    assert assets["linux"]["amd64"] == "https://github.com/graalvm/graalvm-ce-builds/releases/download/jdk-25.0.2/graalvm-community-jdk-25.0.2_linux-x64_bin.tar.gz"
    assert assets["macos"]["aarch64"] == "https://github.com/graalvm/graalvm-ce-builds/releases/download/jdk-25.0.2/graalvm-community-jdk-25.0.2_macos-aarch64_bin.tar.gz"
    assert assets["windows"]["amd64"] == "https://github.com/graalvm/graalvm-ce-builds/releases/download/jdk-25.0.2/graalvm-community-jdk-25.0.2_windows-x64_bin.zip"


def parse_graalvm_assets(release: Dict[str, Any]) -> Dict[str, Dict[str, str]]:
    """Parse GraalVM release assets into download URLs"""
    # Implementation below
```

- [ ] **Step 2: 运行测试验证失败**

```bash
cd tools/generate_versions && python -m pytest test_generate_versions.py::test_graalvm_asset_parsing -v
```
Expected: FAIL

- [ ] **Step 3: 实现 fetch_graalvm_versions 函数**

```python
GITHUB_API_HEADERS = {
    "Accept": "application/vnd.github+json",
    "User-Agent": "version-json-generator"
}

GRAALVM_REPO = "graalvm/graalvm-ce-builds"


def fetch_graalvm_versions() -> Dict[str, Dict[str, str]]:
    """
    Fetch latest GraalVM release from GitHub API.

    Returns:
        Dict mapping platform/arch to download URL.
    """
    result: Dict[str, Dict[str, str]] = {
        "windows": {},
        "linux": {},
        "macos": {}
    }

    url = f"https://api.github.com/repos/{GRAALVM_REPO}/releases/latest"
    try:
        resp = requests.get(url, headers=GITHUB_API_HEADERS, timeout=30)
        resp.raise_for_status()
        release = resp.json()

        result = parse_graalvm_assets(release)

    except requests.RequestException as e:
        print(f"Warning: Failed to fetch GraalVM release: {e}")

    return result


def parse_graalvm_assets(release: Dict[str, Any]) -> Dict[str, Dict[str, str]]:
    """Parse GraalVM release assets into download URLs"""
    result: Dict[str, Dict[str, str]] = {
        "windows": {},
        "linux": {},
        "macos": {}
    }

    tag = release.get("tag_name", "")
    base_download_url = f"https://github.com/{GRAALVM_REPO}/releases/download/{tag}"

    platform_mapping = {
        ("linux", "x64"): ("linux", "amd64"),
        ("linux", "aarch64"): ("linux", "aarch64"),
        ("macos", "x64"): ("macos", "amd64"),
        ("macos", "aarch64"): ("macos", "aarch64"),
        ("windows", "x64"): ("windows", "amd64"),
    }

    for asset in release.get("assets", []):
        name = asset.get("name", "")
        if not (name.endswith(".tar.gz") or name.endswith(".zip")):
            continue
        if "graalvm-community-jdk" not in name:
            continue

        # Parse platform and arch from filename
        # Format: graalvm-community-jdk-{version}_{platform}-{arch}_bin.{ext}
        parts = name.replace(".tar.gz", "").replace(".zip", "").split("_")
        if len(parts) < 2:
            continue

        platform_arch = parts[-1]  # e.g., "linux-x64" or "windows-x64"
        platform_arch_parts = platform_arch.split("-")
        if len(platform_arch_parts) < 2:
            continue

        os_name = platform_arch_parts[0]
        arch = platform_arch_parts[1]

        if (os_name, arch) in platform_mapping:
            target_os, target_arch = platform_mapping[(os_name, arch)]
            download_url = f"{base_download_url}/{name}"
            result[target_os][target_arch] = download_url

    return result
```

- [ ] **Step 4: 运行测试验证通过**

```bash
cd tools/generate_versions && python -m pytest test_generate_versions.py::test_graalvm_asset_parsing -v
```
Expected: PASS

- [ ] **Step 5: 提交**

```bash
git add tools/generate_versions/generate_versions.py
git commit -m "feat: implement GraalVM GitHub API fetch"
```

---

## Task 4: 实现 Liberica GitHub API 获取

**Files:**
- Modify: `tools/generate_versions/generate_versions.py`

- [ ] **Step 1: 编写测试用例**

```python
def test_liberica_asset_parsing():
    """Test Liberica release asset parsing"""
    mock_release = {
        "tag_name": "26.0.1+10",
        "assets": [
            {"name": "bellsoft-jdk26.0.1+10-linux-amd64-full.tar.gz"},
            {"name": "bellsoft-jdk26.0.1+10-linux-aarch64-full.tar.gz"},
            {"name": "bellsoft-jdk26.0.1+10-macos-amd64-full.zip"},
            {"name": "bellsoft-jdk26.0.1+10-macos-aarch64-full.zip"},
            {"name": "bellsoft-jdk26.0.1+10-windows-amd64-full.zip"},
            # Should be ignored
            {"name": "bellsoft-jdk26.0.1+10-linux-amd64-full.deb"},
            {"name": "bellsoft-jdk26.0.1+10-linux-amd64-lite.tar.gz"},
        ]
    }
    assets = parse_liberica_assets(mock_release)
    assert assets["linux"]["amd64"] == "https://github.com/bell-sw/Liberica/releases/download/26.0.1+10/bellsoft-jdk26.0.1+10-linux-amd64-full.tar.gz"
    assert assets["windows"]["amd64"] == "https://github.com/bell-sw/Liberica/releases/download/26.0.1+10/bellsoft-jdk26.0.1+10-windows-amd64-full.zip"
    # Lite packages should be excluded
    assert "aarch64" not in assets["macos"] or assets["macos"].get("aarch64", "").endswith("-full.zip")


def parse_liberica_assets(release: Dict[str, Any]) -> Dict[str, Dict[str, str]]:
    """Parse Liberica release assets into download URLs"""
    # Implementation below
```

- [ ] **Step 2: 运行测试验证失败**

```bash
cd tools/generate_versions && python -m pytest test_generate_versions.py::test_liberica_asset_parsing -v
```
Expected: FAIL

- [ ] **Step 3: 实现 fetch_liberica_versions 和 parse_liberica_assets 函数**

```python
LIBERICA_REPO = "bell-sw/Liberica"


def fetch_liberica_versions() -> Dict[str, Dict[str, str]]:
    """
    Fetch latest Liberica release from GitHub API.

    Returns:
        Dict mapping platform/arch to download URL.
    """
    result: Dict[str, Dict[str, str]] = {
        "windows": {},
        "linux": {},
        "macos": {}
    }

    url = f"https://api.github.com/repos/{LIBERICA_REPO}/releases/latest"
    try:
        resp = requests.get(url, headers=GITHUB_API_HEADERS, timeout=30)
        resp.raise_for_status()
        release = resp.json()

        result = parse_liberica_assets(release)

    except requests.RequestException as e:
        print(f"Warning: Failed to fetch Liberica release: {e}")

    return result


def parse_liberica_assets(release: Dict[str, Any]) -> Dict[str, Dict[str, str]]:
    """Parse Liberica release assets into download URLs"""
    result: Dict[str, Dict[str, str]] = {
        "windows": {},
        "linux": {},
        "macos": {}
    }

    tag = release.get("tag_name", "")
    base_download_url = f"https://github.com/{LIBERICA_REPO}/releases/download/{tag}"

    # Only process .tar.gz and .zip (full packages, not lite)
    for asset in release.get("assets", []):
        name = asset.get("name", "")

        # Skip non-tarball/zip files
        if not (name.endswith(".tar.gz") or name.endswith(".zip")):
            continue

        # Skip lite packages
        if "-lite." in name:
            continue

        # Parse platform and arch from filename
        # Format: bellsoft-jdk{version}-{platform}-{arch}-full.{ext}
        # Examples: bellsoft-jdk26.0.1+10-linux-amd64-full.tar.gz
        #           bellsoft-jdk26.0.1+10-windows-amd64-full.zip
        if not name.startswith("bellsoft-jdk"):
            continue

        parts = name.replace(".tar.gz", "").replace(".zip", "").split("-")
        if len(parts) < 4:
            continue

        # Find platform and arch from the parts
        # Format: bellsoft, jdk{version}, {platform}, {arch}, full
        arch = None
        platform = None

        for i, part in enumerate(parts):
            if part in ("linux", "macos", "windows"):
                platform = part
                if i + 1 < len(parts):
                    arch = parts[i + 1]
                break

        if not platform or not arch:
            continue

        # Map to our standard format
        if platform == "windows":
            if arch == "amd64":
                result["windows"]["amd64"] = f"{base_download_url}/{name}"
        elif platform == "linux":
            if arch in ("amd64", "x64"):
                result["linux"]["amd64"] = f"{base_download_url}/{name}"
            elif arch == "aarch64":
                result["linux"]["aarch64"] = f"{base_download_url}/{name}"
        elif platform == "macos":
            if arch in ("amd64", "x64"):
                result["macos"]["amd64"] = f"{base_download_url}/{name}"
            elif arch == "aarch64":
                result["macos"]["aarch64"] = f"{base_download_url}/{name}"

    return result
```

- [ ] **Step 4: 运行测试验证通过**

```bash
cd tools/generate_versions && python -m pytest test_generate_versions.py::test_liberica_asset_parsing -v
```
Expected: PASS

- [ ] **Step 5: 提交**

```bash
git add tools/generate_versions/generate_versions.py
git commit -m "feat: implement Liberica GitHub API fetch"
```

---

## Task 5: 实现 JSON 组装和主流程

**Files:**
- Modify: `tools/generate_versions/generate_versions.py`

- [ ] **Step 1: 编写集成测试**

```python
def test_full_generation():
    """Test full version.json generation (mocked)"""
    # This test would verify the structure is correct
    result = generate_version_json(["25", "21", "17"])
    assert "openjdk" in result
    assert "graalvm" in result
    assert "liberica" in result
    assert "25" in result["openjdk"]
    assert "21" in result["openjdk"]
    assert "17" in result["openjdk"]


def generate_version_json(versions: list) -> Dict[str, Any]:
    """Generate the complete version.json structure"""
    # Implementation in step 3
```

- [ ] **Step 2: 运行测试验证失败**

```bash
cd tools/generate_versions && python -m pytest test_generate_versions.py::test_full_generation -v
```
Expected: FAIL

- [ ] **Step 3: 实现 generate_version_json 和 main 函数**

```python
LTS_VERSIONS = {"21", "17"}  # OpenJDK 21 and 17 are LTS


def generate_version_json(versions: list) -> Dict[str, Any]:
    """
    Generate complete version.json structure.

    Args:
        versions: List of JDK major versions to fetch (e.g., ["25", "21", "17"])

    Returns:
        Complete version.json structure
    """
    result: Dict[str, Any] = {}

    # OpenJDK
    openjdk_data: Dict[str, Any] = {}
    for version in versions:
        version_data = fetch_adoptium_versions(version)
        openjdk_data[version] = version_data
        openjdk_data[version]["LTS"] = version in LTS_VERSIONS
    result["openjdk"] = openjdk_data

    # GraalVM
    graalvm_data: Dict[str, Any] = {}
    graalvm_versions = fetch_graalvm_versions()
    for version in versions:
        # GraalVM uses different versioning, need to get latest for each
        # For simplicity, use the same latest version for all requested
        if version in graalvm_versions:
            graalvm_data[version] = graalvm_versions[version]
    result["graalvm"] = graalvm_data

    # Liberica
    liberica_data: Dict[str, Any] = {}
    liberica_versions = fetch_liberica_versions()
    for version in versions:
        if version in liberica_versions:
            liberica_data[version] = liberica_versions[version]
    result["liberica"] = liberica_data

    return result


def main():
    parser = argparse.ArgumentParser(description="Generate version.json for JDK distributions")
    parser.add_argument("--output", default="./version.json", help="Output file path")
    parser.add_argument("--jdk-versions", default="25,21,17", help="Comma-separated JDK versions")
    args = parser.parse_args()

    versions = [v.strip() for v in args.jdk_versions.split(",")]
    print(f"Generating version.json for versions: {versions}")

    result = generate_version_json(versions)

    output_path = args.output
    with open(output_path, "w", encoding="utf-8") as f:
        json.dump(result, f, indent=2, ensure_ascii=False)

    print(f"version.json generated successfully at {output_path}")
```

- [ ] **Step 4: 运行集成测试验证通过**

```bash
cd tools/generate_versions && python -m pytest test_generate_versions.py::test_full_generation -v
```
Expected: PASS

- [ ] **Step 5: 手动运行完整生成**

```bash
cd tools/generate_versions && python generate_versions.py && cat version.json
```
Expected: 生成完整的 version.json 文件（可能因网络问题部分数据为空）

- [ ] **Step 6: 提交**

```bash
git add tools/generate_versions/generate_versions.py
git commit -m "feat: implement full generation pipeline"
```

---

## Task 6: 最终测试和文档更新

**Files:**
- Modify: `tools/generate_versions/generate_versions.py`
- Modify: `tools/generate_versions/README.md`

- [ ] **Step 1: 添加 LTS 标记逻辑给 GraalVM 和 Liberica**

```python
# In generate_version_json:
# GraalVM 21 and 25 are considered LTS (need to verify)
# For now, leave as default (no LTS field for graalvm/liberica as in original)
```

- [ ] **Step 2: 更新 README 添加更详细的输出示例**

```markdown
## 输出格式

```json
{
  "openjdk": {
    "25": {
      "windows": { "amd64": "https://..." },
      "linux": { "amd64": "https://...", "aarch64": "https://..." },
      "macos": { "amd64": "https://...", "aarch64": "https://..." },
      "LTS": false
    },
    "21": { "windows": {...}, "LTS": true }
  },
  "graalvm": { "25": {...} },
  "liberica": { "25": {...} }
}
```

## 注意事项

- OpenJDK 数据来自 Adoptium API
- GraalVM 和 Liberica 数据来自 GitHub Releases
- 部分平台/架构可能因官方未发布而为空
```

- [ ] **Step 3: 验证所有测试通过**

```bash
cd tools/generate_versions && python -m pytest -v
```

- [ ] **Step 4: 提交最终更改**

```bash
git add tools/generate_versions/README.md
git commit -m "chore: update README and finalize"
```

---

## 验证检查清单

- [ ] `generate_versions.py` 可独立运行
- [ ] 生成 `version.json` 格式与原文件一致
- [ ] OpenJDK 包含 windows/linux/macos 和 amd64/aarch64
- [ ] GraalVM 包含所有平台
- [ ] Liberica 包含所有平台
- [ ] LTS 标记正确（OpenJDK 21, 17 为 true，25 为 false）
- [ ] 错误处理：单个 API 失败不影响整体生成
- [ ] README 文档完整