using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PdfParserLib;
using PdfParserLib.Config;

namespace PdfApp
{
    public static class AppHostExtensions
    {
        public static IHost GetHost(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);
            var host = builder.ConfigureAIApp().Build();
            return host;
        }

        public static HostApplicationBuilder ConfigureAIApp(this HostApplicationBuilder builder)
        {
            builder.Configuration
                .AddJsonFile("appsettings.json", true)
                .AddJsonFile("appsettings.Development.json", true)
                .AddJsonFile("dwgconfigs.json", true);

            builder.Logging.AddConsole();

            var iconfig = builder.Configuration;

            builder.Services.Configure<AppConfig>(iconfig.GetSection("AppConfig"));
            builder.Services.AddScoped<PdfDrawing>();
            builder.Services.AddScoped<PdfDrawing2>();

            return builder;
        }
    }
}
