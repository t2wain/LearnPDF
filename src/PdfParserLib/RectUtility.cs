using SkiaSharp;
using System.Drawing;
using UglyToad.PdfPig.Core;

namespace PdfParserLib
{
    public static class RectUtility
    {
        public static SKRect ToSkRect(PdfRectangle rect, double pageHeight)
        {
            return new SKRect(
                (float)rect.Left,
                (float)(pageHeight - rect.Top),
                (float)rect.Right,
                (float)(pageHeight - rect.Bottom)
            );
        }

        public static RectangleF ToRectangle(SKRect rect)
        {
            float x = rect.Left;
            float y = rect.Top;
            float width = rect.Width;
            float height = rect.Height;

            return new RectangleF(x, y, width, height);
        }


        public static RectangleF ToRectangle(Tesseract.Rect rect) => 
            new(rect.X1, rect.Y1, rect.Width, rect.Height);


        public static RectangleF ToRectangle(PdfRectangle rect, double pageHeight)
        {
            float x = (float)rect.Left;

            float y = (float)(pageHeight - rect.Top); // flip Y

            float width = (float)rect.Width;
            float height = (float)rect.Height;

            return new RectangleF(x, y, width, height);
        }

        public static PointF GetCenter(RectangleF rect) =>
            new(rect.Left + rect.Width / 2.0F, rect.Top + rect.Height / 2.0F);

    }
}
