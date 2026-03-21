
using System;
using System.Runtime.InteropServices;

namespace GpioForCSharpDriver
{
    using uint32_t = System.UInt32;
    using uint16_t = System.UInt16;
    internal class Program
    {
        static void Main(string[] args)
        {
            /*
           警告：当前所有的芯片提供的是C语言的SDK，并没有提供C#的SDK，所以在C#开发最底层驱动，我认为意义不大，除非你是为了学习或者研究。
           当前我们提供这个例子，主要是为了说明C#可以直接操作寄存器，从而操作硬件。
           最好还是使用C语言来操作硬件，C#层面通过open/read/write/ioctl等接口来操作硬件。
           */
            Pe3OutputInit();
            while (true)
            {
                Pe3WritePin(true);
                System.Threading.Thread.Sleep(500);
                Pe3WritePin(false);
                System.Threading.Thread.Sleep(500);
                Console.WriteLine("Toggle PE3");
            }
            Console.WriteLine("Hello, World!");
        }



        unsafe struct RCC_TypeDef
        {
            public volatile uint32_t CR;             /*!< RCC clock control register,                                              Address offset: 0x00  */
            public volatile uint32_t HSICFGR;        /*!< HSI Clock Calibration Register,                                          Address offset: 0x04  */
            public volatile uint32_t CRRCR;          /*!< Clock Recovery RC  Register,                                             Address offset: 0x08  */
            public volatile uint32_t CSICFGR;        /*!< CSI Clock Calibration Register,                                          Address offset: 0x0C  */
            public volatile uint32_t CFGR;           /*!< RCC clock configuration register,                                        Address offset: 0x10  */
            public volatile uint32_t RESERVED1;       /*!< Reserved,                                                                Address offset: 0x14  */
            public volatile uint32_t D1CFGR;         /*!< RCC Domain 1 configuration register,                                     Address offset: 0x18  */
            public volatile uint32_t D2CFGR;         /*!< RCC Domain 2 configuration register,                                     Address offset: 0x1C  */
            public volatile uint32_t D3CFGR;         /*!< RCC Domain 3 configuration register,                                     Address offset: 0x20  */
            public volatile uint32_t RESERVED2;       /*!< Reserved,                                                                Address offset: 0x24  */
            public volatile uint32_t PLLCKSELR;      /*!< RCC PLLs Clock Source Selection Register,                                Address offset: 0x28  */
            public volatile uint32_t PLLCFGR;        /*!< RCC PLLs  Configuration Register,                                        Address offset: 0x2C  */
            public volatile uint32_t PLL1DIVR;       /*!< RCC PLL1 Dividers Configuration Register,                                Address offset: 0x30  */
            public volatile uint32_t PLL1FRACR;      /*!< RCC PLL1 Fractional Divider Configuration Register,                      Address offset: 0x34  */
            public volatile uint32_t PLL2DIVR;       /*!< RCC PLL2 Dividers Configuration Register,                                Address offset: 0x38  */
            public volatile uint32_t PLL2FRACR;      /*!< RCC PLL2 Fractional Divider Configuration Register,                      Address offset: 0x3C  */
            public volatile uint32_t PLL3DIVR;       /*!< RCC PLL3 Dividers Configuration Register,                                Address offset: 0x40  */
            public volatile uint32_t PLL3FRACR;      /*!< RCC PLL3 Fractional Divider Configuration Register,                      Address offset: 0x44  */
            public volatile uint32_t RESERVED3;      /*!< Reserved,                                                                Address offset: 0x48  */
            public volatile uint32_t D1CCIPR;       /*!< RCC Domain 1 Kernel Clock Configuration Register                         Address offset: 0x4C  */
            public volatile uint32_t D2CCIP1R;      /*!< RCC Domain 2 Kernel Clock Configuration Register                         Address offset: 0x50  */
            public volatile uint32_t D2CCIP2R;      /*!< RCC Domain 2 Kernel Clock Configuration Register                         Address offset: 0x54  */
            public volatile uint32_t D3CCIPR;       /*!< RCC Domain 3 Kernel Clock Configuration Register                         Address offset: 0x58  */
            public volatile uint32_t RESERVED4;      /*!< Reserved,                                                                Address offset: 0x5C  */
            public volatile uint32_t CIER;          /*!< RCC Clock Source Interrupt Enable Register                               Address offset: 0x60  */
            public volatile uint32_t CIFR;          /*!< RCC Clock Source Interrupt Flag Register                                 Address offset: 0x64  */
            public volatile uint32_t CICR;          /*!< RCC Clock Source Interrupt Clear Register                                Address offset: 0x68  */
            public volatile uint32_t RESERVED5;       /*!< Reserved,                                                                Address offset: 0x6C  */
            public volatile uint32_t BDCR;          /*!< RCC Vswitch Backup Domain Control Register,                              Address offset: 0x70  */
            public volatile uint32_t CSR;           /*!< RCC clock control & status register,                                     Address offset: 0x74  */
            public volatile uint32_t RESERVED6;       /*!< Reserved,                                                                Address offset: 0x78  */
            public volatile uint32_t AHB3RSTR;       /*!< RCC AHB3 peripheral reset register,                                      Address offset: 0x7C  */
            public volatile uint32_t AHB1RSTR;       /*!< RCC AHB1 peripheral reset register,                                      Address offset: 0x80  */
            public volatile uint32_t AHB2RSTR;       /*!< RCC AHB2 peripheral reset register,                                      Address offset: 0x84  */
            public volatile uint32_t AHB4RSTR;       /*!< RCC AHB4 peripheral reset register,                                      Address offset: 0x88  */
            public volatile uint32_t APB3RSTR;       /*!< RCC APB3 peripheral reset register,                                      Address offset: 0x8C  */
            public volatile uint32_t APB1LRSTR;      /*!< RCC APB1 peripheral reset Low Word register,                             Address offset: 0x90  */
            public volatile uint32_t APB1HRSTR;      /*!< RCC APB1 peripheral reset High Word register,                            Address offset: 0x94  */
            public volatile uint32_t APB2RSTR;       /*!< RCC APB2 peripheral reset register,                                      Address offset: 0x98  */
            public volatile uint32_t APB4RSTR;       /*!< RCC APB4 peripheral reset register,                                      Address offset: 0x9C  */
            public volatile uint32_t GCR;            /*!< RCC RCC Global Control  Register,                                        Address offset: 0xA0  */
            public volatile uint32_t RESERVED8;       /*!< Reserved,                                                                Address offset: 0xA4  */
            public volatile uint32_t D3AMR;          /*!< RCC Domain 3 Autonomous Mode Register,                                   Address offset: 0xA8  */
            public fixed uint32_t RESERVED11[9];    /*!< Reserved, 0xAC-0xCC                                                      Address offset: 0xAC  */
            public volatile uint32_t RSR;            /*!< RCC Reset status register,                                               Address offset: 0xD0  */
            public volatile uint32_t AHB3ENR;        /*!< RCC AHB3 peripheral clock  register,                                     Address offset: 0xD4  */
            public volatile uint32_t AHB1ENR;        /*!< RCC AHB1 peripheral clock  register,                                     Address offset: 0xD8  */
            public volatile uint32_t AHB2ENR;        /*!< RCC AHB2 peripheral clock  register,                                     Address offset: 0xDC  */
            public volatile uint32_t AHB4ENR;        /*!< RCC AHB4 peripheral clock  register,                                     Address offset: 0xE0  */
            public volatile uint32_t APB3ENR;        /*!< RCC APB3 peripheral clock  register,                                     Address offset: 0xE4  */
            public volatile uint32_t APB1LENR;       /*!< RCC APB1 peripheral clock  Low Word register,                            Address offset: 0xE8  */
            public volatile uint32_t APB1HENR;       /*!< RCC APB1 peripheral clock  High Word register,                           Address offset: 0xEC  */
            public volatile uint32_t APB2ENR;        /*!< RCC APB2 peripheral clock  register,                                     Address offset: 0xF0  */
            public volatile uint32_t APB4ENR;        /*!< RCC APB4 peripheral clock  register,                                     Address offset: 0xF4  */
            public volatile uint32_t RESERVED12;      /*!< Reserved,                                                                Address offset: 0xF8  */
            public volatile uint32_t AHB3LPENR;      /*!< RCC AHB3 peripheral sleep clock  register,                               Address offset: 0xFC  */
            public volatile uint32_t AHB1LPENR;      /*!< RCC AHB1 peripheral sleep clock  register,                               Address offset: 0x100 */
            public volatile uint32_t AHB2LPENR;      /*!< RCC AHB2 peripheral sleep clock  register,                               Address offset: 0x104 */
            public volatile uint32_t AHB4LPENR;      /*!< RCC AHB4 peripheral sleep clock  register,                               Address offset: 0x108 */
            public volatile uint32_t APB3LPENR;      /*!< RCC APB3 peripheral sleep clock  register,                               Address offset: 0x10C */
            public volatile uint32_t APB1LLPENR;     /*!< RCC APB1 peripheral sleep clock  Low Word register,                      Address offset: 0x110 */
            public volatile uint32_t APB1HLPENR;     /*!< RCC APB1 peripheral sleep clock  High Word register,                     Address offset: 0x114 */
            public volatile uint32_t APB2LPENR;      /*!< RCC APB2 peripheral sleep clock  register,                               Address offset: 0x118 */
            public volatile uint32_t APB4LPENR;      /*!< RCC APB4 peripheral sleep clock  register,                               Address offset: 0x11C */
            public fixed uint32_t RESERVED13[4];   /*!< Reserved, 0x120-0x12C                                                    Address offset: 0x120 */


        };
        //#define PERIPH_BASE               (0x40000000UL)
        //#define D3_AHB1PERIPH_BASE       (PERIPH_BASE + 0x18020000UL)
        //#define RCC_BASE              (D3_AHB1PERIPH_BASE + 0x4400UL)
        //#define RCC                 ((RCC_TypeDef *) RCC_BASE)

        static readonly uint32_t PERIPH_BASE = 0x40000000;
        static readonly uint32_t D3_AHB1PERIPH_BASE = PERIPH_BASE + 0x18020000;
        static readonly uint32_t RCC_BASE = D3_AHB1PERIPH_BASE + 0x4400;
        static readonly unsafe RCC_TypeDef* RCC = (RCC_TypeDef*)RCC_BASE;

        /*
         #define SET_BIT(REG, BIT)     ((REG) |= (BIT))
         */
        static void SET_BIT(ref uint32_t REG, uint32_t BIT)
        {
            REG |= BIT;
        }

        /*
         #define RCC_AHB4ENR_GPIOEEN_Pos                (4U)
#define RCC_AHB4ENR_GPIOEEN_Msk                (0x1UL << RCC_AHB4ENR_GPIOEEN_Pos)       
#define RCC_AHB4ENR_GPIOEEN                    RCC_AHB4ENR_GPIOEEN_Msk
         */

        static readonly uint32_t RCC_AHB4ENR_GPIOEEN_Pos = 4U;
        static readonly uint32_t RCC_AHB4ENR_GPIOEEN_Msk = (0x1U << (int)RCC_AHB4ENR_GPIOEEN_Pos);
        static readonly uint32_t RCC_AHB4ENR_GPIOEEN = RCC_AHB4ENR_GPIOEEN_Msk;

        /*
         * 
    #define __HAL_RCC_GPIOE_CLK_ENABLE()   do { \
                                            public volatile  uint32_t tmpreg; \
                                            SET_BIT(RCC->AHB4ENR, RCC_AHB4ENR_GPIOEEN);\
                                            tmpreg = READ_BIT(RCC->AHB4ENR, RCC_AHB4ENR_GPIOEEN);\
                                            UNUSED(tmpreg); \
                                           } while(0)
         */


        static unsafe void HAL_RCC_GPIOE_CLK_ENABLE()
        {
            SET_BIT(ref RCC->AHB4ENR, RCC_AHB4ENR_GPIOEEN);
            uint32_t tmpreg = RCC->AHB4ENR & RCC_AHB4ENR_GPIOEEN;
            //UNUSED(tmpreg);

        }

        unsafe public struct GPIO_TypeDef
        {
            public volatile uint32_t MODER;    /*!< GPIO port mode register,               Address offset: 0x00      */
            public volatile uint32_t OTYPER;   /*!< GPIO port output type register,        Address offset: 0x04      */
            public volatile uint32_t OSPEEDR;  /*!< GPIO port output speed register,       Address offset: 0x08      */
            public volatile uint32_t PUPDR;    /*!< GPIO port pull-up/pull-down register,  Address offset: 0x0C      */
            public volatile uint32_t IDR;      /*!< GPIO port input data register,         Address offset: 0x10      */
            public volatile uint32_t ODR;      /*!< GPIO port output data register,        Address offset: 0x14      */
            public volatile uint32_t BSRR;     /*!< GPIO port bit set/reset,               Address offset: 0x18      */
            public volatile uint32_t LCKR;     /*!< GPIO port configuration lock register, Address offset: 0x1C      */
            public fixed uint32_t AFR[2];   /*!< GPIO alternate function registers,     Address offset: 0x20-0x24 */
        };
        //#define GPIOE_BASE            (D3_AHB1PERIPH_BASE + 0x1000UL)
        static readonly uint32_t GPIOE_BASE = D3_AHB1PERIPH_BASE + 0x1000;
        //#define GPIOE               ((GPIO_TypeDef *) GPIOE_BASE)
        static readonly unsafe GPIO_TypeDef* GPIOE = (GPIO_TypeDef*)GPIOE_BASE;

        /*
         #define GPIO_OSPEEDR_OSPEED0_Pos       (0U)
        #define GPIO_OSPEEDR_OSPEED0_Msk       (0x3UL << GPIO_OSPEEDR_OSPEED0_Pos)   
        #define GPIO_OSPEEDR_OSPEED0           GPIO_OSPEEDR_OSPEED0_Msk
         */
        static readonly uint32_t GPIO_OSPEEDR_OSPEED0_Pos = 0U;
        static readonly uint32_t GPIO_OSPEEDR_OSPEED0_Msk = (0x3U << (int)GPIO_OSPEEDR_OSPEED0_Pos);
        static readonly uint32_t GPIO_OSPEEDR_OSPEED0 = GPIO_OSPEEDR_OSPEED0_Msk;
        /*
         #define GPIO_PIN_0                 ((uint16_t)0x0001) 
#define GPIO_PIN_1                 ((uint16_t)0x0002)  
#define GPIO_PIN_2                 ((uint16_t)0x0004) 
#define GPIO_PIN_3                 ((uint16_t)0x0008)
#define GPIO_PIN_4                 ((uint16_t)0x0010)  
#define GPIO_PIN_5                 ((uint16_t)0x0020) 
#define GPIO_PIN_6                 ((uint16_t)0x0040) 
#define GPIO_PIN_7                 ((uint16_t)0x0080) 
#define GPIO_PIN_8                 ((uint16_t)0x0100)  
#define GPIO_PIN_9                 ((uint16_t)0x0200) 
#define GPIO_PIN_10                ((uint16_t)0x0400) 
#define GPIO_PIN_11                ((uint16_t)0x0800) 
#define GPIO_PIN_12                ((uint16_t)0x1000
#define GPIO_PIN_13                ((uint16_t)0x2000) 
#define GPIO_PIN_14                ((uint16_t)0x4000) 
#define GPIO_PIN_15                ((uint16_t)0x8000)
#define GPIO_PIN_All               ((uint16_t)0xFFFF) 
         */
        static readonly uint16_t GPIO_PIN_0 = 0x0001;
        static readonly uint16_t GPIO_PIN_1 = 0x0002;
        static readonly uint16_t GPIO_PIN_2 = 0x0004;
        static readonly uint16_t GPIO_PIN_3 = 0x0008;
        static readonly uint16_t GPIO_PIN_4 = 0x0010;
        static readonly uint16_t GPIO_PIN_5 = 0x0020;
        static readonly uint16_t GPIO_PIN_6 = 0x0040;
        static readonly uint16_t GPIO_PIN_7 = 0x0080;
        static readonly uint16_t GPIO_PIN_8 = 0x0100;
        static readonly uint16_t GPIO_PIN_9 = 0x0200;
        static readonly uint16_t GPIO_PIN_10 = 0x0400;
        static readonly uint16_t GPIO_PIN_11 = 0x0800;
        static readonly uint16_t GPIO_PIN_12 = 0x1000;
        static readonly uint16_t GPIO_PIN_13 = 0x2000;
        static readonly uint16_t GPIO_PIN_14 = 0x4000;
        static readonly uint16_t GPIO_PIN_15 = 0x8000;
        static readonly uint16_t GPIO_PIN_All = 0xFFFF;

        /*
         #define  GPIO_SPEED_FREQ_LOW         (0x00000000U) 
#define  GPIO_SPEED_FREQ_MEDIUM      (0x00000001U) 
#define  GPIO_SPEED_FREQ_HIGH        (0x00000002U)  
#define  GPIO_SPEED_FREQ_VERY_HIGH   (0x00000003U) 
         */
        static readonly uint32_t GPIO_SPEED_FREQ_LOW = 0x00000000U;
        static readonly uint32_t GPIO_SPEED_FREQ_MEDIUM = 0x00000001U;
        static readonly uint32_t GPIO_SPEED_FREQ_HIGH = 0x00000002U;
        static readonly uint32_t GPIO_SPEED_FREQ_VERY_HIGH = 0x00000003U;

        /*
         #define GPIO_OTYPER_OT0_Pos            (0U)
#define GPIO_OTYPER_OT0_Msk            (0x1UL << GPIO_OTYPER_OT0_Pos)          

#define GPIO_OTYPER_OT0                GPIO_OTYPER_OT0_Msk
         */
        static readonly uint32_t GPIO_OTYPER_OT0_Pos = 0U;
        static readonly uint32_t GPIO_OTYPER_OT0_Msk = (0x1U << (int)GPIO_OTYPER_OT0_Pos);
        static readonly uint32_t GPIO_OTYPER_OT0 = GPIO_OTYPER_OT0_Msk;

        /*
         #define GPIO_MODE_Pos                           0u
#define GPIO_MODE                               (0x3uL << GPIO_MODE_Pos)
#define MODE_INPUT                              (0x0uL << GPIO_MODE_Pos)
#define MODE_OUTPUT                             (0x1uL << GPIO_MODE_Pos)
         */
        static readonly uint32_t GPIO_MODE_Pos = 0;
        static readonly uint32_t GPIO_MODE =(uint32_t) (0x3 << (int)GPIO_MODE_Pos);
        static readonly uint32_t MODE_INPUT = (uint32_t)(0x0 << (int)GPIO_MODE_Pos);
        static readonly uint32_t MODE_OUTPUT = (uint32_t)(0x1 << (int)GPIO_MODE_Pos);

        /*
         #define OUTPUT_TYPE_Pos                         4u
#define OUTPUT_TYPE                             (0x1uL << OUTPUT_TYPE_Pos)
#define OUTPUT_PP                               (0x0uL << OUTPUT_TYPE_Pos)
#define OUTPUT_OD                               (0x1uL << OUTPUT_TYPE_Pos)
         */
        static readonly uint32_t OUTPUT_TYPE_Pos = 4u;
        static readonly uint32_t OUTPUT_TYPE = (0x1u << (int)OUTPUT_TYPE_Pos);
        static readonly uint32_t OUTPUT_PP = (0x0u << (int)OUTPUT_TYPE_Pos);    
        static readonly uint32_t OUTPUT_OD = (0x1u << (int)OUTPUT_TYPE_Pos);

        /*
         #define GPIO_MODER_MODE0_Pos           (0U)
#define GPIO_MODER_MODE0_Msk           (0x3UL << GPIO_MODER_MODE0_Pos)         
#define GPIO_MODER_MODE0               GPIO_MODER_MODE0_Msk
         */
        static readonly uint32_t GPIO_MODER_MODE0_Pos = 0U;
        static readonly uint32_t GPIO_MODER_MODE0_Msk = (0x3U << (int)GPIO_MODER_MODE0_Pos);
        static readonly uint32_t GPIO_MODER_MODE0 = GPIO_MODER_MODE0_Msk;

        /*
         #define GPIO_MODE_INPUT                 MODE_INPUT                           
#define GPIO_MODE_OUTPUT_PP             (MODE_OUTPUT | OUTPUT_PP)                                  
         */
        static readonly uint32_t GPIO_MODE_INPUT = MODE_INPUT;
        static readonly uint32_t GPIO_MODE_OUTPUT_PP = (MODE_OUTPUT | OUTPUT_PP);
        static unsafe void Pe3OutputInit()
        {
            HAL_RCC_GPIOE_CLK_ENABLE();
            uint32_t temp;
            uint32_t position = 3;//(uint32_t)1<<GPIO_PIN_3;
            temp = GPIOE->OSPEEDR;
            temp &= ~(GPIO_OSPEEDR_OSPEED0 << (int)(position * 2U));
            temp |= (GPIO_SPEED_FREQ_VERY_HIGH << (int)(position * 2U));
            GPIOE->OSPEEDR = temp;

            temp = GPIOE->OTYPER;
            temp &= ~(GPIO_OTYPER_OT0 << (int)position);
            temp |= (uint32_t)((uint32_t)((uint32_t)(MODE_OUTPUT & OUTPUT_TYPE) >> (int)OUTPUT_TYPE_Pos) << (int)position);
            GPIOE->OTYPER = temp;

            temp = GPIOE->MODER;
            temp &= ~(GPIO_MODER_MODE0 << (int)(position * 2U));
            temp |= ((GPIO_MODE_OUTPUT_PP & GPIO_MODE) << (int)(position * 2U));
            GPIOE->MODER = temp;


        }
        //#define GPIO_NUMBER           (16U)
        static readonly uint32_t GPIO_NUMBER = 16U;
        static unsafe void Pe3WritePin(bool pinState)
        {
            if (pinState != false)
            {
                GPIOE->BSRR = GPIO_PIN_3;
            }
            else
            {
                GPIOE->BSRR = (uint32_t)GPIO_PIN_3 <<(int) GPIO_NUMBER;
            }
        }
    }
}
