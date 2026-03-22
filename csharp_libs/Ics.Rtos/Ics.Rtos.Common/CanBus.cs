using System;
using System.Runtime.InteropServices;

namespace Ics.Rtos.Abstractions.CanBus
{
    public enum CanXRunMode : UInt32
    {
        Normal = 0,
        LoopBack = 1,
        Silent = 2,
        SilentLoopBack = 3,
    }

    public enum CanXFrameFormat : UInt32
    {
        Classic = 0,
        FdNoBrs = 1,
        FdBrs = 2,
    }

    [Flags]
    public enum CanXPacketFlags : UInt32
    {
        Data = 1 << 0,
        Standard = 1 << 1,
        Brs = 1 << 2,
        CanFd = 1 << 3,
        Esi = 1 << 4,
        Echo = 1 << 5,
    }
    public enum CanFdMode : UInt32
    {
        Iso = 0,
        Bosch = 1,
    }
    public enum CanXBusStateType : UInt32
    {
        BusOff = 0x01,
        Passive = 0x02,
        Warning = 0x04,
        Active = 0x08,
    }
    public enum CanXEventType : UInt32
    {
        RxPacket = 0,
        SentResult = 1,
        BusState = 2,
        RxErrorEvent = 3,
        QuitEvent = 255,
    }

    public enum CanXErrorCode : UInt32
    {
        None = 0,
        Ewg = 1 << 0,
        Epv = 1 << 1,
        Bof = 1 << 2,
        Stf = 1 << 3,
        For = 1 << 4,
        Ack = 1 << 5,
        Br = 1 << 6,
        Bd = 1 << 7,
        Crc = 1 << 8,
        RxOv = 1 << 9,
        TxAlst = 1 << 10,
        TxTerr = 1 << 11,
        NotReady = 1 << 12,
        TxBusy = 1 << 13,
        DataPhase = 1 << 14,
    }

    public class CanXPacket
    {
        public UInt32 Identifier;
        public Byte DataLen;
        public Byte[] Data;
        public CanXPacketFlags Flags;
        public UInt64 Key;
        public UInt32 UserData;
        internal unsafe static NativeMethods.CanXPacketNative ToNative(CanXPacket obj)
        {
            var native = new NativeMethods.CanXPacketNative
            {
                Identifier = obj.Identifier,
                DataLen = obj.DataLen,
                Flags = obj.Flags,
                Key = obj.Key,
                UserData = obj.UserData
            };
            for (int i = 0; i < obj.DataLen; i++)
            {
                native.Data[i] = obj.Data[i];
            }
            return native;
        }
        internal unsafe static CanXPacket FromNative(ref NativeMethods.CanXPacketNative native)
        {
            var packet = new CanXPacket
            {
                Identifier = native.Identifier,
                DataLen = native.DataLen,
                Flags = native.Flags,
                Key = native.Key,
                UserData = native.UserData,
                Data = new byte[native.DataLen]
            };
            for (int i = 0; i < native.DataLen; i++)
            {
                packet.Data[i] = native.Data[i];
            }
            return packet;
        }
    }
    
    public class CanXSentResult
    {
        public CanXPacket Packet;
        public CanXErrorCode ErrorCode;
    }

    

    public class CanXRate
    {
        public UInt32 Rate;
        public UInt32 Prescaler;
        public UInt32 Sjw;
        public UInt32 Ts1;
        public UInt32 Ts2;
    }
    
    public class CanXConfig
    {
        public CanXFrameFormat FrameFormat;
        public Byte IsEnableFilter;
        public Byte IsEnableChannel;
        public CanXRunMode RunMode;
        public CanXRate BaudRateConfig;
        public UInt32 FrameId;
        public UInt32 FrameIdMask;
        public CanFdMode FdMode;
        public CanXRate DataRateConfig;
        internal static NativeMethods.CanXConfigNative ToNative(CanXConfig cfg)
        {
            var native = new NativeMethods.CanXConfigNative
            {
                FrameFormat = cfg.FrameFormat,
                IsEnableFilter = cfg.IsEnableFilter,
                IsEnableChannel = cfg.IsEnableChannel,
                RunMode = cfg.RunMode,
                BaudRateConfig = new NativeMethods.CanXRateNative
                {
                    Rate = cfg.BaudRateConfig.Rate,
                    Prescaler = cfg.BaudRateConfig.Prescaler,
                    Sjw = cfg.BaudRateConfig.Sjw,
                    Ts1 = cfg.BaudRateConfig.Ts1,
                    Ts2 = cfg.BaudRateConfig.Ts2
                },
                FrameId = cfg.FrameId,
                FrameIdMask = cfg.FrameIdMask,
                FdMode = cfg.FdMode,
                DataRateConfig = new NativeMethods.CanXRateNative
                {
                    Rate = cfg.DataRateConfig.Rate,
                    Prescaler = cfg.DataRateConfig.Prescaler,
                    Sjw = cfg.DataRateConfig.Sjw,
                    Ts1 = cfg.DataRateConfig.Ts1,
                    Ts2 = cfg.DataRateConfig.Ts2
                }
            };
            return native;
        }
        internal static CanXConfig FromNative(ref NativeMethods.CanXConfigNative native)
        {
            var cfg = new CanXConfig
            {
                FrameFormat = native.FrameFormat,
                IsEnableFilter = native.IsEnableFilter,
                IsEnableChannel = native.IsEnableChannel,
                RunMode = native.RunMode,
                BaudRateConfig = new CanXRate
                {
                    Rate = native.BaudRateConfig.Rate,
                    Prescaler = native.BaudRateConfig.Prescaler,
                    Sjw = native.BaudRateConfig.Sjw,
                    Ts1 = native.BaudRateConfig.Ts1,
                    Ts2 = native.BaudRateConfig.Ts2
                },
                FrameId = native.FrameId,
                FrameIdMask = native.FrameIdMask,
                FdMode = native.FdMode,
                DataRateConfig = new CanXRate
                {
                    Rate = native.DataRateConfig.Rate,
                    Prescaler = native.DataRateConfig.Prescaler,
                    Sjw = native.DataRateConfig.Sjw,
                    Ts1 = native.DataRateConfig.Ts1,
                    Ts2 = native.DataRateConfig.Ts2
                }
            };
            return cfg;
        }

    }
    


    public class CanXBusState
    {
        public CanXBusStateType State;
        public UInt32 TxErrorCounter;
        public UInt32 RxErrorCounter;
        public UInt32 SentCounter;
        public UInt32 ReceivedCounter;
        public UInt64 TxTimeNs;
        public UInt64 RxTimeNs;
        public UInt32 BusLoad;
        internal static CanXBusState FromNative(ref NativeMethods.CanXBusStateNative native)
        {
            var state = new CanXBusState
            {
                State = native.State,
                TxErrorCounter = native.TxErrorCounter,
                RxErrorCounter = native.RxErrorCounter,
                SentCounter = native.SentCounter,
                ReceivedCounter = native.ReceivedCounter,
                TxTimeNs = native.TxTimeNs,
                RxTimeNs = native.RxTimeNs,
                BusLoad = native.BusLoad
            };
            return state;
        }
        internal static NativeMethods.CanXBusStateNative ToNative(CanXBusState obj)
        {
            var native = new NativeMethods.CanXBusStateNative
            {
                State = obj.State,
                TxErrorCounter = obj.TxErrorCounter,
                RxErrorCounter = obj.RxErrorCounter,
                SentCounter = obj.SentCounter,
                ReceivedCounter = obj.ReceivedCounter,
                TxTimeNs = obj.TxTimeNs,
                RxTimeNs = obj.RxTimeNs,
                BusLoad = obj.BusLoad
            };
            return native;
        }
    }
    
    public class CanXRxErrorEvent
    {
        public CanXPacketFlags Flags;
        public CanXErrorCode ErrorCode;
        internal static CanXRxErrorEvent FromNative(ref NativeMethods.CanXRxErrorEventNative native)
        {
            var evt = new CanXRxErrorEvent
            {
                Flags = native.Flags,
                ErrorCode = native.ErrorCode
            };
            return evt;
        }
        internal static NativeMethods.CanXRxErrorEventNative ToNative(CanXRxErrorEvent obj)
        {
            var native = new NativeMethods.CanXRxErrorEventNative
            {
                Flags = obj.Flags,
                ErrorCode = obj.ErrorCode
            };
            return native;
        }
    }
    

    
    public class CanXEvent
    {
        public CanXEventType EventType;
        public UInt64 TimeStamp;
        public UInt32 ChannelIndex;
        public CanXPacket CanXRxPacket;
        public CanXSentResult CanXSentResult;
        public CanXBusState CanXBusState;
        public CanXRxErrorEvent CanXRxErrorEvent;
        internal unsafe static CanXEvent FromNative(ref NativeMethods.CanXEventNative native)
        {
            var evt = new CanXEvent
            {
                EventType = native.EventType,
                TimeStamp = native.TimeStamp,
                ChannelIndex = native.ChannelIndex
            };
            switch (native.EventType)
            {
                case CanXEventType.RxPacket:
                    evt.CanXRxPacket = CanXPacket.FromNative(ref native.CanXRxPacket);
                    break;
                case CanXEventType.SentResult:
                    evt.CanXSentResult = new CanXSentResult
                    {
                        Packet = null,// 底层返回的是key ,晚点通过key去发送缓存中取
                        ErrorCode = native.CanXSentResult.ErrorCode
                    };
                    break;
                case CanXEventType.BusState:
                    evt.CanXBusState = CanXBusState.FromNative(ref native.CanXBusState);
                    break;
                case CanXEventType.RxErrorEvent:
                    evt.CanXRxErrorEvent = CanXRxErrorEvent.FromNative(ref native.CanXRxErrorEvent);
                    break;
                default:
                    break;
            }
            return evt;
        }
        internal unsafe static NativeMethods.CanXEventNative ToNative(CanXEvent obj)
        {
            var native = new NativeMethods.CanXEventNative
            {
                EventType = obj.EventType,
                TimeStamp = obj.TimeStamp,
                ChannelIndex = obj.ChannelIndex
            };
            switch (obj.EventType)
            {
                case CanXEventType.RxPacket:
                    native.CanXRxPacket = CanXPacket.ToNative(obj.CanXRxPacket);
                    break;
                case CanXEventType.SentResult:
                    // 注意：这里只转换 ErrorCode，Packet 需要额外处理
                    native.CanXSentResult = new NativeMethods.CanXSentResultNative
                    {
                        Key = obj.CanXSentResult.Packet != null ? obj.CanXSentResult.Packet.Key : 0,
                        ErrorCode = obj.CanXSentResult.ErrorCode
                    };
                    break;
                case CanXEventType.BusState:
                    native.CanXBusState = CanXBusState.ToNative(obj.CanXBusState);
                    break;
                case CanXEventType.RxErrorEvent:
                    native.CanXRxErrorEvent = CanXRxErrorEvent.ToNative(obj.CanXRxErrorEvent);
                    break;
                default:
                    break;
            }
            return native;
        }
    }




    internal static class NativeMethods
    {
        //查询can device索引是否存在
        [DllImport("*")]
        internal static extern bool Hal_CanX_IsDeviceIndexValid(int deviceIndex);

        //设置全局CAN设备的接收事件句柄
        [DllImport("*")]
        internal static extern bool Hal_CanX_SetGlobalEventHandle(IntPtr eventHandle,int itemSize);//itemSize为 CanXEventNative 大小,传递itemsize以便native端校验

        [DllImport("*")]
        internal static extern bool Hal_CanX_Open(int deviceIndex, ref CanXConfigNative config);
        [DllImport("*")]
        internal static extern void Hal_CanX_Close(int deviceIndex);
        [DllImport("*")]
        internal static extern void Hal_CanX_SendPacket(int deviceIndex, ref CanXPacketNative packet); //规定永远返回true 表示发送请求已提交


        public const int MaxCanDataLen = 64; // 根据实际 native 最大数据长度调整

        [StructLayout(LayoutKind.Sequential,Pack =4)]
        internal unsafe struct CanXPacketNative
        {
            public UInt32 Identifier;
            public byte DataLen;
            // 使其成为 blittable fixed buffer
            public fixed byte Data[MaxCanDataLen];
            public CanXPacketFlags Flags;
            public UInt64 Key;
            public UInt32 UserData;
        }

        [StructLayout(LayoutKind.Sequential,Pack =4)]
        internal struct CanXSentResultNative
        {
            public UInt64 Key;
            public CanXErrorCode ErrorCode;
        }


        [StructLayout(LayoutKind.Sequential,Pack =4)]
        internal struct CanXRateNative
        {
            public UInt32 Rate;
            public UInt32 Prescaler;
            public UInt32 Sjw;
            public UInt32 Ts1;
            public UInt32 Ts2;
        }
        [StructLayout(LayoutKind.Sequential,Pack =4)]
        internal struct CanXConfigNative
        {
            public CanXFrameFormat FrameFormat;
            public byte IsEnableFilter;
            public byte IsEnableChannel;
            public CanXRunMode RunMode;
            public CanXRateNative BaudRateConfig;
            public UInt32 FrameId;
            public UInt32 FrameIdMask;
            public CanFdMode FdMode;
            public CanXRateNative DataRateConfig;
        }

        [StructLayout(LayoutKind.Sequential,Pack =4)]
        internal struct CanXBusStateNative
        {
            public CanXBusStateType State;
            public UInt32 TxErrorCounter;
            public UInt32 RxErrorCounter;
            public UInt32 SentCounter;
            public UInt32 ReceivedCounter;
            public UInt64 TxTimeNs;
            public UInt64 RxTimeNs;
            public UInt32 BusLoad;
        }
        [StructLayout(LayoutKind.Sequential,Pack =4)]
        internal struct CanXRxErrorEventNative
        {

            public CanXPacketFlags Flags;
            public CanXErrorCode ErrorCode;
        }

        // EventNative 保持 Explicit 布局，payload 从 offset 16 开始
        [StructLayout(LayoutKind.Explicit)]
        internal unsafe struct CanXEventNative
        {
            [FieldOffset(0)] public CanXEventType EventType;
            [FieldOffset(4)] public UInt64 TimeStamp;
            [FieldOffset(12)] public UInt32 ChannelIndex;
            // payload union at offset 16
            [FieldOffset(16)] public CanXPacketNative CanXRxPacket;
            [FieldOffset(16)] public CanXSentResultNative CanXSentResult;
            [FieldOffset(16)] public CanXBusStateNative CanXBusState;
            [FieldOffset(16)] public CanXRxErrorEventNative CanXRxErrorEvent;
        }
    }
}
