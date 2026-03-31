#ifndef ICS_NATIVEAOT_H
#define ICS_NATIVEAOT_H

#include <stdbool.h>
#include <stddef.h>
#include <stdint.h>

//定义NATIVEAOT 进程环境变量结构体

struct NativeAotProcessEnvironment
{
    const char *Key;
    const char *Value;
};

#define ICS_USER_AOT_IMAGE_MAGIC   0x544F4155u /* "UAOT" */
#define ICS_USER_AOT_IMAGE_VERSION 0x00000001u
#define ICS_NATIVEAOT_CLASSLIB_FUNCTIONS_COUNT 12u



/* 用户镜像导出查询函数类型
 * 由 ILC 生成，loader 在 .data/.bss 初始化完成后直接调用
 * 返回 NULL 表示该符号不存在
 */
typedef void *(*IcsGetExportFn)(const char *name);

struct IcsUserAotImageHeader
{
    uint32_t  Magic;
    uint32_t  HeaderCrc32;          /* Header CRC32（从 OsAotRuntimeVersion 到 Header 结束） */
    uint32_t  OsAotRuntimeVersion;  /* Os+Aot运行时版本号，与 Nuttx 版本关联 */
    uint32_t  ImageCRC32;           /* 镜像 CRC32（后链接工具写入，校验时该域自身置0计算）*/
    void     *ImageStart;           /* 镜像运行起始地址（链接时写死） */
    void     *ImageEnd;             /* 镜像运行结束地址（链接时写死） */

    void     *GetExportAddress;     /* IcsGetExportFn 函数指针（Thumb bit0=1） */
    char      VendorString[32];     /* 厂商字符串（32字节） */

    /* 以下字段均为链接时写死的绝对地址对，全部使用 void* */
    void     *ManagedCodeStart;
    void     *ManagedCodeEnd;
    void     *UnboxCodeStart;
    void     *UnboxCodeEnd;

    void     *ModulesStart;         /* void** 数组起始 */
    void     *ModulesEnd;           /* void** 数组结束（元素数 = (End-Start)/4） */

    void     *ExidxStart;
    void     *ExidxEnd;
    void     *ExtabStart;
    void     *ExtabEnd;

    void     *HydratedStart;
    void     *HydratedEnd;
    void     *DataSectionLoadStart; /* flash 中 data 段存储地址（LMA） */
    void     *DataSectionRunStart;  /* RAM 中 data 段运行地址（VMA） */
    void     *DataSectionRunEnd;    /* RAM 中 data 段结束地址 */
    void     *BssSectionRunStart;
    void     *BssSectionRunEnd;
};

int nativeaot_main(int argc, char *argv[]);
bool ics_nativeaot_gc_set_segment_align(size_t align);
void ics_nativeaot_set_environment(struct NativeAotProcessEnvironment *env, size_t count);
bool ics_nativeaot_user_image_load(const char *image_path);
bool ics_nativeaot_user_image_init_from_address(uintptr_t image_addr);
const struct IcsUserAotImageHeader *ics_nativeaot_user_image_get_loaded_header(void);
uintptr_t ics_nativeaot_user_image_get_export_address(const char *name);
bool ics_nativeaot_user_image_init_required_exports(void);
const void *const *ics_nativeaot_user_image_get_classlib_functions(void);
const void *ics_nativeaot_user_image_get_compiler_embedded_settings_blob(void);
const void *ics_nativeaot_user_image_get_compiler_embedded_knobs_blob(void);
#endif