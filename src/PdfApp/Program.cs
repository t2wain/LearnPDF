using Microsoft.Extensions.DependencyInjection;
using PdfParserLib.Dwg;

namespace PdfApp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var host = AppHostExtensions.GetHost(args);
            var d = host.Services.GetRequiredService<PdfDrawing>();
            var docs = d.ParseDoc();
        }
    }
}
