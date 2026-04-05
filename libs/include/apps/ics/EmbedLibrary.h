#ifndef El_PortInterface_H
#define El_PortInterface_H
#include "stdint.h"
#include "assert.h"
#include "string.h"
#include "stdio.h"
#include "stdbool.h"
//#include "console.h"
#ifdef __cplusplus
extern "C" {
#endif
	typedef enum _QueueState
	{
		QueueState_Empty,
		QueueState_Full,
		QueueState_OK,
		QueueState_Timeout,
	}QueueState;


#define EMBEDXRPC_SERVICE_INIT(type)	\
	type type##_Instance



#ifndef Bool
#define Bool uint8_t 
#endif

#ifndef True
#define True 1
#endif

#ifndef False
#define False 0
#endif
#define El_WAIT_FOREVER	0xFFFFFFFF

#define Little_Endian	1
#define Big_Endian		2
#define Host_Endian		Little_Endian

#define EmbedLibrary_Use_Rtos 1

typedef void* IntPtr;


#include "assert.h"
	typedef void* El_Semaphore_t;
	typedef void* El_Mutex_t;
	typedef void* El_Thread_t;
	typedef void* El_Queue_t;
	typedef void* El_Timer_t;
	
#define El_Debug(...)	
#define El_Delay(x)    usleep(x*1000)
#define El_Assert(x) assert(x)

	//为了移植FreeRTOS自带的IPC机制 所定义的宏


#define ThreadBeginHook()
#define ThreadExitHook()	


    //为了移植FreeRTOS自带的IPC机制 所定义的宏
#define PRIVILEGED_FUNCTION
#define configSUPPORT_DYNAMIC_ALLOCATION	1
#define configSUPPORT_STATIC_ALLOCATION	1
#define configMESSAGE_BUFFER_LENGTH_TYPE size_t

#define configASSERT_DEFINED	1 //校验静态和动态结构体是否一致的宏，建议打开

#define configMIN( a, b )    ( ( ( a ) < ( b ) ) ? ( a ) : ( b ) )

#define traceSTREAM_BUFFER_CREATE	
#define traceSTREAM_BUFFER_CREATE_FAILED
#define traceSTREAM_BUFFER_CREATE_STATIC_FAILED
#define traceSTREAM_BUFFER_DELETE
#define traceSTREAM_BUFFER_RESET
#define traceBLOCKING_ON_STREAM_BUFFER_SEND
#define traceSTREAM_BUFFER_SEND
#define traceSTREAM_BUFFER_SEND_FAILED
#define traceBLOCKING_ON_STREAM_BUFFER_RECEIVE
#define traceSTREAM_BUFFER_RECEIVE
#define traceSTREAM_BUFFER_RECEIVE_FAILED
#define traceSTREAM_BUFFER_RECEIVE_FROM_ISR
#define traceSTREAM_BUFFER_SEND_FROM_ISR

#define sbRECEIVE_COMPLETED
#define sbRECEIVE_COMPLETED_FROM_ISR

#define mtCOVERAGE_TEST_MARKER()



#define pdTRUE	1
#define	pdPASS	1
#define pdFALSE 0
#define pdFAIL	0
#define errQUEUE_EMPTY ((BaseType_t)0)
#define errQUEUE_FULL ((BaseType_t)0)

#define configASSERT	El_Assert

	typedef int32_t BaseType_t;
	typedef uint32_t TickType_t;
	typedef uint32_t UBaseType_t;

	void* pvPortMalloc(size_t size);
	void vPortFree(void*);

#include "message_buffer_noos_and_win32.h"
#include "queue_noos_and_win32.h"

#define El_Strncpy strncpy
#define El_Strncmp    strncmp
#define El_Strlen      strlen
#define El_Strcat		strcat



#define CallFunction(Function,...) do{if (Function != NULL){Function(__VA_ARGS__);}}while(0); //using C99 Mode.

	El_Thread_t El_CreateThread(const char* threadName, uint8_t priority, void (*Thread)(void*), void* Arg, uint16_t stack_size);
	El_Semaphore_t El_CreateSemaphore(const char* semaphoreName);
	El_Mutex_t El_CreateMutex(const char* mutexName);
	El_Queue_t El_CreateQueue(const char* queueName, uint32_t queueItemSize, uint32_t maxItemLen);
	El_Timer_t El_CreateTimer(const char* timerName, uint32_t timeout, void (*timercb)(void* arg), void* Arg);

	void El_DeleteThread(El_Thread_t thread);
	void El_DeleteMutex(El_Mutex_t);
	void El_DeleteQueue(El_Queue_t);
	void El_DeleteSemaphore(El_Semaphore_t);
	void El_DeleteTimer(El_Timer_t);

	void El_ThreadStart(El_Thread_t thread,int isIsr);

	void El_TimerStart(El_Timer_t timer, uint16_t interval, int isIsr);
	void El_TimerReset(El_Timer_t timer, int isIsr);
	void El_TimerStop(El_Timer_t timer, int isIsr);


	bool El_TakeMutex(El_Mutex_t mutex, uint32_t timeout, int isIsr);
	bool El_ReleaseMutex(El_Mutex_t mutex, int isIsr);

	QueueState El_TakeSemaphore(El_Semaphore_t sem, uint32_t timeout,int isIsr);
	QueueState El_ReleaseSemaphore(El_Semaphore_t sem,int isIsr);
	QueueState El_ResetSemaphore(El_Semaphore_t sem,int isIsr);

	QueueState El_ReceiveQueue(El_Queue_t queue, void* item, uint32_t itemSize, uint32_t timeout, int isIsr);
	QueueState El_SendQueue(El_Queue_t queue, void* item, uint32_t itemSize,int isIsr);
	QueueState El_ResetQueue(El_Queue_t queue, int isIsr);
	uint32_t El_QueueSpacesAvailable(El_Queue_t queue, int isIsr);

	void* El_Malloc(uint32_t size);
	void El_Free(void* ptr);
	void El_Memcpy(void* d, const void* s, uint32_t size);
	void El_Memset(void* d, int v, uint32_t size);
	uint32_t El_GetTick(int isIsr);

#ifdef EmbedLibrary_Use_Rtos
static inline irqstate_t EmbedLibrary_EnterCriticalSection(void)
{
	return enter_critical_section();
}
static inline void EmbedLibrary_LeaveCriticalSection(irqstate_t flags)
{
	leave_critical_section(flags);
}
#endif

#ifdef __cplusplus
}
#endif

#endif
