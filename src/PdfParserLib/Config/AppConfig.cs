namespace PdfParserLib.Config
{
    public class AppConfig
    {
        //public string? PDFFolder { get; set; }
        //public IList<string> PDFFiles { get; set; } = [];
        public string? ImageFolder { get; set; }
        public IList<string> ImageFiles { get; set; } = [];
        public string? OcrTrainedDataFolderPath { get; set; }
        //public IList<string> TagPatterns { get; set; } = [];
        public IList<DwgConfig> DwgConfigs { get; set; } = [];
    }

}
