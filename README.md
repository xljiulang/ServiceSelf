# ServiceSelf
让.net6.0+的应用程序自安装为服务进程，支持windows和linux

### nuget
https://www.nuget.org/packages/ServiceSelf/

### 如何使用
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
