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