using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Ics.Rtos.Common
{
    internal static class InternalEmbedLibrary
    {
        internal enum QueueState
        {
            QueueState_Empty,
            QueueState_Full,
            QueueState_OK,
            QueueState_Timeout,
        }

        //EL_WAIT_FOREVER
        public const uint EL_WAIT_FOREVER = 0xFFFFFFFF;

        //El_Queue_t:IntPtr
        //El_Queue_t El_CreateQueue(const char* queueName, uint32_t queueItemSize, uint32_t maxItemLen);
        [DllImport("*")]
        public static extern IntPtr El_CreateQueue(string queueName, uint itemSize, uint maxItemLen);

        //void El_DeleteQueue(El_Queue_t);
        [DllImport("*")]
        public static extern void El_DeleteQueue(IntPtr queue);

        //QueueState El_ReceiveQueue(El_Queue_t queue, void* item, uint32_t itemSize, uint32_t timeout, int isIsr);
        [DllImport("*")]
        public static extern QueueState El_ReceiveQueue(IntPtr queue, IntPtr item, uint itemSize, uint timeout, int isIsr);

        //QueueState El_SendQueue(El_Queue_t queue, void* item, uint32_t itemSize, int isIsr);
        [DllImport("*")]
        public static extern QueueState El_SendQueue(IntPtr queue, IntPtr item, uint itemSize, int isIsr);

        //void El_ResetQueue(El_Queue_t queue, int isIsr);
        [DllImport("*")]
        public static extern void El_ResetQueue(IntPtr queue, int isIsr);

        //uint32_t El_QueueSpacesAvailable(El_Queue_t queue, int isIsr);
        [DllImport("*")]
        public static extern uint El_QueueSpacesAvailable(IntPtr queue, int isIsr);

        //xStreamBufferGenericCreate( xBufferSizeBytes, ( size_t ) 0, pdTRUE )
        [DllImport("*")]
        public static extern IntPtr xStreamBufferGenericCreate(uint bufferSizeBytes, uint triggerLevelBytes, int isMessageBuffer);

        /*
         size_t xStreamBufferSend( StreamBufferHandle_t xStreamBuffer,
                          const void * pvTxData,
                          size_t xDataLengthBytes,
                          TickType_t xTicksToWait )
         */
        [DllImport("*")]
        public static extern uint xStreamBufferSend(IntPtr streamBuffer, IntPtr txData, uint dataLengthBytes, uint ticksToWait);

        /*
         size_t xStreamBufferSendFromISR( StreamBufferHandle_t xStreamBuffer,
                                 const void * pvTxData,
                                 size_t xDataLengthBytes,
                                 BaseType_t * const pxHigherPriorityTaskWoken )
         */
        [DllImport("*")]
        public static extern uint xStreamBufferSendFromISR(IntPtr streamBuffer, IntPtr txData, uint dataLengthBytes, IntPtr higherPriorityTaskWoken);

        /*
         size_t xStreamBufferReceive( StreamBufferHandle_t xStreamBuffer,
                             void * pvRxData,
                             size_t xBufferLengthBytes,
                             TickType_t xTicksToWait )
         */
        [DllImport("*")]
        public static extern uint xStreamBufferReceive(IntPtr streamBuffer, IntPtr rxData, uint bufferLengthBytes, uint ticksToWait);

        /*
         size_t xStreamBufferReceiveFromISR( StreamBufferHandle_t xStreamBuffer,
                                    void * pvRxData,
                                    size_t xBufferLengthBytes,
                                    BaseType_t * const pxHigherPriorityTaskWoken )
         */
        [DllImport("*")]
        public static extern uint xStreamBufferReceiveFromISR(IntPtr streamBuffer, IntPtr rxData, uint bufferLengthBytes, IntPtr higherPriorityTaskWoken);

        //void vStreamBufferDelete( StreamBufferHandle_t xStreamBuffer )
        [DllImport("*")]
        public static extern void vStreamBufferDelete(IntPtr streamBuffer);

        //BaseType_t xStreamBufferIsFull( StreamBufferHandle_t xStreamBuffer )
        [DllImport("*")]
        public static extern int xStreamBufferIsFull(IntPtr streamBuffer);

        //BaseType_t xStreamBufferIsEmpty( StreamBufferHandle_t xStreamBuffer )
        [DllImport("*")]
        public static extern int xStreamBufferIsEmpty(IntPtr streamBuffer);

        //BaseType_t xStreamBufferReset( StreamBufferHandle_t xStreamBuffer )
        [DllImport("*")]
        public static extern int xStreamBufferReset(IntPtr streamBuffer);

        //size_t xStreamBufferSpacesAvailable( StreamBufferHandle_t xStreamBuffer )
        [DllImport("*")]
        public static extern uint xStreamBufferSpacesAvailable(IntPtr streamBuffer);

        //size_t xStreamBufferNextMessageLengthBytes( StreamBufferHandle_t xStreamBuffer )
        [DllImport("*")]
        public static extern uint xStreamBufferNextMessageLengthBytes(IntPtr streamBuffer);

    }

}
