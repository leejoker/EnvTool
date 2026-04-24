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