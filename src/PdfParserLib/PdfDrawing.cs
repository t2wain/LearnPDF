using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

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
