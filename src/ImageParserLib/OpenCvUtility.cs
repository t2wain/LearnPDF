using OpenCvSharp;

namespace ImageParserLib
{
    public static class OpenCvUtility
    {
        public static void Detect(string imagePath, string templatePath)
        {
            // Load source image
            Mat source = Cv2.ImRead(imagePath, ImreadModes.Color);

            // Load template (ANSI transformer symbol image)
            Mat template = Cv2.ImRead(templatePath, ImreadModes.Grayscale);

            // Convert source to grayscale
            Mat gray = new Mat();
            Cv2.CvtColor(source, gray, ColorConversionCodes.BGR2GRAY);

            // Template matching
            Mat result = new Mat();
            Cv2.MatchTemplate(gray, template, result, TemplateMatchModes.CCoeffNormed);

            // Threshold for detection
            double threshold = 0.7;

            while (true)
            {
                Cv2.MinMaxLoc(result, out _, out double maxVal, out _, out Point maxLoc);

                if (maxVal < threshold)
                    break;

                // Draw rectangle where symbol is detected
                var rect = new Rect(maxLoc.X, maxLoc.Y, template.Width, template.Height);
                Cv2.Rectangle(source, rect, Scalar.Red, 2);

                // Suppress the detected area (avoid duplicates)
                Cv2.FloodFill(result, maxLoc, new Scalar(0));
            }

            // Show result
            Cv2.ImShow("Detected Transformers", source);
            Cv2.WaitKey();
            Cv2.DestroyAllWindows();
        }

        public static void Detect2(string imagePath)
        {
            Mat image = Cv2.ImRead(imagePath);
            Mat gray = new Mat();

            Cv2.CvtColor(image, gray, ColorConversionCodes.BGR2GRAY);

            // Threshold
            Mat binary = new Mat();
            Cv2.Threshold(gray, binary, 150, 255, ThresholdTypes.BinaryInv);

            // Edge detect
            Mat edges = new Mat();
            Cv2.Canny(binary, edges, 50, 150);

            // Find contours
            Cv2.FindContours(edges, out Point[][] contours, out _, RetrievalModes.Tree, ContourApproximationModes.ApproxSimple);

            foreach (var contour in contours)
            {
                var rect = Cv2.BoundingRect(contour);

                // Filter by size (tune these values)
                if (rect.Width > 20 && rect.Height > 20)
                {
                    double aspect = (double)rect.Width / rect.Height;

                    // Transformer symbols often near square-ish
                    if (aspect > 0.5 && aspect < 2.0)
                    {
                        Cv2.Rectangle(image, rect, Scalar.Blue, 2);
                    }
                }
            }

            Cv2.ImShow("Detected Shapes", image);
            Cv2.WaitKey();
        }

        public static void Detect3(string imagePath, string templatePath)
        {
            Mat img = Cv2.ImRead(imagePath, ImreadModes.Grayscale);
            Mat tpl = Cv2.ImRead(templatePath, ImreadModes.Grayscale);

            var orb = ORB.Create();

            Mat des1 = new Mat();
            Mat des2 = new Mat();

            orb.DetectAndCompute(img, null, out KeyPoint[] kp1, des1);
            orb.DetectAndCompute(tpl, null, out KeyPoint[] kp2, des2);

            var bf = new BFMatcher(NormTypes.Hamming);
            var matches = bf.Match(des1, des2);

            if (matches.Length > 20)
            {
                Console.WriteLine("Transformer symbol detected");
            }
        }

        public static void DetectCircles(string imagePath)
        {
            // Load image (grayscale is required)
            Mat src = Cv2.ImRead(imagePath, ImreadModes.Grayscale);

            /*
             * Single-line diagrams often have:
             *
             *  - Thin lines
             *  - Noise
             *  - Imperfect circles
             *
             * To improve results:
             * 
             *  1. Use edge detection first
             *  2. Try adaptive thresholding
             *  3. Morphological cleanup
             *  
             */

            // Reduce noise (very important for good results)
            Mat blurred = new Mat();
            Cv2.GaussianBlur(src, blurred, new Size(9, 9), 2);

            /*
             * Detect circles
             * 
             * dp : Resolution scaling : 1–2 (start with 1.2)
             * minDist : Min - distance between circles : Increase to avoid duplicates
             * param1 : Edge detection threshold : Usually 100–200
             * param2 : Detection sensitivity : Lower → more circles (more false positives)
             * minRadius / maxRadius : Size filter : Set based on symbol size
             * 
             */
            CircleSegment[] circles = Cv2.HoughCircles(
                blurred,
                HoughModes.Gradient,
                dp: 1.2,              // Inverse ratio of resolution
                minDist: 50,          // Minimum distance between circles
                param1: 100,          // Canny edge upper threshold
                param2: 30,           // Accumulator threshold (lower = more detections)
                minRadius: 10,        // Minimum radius
                maxRadius: 100        // Maximum radius
            );

            // Convert image to color for visualization
            Mat output = new Mat();
            Cv2.CvtColor(src, output, ColorConversionCodes.GRAY2BGR);

            foreach (var circle in circles)
            {
                // Draw circle
                Cv2.Circle(output, (int)circle.Center.X, (int)circle.Center.Y,
                           (int)circle.Radius, Scalar.Red, 2);

                // Draw center
                Cv2.Circle(output, (int)circle.Center.X, (int)circle.Center.Y,
                           2, Scalar.Blue, 3);
            }

            Cv2.ImShow("Detected Circles", output);
            Cv2.WaitKey();
        }

        public static void DetectRectangles(string imagePath)
        {
            // Load image
            Mat src = Cv2.ImRead(imagePath);

            // Convert to grayscale
            Mat gray = new Mat();
            Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);

            //Binary threshold(better for drawings)
            //Cv2.Threshold(gray, gray, 150, 255, ThresholdTypes.BinaryInv);

            //Morphological closing (fix broken rectangles)
            //Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(3, 3));
            //Cv2.MorphologyEx(gray, gray, MorphTypes.Close, kernel);

            // Blur (reduce noise)
            Cv2.GaussianBlur(gray, gray, new Size(5, 5), 0);

            // Edge detection
            Mat edges = new Mat();
            Cv2.Canny(gray, edges, 50, 150);

            // Find contours
            Cv2.FindContours(edges, out Point[][] contours, out _,
            RetrievalModes.List, ContourApproximationModes.ApproxSimple);

            foreach (var contour in contours)
            {
                // Approximate polygon
                double epsilon = 0.02 * Cv2.ArcLength(contour, true);
                Point[] approx = Cv2.ApproxPolyDP(contour, epsilon, true);

                // ✅ Rectangle condition
                if (approx.Length == 4 && Cv2.IsContourConvex(approx))
                {
                    double area = Cv2.ContourArea(approx);

                    // Filter small noise
                    if (area > 500)
                    {
                        // Draw rectangle
                        Cv2.Polylines(src, new[] { approx }, true, Scalar.Red, 2);
                    }
                }
            }

            Cv2.ImShow("Rectangles", src);
            Cv2.WaitKey();
        }

        public static bool IsRectangle(Point[] pts)
        {
            double angle(Point a, Point b, Point c)
            {
                var ab = new Point(b.X - a.X, b.Y - a.Y);
                var cb = new Point(b.X - c.X, b.Y - c.Y);

                double dot = ab.X * cb.X + ab.Y * cb.Y;
                double magA = Math.Sqrt(ab.X * ab.X + ab.Y * ab.Y);
                double magB = Math.Sqrt(cb.X * cb.X + cb.Y * cb.Y);

                return Math.Acos(dot / (magA * magB)) * (180.0 / Math.PI);
            }

            for (int i = 0; i < 4; i++)
            {
                double ang = angle(pts[i], pts[(i + 1) % 4], pts[(i + 2) % 4]);
                if (Math.Abs(ang - 90) > 15)
                    return false;
            }

            return true;
        }
    }
}
