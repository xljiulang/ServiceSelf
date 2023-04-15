# ServiceSelf
为[.NET 泛型主机](https://learn.microsoft.com/zh-cn/dotnet/core/extensions/generic-host)的应用程序提供自安装为服务进程的能力，支持windows和linux平台。

### 1 nuget
https://www.nuget.org/packages/ServiceSelf/

### 2 使用示例
```csharp
static void Main(string[] args)
{
    // 创建Host之前调用Service.UseServiceSelf(args)
    if (Service.UseServiceSelf(args))
    {
        var builder = WebApplication.CreateBuilder(args);

        // 为Host配置UseServiceSelf()
        builder.Host.UseServiceSelf();

        var app = builder.Build();
        app.MapGet("/", context => context.Response.WriteAsync("hello ServiceSelf"));
        app.Run();
    }
}
```

### 3 服务控制
当yourapp集成ServiceSelf之后，在管理员或root下使用如下命令控制yourapp服务

> windows平台

```bat
yourapp.exe start // 安装并启动服务
```

```bat
yourapp.exe stop // 停止并删除服务
```

> linux平台

```bash
sudo ./yourapp start // 安装并启动服务
```

```bash
sudo ./yourapp stop // 停止并删除服务
```