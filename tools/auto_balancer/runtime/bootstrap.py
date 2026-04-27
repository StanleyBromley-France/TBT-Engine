from __future__ import annotations


DEAP_SPEC = "deap==1.4.4"


def ensure_deap_available() -> None:
    try:
        __import__("deap")
        return
    except ModuleNotFoundError:
        raise RuntimeError(
            f"Missing Python dependency: {DEAP_SPEC}. "
            "Install tool dependencies with `py -m pip install -r tools/requirements.txt`."
        ) from None
