using Ics.Rtos.Common;
using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading;

namespace Ics.Rtos.Abstractions.Uart
{
	public enum UartEventType : uint
	{
		RxData = 0,
		Error = 1,
		Overflow = 2,
		QuitEvent = 255,
	}

	public enum UartDataBits : uint
	{
		Bits7 = 7,
		Bits8 = 8,
		Bits9 = 9,
	}

	public enum UartStopBits : uint
	{
		One = 0,
		OnePointFive = 1,
		Two = 2,
	}

	public enum UartParity : uint
	{
		None = 0,
		Odd = 1,
		Even = 2,
	}

	public enum UartFlowControl : uint
	{
		None = 0,
		RtsCts = 1,
		XonXoff = 2,
	}

	[Flags]
	public enum UartErrorCode : uint
	{
		None = 0,
		Parity = 1u << 0,
		Framing = 1u << 1,
		Overrun = 1u << 2,
		Noise = 1u << 3,
		Timeout = 1u << 4,
		TxBusy = 1u << 5,
		BufferFull = 1u << 6,
	}

	public sealed class UartConfig
	{
		public uint BaudRate { get; set; } = 115200;
		public UartDataBits DataBits { get; set; } = UartDataBits.Bits8;
		public UartStopBits StopBits { get; set; } = UartStopBits.One;
		public UartParity Parity { get; set; } = UartParity.None;
		public UartFlowControl FlowControl { get; set; } = UartFlowControl.None;
		public uint RxBufferSize { get; set; } = 256;
	}

	public sealed class UartRxData
	{
		public uint Length { get; set; }
		public byte[] Data { get; set; } = Array.Empty<byte>();
	}

	public sealed class UartErrorEvent
	{
		public UartErrorCode ErrorCode { get; set; }
	}

	public sealed class UartOverflowEvent
	{
		public uint LostBytes { get; set; }
	}

	public sealed class UartEvent
	{
		public UartEventType EventType { get; set; }
		public ulong TimeStamp { get; set; }
		public uint UartIndex { get; set; }
		public UartRxData? RxData { get; set; }
		public UartErrorEvent? ErrorEvent { get; set; }
		public UartOverflowEvent? OverflowEvent { get; set; }

		internal static UartEvent FromNative(ref UartNativeMethods.UartEventNative native)
		{
			var evt = new UartEvent
			{
				EventType = native.EventType,
				TimeStamp = native.TimeStamp,
				UartIndex = native.UartIndex,
			};

			switch (native.EventType)
			{
				case UartEventType.RxData:
					evt.RxData = new UartRxData
					{
						Length = native.RxData.Length,
					};
					break;
				case UartEventType.Error:
					evt.ErrorEvent = new UartErrorEvent
					{
						ErrorCode = (UartErrorCode)native.ErrorEvent.ErrorCode,
					};
					break;
				case UartEventType.Overflow:
					evt.OverflowEvent = new UartOverflowEvent
					{
						LostBytes = native.OverflowEvent.LostBytes,
					};
					break;
				default:
					break;
			}

			return evt;
		}
	}

	public sealed class UartDevice : IDisposable
	{
		private readonly object _sync = new();
		private bool _opened;
		private IntPtr _rxBufferPtr;
		private uint _rxBufferSize;

		public int DeviceIndex { get; }

		public event Action<UartEvent>? EventReceived;

		public UartDevice(int deviceIndex)
		{
			DeviceIndex = deviceIndex;
		}

		private static int _globalReceiveQueueSize = 1024;
		public static int GlobalReceiveQueueSize
		{
			get => _globalReceiveQueueSize;
			set
			{
				if (value <= 0) throw new ArgumentOutOfRangeException(nameof(GlobalReceiveQueueSize));
				_globalReceiveQueueSize = value;
				UartSharedReceiver.EnsureGlobalQueueStarted(value);
			}
		}

		public static void DeInit() => UartSharedReceiver.StopAll();

		public static bool IsDeviceIndexValid(int deviceIndex) => UartNativeMethods.Hal_Uart_IsDeviceIndexValid(deviceIndex);

		public bool Open(UartConfig config)
		{
			if (config == null) throw new ArgumentNullException(nameof(config));

			lock (_sync)
			{
				if (_opened) throw new InvalidOperationException("Device already opened.");
				if (!IsDeviceIndexValid(DeviceIndex)) return false;

				UartSharedReceiver.EnsureGlobalQueueStarted(_globalReceiveQueueSize);

				var rxSize = config.RxBufferSize == 0 ? 1u : config.RxBufferSize;
				_rxBufferPtr = Marshal.AllocHGlobal((int)rxSize);
				_rxBufferSize = rxSize;

				var nativeConfig = new UartNativeMethods.UartConfigNative
				{
					BaudRate = config.BaudRate,
					DataBits = config.DataBits,
					StopBits = config.StopBits,
					Parity = config.Parity,
					FlowControl = config.FlowControl,
					RxBuffer = _rxBufferPtr,
					RxBufferSize = _rxBufferSize,
				};

				var opened = false;
				try
				{
					opened = UartNativeMethods.Hal_Uart_Open(DeviceIndex, ref nativeConfig);
					if (!opened)
					{
						FreeRxBuffer();
						return false;
					}

					UartSharedReceiver.Register(DeviceIndex, this);

					if (!UartNativeMethods.Hal_Uart_StartReceive(DeviceIndex))
					{
						UartSharedReceiver.Unregister(DeviceIndex);
						UartNativeMethods.Hal_Uart_Close(DeviceIndex);
						FreeRxBuffer();
						return false;
					}

					_opened = true;
					return true;
				}
				catch
				{
					if (opened)
					{
						UartNativeMethods.Hal_Uart_Close(DeviceIndex);
						UartSharedReceiver.Unregister(DeviceIndex);
					}
					FreeRxBuffer();
					throw;
				}
			}
		}

		public void Close()
		{
			lock (_sync)
			{
				if (!_opened)
				{
					FreeRxBuffer();
					return;
				}

				UartSharedReceiver.Unregister(DeviceIndex);
				UartNativeMethods.Hal_Uart_Close(DeviceIndex);
				_opened = false;
				FreeRxBuffer();
			}
		}

		public bool Send(byte[] data, uint timeoutMs = 1000)
		{
			if (data == null) throw new ArgumentNullException(nameof(data));

			lock (_sync)
			{
				if (!_opened) throw new InvalidOperationException("Device not opened.");
				if (data.Length == 0) return false;
				return UartNativeMethods.Hal_Uart_Send(DeviceIndex, data, (uint)data.Length, timeoutMs);
			}
		}

		internal byte[] SnapshotRxData(uint length)
		{
			lock (_sync)
			{
				if (_rxBufferPtr == IntPtr.Zero || _rxBufferSize == 0 || length == 0)
				{
					return Array.Empty<byte>();
				}

				var copyLen = (int)Math.Min(length, _rxBufferSize);
				var data = new byte[copyLen];
				Marshal.Copy(_rxBufferPtr, data, 0, copyLen);
				return data;
			}
		}

		internal bool RestartReceive()
		{
			lock (_sync)
			{
				if (!_opened) return false;
				return UartNativeMethods.Hal_Uart_StartReceive(DeviceIndex);
			}
		}

		internal void OnEventReceived(UartEvent evt)
		{
			try
			{
				EventReceived?.Invoke(evt);
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"UartDevice[{DeviceIndex}] EventReceived handler error: {ex}");
			}
		}

		private void FreeRxBuffer()
		{
			if (_rxBufferPtr != IntPtr.Zero)
			{
				Marshal.FreeHGlobal(_rxBufferPtr);
				_rxBufferPtr = IntPtr.Zero;
				_rxBufferSize = 0;
			}
		}

		public void Dispose() => Close();
	}

	internal static class UartSharedReceiver
	{
		private sealed class Registration
		{
			public int DeviceIndex;
			public UartDevice Device = null!;
		}

		private static readonly ConcurrentDictionary<int, Registration> s_regs = new();
		private static CancellationTokenSource? s_cts;
		private static Thread? s_receiveThread;

		public static EmbedQueue<UartNativeMethods.UartEventNative>? GlobalQueueHandle { get; private set; }

		public static void EnsureGlobalQueueStarted(int maxItems)
		{
			if (GlobalQueueHandle != null) return;

			GlobalQueueHandle = new EmbedQueue<UartNativeMethods.UartEventNative>("uart_global_events", (uint)maxItems);

			try
			{
				UartNativeMethods.Hal_Uart_SetGlobalEventHandle(GlobalQueueHandle.Handle.DangerousGetHandle());
			}
			catch
			{
				GlobalQueueHandle.Dispose();
				GlobalQueueHandle = null;
				throw;
			}

			if (s_receiveThread == null || !s_receiveThread.IsAlive)
			{
				s_cts = new CancellationTokenSource();
				s_receiveThread = new Thread(() => ReceiveLoopProc(s_cts.Token))
				{
					IsBackground = true,
					Name = "UartDevice_SharedReceiver",
				};
				s_receiveThread.Start();
			}
		}

		public static void Register(int deviceIndex, UartDevice device)
		{
			var reg = new Registration { DeviceIndex = deviceIndex, Device = device };
			if (!s_regs.TryAdd(deviceIndex, reg))
			{
				throw new InvalidOperationException($"Device {deviceIndex} already registered.");
			}
		}

		public static void Unregister(int deviceIndex)
		{
			s_regs.TryRemove(deviceIndex, out _);

			if (s_regs.IsEmpty)
			{
				CleanupResources();
			}
		}

		private static void CleanupResources()
		{
			s_cts?.Cancel();
			s_receiveThread?.Join(100);
			s_receiveThread = null;

			GlobalQueueHandle?.Dispose();
			GlobalQueueHandle = null;

			UartNativeMethods.Hal_Uart_SetGlobalEventHandle(IntPtr.Zero);

			s_cts?.Dispose();
			s_cts = null;
		}

		private static void ReceiveLoopProc(CancellationToken token)
		{
			const uint perReceiveTimeoutMs = 10;

			while (!token.IsCancellationRequested)
			{
				if (GlobalQueueHandle == null || s_regs.IsEmpty)
				{
					Thread.Sleep(10);
					continue;
				}

				try
				{
					var state = GlobalQueueHandle.Receive(out UartNativeMethods.UartEventNative nativeEvt, perReceiveTimeoutMs, isIsr: false);
					if (state == QueueState.Ok)
					{
						var evt = UartEvent.FromNative(ref nativeEvt);

						if (s_regs.TryGetValue((int)evt.UartIndex, out var reg))
						{
							if (evt.EventType == UartEventType.RxData && evt.RxData != null)
							{
								evt.RxData.Data = reg.Device.SnapshotRxData(evt.RxData.Length);
								reg.Device.RestartReceive();
							}

							reg.Device.OnEventReceived(evt);

							if (evt.EventType == UartEventType.QuitEvent)
							{
								return;
							}
						}
						else
						{
							Console.Error.WriteLine($"Received UART event for unregistered channel {evt.UartIndex}");
						}
					}
					else if (state == QueueState.Timeout)
					{
						Thread.Yield();
					}
					else
					{
						Thread.Yield();
					}
				}
				catch (OperationCanceledException) when (token.IsCancellationRequested)
				{
					break;
				}
				catch (Exception ex)
				{
					Console.Error.WriteLine($"UartSharedReceiver loop error: {ex}");
					for (int i = 0; i < 5 && !token.IsCancellationRequested; i++)
					{
						Thread.Sleep(10);
					}
				}
			}
		}

		internal static void StopAll()
		{
			foreach (var key in s_regs.Keys)
			{
				s_regs.TryRemove(key, out _);
			}

			CleanupResources();
		}
	}

	internal static class UartNativeMethods
	{
		[DllImport("*")]
		internal static extern bool Hal_Uart_IsDeviceIndexValid(int uartIndex);

		[DllImport("*")]
		internal static extern bool Hal_Uart_SetGlobalEventHandle(IntPtr eventHandle);

		[DllImport("*")]
		internal static extern bool Hal_Uart_Open(int uartIndex, ref UartConfigNative config);

		[DllImport("*")]
		internal static extern void Hal_Uart_Close(int uartIndex);

		[DllImport("*")]
		internal static extern bool Hal_Uart_Send(int uartIndex, byte[] buffer, uint length, uint timeoutMs);

		[DllImport("*")]
		internal static extern bool Hal_Uart_StartReceive(int uartIndex);

		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		internal struct UartConfigNative
		{
			public uint BaudRate;
			public UartDataBits DataBits;
			public UartStopBits StopBits;
			public UartParity Parity;
			public UartFlowControl FlowControl;
			public IntPtr RxBuffer;
			public uint RxBufferSize;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		internal struct UartRxDataNative
		{
			public uint Length;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		internal struct UartErrorEventNative
		{
			public uint ErrorCode;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		internal struct UartOverflowEventNative
		{
			public uint LostBytes;
		}

		[StructLayout(LayoutKind.Explicit, Pack = 4)]
		internal struct UartEventNative
		{
			[FieldOffset(0)] public UartEventType EventType;
			[FieldOffset(4)] public ulong TimeStamp;
			[FieldOffset(12)] public uint UartIndex;
			[FieldOffset(16)] public UartRxDataNative RxData;
			[FieldOffset(16)] public UartErrorEventNative ErrorEvent;
			[FieldOffset(16)] public UartOverflowEventNative OverflowEvent;
		}
	}
}
