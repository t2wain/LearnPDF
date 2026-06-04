using UglyToad.PdfPig.Annotations;

namespace PdfParserLib.Entity
{
    public class PdfAnnoData
    {
        public string? Content { get; set; }
        public string? ModifiedDate { get; set; }
        public string? Name { get; set; }
        public AnnotationType Type { get; set; }
        public string? InReplayTo { get; set; }
    }
}
