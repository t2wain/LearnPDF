namespace PdfParserLib
{
    public class AppConfig
    {
        public IList<string> PDFFiles { get; set; } = [];
        public string ImageFolder { get; set; } = "";
        public IList<string> ImageFiles { get; set; } = [];
        public string OcrTrainedDataFolderPath { get; set; } = "";
    }
}
