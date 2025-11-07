using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Algorythms
{
    public static class HexFov
    {
        // точность геометрии
        private const double EPS = 1e-6;

        // Канонический pointy-top шестиугольник (radius = 1), по часовой
        private static readonly (double dx, double dy)[] UnitHexCorners = Enumerable
            .Range(0, 6)
            .Select(i =>
            {
                double ang = Math.PI / 180.0 * (60 * i - 30);
                return (Math.Cos(ang), Math.Sin(ang));
            })
            .ToArray();

        public static List<HexCell> ComputeFov(
            List<HexCell> cells,
            HexCell source,
            int radius,
            double hexSize = 1.0)
        {

            var visible = new HashSet<HexCell> { source };

            var inRadius = cells
                .Where(c => c != source && HexDistance(source, c) <= radius)
                .OrderBy(c => HexDistance(source, c))
                .ThenBy(c => CenterAngle(source, c, hexSize))
                .ToList();

            foreach (var target in inRadius)
            {
                if (HasLineOfSight(source, target, cells, hexSize))
                {
                    visible.Add(target);
                }
            }
            return visible.ToList();
        }

        private static bool HasLineOfSight(
            HexCell src,
            HexCell tgt,
            List<HexCell> cells,
            double size)
        {
            var (sx, sy) = ToPixel(src.Q, src.R, size);
            var (tx, ty) = ToPixel(tgt.Q, tgt.R, size);

            foreach (var other in cells)
            {
                if (other == src || other == tgt) continue;

                // блокирует только тот, кто выше источника
                if (other.Height <= src.Height) continue;

                // проверяем только гексы между src и tgt (по шагам)
                if (HexDistance(src, other) >= HexDistance(src, tgt))
                    continue;

                if (LineIntersectsHex(sx, sy, tx, ty, other.Q, other.R, size))
                    return false;
            }

            return true;
        }

        private static bool LineIntersectsHex(
            double x1, double y1,
            double x2, double y2,
            int hq, int hr,
            double size)
        {
            var (cx, cy) = ToPixel(hq, hr, size);

            // вершины гекса
            var v = new (double x, double y)[6];
            for (int i = 0; i < 6; i++)
                v[i] = (cx + UnitHexCorners[i].dx * size,
                        cy + UnitHexCorners[i].dy * size);

            // быстрый bbox-отсев с запасом EPS
            double segMinX = Math.Min(x1, x2) - EPS, segMaxX = Math.Max(x1, x2) + EPS;
            double segMinY = Math.Min(y1, y2) - EPS, segMaxY = Math.Max(y1, y2) + EPS;

            double hexMinX = double.PositiveInfinity, hexMaxX = double.NegativeInfinity;
            double hexMinY = double.PositiveInfinity, hexMaxY = double.NegativeInfinity;
            for (int i = 0; i < 6; i++)
            {
                hexMinX = Math.Min(hexMinX, v[i].x);
                hexMaxX = Math.Max(hexMaxX, v[i].x);
                hexMinY = Math.Min(hexMinY, v[i].y);
                hexMaxY = Math.Max(hexMaxY, v[i].y);
            }
            if (segMaxX < hexMinX || segMinX > hexMaxX || segMaxY < hexMinY || segMinY > hexMaxY)
                return false;

            // пересечение с любым ребром (с учётом касаний/коллинеарности)
            for (int i = 0; i < 6; i++)
            {
                var (x3, y3) = v[i];
                var (x4, y4) = v[(i + 1) % 6];

                if (SegmentsIntersectRobust(x1, y1, x2, y2, x3, y3, x4, y4))
                    return true;
            }

            return false;
        }

        // Робастная проверка: пересекаются ли отрезки с учётом EPS,
        // включая касание вершины/рёбра и коллинеарное наложение.
        private static bool SegmentsIntersectRobust(
            double x1, double y1, double x2, double y2,
            double x3, double y3, double x4, double y4)
        {
            static int Orient(double ax, double ay, double bx, double by, double cx, double cy)
            {
                double v = (bx - ax) * (cy - ay) - (by - ay) * (cx - ax);
                if (v > EPS)  return 1;
                if (v < -EPS) return -1;
                return 0; // почти коллинеарны
            }

            static bool OnSeg(double ax, double ay, double bx, double by, double cx, double cy)
            {
                // c лежит на отрезке ab (с запасом EPS)
                if (Math.Abs((bx - ax) * (cy - ay) - (by - ay) * (cx - ax)) > EPS) return false;
                double minx = Math.Min(ax, bx) - EPS, maxx = Math.Max(ax, bx) + EPS;
                double miny = Math.Min(ay, by) - EPS, maxy = Math.Max(ay, by) + EPS;
                return (cx >= minx && cx <= maxx && cy >= miny && cy <= maxy);
            }

            int o1 = Orient(x1, y1, x2, y2, x3, y3);
            int o2 = Orient(x1, y1, x2, y2, x4, y4);
            int o3 = Orient(x3, y3, x4, y4, x1, y1);
            int o4 = Orient(x3, y3, x4, y4, x2, y2);

            // общий случай
            if (o1 != o2 && o3 != o4) return true;

            // частные случаи: касания/коллинеарность
            if (o1 == 0 && OnSeg(x1, y1, x2, y2, x3, y3)) return true;
            if (o2 == 0 && OnSeg(x1, y1, x2, y2, x4, y4)) return true;
            if (o3 == 0 && OnSeg(x3, y3, x4, y4, x1, y1)) return true;
            if (o4 == 0 && OnSeg(x3, y3, x4, y4, x2, y2)) return true;

            return false;
        }

        private static (double x, double y) ToPixel(int q, int r, double size)
        {
            double x = size * Math.Sqrt(3) * (q + r / 2.0);
            double y = size * 1.5 * r;
            return (x, y);
        }

        private static double CenterAngle(HexCell src, HexCell cell, double size)
        {
            var (sx, sy) = ToPixel(src.Q, src.R, size);
            var (cx, cy) = ToPixel(cell.Q, cell.R, size);
            return Math.Atan2(cy - sy, cx - sx);
        }

        public static int HexDistance(HexCell a, HexCell b)
        {
            int ax = a.Q, az = a.R, ay = -ax - az;
            int bx = b.Q, bz = b.R, by = -bx - bz;
            return Math.Max(Math.Abs(ax - bx),
                   Math.Max(Math.Abs(ay - by), Math.Abs(az - bz)));
        }
    }
}
