using System.Drawing;

namespace PdfParserLib.Entity
{
    public class PdfWordData
    {
        public string? Text { get; set; }
        public string? FontName { get; set; }
        public double BottomLeftX { get; set; }
        public double BottomLeftY { get; set; }
        public double TopRightX { get; set; }
        public double TopRightY { get; set; }
        public string? TextOrientation { get; set; }
        public double Rotation { get; set; }
        public RectangleF? Bound { get; set; }
    }
}
