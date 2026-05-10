from __future__ import annotations

from pathlib import Path


TOOLS_DIR = Path(__file__).resolve().parents[2]
REPO_ROOT = TOOLS_DIR.parent
DEFAULT_CONTENT_DIR = REPO_ROOT / "content"
DEFAULT_GA_CONTENT_DIR = DEFAULT_CONTENT_DIR
SCRATCH_DIR = REPO_ROOT / "scratch"
GENERATED_CONTENT_DIR = SCRATCH_DIR / "generated-content"
