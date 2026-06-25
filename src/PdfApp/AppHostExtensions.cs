using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PdfParserLib.Config;
using PdfParserLib.Dwg;

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
                .AddJsonFile("dwgconfigs2.Development.json", true);

            builder.Logging.AddConsole();

            var iconfig = builder.Configuration;

            builder.Services.Configure<AppConfig>(iconfig.GetSection("AppConfig"));
            builder.Services.AddScoped<PdfDrawing>();
            builder.Services.AddScoped<IDocParser>(p => new DocParserInstr { Name = "M109"});

            return builder;
        }
    }
}
