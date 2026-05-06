from __future__ import annotations

import sys
from pathlib import Path

from auto_balancer.runtime.paths import TOOLS_DIR


DEAP_VERSION = "1.4.3"
DEAP_VENDOR_DIR = TOOLS_DIR / "dependences" / f"deap-{DEAP_VERSION}"


def ensure_deap_available() -> None:
    if not DEAP_VENDOR_DIR.is_dir():
        raise RuntimeError(
            f"Missing vendored Python dependency: deap {DEAP_VERSION}. "
            f"Expected it at `{DEAP_VENDOR_DIR}`."
        )

    vendor_path = str(DEAP_VENDOR_DIR)
    if vendor_path not in sys.path:
        sys.path.insert(0, vendor_path)

    try:
        deap = __import__("deap")
    except ModuleNotFoundError:
        raise RuntimeError(
            f"Missing vendored Python dependency: deap {DEAP_VERSION}. "
            f"Expected it at `{DEAP_VENDOR_DIR}`."
        ) from None

    package_path = Path(deap.__file__).resolve()
    vendor_root = DEAP_VENDOR_DIR.resolve()
    if vendor_root not in package_path.parents:
        raise RuntimeError(
            f"Loaded deap from `{package_path}`, but expected the vendored copy under `{vendor_root}`."
        )
