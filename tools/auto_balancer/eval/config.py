from __future__ import annotations

from dataclasses import dataclass
from pathlib import Path


@dataclass(frozen=True)
class EvalCommandConfig:
    cli_path: Path
    content_path: Path
    game_state: str
    validation: str
    seed: int
    repeat_count: int
    timeout_seconds: int
    log_mode: str = "quiet"

    def with_repeat_count(self, repeat_count: int) -> EvalCommandConfig:
        return EvalCommandConfig(
            cli_path=self.cli_path,
            content_path=self.content_path,
            game_state=self.game_state,
            validation=self.validation,
            seed=self.seed,
            repeat_count=repeat_count,
            timeout_seconds=self.timeout_seconds,
            log_mode=self.log_mode,
        )
