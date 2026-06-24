using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using PdfParserLib;
using PdfParserLib.Config;
using PdfParserLib.Dwg;
using ShimSkiaSharp;
using UglyToad.PdfPig.DocumentLayoutAnalysis;

namespace PdfTest
{
    public class Context : IDisposable
    {
        IHost _host;
        AppConfig _cfg;
        IEnumerable<TextBlock> _blocks = null!;

        public void Dispose()
        {
            if (_host != null)
                _host.Dispose();
            _host = null!;
        }

        public Context()
        {
            _host = PdfApp.AppHostExtensions.GetHost([]);
            _cfg = _host.Services.GetRequiredService<IOptions<AppConfig>>().Value;
        }

        public DwgConfig GetDwgConfig() => _cfg.DwgConfigs.First();

        public PdfDrawing GetDrawing() => _host.Services.GetRequiredService<PdfDrawing>();

        public IDocParser GetDocParser() => _host.Services.GetRequiredService<IDocParser>();

        public IEnumerable<TextBlock> GetBlocks(string fileName)
        {
            if (_blocks == null)
                _blocks = PdfTextUtility2.GetTextBlock(fileName);
            return _blocks;
        }

        public AppConfig Config => _cfg;

        public IList<string> FileNames => GetDwgConfig().DwgFiles;

        public string ImageFolderPath => _cfg.ImageFolder;
    }
}
