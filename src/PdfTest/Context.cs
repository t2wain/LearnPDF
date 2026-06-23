using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using PdfParserLib;
using PdfParserLib.Config;

namespace PdfTest
{
    public class Context : IDisposable
    {
        IHost _host;
        AppConfig _cfg;

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

        public PdfDrawing GetDrawing() => _host.Services.GetRequiredService<PdfDrawing>();

        public PdfDrawing2 GetDrawing2() => _host.Services.GetRequiredService<PdfDrawing2>();

        public AppConfig Config => _cfg;

        public IList<string> FileNames => _cfg.PDFFiles;

        public string ImageFolderPath => _cfg.ImageFolder;
    }
}
