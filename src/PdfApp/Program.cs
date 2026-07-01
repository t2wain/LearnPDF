using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using PdfParserLib.Config;
using PdfParserLib.Dwg;

namespace PdfApp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var host = AppHostExtensions.GetHost(args);

            ParseDoc(host);
            ShowMarkupShapes(host);
        }

        static void ParseDoc(IHost host)
        {
            var d = host.Services.GetRequiredService<IPdfDrawing>();
            var docs = d.ParseDoc();
        }

        static void ShowMarkupShapes(IHost host)
        {
            var cfg = host.Services.GetRequiredService<IOptions<AppConfig>>().Value;
            var imgFile = cfg.ImageFiles[0];
            var pdfFile = cfg.DwgConfigs.First().DwgFiles[0];
            MainUtility.ShowMarkups(pdfFile, imgFile);
        }
    }
}
