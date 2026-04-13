#!/usr/bin/env python3
"""
post_link_crc.py
================
链接完成后，对用户 AOT 镜像的 flat binary 计算 CRC32，
并将结果写入 IcsUserAotImageHeader.HeaderCrc32 / ImageCRC32 字段。

算法与运行时 ics_user_aot_image.cpp 一致：
    - IEEE 802.3 CRC32（等同 Python zlib.crc32()）
    - Header CRC：对 header[OsAotRuntimeVersion:HeaderEnd] 计算
    - Image  CRC：对镜像内容区 [ImageStart + sizeof(IcsUserAotImageHeader), ImageEnd) 计算（不含Header）

IcsUserAotImageHeader ARM32 内存布局（字节偏移）：
  +0  Magic                uint32_t
    +4  HeaderCrc32          uint32_t  ← 本脚本写入此处
    +8  OsAotRuntimeVersion  uint32_t
    +12 ImageCRC32           uint32_t  ← 本脚本写入此处
    +16 ImageStart           void*
    +20 ImageEnd             void*
    +24 GetExportAddress     void*
    +28 VendorString[32]     char[32]
    +60 ManagedCodeStart     void*
  ...

用法：
  python3 post_link_crc.py <ics_user.elf> [--bin <ics_user.bin>]
                           [--objcopy arm-none-eabi-objcopy]
                           [--nm     arm-none-eabi-nm]

CMake 集成示例（在用户镜像 target 的 POST_BUILD 中调用）：
  add_custom_command(TARGET ics_user POST_BUILD
    COMMAND python3 ${CMAKE_SOURCE_DIR}/post_link_crc.py
            $<TARGET_FILE:ics_user>
            --bin ${CMAKE_BINARY_DIR}/ics_user.bin
            --objcopy ${CMAKE_OBJCOPY}
            --nm ${CMAKE_NM}
        COMMENT "Embedding Header/Image CRC32 into IcsUserAotImageHeader"
  )
"""

import argparse
import os
import struct
import subprocess
import sys
import zlib
from typing import Optional

# -----------------------------------------------------------------------
# IcsUserAotImageHeader 字段偏移（ARM32，与 nativeaot.h 保持同步）
# -----------------------------------------------------------------------
OFFSETOF_MAGIC       = 0
OFFSETOF_HEADER_CRC32 = 4
OFFSETOF_OS_AOT_RUNTIME_VERSION = 8
OFFSETOF_IMAGE_CRC32  = 12
OFFSETOF_IMAGE_START = 16
HEADER_SIZE_BYTES = 128  # ARM32，需与 nativeaot.h 中 IcsUserAotImageHeader 保持同步

ICS_USER_AOT_IMAGE_MAGIC = 0x544F4155  # "UAOT" little-endian


# -----------------------------------------------------------------------
# 工具函数
# -----------------------------------------------------------------------

def elf_to_bin(objcopy: str, elf: str, out_bin: str) -> None:
    """将 ELF 转换为 flat binary（间隙填 0xFF）。"""
    subprocess.run(
        [objcopy, "-O", "binary", "--gap-fill=0xff", elf, out_bin],
        check=True,
    )


def get_symbol_addr(nm: str, elf: str, symbol: str) -> Optional[int]:
    """用 nm 获取符号的 VMA（十六进制地址），未找到返回 None。"""
    try:
        result = subprocess.run([nm, elf], capture_output=True, text=True, check=True)
    except subprocess.CalledProcessError:
        return None
    for line in result.stdout.splitlines():
        parts = line.strip().split()
        if len(parts) == 3 and parts[2] == symbol:
            try:
                return int(parts[0], 16)
            except ValueError:
                pass
    return None


def find_header_in_binary(data: bytes) -> int:
    """
    在 flat binary 中定位 IcsUserAotImageHeader。
    通过匹配 Magic + 合理的 ImageStart/ImageEnd 双重验证，返回文件偏移。
    """
    magic_bytes = struct.pack("<I", ICS_USER_AOT_IMAGE_MAGIC)
    candidates = []
    pos = 0
    while True:
        idx = data.find(magic_bytes, pos)
        if idx < 0:
            break
        # 读取 ImageStart/ImageEnd，验证其合理性
        if idx + OFFSETOF_IMAGE_START + 8 <= len(data):
            img_start = struct.unpack_from("<I", data, idx + OFFSETOF_IMAGE_START)[0]
            img_end = struct.unpack_from("<I", data, idx + OFFSETOF_IMAGE_START + 4)[0]
            if img_end > img_start:
                candidates.append(idx)
        pos = idx + 1

    if not candidates:
        print("[CRC] ERROR: ICS_USER_AOT_IMAGE_MAGIC (0x544F4155) not found in binary",
              file=sys.stderr)
        sys.exit(1)

    if len(candidates) > 1:
        print(f"[CRC] WARNING: Magic found at multiple offsets {candidates!r}, "
              f"using first (0x{candidates[0]:X})")

    return candidates[0]


# -----------------------------------------------------------------------
# 主流程
# -----------------------------------------------------------------------

def main() -> None:
    parser = argparse.ArgumentParser(
        description="Compute Header/Image CRC32 and embed them into IcsUserAotImageHeader"
    )
    parser.add_argument("elf", help="输入 ELF 文件路径（如 ics_user.elf）")
    parser.add_argument(
        "--bin",
        dest="bin_path",
        help="输出 flat binary 路径（默认：<elf>.bin）",
    )
    parser.add_argument(
        "--objcopy",
        default="arm-none-eabi-objcopy",
        help="objcopy 工具路径（默认：arm-none-eabi-objcopy）",
    )
    parser.add_argument(
        "--nm",
        default="arm-none-eabi-nm",
        help="nm 工具路径（默认：arm-none-eabi-nm）",
    )
    args = parser.parse_args()

    elf_path = args.elf
    bin_path = args.bin_path or (os.path.splitext(elf_path)[0] + ".bin")

    # ── Step 1: ELF → flat binary ────────────────────────────────────────
    print(f"[CRC] ELF  : {elf_path}")
    print(f"[CRC] BIN  : {bin_path}")
    elf_to_bin(args.objcopy, elf_path, bin_path)

    with open(bin_path, "rb") as f:
        data = bytearray(f.read())

    print(f"[CRC] Image size : {len(data)} bytes (0x{len(data):X})")

    # ── Step 2: 定位 header ──────────────────────────────────────────────
    hdr_file_off = find_header_in_binary(bytes(data))
    print(f"[CRC] Header file offset : 0x{hdr_file_off:08X}")

    # 用 nm 验证（可选，工具不存在时静默跳过）
    hdr_vma = get_symbol_addr(args.nm, elf_path, "g_icsUserAotImageHeader")
    if hdr_vma is not None:
        # ImageStart 在偏移 +16，是 4 字节 LE void*（ARM32）
        img_start_vma = struct.unpack_from("<I", data, hdr_file_off + OFFSETOF_IMAGE_START)[0]
        derived_base = img_start_vma  # flat binary 起点地址
        expected_off = hdr_vma - derived_base
        if expected_off != hdr_file_off:
            print(
                f"[CRC] WARNING: nm says header VMA=0x{hdr_vma:08X}, "
                f"ImageStart=0x{img_start_vma:08X} → expected file offset 0x{expected_off:X}, "
                f"but magic found at 0x{hdr_file_off:X}",
                file=sys.stderr,
            )
        else:
            print(f"[CRC] Header VMA       : 0x{hdr_vma:08X}  (cross-check OK)")
        print(f"[CRC] Image base addr  : 0x{derived_base:08X}")
    else:
        print("[CRC] Note: nm symbol lookup skipped (tool not found or symbol absent)")

    # ── Step 3: 校验 Header 固定大小（ARM32）──────────────────────────────
    header_size = HEADER_SIZE_BYTES
    if (hdr_file_off + header_size) > len(data):
        print(f"[CRC] ERROR: invalid header range, size={header_size}", file=sys.stderr)
        sys.exit(1)

    # ── Step 4: 计算并写入 ImageCRC32（仅镜像内容区，不含Header）────────────
    # 注意：必须严格以 ImageStart/ImageEnd 定义的镜像边界为准，
    # 不能直接使用 len(data)，否则会把镜像外的填充字节算进去，导致与运行时不一致。
    image_crc_field_off = hdr_file_off + OFFSETOF_IMAGE_CRC32
    img_start_vma = struct.unpack_from("<I", data, hdr_file_off + OFFSETOF_IMAGE_START)[0]
    img_end_vma = struct.unpack_from("<I", data, hdr_file_off + OFFSETOF_IMAGE_START + 4)[0]
    if img_end_vma <= img_start_vma:
        print(f"[CRC] ERROR: invalid image range: start=0x{img_start_vma:08X} end=0x{img_end_vma:08X}", file=sys.stderr)
        sys.exit(1)

    image_size = img_end_vma - img_start_vma
    payload_start_off = hdr_file_off + header_size
    payload_end_off = hdr_file_off + image_size
    if payload_start_off > payload_end_off:
        print("[CRC] ERROR: invalid payload range", file=sys.stderr)
        sys.exit(1)
    if payload_end_off > len(data):
        print(f"[CRC] ERROR: payload end out of range: payload_end=0x{payload_end_off:X}, file_size=0x{len(data):X}", file=sys.stderr)
        sys.exit(1)
    image_crc = zlib.crc32(bytes(data[payload_start_off:payload_end_off])) & 0xFFFFFFFF
    struct.pack_into("<I", data, image_crc_field_off, image_crc)
    print(f"[CRC] Computed ImageCRC32  : 0x{image_crc:08X}")

    # ── Step 5: 计算并写入 HeaderCrc32（范围：OsAotRuntimeVersion..HeaderEnd）─
    header_crc_field_off = hdr_file_off + OFFSETOF_HEADER_CRC32
    header_bytes = bytearray(data[hdr_file_off:hdr_file_off + header_size])
    header_crc = zlib.crc32(bytes(header_bytes[OFFSETOF_OS_AOT_RUNTIME_VERSION:])) & 0xFFFFFFFF
    struct.pack_into("<I", data, header_crc_field_off, header_crc)
    print(f"[CRC] Computed HeaderCrc32 : 0x{header_crc:08X}")

    # ── Step 6: 写入并保存 ────────────────────────────────────────────────
    with open(bin_path, "wb") as f:
        f.write(data)
    print(f"[CRC] Patched binary written: {bin_path}")


if __name__ == "__main__":
    main()
