from __future__ import annotations

from pathlib import Path


TOOLS_DIR = Path(__file__).resolve().parents[2]
REPO_ROOT = TOOLS_DIR.parent
DEFAULT_GA_CONTENT_DIR = TOOLS_DIR / "ga-content"
