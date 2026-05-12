from __future__ import annotations

from auto_balancer.runtime.paths import TOOLS_DIR


DEAP_VERSION = "1.4.3"
DEAP_VENDOR_DIR = TOOLS_DIR / "dependences" / f"deap-{DEAP_VERSION}"


def ensure_deap_available() -> None:
    try:
        import deap  # noqa: F401
    except ModuleNotFoundError:
        raise RuntimeError(
            f"Could not import deap. Expected the vendored copy at `{DEAP_VENDOR_DIR}` "
            f"or a compatible system installation."
        ) from None
