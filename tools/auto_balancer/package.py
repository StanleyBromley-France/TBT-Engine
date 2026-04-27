from __future__ import annotations

import json
import shutil
from dataclasses import asdict, is_dataclass
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

CONTENT_FILES = (
    "unitTemplates.json",
    "abilities.json",
    "effectTemplates.json",
    "effectComponentTemplates.json",
    "gameStates.json",
)


def resolve_package_content_path(package_path: Path | None, fallback_content_path: Path) -> Path:
    if package_path is None:
        return fallback_content_path

    content_path = package_path / "content"
    if not content_path.is_dir():
        raise FileNotFoundError(f"Balance package did not contain a content directory: {content_path}")
    return content_path


def write_balance_package(
    package_path: Path,
    stage_name: str,
    input_content_path: Path,
    output_content_path: Path,
    report: dict[str, Any],
    changed_files: tuple[str, ...] | None = None,
) -> None:
    if package_path.exists():
        validate_existing_package_path_for_replacement(package_path)
        shutil.rmtree(package_path)

    content_dir = package_path / "content"
    diffs_dir = package_path / "diffs"
    content_dir.mkdir(parents=True, exist_ok=True)
    diffs_dir.mkdir(parents=True, exist_ok=True)

    tuned_files = set(CONTENT_FILES if changed_files is None else changed_files)
    for file_name in CONTENT_FILES:
        source_root = output_content_path if file_name in tuned_files else input_content_path
        source_file = source_root / file_name
        if source_file.is_file():
            shutil.copy2(source_file, content_dir / file_name)

    diff_summary = write_content_diffs(input_content_path, output_content_path, diffs_dir, tuned_files)
    normalized_report = normalize_report(report)
    normalized_report.update(
        {
            "stage": stage_name,
            "createdUtc": datetime.now(timezone.utc).isoformat(),
            "inputContent": str(input_content_path),
            "outputContent": str(output_content_path),
            "diffSummary": diff_summary,
        }
    )

    write_json(package_path / "report.json", normalized_report)
    write_markdown_report(package_path / "report.md", normalized_report)


def validate_existing_package_path_for_replacement(package_path: Path) -> None:
    if not package_path.is_dir():
        raise FileExistsError(f"Output package path already exists and is not a directory: {package_path}")

    children = {child.name for child in package_path.iterdir()}
    if not children:
        return

    if "content" in children and ({"report.json", "report.md", "diffs"} & children):
        return

    raise ValueError(
        "Refusing to replace output package path because it does not look like "
        f"a balance package: {package_path}"
    )


def write_content_diffs(
    input_content_path: Path,
    output_content_path: Path,
    diffs_dir: Path,
    changed_files: set[str],
) -> dict[str, Any]:
    summary: dict[str, Any] = {}
    for file_name in CONTENT_FILES:
        if file_name not in changed_files:
            summary[file_name] = {"changedEntries": 0}
            continue
        before_path = input_content_path / file_name
        after_path = output_content_path / file_name
        if not before_path.is_file() or not after_path.is_file():
            continue

        before = load_json(before_path)
        after = load_json(after_path)
        diff = diff_json_payload(before, after)
        changed_count = count_changes(diff)
        summary[file_name] = {"changedEntries": changed_count}
        if changed_count > 0:
            write_json(diffs_dir / f"{file_name}.diff.json", diff)
    return summary


def diff_json_payload(before: Any, after: Any) -> Any:
    if isinstance(before, list) and isinstance(after, list):
        return diff_json_array(before, after)
    if isinstance(before, dict) and isinstance(after, dict):
        return diff_dict(before, after)
    if before == after:
        return {}
    return {"before": before, "after": after}


def diff_json_array(before: list[Any], after: list[Any]) -> dict[str, Any]:
    if all(isinstance(item, dict) and isinstance(item.get("id"), str) for item in before + after):
        before_by_id = {item["id"]: item for item in before}
        after_by_id = {item["id"]: item for item in after}
        changed: dict[str, Any] = {}
        for item_id in sorted(set(before_by_id) | set(after_by_id)):
            if item_id not in before_by_id:
                changed[item_id] = {"added": after_by_id[item_id]}
                continue
            if item_id not in after_by_id:
                changed[item_id] = {"removed": before_by_id[item_id]}
                continue
            item_diff = diff_dict(before_by_id[item_id], after_by_id[item_id])
            if item_diff:
                changed[item_id] = item_diff
        return changed

    if before == after:
        return {}
    return {"before": before, "after": after}


def diff_dict(before: dict[str, Any], after: dict[str, Any]) -> dict[str, Any]:
    changed: dict[str, Any] = {}
    for key in sorted(set(before) | set(after)):
        if key not in before:
            changed[key] = {"added": after[key]}
            continue
        if key not in after:
            changed[key] = {"removed": before[key]}
            continue
        if before[key] != after[key]:
            changed[key] = {"before": before[key], "after": after[key]}
    return changed


def count_changes(diff: Any) -> int:
    if not diff:
        return 0
    if isinstance(diff, dict):
        return len(diff)
    return 1


def normalize_report(value: Any) -> Any:
    if is_dataclass(value):
        return normalize_report(asdict(value))
    if isinstance(value, dict):
        return {str(key): normalize_report(item) for key, item in value.items()}
    if isinstance(value, (list, tuple)):
        return [normalize_report(item) for item in value]
    return value


def write_json(path: Path, payload: Any) -> None:
    with path.open("w", encoding="utf-8") as handle:
        json.dump(payload, handle, indent=2)
        handle.write("\n")


def load_json(path: Path) -> Any:
    with path.open("r", encoding="utf-8") as handle:
        return json.load(handle)


def write_markdown_report(path: Path, report: dict[str, Any]) -> None:
    lines = [
        f"# Balance Package: {report.get('stage', 'unknown')}",
        "",
        f"- Created UTC: `{report.get('createdUtc', '')}`",
        f"- Input content: `{report.get('inputContent', '')}`",
        f"- Output content: `{report.get('outputContent', '')}`",
        "",
        "## Goal Evidence",
        "",
    ]

    evidence = report.get("evidence", {})
    if isinstance(evidence, dict) and evidence:
        for name, values in evidence.items():
            lines.append(f"### {name}")
            if isinstance(values, dict):
                for key, value in values.items():
                    lines.append(f"- {key}: `{format_report_value(value)}`")
            else:
                lines.append(f"- `{format_report_value(values)}`")
            lines.append("")
    else:
        lines.append("- No before/after evidence was recorded.")
        lines.append("")

    lines.extend(["## Changed Content", ""])
    diff_summary = report.get("diffSummary", {})
    if isinstance(diff_summary, dict) and diff_summary:
        for file_name, values in diff_summary.items():
            changed_entries = values.get("changedEntries", 0) if isinstance(values, dict) else values
            lines.append(f"- `{file_name}`: `{changed_entries}` changed entries")
    else:
        lines.append("- No content changes detected.")

    path.write_text("\n".join(lines) + "\n", encoding="utf-8")


def format_report_value(value: Any) -> str:
    if isinstance(value, float):
        return f"{value:.4f}"
    return str(value)
