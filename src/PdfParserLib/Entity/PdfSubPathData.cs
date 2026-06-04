namespace PdfParserLib.Entity
{
    public class PdfSubPathData
    {
        public bool IsClosed { get; set; }
        public bool IsClockwise { get; set; }
        public bool IsCounterClockwise { get; set; }
        public bool IsDrawnAsRectangle { get; set; }
        public double? BottomLeftX { get; set; }
        public double? BottomLeftY { get; set; }
        public double? TopRightX { get; set; }
        public double? TopRightY { get; set; }
        public List<string> SVGs { get; set; } = [];
    }
}
