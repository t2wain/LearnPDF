namespace PdfParserLib.Config
{
    public class AppConfig
    {
        public string? ImageFolder { get; set; }
        public IList<string> ImageFiles { get; set; } = [];
        public string? OcrTrainedDataFolderPath { get; set; }
        public IList<DwgConfig> DwgConfigs { get; set; } = [];
    }

}
