# ICS CSharp Board SDK 使用说明

English version: [README in English](./readme_en.md)

使用本 SDK，你可以直接使用 C# 开发运行在板卡上的嵌入式应用，无需额外编写 C 语言业务代码，也无需手动处理底层 NativeAOT 集成流程。

目标板实物图：

![Target Board](./image/board.jpg)

## 环境要求

- Windows 操作系统
- [.NET 10 SDK](https://dotnet.microsoft.com/zh-cn/download/dotnet/10.0)
- [ICS C# SDK](https://gitee.com/ICS_PUBLIC/ics-csharp-sdk)

## 第一步：发布 SDK

下载 SDK 后，在 SDK 根目录执行以下命令，完成本地发布：

```powershell
python .\publish_ics_csharp_sdk.py
```

## 第二步：创建用户工程

新建一个目录作为你的工程，然后创建一个 .csproj 文件，示例如下：

```xml
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <OutputType>Exe</OutputType>
        <TargetFramework>net10.0</TargetFramework>
        <ImplicitUsings>enable</ImplicitUsings>
        <Nullable>enable</Nullable>
        <IcsCSharpSdk>D:\Project\ICS_CSharpBoardSdk\ICS</IcsCSharpSdk>
        <DisableUnsupportedError>true</DisableUnsupportedError>
        <InvariantGlobalization>true</InvariantGlobalization>
        <AllowUnsafeBlocks>True</AllowUnsafeBlocks>
    </PropertyGroup>

    <ItemGroup>
        <ProjectReference Include="$(IcsCSharpSdk)\csharp_libs\Ics.Rtos\Ics.Rtos.Common\Ics.Rtos.Common.csproj" />
    </ItemGroup>

    <Import Project="$(IcsCSharpSdk)\targets\Ics.NativeAot.Nuttx.targets" />

</Project>
```

说明：

- IcsCSharpSdk 需要改成你本机 SDK 的实际路径。
- ProjectReference 可以按需增减，示例中引用的是常用的 RTOS 通用库。

## 第三步：编写应用程序

在工程目录下创建 Program.cs，编写应用逻辑。例如：

```csharp
using System;
using System.Threading;
using Ics.Rtos.Common;

Ics.Initialize();
Console.WriteLine("Hello from C# on embedded board!");

while (true)
{
    Thread.Sleep(1000);
}
```

你可以直接使用标准 C# 语法编写业务逻辑，SDK 会负责运行时和目标板侧的集成。

## 第四步：编译工程

在工程目录下执行：

```bash
dotnet publish -c Release -r linux-arm /p:PublishAot=true -p:Platforms=ARM32    
```
or
```bash
dotnet publish -c Debug -r linux-arm /p:PublishAot=true -p:Platforms=ARM32    
```

编译成功后，产物默认位于以下目录：

```text
ics_nativeaot_user\build\ics_nativeaot_user.bin
ics_nativeaot_user\build\ics_nativeaot_user.elf
ics_nativeaot_user\build\ics_nativeaot_user.map
```

## 第五步：调试运行（加载到 SDRAM，掉电丢失）

在 VS Code 中启动调试时，选择 load via igdbxrpc，对目标程序进行加载和调试。

典型流程如下：

1. 打开工程对应的 VS Code 工作区。
2. 在调试配置中选择 load via igdbxrpc。
3. 启动调试，程序会先加载到 SDRAM，然后进入调试会话。

加载阶段界面示意：

![Load via igdbxrpc](./image/debug.load.png)

调试中的界面示意：

![Debugging Session](./image/debug.debugging.png)

## 第六步：固化到 SD NAND 并设置上电启动

如果需要让程序掉电后仍然保留，并在上电后自动启动，可以将生成的固件写入设备文件系统。

先在 PC 端启动 igdbxrpc shell：

```powershell
cd .\PcTools\GdbServer
.\Ics.IgdbXrpc.GdbServer.exe shell --serial COM9
```

连接成功后，会进入 igdbxrpc 交互提示符。执行类似下面的命令，将生成的 bin 文件写入开发板：

```text
(igdbxrpc) fwrite D:\Project\Ics.Nativeaot.Sample\InterfaceDispatchTest\ics_nativeaot_user\build\ics_nativeaot_user.bin /dev/firmware
```

写入完成后，在设备侧 NSH 中执行：

```text
debug off
```

随后重启设备即可。

如果你需要查看 igdbxrpc shell 支持的调试命令，可以在交互模式下输入：

```text
(igdbxrpc) help
```

常用能力包括：

- 查看线程与寄存器
- 单步、继续、打断点
- 读写目标内存
- 上传或下载设备文件
- 从指定内存地址执行镜像



