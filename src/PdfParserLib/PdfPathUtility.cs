using System.Text;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Graphics;
using SP = UglyToad.PdfPig.Core.PdfSubpath;

namespace PdfParserLib
{
    public static class PdfPathUtility
    {
        public record PathCommands(PdfPath Path, SP SubPath, IReadOnlyList<SP.IPathCommand> Commands, PdfRectangle? Bound);

        public static List<PathCommands> GetCommands(IEnumerable<PdfPath> paths) =>
                paths.SelectMany(p => p.Select(sp => new PathCommands(p, sp, sp.Commands, sp.GetBoundingRectangle()))).ToList();

        public static void GetShapes(IEnumerable<PdfPath> paths)
        {
            var pc = GetCommands(paths);

            var circles = pc
                .Where(i => i.SubPath.IsClosed())
                .Where(i => i.Commands.Count() == 5)
                .Where(i => i.Commands.All(cmd => cmd is SP.CubicBezierCurve || cmd is SP.Move))
                .Where(i => i.Bound?.Width > 5 && i.Bound?.Width < 50)
                .ToList();

            var rectangles = pc
                .Where(i => i.SubPath.IsClosed())
                .Where(i => i.Commands.Count() == 5)
                .Where(i => i.Commands.All(cmd => cmd is SP.Line || cmd is SP.Move))
                .Where(i => i.Bound?.Width > 5 && i.Bound?.Width < 200)
                .ToList();
        }


        public static List<string> GetSvgFromPathCommand(
            IEnumerable<SP.IPathCommand> segments, double? height = null)
        {
            StringBuilder b = new();
            var lst = new List<string>();
            foreach (SP.IPathCommand cmd in segments)
            {
                cmd.WriteSvg(b, height ?? 10);
                var svg = b.ToString();
                lst.Add(svg);
                b.Clear();
            }
            return lst;
        }

        public static List<PdfPoint> GetPdfPointFromPathCommand(IEnumerable<SP.IPathCommand> segments)
        {
            List<PdfPoint> lstPoint = new();
            foreach (SP.IPathCommand cmd in segments)
            {
                switch (cmd)
                {
                    case SP.Line line:
                        lstPoint.Add(line.From);
                        lstPoint.Add(line.To);
                        break;
                    case SP.Move move:
                        lstPoint.Add(move.Location);
                        break;
                    case SP.Close close:
                        break;
                    case SP.CubicBezierCurve curve:
                        lstPoint.Add(curve.StartPoint);
                        lstPoint.Add(curve.EndPoint);
                        lstPoint.Add(curve.FirstControlPoint);
                        lstPoint.Add(curve.SecondControlPoint);
                        break;
                    case SP.QuadraticBezierCurve curve2:
                        break;
                    case SP.BezierCurve curve3:
                        break;
                }
            }
            return lstPoint;
        }


        public static bool IsCircle(
            IReadOnlyList<PdfPoint> points,
            double radiusTolerancePercent = 0.05)
        {
            if (points.Count < 8)
            {
                return false;
            }

            double minX = points.Min(p => p.X);
            double maxX = points.Max(p => p.X);
            double minY = points.Min(p => p.Y);
            double maxY = points.Max(p => p.Y);

            double width = maxX - minX;
            double height = maxY - minY;

            // Circle should have nearly equal width and height
            double aspectRatioError = Math.Abs(width - height) / Math.Max(width, height);

            if (aspectRatioError > 0.05)
            {
                return false;
            }

            double centerX = (minX + maxX) / 2.0;
            double centerY = (minY + maxY) / 2.0;

            var radii = points
                .Select(p =>
                {
                    double dx = p.X - centerX;
                    double dy = p.Y - centerY;
                    return Math.Sqrt(dx * dx + dy * dy);
                })
                .ToList();

            double averageRadius = radii.Average();

            double stdDev = Math.Sqrt(
                radii.Sum(r => Math.Pow(r - averageRadius, 2))
                / radii.Count);

            double variation = stdDev / averageRadius;

            return variation < radiusTolerancePercent;
        }

    }
}
