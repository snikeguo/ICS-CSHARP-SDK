#ifndef CanIf_H
#define CanIf_H

#include "nuttx/config.h"   
#include <stdint.h>
#include <stdbool.h>

typedef enum CanIf_Packet_Flags // serialize number type:UInt32
    {
        Data = 1,
        Std = 2,
        Brs = 4,
        CanFd = 8,
        Esi = 16,
        Echo = 32,
    } CanIf_Packet_Flags;

    enum CanIf_State_Type
    {
        CANIF_CS_STARTED,
        CANIF_CS_STOPPED,
    };

    struct CanIf_Packet
    {
        uint32_t Identifier;
        uint8_t DataLen;
        uint8_t Data[64];
        uint8_t ChannelIndex;
        uint64_t TimeStamp;
        uint32_t Flags;
    };
    struct CanIf_ProcessMessage_Type
    {
        bool IsRegionalMode;
        union
        {
            struct
            {
                uint32_t MinIdentifier;
                uint32_t MaxIdentifier;
            } IdentifierRang;
            uint32_t Identifier;
        } IdentifierConfig;
        uint32_t Flags;
        uint8_t ChannelIndex : 3; // max 7 channels
        void (*ProcessCanMessage)(struct CanIf_Packet *rx);
    };

    enum CanIf_Event
    {
        RxMsgOk,
        RxMsgLost,
        TxMsgOk,
        TxMsgCancelFinished,
        TxMsgLost,
        BusOff,

    };

    struct CanIf_PeriodicTransmission_Type
    {
        uint32_t Identifier;
        uint8_t ChannelIndex;
        uint32_t OverFlow;
        uint32_t Count;
        void (*CallBack)(void);
        bool IsEnable;
    };

    struct CanIf_BusOffConfig
    {
        uint32_t MaxQuickRestoreTimes;
        uint32_t QuickRestoreTimeSpan;
        uint32_t SlowRestoreTimeSpan;
    };


    #ifdef __cplusplus
    extern "C" {
    #endif

    int CanIf_Init(int processMessageArrayLength,struct CanIf_ProcessMessage_Type **processMessageArray, struct CanIf_BusOffConfig **busOffConfigs);
    int CanIf_DeInit(void);

    int CanIf_RxIndicationFromIsr(struct CanIf_Packet *rxMsg);
    int CanIf_TxConfirmationFromIsr(int canChannelIndex, int32_t result);

    int CanIf_Transmit(struct CanIf_Packet *tx, uint32_t timeout);
    int CanIf_WaitTxConfirmation(int canChannelIndex, int32_t *result, uint32_t timeout);

    enum CanIf_State_Type CanIf_GetState(uint8_t channelIndex);
    int CanIf_SetState(uint8_t channelIndex, enum CanIf_State_Type state);

    void CanIf_EventNotify(uint8_t channelIndex, void *arg, enum CanIf_Event eve);


    void CanIf_ProcessMessageObject(struct CanIf_Packet *packet);
    #ifdef __cplusplus
    }
    #endif

    
#endif
