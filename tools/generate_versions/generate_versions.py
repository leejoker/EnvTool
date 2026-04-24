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

ADOPTIUM_API_BASE = "https://api.adoptium.net/v3/assets/latest"

GITHUB_API_HEADERS = {
    "Accept": "application/vnd.github+json",
    "User-Agent": "version-json-generator"
}

GRAALVM_REPO = "graalvm/graalvm-ce-builds"


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
        if len(parts) < 3:
            continue

        platform_arch = parts[-2]  # e.g., "linux-x64" or "windows-x64"
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


def extract_adoptium_url(data: Dict[str, Any], arch: str, os_name: str) -> Optional[str]:
    """Extract download URL from Adoptium API response"""
    for binary in data.get("binaries", []):
        if binary.get("architecture") == arch and binary.get("os") == os_name:
            pkg = binary.get("package", {})
            if "link" in pkg:
                return pkg["link"]
    return None


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


def main():
    parser = argparse.ArgumentParser(description="Generate version.json for JDK distributions")
    parser.add_argument("--output", default="./version.json", help="Output file path")
    parser.add_argument("--jdk-versions", default="25,21,17", help="Comma-separated JDK versions")
    args = parser.parse_args()

    versions = [v.strip() for v in args.jdk_versions.split(",")]
    print(f"Generating version.json for versions: {versions}")


if __name__ == "__main__":
    main()