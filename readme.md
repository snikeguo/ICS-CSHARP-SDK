# ICS CSharp Board SDK 使用说明

使用本 SDK，你可以**完全使用 C# 语言**开发嵌入式应用程序，无需接触任何 C 语言或底层代码。

---

## 环境要求

- Windows 操作系统
- [.NET 10 SDK](https://dotnet.microsoft.com/zh-cn/download/dotnet/10.0)
- [ICS C# SDK](https://gitee.com/ICS_PUBLIC/ics-csharp-sdk)
---

## 第一步： ICS C# SDK

下载 SDK 后，SDK 目录下运行
```
python .\publish_ics_csharp_sdk.py
```
---

## 第二步：创建用户工程

新建一个目录作为你的工程，并创建 `.csproj` 文件，内容如下：

```xml
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <OutputType>Exe</OutputType>
        <TargetFramework>net10.0</TargetFramework>
        <ImplicitUsings>enable</ImplicitUsings>
        <Nullable>enable</Nullable>
        <!-- 指向 SDK 所在目录 -->
        <IcsCSharpSdk>D:\Project\ICS_CSharpBoardSdk\ICS</IcsCSharpSdk>
        <DisableUnsupportedError>true</DisableUnsupportedError>
        <InvariantGlobalization>true</InvariantGlobalization>
        <AllowUnsafeBlocks>True</AllowUnsafeBlocks>
    </PropertyGroup>

    <ItemGroup>
        <PrivateSdkAssemblies Include="$(IlcSdkPath)\*.dll" />
    </ItemGroup>

    <!-- 按需引用 SDK 中的库，例如 RTOS 通用库 -->
    <ItemGroup>
        <ProjectReference Include="$(IcsCSharpSdk)\csharp_libs\Ics.Rtos\Ics.Rtos.Common\Ics.Rtos.Common.csproj" />
    </ItemGroup>

    <Import Project="$(IcsCSharpSdk)\targets\Ics.NativeAot.Nuttx.targets" />

</Project>
```

> 将 `<IcsCSharpSdk>` 的值修改为你本机 SDK 的实际路径。

---

## 第三步：编写应用程序

在工程目录下创建 `Program.cs`，编写你的应用逻辑，例如：

```csharp
using System;
using Ics.Rtos.Common;
Ics.Rtos.Common.Ics.Initialize();//初始化RTOS环境
Console.WriteLine("Hello from C# on embedded board!");

while (true)
{
    // 你的业务逻辑
    System.Threading.Thread.Sleep(1000);
}
```

完全使用标准 C# 语法，无需关心任何底层细节。

---

## 第四步：编译工程

在工程目录下执行以下命令：

```bash
dotnet publish -c Release -r linux-arm /p:PublishAot=true
```

编译成功后，输出文件位于：

```
bin\ARM32\Release\net10.0\linux-arm\publish\nuttx.bin
```

---

## 第五步：部署运行

1. 将 `nuttx.bin` 文件复制到 SD 卡的 `ics` 文件夹中
2. 将 SD 卡插回开发板
3. 重新上电，程序即自动运行 🎉

---
