using System.Drawing;

namespace PdfParserLib.Entity
{
    public class PdfPathData
    {
        public double? BottomLeftX { get; set; }
        public double? BottomLeftY { get; set; }
        public double? TopRightX { get; set; }
        public double? TopRightY { get; set; }
        public double? Rotation { get; set; }
        public List<PdfSubPathData> SubPaths { get; set; } = [];
        public RectangleF? Bound { get; set; }
    }
}
