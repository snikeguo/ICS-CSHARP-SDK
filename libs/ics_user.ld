/*
 * ics_user.ld — 第二阶段用户代码链接脚本
 * 由 gen_user_link_script.py 自动生成，请勿手动修改。
 *
 * 第一阶段符号通过 nuttx.elf 提取：
 *   ICS_NATIVEAOT_USER_IMAGE_START = 0xc0001000
 *   USER_RAM_START                = 0xc1800000
 *   USER_RAM_SIZE                 = 0x02800000
 *   ram_end                  = USER_RAM_START[0xc1800000] + USER_RAM_SIZE[0x02800000] = 0xc4000000
 *   align                    = 1024
 *
 * 计算过程：
 *   user_flash_origin = ICS_NATIVEAOT_USER_IMAGE_START[0xc0001000] = 0xc0001000
 *   user_ram_origin  = align_up(USER_RAM_START[0xc1800000], align[1024]) = 0xc1800000
 *   user_flash_length = USER_RAM_START[0xc1800000] - user_flash_origin[0xc0001000] = 0x17ff000
 *   user_ram_length   = ram_end[0xc4000000] - user_ram_origin[0xc1800000] = 0x2800000
 */


MEMORY
{
    flash (rx)  : ORIGIN = 0xc0001000, LENGTH = 0x017ff000   /* ICS_NATIVEAOT_USER_IMAGE_START, 长度=USER_RAM_START-flash ORIGIN */
    ram   (rwx) : ORIGIN = 0xc1800000,   LENGTH = 0x02800000   /* align_up(USER_RAM_START, align), 长度=ram_end-ram ORIGIN */
}

OUTPUT_ARCH(arm)
ENTRY(__managed__Main)

SECTIONS
{
    . = ORIGIN(flash);

    .ics_nativeaot_user_header :
    {
        __ics_nativeaot_user_image_start = ABSOLUTE(.);
        KEEP(*(.ics_nativeaot_user_header))
    } > flash

    /* ------------------------------------------------------------------ */
    /* 代码段 & 只读数据                                                    */
    /* ------------------------------------------------------------------ */
    .text :
    {
        _suser_text = ABSOLUTE(.);
        __start___managedcode = .;
        *(__managedcode)
        __stop___managedcode = .;

        __start___unbox = .;
        *(__unbox)
        __stop___unbox = .;

        *(.text .text.*)
        *(.rodata .rodata.*)
        *(.gnu.linkonce.t.*)
        *(.gnu.linkonce.r.*)
        *(.fixup)
        *(.gnu.warning)
        *(.glue_7)
        *(.glue_7t)
        *(.got)
        *(.gcc_except_table)

        . = ALIGN(4);
        __start___modules = .;
        KEEP(*(__modules))
        __stop___modules = .;

        . = ALIGN(4);
    } > flash

    .ARM.extab :
    {
        __user_extab_start = ABSOLUTE(.);
        *(.ARM.extab* .gnu.linkonce.armextab.*)
        __user_extab_end = ABSOLUTE(.);
    } > flash

    .ARM.exidx :
    {
        __user_exidx_start = ABSOLUTE(.);
        *(.ARM.exidx* .gnu.linkonce.armexidx.*)
        __user_exidx_end = ABSOLUTE(.);
    } > flash

    .preinit_array :
    {
        . = ALIGN(4);
        PROVIDE_HIDDEN (__user_preinit_array_start = .);
        KEEP (*(.preinit_array*))
        PROVIDE_HIDDEN (__user_preinit_array_end = .);
    } > flash

    .init_array :
    {
        . = ALIGN(4);
        PROVIDE_HIDDEN (__user_init_array_start = .);
        KEEP (*(SORT(.init_array.*)))
        KEEP (*(.init_array))
        PROVIDE_HIDDEN (__user_init_array_end = .);
    } > flash

    .fini_array :
    {
        . = ALIGN(4);
        PROVIDE_HIDDEN (__user_fini_array_start = .);
        KEEP (*(SORT(.fini_array.*)))
        KEEP (*(.fini_array))
        PROVIDE_HIDDEN (__user_fini_array_end = .);
    } > flash

    .eh_frame :
    {
        __user_eh_frame_start = .;
        KEEP (*(.eh_frame))
        __user_eh_frame_end = .;
    } > flash

    /* ------------------------------------------------------------------ */
    /* 已初始化数据（flash 中存储，运行时复制到 RAM）                        */
    /* ------------------------------------------------------------------ */
    .data :
    {
        _suser_data = ABSOLUTE(.);
        *(.data .data.*)
        *(.gnu.linkonce.d.*)
        CONSTRUCTORS
        . = ALIGN(4);
        _euser_data = ABSOLUTE(.);
    } > ram AT > flash

    /* ------------------------------------------------------------------ */
    /* 未初始化数据（BSS）                                                  */
    /* ------------------------------------------------------------------ */
    .bss (NOLOAD) :
    {
        _suser_bss = ABSOLUTE(.);
        __hydrated_start = .;
        *(.hydrated)
        __hydrated_end = .;
        *(.bss .bss.*)
        *(.gnu.linkonce.b.*)
        *(COMMON)
        . = ALIGN(4);
        _euser_bss = ABSOLUTE(.);
    } > ram

    _sidata = LOADADDR(.data);
    /* .data 的 LMA 结束地址（即 bin 中 .data 的结束，开区间末端） */
    __ics_nativeaot_user_image_end = (LOADADDR(.data) + SIZEOF(.data));

    /DISCARD/ :
    {
        *(.note.*)
        *(.comment)
    }
}
