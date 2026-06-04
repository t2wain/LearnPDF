namespace PdfParserLib.Entity
{
    public class PdfDocData
    {
        public string? FileName { get; set; }
        public string? Author { get; set; }
        public string? CreationDate { get; set; }
        public string? Creator { get; set; }
        public string? Keywords { get; set; }
        public string? ModifiedDate { get; set; }
        public string? Producer { get; set; }
        public string? Subject { get; set; }
        public string? Title { get; set; }
        public bool IsEncrypted { get; set; }
        public int NumberOfPages { get; set; }
        public double Version { get; set; }
        public List<PdfPageData> Pages { get; set; } = [];
    }
}
