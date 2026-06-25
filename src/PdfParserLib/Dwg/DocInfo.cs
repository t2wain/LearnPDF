namespace PdfParserLib.Dwg
{
    public record DocInfo
    {
        public string FileName { get; set; } = "";
        public List<string> Title { get; set; } = [];
        public string ProjectNo { get; set; } = "";
        public string DrawingNo { get; set; } = "";
        public string RevNo { get; set; } = "";
        public List<string> Tags { get; set; } = [];
        public List<DocRevision> Revisions { get; set; } = [];
        public int PageNumber { get; set; }
        public string PageSize { get; set; } = "";
        public double Width { get; set; }
        public double Height { get; set; }
    }
}
