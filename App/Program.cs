using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ServiceSelf;

namespace App
{
    class Program
    {
        static void Main(string[] args)
        {
            // 创建Host之前调用Service.UseServiceSelf(args)
            if (Service.UseServiceSelf(args))
            {
                var host = Host.CreateDefaultBuilder(args)
                    // 为Host配置UseServiceSelf()
                    .UseServiceSelf()
                    .ConfigureServices(service =>
                    {
                        service.AddHostedService<AppHostedService>();
                    })
                    .Build();

                host.Run();
            }
        }
    }
}