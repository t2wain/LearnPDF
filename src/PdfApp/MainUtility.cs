using ImageParserLib;
using OpenCvSharp;
using PdfParserLib;

namespace PdfApp
{
    public static class MainUtility
    {
        public static void ShowMarkups(string pdfFilePath, string imagePath)
        {
            Mat output = MarkupShapes(pdfFilePath, imagePath);
            Cv2.ImShow("Detected Shapes", output);
            Cv2.WaitKey();
        }

        public static Mat MarkupShapes(string pdfFilePath, string imagePath)
        {
            var paths = PdfUtility.GetPdfPaths(pdfFilePath);
            var pathCommands = PdfPathUtility.GetCommands(paths);
            var circuleShapes = PdfPathUtility.GetCircles(pathCommands, 2, 25);
            var rectShapes = PdfPathUtility.GetRectangles(pathCommands, 10, 100);

            Mat output = Cv2.ImRead(imagePath).Clone();
            MarkupCircles(circuleShapes, output);
            MarkupRectangles(rectShapes, output);

            return output;
        }

        public static void MarkupCircles(IEnumerable<PdfPathUtility.PathCommands> circleShapes, Mat image)
        {
            var h = image.Size().Height;

            var circles = circleShapes
                .Select(s =>
                {
                    var (centerX, centerY, radius) = s.CalcCircle();
                    centerY = h - centerY; // Y-axis is inverted between PDF and OpenCV
                    var center = new Point2f(centerX, centerY);
                    return new CircleSegment(center, radius);
                })
                .ToList();

            OpenCvUtility.DrawCircle(image, circles);
        }

        public static void MarkupRectangles(IEnumerable<PdfPathUtility.PathCommands> rectangleShapes, Mat image)
        {
            var h = image.Size().Height;

            IEnumerable<Rect> rects = rectangleShapes
                .Select(s =>
                {
                    Rect rect = new Rect((int)s.Bound.Left, (int)s.Bound.Top, (int)s.Bound.Width, (int)s.Bound.Height);
                    rect.Y = h - rect.Y; // Y-axis is inverted between PDF and OpenCV
                    return rect;
                })
                .ToList();

            OpenCvUtility.DrawingBoundingBox(image, rects);
        }

    }
}
