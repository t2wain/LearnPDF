namespace PdfParserLib.Entity
{
    public class PdfImageData
    {
        public int HeightInSamples { get; set; }
        public int WidthInSamples { get; set; }
        public bool IsInlineImage { get; set; }
        public int BitsPerComponent { get; set; }
        public double BottomLeftX { get; set; }
        public double BottomLeftY { get; set; }
        public double TopRightX { get; set; }
        public double TopRightY { get; set; }
        public byte[]? PNG { get; set; }

    }
}
