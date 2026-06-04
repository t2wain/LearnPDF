using System.Text.Json.Serialization;
using UglyToad.PdfPig.Content;

namespace PdfParserLib.Entity
{
    public class PdfPageData
    {
        public int PageNumber { get; set; }
        public PageSize PageSize { get; set; }
        public double Height { get; set; }
        public double Width { get; set; }
        public int RotationDeg { get; set; }
        public bool IsFlip { get; set; }
        public int NumberOfImages { get; set; }
        public int NumberOfPaths { get; set; }
        public string? Text { get; set; }
        public double BottomLeftX { get; set; }
        public double BottomLeftY { get; set; }
        public double TopRightX { get; set; }
        public double TopRightY { get; set; }
        public List<PdfPathData> Paths { get; set; } = [];
        public List<PdfAnnoData> Annotations { get; set; } = [];
        public List<PdfWordData> Words { get; set; } = [];
        [JsonIgnore]
        public List<PdfImageData> Images { get; set; } = [];
    }
}
