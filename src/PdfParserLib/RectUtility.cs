using PdfParserLib.Config;
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


        public static bool Intersects(PdfRectangle a, PdfRectangle b)
        {
            return a.Left < b.Right &&
                   a.Right > b.Left &&
                   a.Bottom < b.Top &&
                   a.Top > b.Bottom;
        }

        public static PdfRectangle? GetIntersection(PdfRectangle a, PdfRectangle b)
        {
            double left = Math.Max(a.Left, b.Left);
            double right = Math.Min(a.Right, b.Right);
            double bottom = Math.Max(a.Bottom, b.Bottom);
            double top = Math.Min(a.Top, b.Top);

            if (left < right && bottom < top)
            {
                return new PdfRectangle(left, bottom, right, top);
            }

            return null; // no overlap
        }

        public static PdfRectangle GetEnclosingRectangle(IEnumerable<PdfRectangle> rectangles)
        {
            if (rectangles == null || !rectangles.Any())
                throw new ArgumentException("Rectangle collection is empty.");

            double minLeft = double.MaxValue;
            double maxRight = double.MinValue;
            double minBottom = double.MaxValue;
            double maxTop = double.MinValue;

            foreach (var rect in rectangles)
            {
                if (rect.Left < minLeft) minLeft = rect.Left;
                if (rect.Right > maxRight) maxRight = rect.Right;
                if (rect.Bottom < minBottom) minBottom = rect.Bottom;
                if (rect.Top > maxTop) maxTop = rect.Top;
            }

            return new PdfRectangle(minLeft, minBottom, maxRight, maxTop);
        }

        public static PdfRectangle BuildRectangle(DwgRegion region) =>
            new(region.X, region.Y, region.X + region.Width, region.Y + region.Height);
    }
}
