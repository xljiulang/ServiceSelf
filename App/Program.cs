using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using ServiceSelf;
using System.IO;

namespace App
{
    class Program
    {
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
    }
}