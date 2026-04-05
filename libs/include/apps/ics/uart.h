#ifndef ICS_UART_H
#define ICS_UART_H

#include <stdint.h>
#include <stdbool.h>
#include <ics/EmbedLibrary.h>
// 常量定义
#define MAX_UART_DATA_LEN 1024

// ============================================================================
// 枚举类型定义
// ============================================================================

// UART事件类型
typedef enum {
    ICS_UART_EVENT_TYPE_RX_DATA = 0,        // 接收数据事件
    ICS_UART_EVENT_TYPE_ERROR = 1,          // 错误事件
    ICS_UART_EVENT_TYPE_OVERFLOW = 2,       // 溢出事件
    ICS_UART_EVENT_TYPE_QUIT_EVENT = 255    // 退出事件
} UartEventType;

// UART数据位
typedef enum {
    ICS_UART_DATA_BITS_7 = 7,
    ICS_UART_DATA_BITS_8 = 8,
    ICS_UART_DATA_BITS_9 = 9
} UartDataBits;

// UART停止位
typedef enum {
    ICS_UART_STOP_BITS_1 = 0,
    ICS_UART_STOP_BITS_1_5 = 1,
    ICS_UART_STOP_BITS_2 = 2
} UartStopBits;

// UART校验位
typedef enum {
    ICS_UART_PARITY_NONE = 0,
    ICS_UART_PARITY_ODD = 1,
    ICS_UART_PARITY_EVEN = 2
} UartParity;

// UART流控
typedef enum {
    ICS_UART_FLOW_CONTROL_NONE = 0,
    ICS_UART_FLOW_CONTROL_RTS_CTS = 1,
    ICS_UART_FLOW_CONTROL_XON_XOFF = 2
} UartFlowControl;

// UART错误码
#define ICS_UART_ERROR_NONE (0)           /*!< No error                    */
#define ICS_UART_ERROR_PARITY (1 << 0)    /*!< Parity error               */
#define ICS_UART_ERROR_FRAMING (1 << 1)   /*!< Framing error              */
#define ICS_UART_ERROR_OVERRUN (1 << 2)   /*!< Overrun error              */
#define ICS_UART_ERROR_NOISE (1 << 3)     /*!< Noise error                */
#define ICS_UART_ERROR_TIMEOUT (1 << 4)   /*!< Timeout error              */
#define ICS_UART_ERROR_TX_BUSY (1 << 5)   /*!< TX busy                    */
#define ICS_UART_ERROR_BUFFER_FULL (1 << 6) /*!< Buffer full              */

// ============================================================================
// 结构体定义 (Pack = 4字节对齐)
// ============================================================================

#pragma pack(push, 4)

// UART配置结构
typedef struct {
    uint32_t BaudRate;              // 波特率
    UartDataBits DataBits;          // 数据位
    UartStopBits StopBits;          // 停止位
    UartParity Parity;              // 校验位
    UartFlowControl FlowControl;    // 流控
    uint8_t *RxBuffer;              // 接收缓冲区指针
    uint32_t RxBufferSize;          // 接收缓冲区大小
} UartConfigNative;

// UART接收数据结构
typedef struct {
    uint32_t Length;        // 接收到的数据长度
} UartRxDataNative;

// UART错误事件结构
typedef struct {
    uint32_t ErrorCode;     // 错误码
} UartErrorEventNative;

// UART溢出事件结构
typedef struct {
    uint32_t LostBytes;     // 丢失的字节数
} UartOverflowEventNative;

// UART事件结构
typedef struct {
    UartEventType EventType;      // offset 0, size 4
    uint64_t TimeStamp;            // offset 4, size 8
    uint32_t UartIndex;            // offset 12, size 4
    union 
    {
        UartRxDataNative RxData;
        UartErrorEventNative ErrorEvent;
        UartOverflowEventNative OverflowEvent;
    };
} UartEventNative;

#pragma pack(pop)

// ============================================================================
// 函数声明
// ============================================================================

#ifdef __cplusplus
extern "C" {
#endif

/**
 * @brief 查询UART设备索引是否存在
 * @param uartIndex UART设备索引
 * @return true 设备索引有效, false 设备索引无效
 */
bool Hal_Uart_IsDeviceIndexValid(int32_t uartIndex);

/**
 * @brief 设置全局的UART事件队列句柄（所有UART设备共享）
 * @param eventHandle 事件队列句柄
 * @return true 设置成功, false 设置失败
 */
bool Hal_Uart_SetGlobalEventHandle(El_Queue_t eventHandle);

/**
 * @brief 打开UART设备
 * @param uartIndex UART设备索引
 * @param config 指向UartConfigNative结构的指针
 * @return true 打开成功, false 打开失败
 */
bool Hal_Uart_Open(int32_t uartIndex, UartConfigNative* config);

/**
 * @brief 关闭UART设备
 * @param uartIndex UART设备索引
 */
void Hal_Uart_Close(int32_t uartIndex);

/**
 * @brief 同步发送UART数据
 * @param uartIndex UART设备索引
 * @param buffer 发送数据缓冲区指针
 * @param length 发送数据长度
 * @param timeoutMs 超时时间（毫秒）
 * @return true 发送成功, false 发送失败
 */
bool Hal_Uart_Send(int32_t uartIndex, const uint8_t* buffer, uint32_t length, 
                    uint32_t timeoutMs);

/**
 * @brief 启动UART接收（用于重新开启DMA接收）
 * @param uartIndex UART设备索引
 * @return true 启动成功, false 启动失败
 */
bool Hal_Uart_StartReceive(int32_t uartIndex);

#ifdef __cplusplus
}
#endif

#endif // ICS_UART_H
