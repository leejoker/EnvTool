# version.json 生成工具设计

## 概述

独立的 Python 工具，用于从各发行版的官方数据源自动抓取最新版本信息，生成 `version.json` 文件。

## 数据源

| 发行版 | 数据来源 | API 端点 |
|--------|----------|----------|
| OpenJDK | Adoptium API | `https://api.adoptium.net/v3/assets/latest/{version}?architecture={arch}&os={os}&image_type=jdk` |
| GraalVM | GitHub API | `https://api.github.com/repos/graalvm/graalvm-ce-builds/releases/latest` |
| Liberica | GitHub API | `https://api.github.com/repos/bell-sw/Liberica/releases/latest` |

## 输出格式

与原 `versions.json` 结构完全一致：

```json
{
  "openjdk": {
    "25": {
      "windows": { "amd64": "url" },
      "linux": { "amd64": "url", "aarch64": "url" },
      "macos": { "amd64": "url", "aarch64": "url" },
      "LTS": false
    },
    "21": {
      "windows": { "amd64": "url" },
      "linux": { "amd64": "url", "aarch64": "url" },
      "macos": { "amd64": "url", "aarch64": "url" },
      "LTS": true
    },
    "17": { ... }
  },
  "graalvm": {
    "25": { ... },
    "21": { ... },
    "17": { ... }
  },
  "liberica": {
    "25": { ... },
    "21": { ... },
    "17": { ... }
  }
}
```

## 架构

### 文件结构

```
tools/generate_versions/
  generate_versions.py   # 主程序（单文件）
  requirements.txt       # 依赖（仅 requests）
  README.md              # 使用说明
```

### 核心模块

| 函数 | 职责 |
|------|------|
| `fetch_adoptium_versions(version)` | 从 Adoptium API 获取 OpenJDK 版本信息 |
| `fetch_graalvm_versions()` | 从 GitHub 获取 GraalVM 最新 release |
| `fetch_liberica_versions()` | 从 GitHub 获取 Liberica 最新 release |
| `build_download_url(asset_name, release_tag)` | 从 asset name 构建下载 URL |
| `generate_version_json(versions)` | 组装最终 JSON 结构 |
| `main()` | 入口函数，协调整个流程 |

## 各发行版处理逻辑

### OpenJDK (Adoptium API)

1. 调用 `GET https://api.adoptium.net/v3/assets/latest/{version}?architecture={arch}&os={os}&image_type=jdk`
2. 对每个版本 (25, 21, 17) 和架构 (x64, aarch64) 和平台 (windows, linux, macos) 发起请求
3. 提取 `binaries[].download_url`

### GraalVM (GitHub API)

1. 调用 `GET https://api.github.com/repos/graalvm/graalvm-ce-builds/releases/latest`
2. 从 `tag_name` 提取版本号 (如 `jdk-25.0.2` → `25.0.2`)
3. 遍历 `assets[]`，过滤以 `.tar.gz` 或 `.zip` 结尾且包含平台标识的名称
4. 映射 asset name 到平台/架构：
   - `graalvm-community-jdk-{ver}_linux-x64_bin.tar.gz` → linux amd64
   - `graalvm-community-jdk-{ver}_linux-aarch64_bin.tar.gz` → linux aarch64
   - `graalvm-community-jdk-{ver}_macos-x64_bin.tar.gz` → macos amd64
   - `graalvm-community-jdk-{ver}_macos-aarch64_bin.tar.gz` → macos aarch64
   - `graalvm-community-jdk-{ver}_windows-x64_bin.zip` → windows amd64

### Liberica (GitHub API)

1. 调用 `GET https://api.github.com/repos/bell-sw/Liberica/releases/latest`
2. 从 `tag_name` 提取版本号 (如 `26.0.1+10`)
3. 遍历 `assets[]`，过滤 `.tar.gz` 和 `.zip` (非 lite，非 deb/rpm)
4. 映射 asset name 到平台/架构：
   - `bellsoft-jdk{ver}-linux-amd64-full.tar.gz` → linux amd64
   - `bellsoft-jdk{ver}-linux-aarch64-full.tar.gz` → linux aarch64
   - `bellsoft-jdk{ver}-macos-amd64-full.zip` → macos amd64
   - `bellsoft-jdk{ver}-macos-aarch64-full.zip` → macos aarch64
   - `bellsoft-jdk{ver}-windows-amd64-full.zip` → windows amd64

## 错误处理

| 场景 | 处理方式 |
|------|----------|
| API 请求失败 | 打印警告，跳过该发行版 |
| 版本未找到 | 输出空对象 `{}` |
| asset 解析失败 | 跳过该 asset，继续处理其他平台 |
| 网络超时 | 重试 3 次，失败则跳过该版本 |

## 依赖

- Python 3.8+
- `requests` 库（用于 HTTP 请求）

## 使用方式

```bash
cd tools/generate_versions
pip install -r requirements.txt
python generate_versions.py
```

输出 `version.json` 到当前目录。

## 配置

通过命令行参数：

| 参数 | 说明 | 默认值 |
|------|------|--------|
| `--output` | 输出文件路径 | `./version.json` |
| `--jdk-versions` | 要抓取的 JDK 主版本 | `25,21,17` |

示例：
```bash
python generate_versions.py --output ./version.json --jdk-versions 25,21,17
```