from __future__ import annotations

import ctypes
import sys


def disable_windows_quick_edit() -> None:
    if sys.platform != "win32":
        return

    kernel32 = ctypes.windll.kernel32
    handle = kernel32.GetStdHandle(-10)  # STD_INPUT_HANDLE
    if handle == -1:
        return

    mode = ctypes.c_uint()
    if not kernel32.GetConsoleMode(handle, ctypes.byref(mode)):
        return

    enable_extended_flags = 0x0080
    enable_quick_edit_mode = 0x0040
    new_mode = (mode.value | enable_extended_flags) & ~enable_quick_edit_mode
    kernel32.SetConsoleMode(handle, new_mode)
