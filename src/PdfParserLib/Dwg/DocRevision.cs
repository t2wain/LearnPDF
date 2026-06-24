namespace PdfParserLib.Dwg
{
    public record DocRevision
    {
        public string Issue { get; set; } = "";
        public string Rev { get; set; } = "";
        public string IssueDate { get; set; } = "";
        public string Desc { get; set; } = "";
        public string IssuedBy { get; set; } = "";
        public string CheckedBy { get; set; } = "";
        public string ApprovedBy { get; set; } = "";
    }
}
