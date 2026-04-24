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

## 输出格式

生成 `version.json` 文件，格式与 jpvm/versions.json 一致：

```json
{
  "openjdk": {
    "25": {
      "windows": { "amd64": "https://..." },
      "linux": { "amd64": "https://...", "aarch64": "https://..." },
      "macos": { "amd64": "https://...", "aarch64": "https://..." },
      "LTS": false
    },
    "21": {
      "windows": { "amd64": "https://..." },
      "linux": { "amd64": "https://...", "aarch64": "https://..." },
      "macos": { "amd64": "https://...", "aarch64": "https://..." },
      "LTS": true
    },
    "17": {
      "windows": { "amd64": "https://..." },
      "linux": { "amd64": "https://...", "aarch64": "https://..." },
      "macos": { "amd64": "https://...", "aarch64": "https://..." },
      "LTS": true
    }
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

## 注意事项

- OpenJDK 数据来自 Adoptium API
- GraalVM 和 Liberica 数据来自 GitHub Releases
- 部分平台/架构可能因官方未发布而为空
- LTS 标记：OpenJDK 21 和 17 为 LTS 版本