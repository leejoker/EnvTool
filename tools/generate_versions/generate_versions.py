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
ADOPTIUM_INFO_BASE = "https://api.adoptium.net/v3/info"

# Adoptium API uses "hotspot" as the default JVM implementation
ADOPTIUM_JVM_IMPL = "hotspot"

GITHUB_API_HEADERS = {
    "Accept": "application/vnd.github+json",
    "User-Agent": "version-json-generator"
}

GRAALVM_REPO = "graalvm/graalvm-ce-builds"
LIBERICA_REPO = "bell-sw/Liberica"


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


def extract_adoptium_url(assets: list, arch: str, os_name: str) -> Optional[str]:
    """
    Extract download URL from Adoptium API response.

    Args:
        assets: List of BinaryAssetView objects from Adoptium API
        arch: Architecture to filter (e.g., "x64", "aarch64")
        os_name: OS to filter (e.g., "windows", "linux", "mac")

    Returns:
        Download URL or None if not found
    """
    for asset in assets:
        binary = asset.get("binary", {})
        # Only consider JDK images, not testimage, debugimage, etc.
        if (binary.get("architecture") == arch and
            binary.get("os") == os_name and
            binary.get("image_type") == "jdk"):
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
        "mac": "macos"  # API uses "mac", we map to "macos"
    }

    # Adoptium API uses /v3/assets/latest/{feature_version}/{jvm_impl}
    url = f"{ADOPTIUM_API_BASE}/{version}/{ADOPTIUM_JVM_IMPL}"

    try:
        resp = requests.get(url, timeout=30)
        resp.raise_for_status()
        assets = resp.json()  # API returns an array of BinaryAssetView

        for arch, arch_key in arch_map.items():
            for api_os, os_key in os_map.items():
                download_url = extract_adoptium_url(assets, arch, api_os)
                if download_url:
                    result[os_key][arch_key] = download_url

    except requests.RequestException as e:
        print(f"Warning: Failed to fetch OpenJDK {version}: {e}")

    return result


def get_adoptium_available_versions() -> Dict[str, Any]:
    """
    Fetch available JDK versions from Adoptium API.

    Returns:
        Dict with available_releases, available_lts_releases, most_recent_lts, etc.
    """
    url = f"{ADOPTIUM_INFO_BASE}/available_releases"
    try:
        resp = requests.get(url, timeout=30)
        resp.raise_for_status()
        return resp.json()
    except requests.RequestException as e:
        print(f"Warning: Failed to fetch available releases: {e}")
        return {}


def generate_version_json(versions: list, lts_versions: set = None) -> Dict[str, Any]:
    """
    Generate complete version.json structure.

    Args:
        versions: List of JDK major versions to fetch (e.g., ["25", "21", "17"])
        lts_versions: Set of LTS version strings (e.g., {"21", "17"})

    Returns:
        Complete version.json structure
    """
    if lts_versions is None:
        lts_versions = set()

    result: Dict[str, Any] = {}

    # OpenJDK
    openjdk_data: Dict[str, Any] = {}
    for version in versions:
        version_data = fetch_adoptium_versions(version)
        version_data_copy = version_data.copy()
        version_data_copy["LTS"] = version in lts_versions
        openjdk_data[version] = version_data_copy
    result["openjdk"] = openjdk_data

    # GraalVM
    graalvm_data: Dict[str, Any] = {}
    graalvm_versions = fetch_graalvm_versions()
    for version in versions:
        # GraalVM uses different versioning, use same latest for all
        graalvm_data[version] = graalvm_versions.copy()
    result["graalvm"] = graalvm_data

    # Liberica
    liberica_data: Dict[str, Any] = {}
    liberica_versions = fetch_liberica_versions()
    for version in versions:
        liberica_data[version] = liberica_versions.copy()
    result["liberica"] = liberica_data

    return result


def main():
    parser = argparse.ArgumentParser(description="Generate version.json for JDK distributions")
    parser.add_argument("--output", default="./version.json", help="Output file path")
    parser.add_argument("--latest", type=int, default=None, help="Number of latest versions to fetch (fetches LTS by default if not specified)")
    parser.add_argument("--jdk-versions", default=None, help="Comma-separated JDK versions (overrides --latest)")
    args = parser.parse_args()

    # Get available versions from Adoptium API
    available = get_adoptium_available_versions()
    lts_versions = set(str(v) for v in available.get("available_lts_releases", []))
    all_versions = available.get("available_releases", [])

    if args.jdk_versions:
        # Use explicitly specified versions
        versions = [v.strip() for v in args.jdk_versions.split(",") if v.strip()]
        print(f"Using specified versions: {versions}")
    elif args.latest:
        # Get latest N versions
        # Filter to only include versions that have LTS or are in the latest N
        versions = [str(v) for v in all_versions[-args.latest:]] if args.latest else []
        # If latest is specified but not LTS only, include all recent versions
        print(f"Fetching latest {args.latest} versions: {versions}")
    else:
        # Default: use all LTS versions
        versions = [str(v) for v in lts_versions]
        print(f"Using LTS versions: {versions}")

    print(f"LTS versions detected: {lts_versions}")

    result = generate_version_json(versions, lts_versions)

    output_path = args.output
    with open(output_path, "w", encoding="utf-8") as f:
        json.dump(result, f, indent=2, ensure_ascii=False)

    print(f"version.json generated successfully at {output_path}")


if __name__ == "__main__":
    main()
