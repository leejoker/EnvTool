"""Tests for generate_versions.py"""
import pytest
from generate_versions import extract_adoptium_url, parse_graalvm_assets


def test_extract_adoptium_url_found():
    """Test extracting URL when binary matches"""
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
    url = extract_adoptium_url(mock_response, "x64", "windows")
    assert url == "https://example.com/openjdk-21.0.2_windows-x64_bin.zip"


def test_extract_adoptium_url_not_found():
    """Test extracting URL when no matching binary"""
    mock_response = {
        "binaries": [
            {
                "architecture": "x64",
                "os": "linux",
                "package": {"link": "https://example.com/linux.zip"}
            }
        ]
    }
    url = extract_adoptium_url(mock_response, "x64", "windows")
    assert url is None


def test_parse_graalvm_assets():
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