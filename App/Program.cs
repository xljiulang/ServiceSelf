using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ServiceSelf;

namespace App
{
    class Program
    {
        static void Main(string[] args)
        {
            var serviceName = "app";
            var serviceOptions = new ServiceOptions
            {
                Arguments = new[] { new Argument("key", "value") },
                Description = "这是演示示例应用",
            };
            serviceOptions.OSLinux.Service.Restart = "always";
            serviceOptions.OSLinux.Service.RestartSec = "10";
            serviceOptions.OSWindows.DisplayName = "演示示例";
            serviceOptions.OSWindows.FailureActionType = WindowsServiceActionType.Restart;

            if (Service.UseServiceSelf(args, serviceName, serviceOptions))
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