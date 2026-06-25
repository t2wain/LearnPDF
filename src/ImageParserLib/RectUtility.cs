using System.Drawing;

namespace ImageParserLib
{
    public static class RectUtility
    {
        public static RectangleF ToRectangle(Tesseract.Rect rect) =>
            new(rect.X1, rect.Y1, rect.Width, rect.Height);
    }
}
