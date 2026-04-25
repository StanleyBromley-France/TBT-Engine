from __future__ import annotations

import subprocess
import sys

from auto_balancer.runtime.paths import LOCAL_PACKAGE_DIR, REPO_ROOT


DEAP_SPEC = "deap==1.4.4"


def ensure_local_deap() -> None:
    LOCAL_PACKAGE_DIR.mkdir(parents=True, exist_ok=True)
    if str(LOCAL_PACKAGE_DIR) not in sys.path:
        sys.path.insert(0, str(LOCAL_PACKAGE_DIR))

    try:
        __import__("deap")
        return
    except ModuleNotFoundError:
        pass

    print(f"Installing {DEAP_SPEC} into {LOCAL_PACKAGE_DIR}...", file=sys.stderr)
    subprocess.check_call(
        [
            sys.executable,
            "-m",
            "pip",
            "install",
            "--disable-pip-version-check",
            "--target",
            str(LOCAL_PACKAGE_DIR),
            DEAP_SPEC,
        ],
        cwd=REPO_ROOT,
    )
