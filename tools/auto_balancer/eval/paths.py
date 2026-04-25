from __future__ import annotations

from pathlib import Path

from auto_balancer.runtime.paths import DEFAULT_GA_CONTENT_DIR, REPO_ROOT


def resolve_cli_path(cli_path: Path | None) -> Path:
    if cli_path is not None:
        resolved = cli_path.resolve()
        if not resolved.is_file():
            raise FileNotFoundError(f"CLI executable was not found: {resolved}")
        return resolved

    candidates = [
        REPO_ROOT / "src" / "Cli" / "bin" / "Release" / "net8.0" / "Cli.exe",
        REPO_ROOT / "src" / "Cli" / "bin" / "Debug" / "net8.0" / "Cli.exe",
    ]

    for candidate in candidates:
        if candidate.is_file():
            return candidate

    raise FileNotFoundError(
        "Could not find Cli.exe in the usual build locations. Build src/Cli first or pass --cli-path."
    )


def resolve_content_path(content_path: Path | None, cli_path: Path) -> Path:
    if content_path is not None:
        resolved = content_path.resolve()
        if not resolved.is_dir():
            raise FileNotFoundError(f"Content directory was not found: {resolved}")
        return resolved

    if DEFAULT_GA_CONTENT_DIR.is_dir():
        return DEFAULT_GA_CONTENT_DIR

    default_content = cli_path.parent / "content"
    if default_content.is_dir():
        return default_content

    raise FileNotFoundError(
        "Could not find a GA content directory at "
        f"{DEFAULT_GA_CONTENT_DIR} or a content directory next to the CLI binary at {default_content}."
    )
