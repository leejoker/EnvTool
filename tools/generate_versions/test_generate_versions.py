"""Tests for generate_versions.py"""
import pytest
from generate_versions import extract_adoptium_url


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