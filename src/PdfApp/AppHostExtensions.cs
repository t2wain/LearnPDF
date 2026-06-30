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
                .AddJsonFile("appsettings.Development.json", true);

            builder.Logging.AddConsole();

            var iconfig = builder.Configuration;

            builder.Services.Configure<AppConfig>(iconfig.GetSection("AppConfig"));
            builder.Services.PostConfigureAll<AppConfig>(options =>
            {
                var dwgConfigs = ReadDwgConfigs();
                foreach( var dwgConfig in dwgConfigs)
                    options.DwgConfigs.Add(dwgConfig);
            });

            builder.Services.AddScoped<IPdfDrawing, PdfDrawing>();
            builder.Services.AddScoped<IDocParser>(p => new DocParserInstr { Name = "INSTR"});
            builder.Services.AddScoped<IDocParser>(p => new DocParser { Name = "SLD" });

            return builder;
        }

        public static List<DwgConfig> ReadDwgConfigs()
        {
            var files = Directory.GetFiles(AppContext.BaseDirectory, "dwgconfig*.json");

            List<DwgConfig> res = new();
            foreach (var provider in files)
            {
                var tempConfig = new ConfigurationBuilder()
                    .AddJsonFile(provider, optional: true)
                    .Build();

                var list = tempConfig.GetSection("AppConfig:DwgConfigs").Get<List<DwgConfig>>();
                if (list != null)
                    res.AddRange(list);
            }
            return res;
        }
    }
}
