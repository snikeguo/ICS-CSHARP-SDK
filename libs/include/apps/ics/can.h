#ifndef ICS_CAN_H
#define ICS_CAN_H

#include <stdint.h>
#include <stdbool.h>

// 常量定义
#define MAX_CAN_DATA_LEN 64

// ============================================================================
// 枚举类型定义
// ============================================================================

// CAN运行模式
typedef enum {
    CANX_RUN_MODE_NORMAL = 0,
    CANX_RUN_MODE_LOOPBACK = 1,
    CANX_RUN_MODE_SILENT = 2,
    CANX_RUN_MODE_SILENT_LOOPBACK = 3
} CanXRunMode;

// CAN帧格式
typedef enum {
    CANX_FRAME_FORMAT_CLASSIC = 0,
    CANX_FRAME_FORMAT_FD_NO_BRS = 1,
    CANX_FRAME_FORMAT_FD_BRS = 2
} CanXFrameFormat;

// CAN数据包标志位（位域）
typedef enum {
    CANX_PACKET_FLAGS_DATA = (1 << 0),           // bit 0
    CANX_PACKET_FLAGS_STANDARD = (1 << 1),       // bit 1
    CANX_PACKET_FLAGS_BRS = (1 << 2),            // bit 2
    CANX_PACKET_FLAGS_CAN_FD = (1 << 3),         // bit 3
    CANX_PACKET_FLAGS_ESI = (1 << 4),            // bit 4
    CANX_PACKET_FLAGS_ECHO = (1 << 5)            // bit 5
} CanXPacketFlags;

// CAN FD模式
typedef enum {
    CANFD_MODE_ISO = 0,
    CANFD_MODE_BOSCH = 1
} CanFdMode;

// CAN总线状态类型
typedef enum {
    CANX_BUS_STATE_BUS_OFF = 0x01,
    CANX_BUS_STATE_PASSIVE = 0x02,
    CANX_BUS_STATE_WARNING = 0x04,
    CANX_BUS_STATE_ACTIVE = 0x08
} CanXBusStateType;

// CAN事件类型
typedef enum {
    CANX_EVENT_TYPE_RX_PACKET = 0,
    CANX_EVENT_TYPE_SENT_RESULT = 1,
    CANX_EVENT_TYPE_BUS_STATE = 2,
    CANX_EVENT_TYPE_RX_ERROR_EVENT = 3,
    CANX_EVENT_TYPE_QUIT_EVENT = 255
} CanXEventType;

#define CAN_ERROR_NONE (0)             /*!< No error                                             */
#define CAN_ERROR_EWG (1 << 0)         /*!< Protocol Error Warning                               */
#define CAN_ERROR_EPV (1 << 1)         /*!< Error Passive                                        */
#define CAN_ERROR_BOF (1 << 2)         /*!< Bus-off error                                        */
#define CAN_ERROR_STF (1 << 3)         /*!< Stuff error                                          */
#define CAN_ERROR_FOR (1 << 4)         /*!< Form error                                           */
#define CAN_ERROR_ACK (1 << 5)         /*!< Acknowledgment error                                 */
#define CAN_ERROR_BR (1 << 6)          /*!< Bit recessive error                                  */
#define CAN_ERROR_BD (1 << 7)          /*!< Bit dominant error                                   */
#define CAN_ERROR_CRC (1 << 8)         /*!< CRC error                                            */
#define CAN_ERROR_RX_OV (1 << 9)       /*!< By David:Rx overrun error*/
#define CAN_ERROR_TX_ALST (1 << 10)    /*!< By David:TxMailbox transmit failure due to arbitration lost */
#define CAN_ERROR_TX_TERR (1 << 11)    /*!< By David: TxMailbox  transmit failure due to tranmit error    */
#define CAN_ERROR_NOT_READY (1 << 12)  /*!< Peripheral not ready                                 */
#define CAN_ERROR_TX_BUSY (1 << 13)    /*By David:*/
#define CAN_ERROR_DATA_PHASE (1 << 14) /*By David:Protocol Error in Data Phase (Data Bit Time is used) */


// ============================================================================
// 结构体定义 (Pack = 4字节对齐)
// ============================================================================

#pragma pack(push, 4)

// CAN数据包结构
typedef struct {
    uint32_t Identifier;
    uint8_t DataLen;
    uint8_t Data[MAX_CAN_DATA_LEN];
    uint32_t Flags;
    uint64_t Key;
    uint32_t UserData;
} CanXPacketNative;

// CAN发送结果结构
typedef struct {
    uint64_t Key;
    uint32_t ErrorCode;
} CanXSentResultNative;

// CAN波特率配置结构
typedef struct {
    uint32_t Rate;
    uint32_t Prescaler;
    uint32_t Sjw;
    uint32_t Ts1;
    uint32_t Ts2;
} CanXRateNative;

// CAN配置结构
typedef struct {
    CanXFrameFormat FrameFormat;
    uint8_t IsEnableFilter;
    uint8_t IsEnableChannel;
    CanXRunMode RunMode;
    CanXRateNative BaudRateConfig;
    uint32_t FrameId;
    uint32_t FrameIdMask;
    CanFdMode FdMode;
    CanXRateNative DataRateConfig;
} CanXConfigNative;

// CAN总线状态结构
typedef struct {
    CanXBusStateType State;
    uint32_t TxErrorCounter;
    uint32_t RxErrorCounter;
    uint32_t SentCounter;
    uint32_t ReceivedCounter;
    uint64_t TxTimeNs;
    uint64_t RxTimeNs;
    uint32_t BusLoad;
} CanXBusStateNative;

// CAN接收错误事件结构
typedef struct {
    CanXPacketFlags Flags;
    uint32_t ErrorCode;
} CanXRxErrorEventNative;


// CAN事件结构（Explicit布局）
typedef struct {
    CanXEventType EventType;      // offset 0, size 4
    uint64_t TimeStamp;            // offset 4, size 8
    uint32_t ChannelIndex;         // offset 12, size 4
    union 
    {
        CanXPacketNative CanXRxPacket;
        CanXSentResultNative CanXSentResult;
        CanXBusStateNative CanXBusState;
        CanXRxErrorEventNative CanXRxErrorEvent;
    };
    
} CanXEventNative;

#pragma pack(pop)

// ============================================================================
// 函数声明
// ============================================================================

#ifdef __cplusplus
extern "C" {
#endif

/**
 * @brief 查询CAN设备索引是否存在
 * @param deviceIndex CAN设备索引
 * @return true 设备索引有效, false 设备索引无效
 */
bool Hal_CanX_IsDeviceIndexValid(int32_t deviceIndex);

/**
 * @brief 设置全局CAN设备的接收事件句柄
 * @param eventHandle 事件句柄指针
 */
bool Hal_CanX_SetGlobalEventHandle(QueueHandle_t eventHandle,int itemSize);

/**
 * @brief 打开CAN设备
 * @param deviceIndex CAN设备索引
 * @param config 指向CanXConfigNative结构的指针
 * @return true 打开成功, false 打开失败
 */
bool Hal_CanX_Open(int32_t deviceIndex, CanXConfigNative* config);

/**
 * @brief 关闭CAN设备
 * @param deviceIndex CAN设备索引
 */
void Hal_CanX_Close(int32_t deviceIndex);

/**
 * @brief 发送CAN数据包
 * @param deviceIndex CAN设备索引
 * @param packet 指向CanXPacketNative结构的指针
 * @param timeoutMs 超时时间（毫秒）
 */
void Hal_CanX_SendPacket(int32_t deviceIndex, CanXPacketNative* packet);

#ifdef __cplusplus
}
#endif

#endif // ICS_CAN_H