#!/usr/bin/env python3
"""
Python 版本的 PublishIcsCsharpSdk。

行为与 Program.cs 保持一致：
1) 在脚本目录下的 demo/ 找唯一 .csproj
2) 执行 dotnet publish (AOT, linux-arm)
3) 从 project.assets.json 或 NuGet 缓存解析 ILC 版本
4) 定位并修补 Microsoft.NETCore.Native.targets
"""

from __future__ import annotations

import os
import re
import subprocess
import sys
import threading
from pathlib import Path
from typing import Optional

ILC_PACKAGE_ID = "microsoft.dotnet.ilcompiler"
TARGETS_FILE_NAME = "Microsoft.NETCore.Native.targets"


def find_demo_project(base_dir: Path) -> Optional[Path]:
    if not base_dir.is_dir():
        return None

    demo_dir = base_dir / "demo"
    if not demo_dir.is_dir():
        return None

    csproj = list(demo_dir.glob("*.csproj"))
    if len(csproj) == 1:
        return csproj[0]

    if len(csproj) > 1:
        print("Multiple .csproj found in EXE\\demo directory. Keep only one demo project.", file=sys.stderr)
    return None


def find_assets_file(demo_dir: Path) -> Optional[Path]:
    obj_dir = demo_dir / "obj"
    if not obj_dir.is_dir():
        return None

    assets = list(obj_dir.rglob("project.assets.json"))
    if not assets:
        return None

    assets.sort(key=lambda p: p.stat().st_mtime, reverse=True)
    return assets[0]


def find_ilc_version(assets_path: Path) -> Optional[str]:
    text = assets_path.read_text(encoding="utf-8", errors="ignore")
    match = re.search(rf"{re.escape(ILC_PACKAGE_ID)}/([^\"\s]+)", text, flags=re.IGNORECASE)
    return match.group(1) if match else None


def find_latest_ilc_version() -> Optional[str]:
    nuget_root = Path.home() / ".nuget" / "packages" / ILC_PACKAGE_ID
    if not nuget_root.is_dir():
        return None

    version_dirs = [d for d in nuget_root.iterdir() if d.is_dir()]
    if not version_dirs:
        return None

    best_name: Optional[str] = None
    best_tuple: Optional[tuple[int, ...]] = None
    best_time: float = 0.0

    for d in version_dirs:
        name = d.name
        nums = try_parse_version(name)
        if nums is not None:
            if best_tuple is None or nums > best_tuple:
                best_tuple = nums
                best_name = name
            continue

        if best_tuple is None:
            mtime = d.stat().st_mtime
            if mtime > best_time:
                best_time = mtime
                best_name = name

    return best_name


def try_parse_version(version: str) -> Optional[tuple[int, ...]]:
    if not re.fullmatch(r"\d+(?:\.\d+)*", version):
        return None
    return tuple(int(x) for x in version.split("."))


def get_targets_path_from_version(ilc_version: str) -> Path:
    nuget_root = Path.home() / ".nuget" / "packages" / ILC_PACKAGE_ID / ilc_version
    return nuget_root / "build" / TARGETS_FILE_NAME


def detect_encoding(raw: bytes) -> str:
    if raw.startswith(b"\xef\xbb\xbf"):
        return "utf-8-sig"
    return "utf-8"


def patch_targets_file(path: Path) -> bool:
    raw = path.read_bytes()
    encoding = detect_encoding(raw)
    content = raw.decode(encoding, errors="strict")
    original = content

    content = ensure_linknative_condition(content)
    content = remove_skip_condition_from_linker_exec(content)

    if content == original:
        return False

    path.write_text(content, encoding=encoding, newline="")
    return True


def ensure_linknative_condition(content: str) -> str:
    target_regex = re.compile(r"<Target\s+Name=\"LinkNative\"(?P<attrs>[^>]*)>", re.IGNORECASE)
    match = target_regex.search(content)
    if not match:
        raise RuntimeError("LinkNative target not found.")

    attrs = match.group("attrs")
    cond_regex = re.compile(r"Condition=\"(?P<cond>[^\"]*)\"", re.IGNORECASE)
    cond_match = cond_regex.search(attrs)

    skip_cond = "'$(_SkipNativeLink)' != 'true'"
    if cond_match:
        cond = cond_match.group("cond")
        if "$( _SkipNativeLink)" in cond:
            # 兼容误写，后续统一替换
            cond = cond.replace("$( _SkipNativeLink)", "$(_SkipNativeLink)")

        if "$(" in cond and "_SkipNativeLink" in cond:
            cond = replace_skip_condition(cond, skip_cond)
        else:
            cond = f"{cond} and {skip_cond}"

        new_attrs = cond_regex.sub(f'Condition="{cond}"', attrs, count=1)
    else:
        new_attrs = attrs + f' Condition="{skip_cond}"'

    replacement = f"<Target Name=\"LinkNative\"{new_attrs}>"
    return target_regex.sub(replacement, content, count=1)


def replace_skip_condition(cond: str, skip_cond: str) -> str:
    return re.sub(
        r"'\$\(_SkipNativeLink\)'\s*!=\s*'true'",
        skip_cond,
        cond,
        flags=re.IGNORECASE,
    )


def remove_skip_condition_from_linker_exec(content: str) -> str:
    newline = "\r\n" if "\r\n" in content else "\n"
    lines = content.splitlines()
    in_cpp_linker_exec = False

    for i, line in enumerate(lines):
        if (
            'Exec Command="&quot;$(CppLinker)&quot;' in line
            and "@(CustomLinkerArg" in line
        ):
            in_cpp_linker_exec = True
            continue

        if in_cpp_linker_exec and 'Condition="' in line:
            m = re.match(r'^(?P<prefix>\s*Condition=")(?P<cond>[^\"]*)(?P<suffix>".*)$', line)
            if m:
                cond = m.group("cond")
                cleaned = re.sub(
                    r"\s+and\s+'\$\(_SkipNativeLink\)'\s*!=\s*'true'",
                    "",
                    cond,
                    flags=re.IGNORECASE,
                ).strip()
                cleaned = re.sub(r"\s{2,}", " ", cleaned)
                lines[i] = f'{m.group("prefix")}{cleaned}{m.group("suffix")}'

            in_cpp_linker_exec = False

    return newline.join(lines)


def run_process(file_name: str, arguments: list[str], working_directory: Path, suppress_output: bool) -> int:
    process = subprocess.Popen(
        [file_name, *arguments],
        cwd=str(working_directory),
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        text=True,
        encoding="utf-8",
        errors="replace",
        bufsize=1,
    )

    if suppress_output:
        process.communicate()
        return int(process.returncode or 0)

    def _pump(stream, writer):
        if stream is None:
            return
        for line in iter(stream.readline, ""):
            writer.write(line)
            writer.flush()
        stream.close()

    t_out = threading.Thread(target=_pump, args=(process.stdout, sys.stdout), daemon=True)
    t_err = threading.Thread(target=_pump, args=(process.stderr, sys.stderr), daemon=True)
    t_out.start()
    t_err.start()

    process.wait()
    t_out.join()
    t_err.join()
    return int(process.returncode or 0)


def main() -> int:
    try:
        base_dir = Path(__file__).resolve().parent
        demo_path = find_demo_project(base_dir)
        if not demo_path or not demo_path.is_file():
            print("Demo project not found in EXE\\demo directory.", file=sys.stderr)
            return 2

        demo_dir = demo_path.parent
        publish_exit = run_process(
            "dotnet",
            [
                "publish",
                str(demo_path),
                "-c",
                "Release",
                "-r",
                "linux-arm",
                "/p:PublishAot=true",
                "/p:platform=ARM32",
            ],
            demo_dir,
            suppress_output=False,
        )
        if publish_exit != 0:
            print("dotnet publish failed. Continue patching if possible...", file=sys.stderr)

        ilc_version: Optional[str] = None
        assets_path = find_assets_file(demo_dir)
        if assets_path and assets_path.is_file():
            ilc_version = find_ilc_version(assets_path)

        if not ilc_version:
            print("ILC version not found in project.assets.json, fallback to latest NuGet package.", file=sys.stderr)
            ilc_version = find_latest_ilc_version()

        if not ilc_version:
            print(
                "ILC package version not found. Run publish/restore with PublishAot=true at least once.",
                file=sys.stderr,
            )
            return 4

        targets_path = get_targets_path_from_version(ilc_version)
        if not targets_path.is_file():
            print(f"Microsoft.NETCore.Native.targets not found for ILC {ilc_version}.", file=sys.stderr)
            return 6

        print(f"Patching: {targets_path}")
        changed = patch_targets_file(targets_path)
        print("Patch applied." if changed else "Patch already present.")
        return 0

    except Exception as ex:
        print(str(ex), file=sys.stderr)
        return 99


if __name__ == "__main__":
    sys.exit(main())
