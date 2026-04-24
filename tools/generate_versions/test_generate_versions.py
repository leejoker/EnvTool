"""Tests for generate_versions.py"""
import pytest
from unittest.mock import patch, MagicMock
from generate_versions import (
    extract_adoptium_url,
    parse_graalvm_assets,
    parse_liberica_assets,
    generate_version_json,
)


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


def test_parse_liberica_assets():
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
    assert assets["linux"]["aarch64"] == "https://github.com/bell-sw/Liberica/releases/download/26.0.1+10/bellsoft-jdk26.0.1+10-linux-aarch64-full.tar.gz"
    assert assets["macos"]["amd64"] == "https://github.com/bell-sw/Liberica/releases/download/26.0.1+10/bellsoft-jdk26.0.1+10-macos-amd64-full.zip"
    assert assets["macos"]["aarch64"] == "https://github.com/bell-sw/Liberica/releases/download/26.0.1+10/bellsoft-jdk26.0.1+10-macos-aarch64-full.zip"
    assert assets["windows"]["amd64"] == "https://github.com/bell-sw/Liberica/releases/download/26.0.1+10/bellsoft-jdk26.0.1+10-windows-amd64-full.zip"
    # Verify filtered items are absent (lite and deb packages should not appear)
    # Since assets is a nested dict, check that only expected keys exist
    assert "lite" not in str(assets)


def test_generate_version_json_structure():
    """Test that generate_version_json produces correct structure"""
    # This test verifies the structure without making actual API calls
    # by mocking the fetch functions
    mock_adoptium = {"windows": {"amd64": "https://example.com/openjdk.zip"}, "linux": {}, "macos": {}}
    mock_graalvm = {"windows": {"amd64": "https://example.com/graalvm.zip"}, "linux": {}, "macos": {}}
    mock_liberica = {"windows": {"amd64": "https://example.com/liberica.zip"}, "linux": {}, "macos": {}}

    with patch("generate_versions.fetch_adoptium_versions", return_value=mock_adoptium), \
         patch("generate_versions.fetch_graalvm_versions", return_value=mock_graalvm), \
         patch("generate_versions.fetch_liberica_versions", return_value=mock_liberica):
        result = generate_version_json(["25", "21", "17"])

    assert "openjdk" in result
    assert "graalvm" in result
    assert "liberica" in result
    assert "25" in result["openjdk"]
    assert "21" in result["openjdk"]
    assert "17" in result["openjdk"]
    # Each version should have platform keys and LTS flag
    assert "windows" in result["openjdk"]["25"]
    assert "linux" in result["openjdk"]["25"]
    assert "macos" in result["openjdk"]["25"]
    # LTS should be set for 21 and 17 but not 25
    assert result["openjdk"]["21"]["LTS"] is True
    assert result["openjdk"]["17"]["LTS"] is True
    assert result["openjdk"]["25"]["LTS"] is False