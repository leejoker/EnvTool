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