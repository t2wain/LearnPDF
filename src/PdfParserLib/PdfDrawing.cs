using Microsoft.Extensions.Options;

namespace PdfParserLib
{
    public class PdfDrawing
    {
        AppConfig _appConfig;

        public PdfDrawing(IOptions<AppConfig> appConfig)
        {
            this._appConfig = appConfig.Value;
        }


    }
}
