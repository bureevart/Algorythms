using System;
using System.Collections.Generic;
using System.Linq;

namespace Algorythms
{
    public static class HexFov
    {
        // ---------- PUBLIC API ----------

        /// <summary>
        /// Поле зрения (shadowcasting по углам) для pointy-top hex.
        /// Высокие клетки создают теневые сектора. Клетка видима,
        /// если её угловой сектор НЕ полностью покрыт тенью.
        /// </summary>
        public static List<HexCell> ComputeFov(List<HexCell> cells, HexCell source, int radius, double hexSize = 1.0)
        {
            var visible = new List<HexCell> { source };

            // Все клетки в радиусе (в шагах гекса)
            var inRadius = cells
                .Where(c => c != source && HexDistance(source, c) <= radius)
                // сортировка по расстоянию от ближних к дальним
                .OrderBy(c => HexDistance(source, c))
                .ThenBy(c => CenterAngle(source, c, hexSize))
                .ToList();

            // Список теней = [from,to] в радианах, from<=to (в «развёрнутых» углах)
            var shadows = new List<(double from, double to)>();

            foreach (var cell in inRadius)
            {
                // Сектор клетки (углы до её вершин), уже «развёрнут относительно центра»
                var (from, to) = HexAngularBounds(source, cell, hexSize);

                // Если сектор полностью закрыт имеющимися тенями — невидима
                if (IsCoveredByShadows(shadows, from, to))
                    continue;

                // Иначе — видима
                visible.Add(cell);

                // Если клетка выше источника — добавляет тень
                if (cell.Height > source.Height)
                {
                    AddShadow(shadows, from, to);
                }
            }

            return visible;
        }

        /// <summary>Hex distance в axial координатах.</summary>
        public static int HexDistance(HexCell a, HexCell b)
        {
            int ax = a.Q, az = a.R, ay = -ax - az;
            int bx = b.Q, bz = b.R, by = -bx - bz;
            return Math.Max(Math.Abs(ax - bx),
                   Math.Max(Math.Abs(ay - by), Math.Abs(az - bz)));
        }

        // ---------- INTERNAL GEOMETRY ----------

        // Перевод центра axial (q,r) в пиксели для pointy-top
        // (RedBlobGames: x = size*sqrt(3)*(q + r/2), y = size*3/2*r)
        private static (double x, double y) AxialToPixel(int q, int r, double size)
        {
            double x = size * Math.Sqrt(3) * (q + r / 2.0);
            double y = size * 1.5 * r;
            return (x, y);
        }

        // Угол до центра клетки (в радианах) — в пиксельных координатах
        private static double CenterAngle(HexCell src, HexCell cell, double size)
        {
            var (sx, sy) = AxialToPixel(src.Q, src.R, size);
            var (cx, cy) = AxialToPixel(cell.Q, cell.R, size);
            return Math.Atan2(cy - sy, cx - sx);
        }

        // 6 смещений к вершинам гекса для pointy-top (единичный hex в пикселях)
        private static readonly (double dx, double dy)[] CornerOffsetsUnit = Enumerable
            .Range(0, 6)
            .Select(i =>
            {
                // pointy-top: угол вершины = 60*i - 30 градусов
                double ang = (Math.PI / 180.0) * (60 * i - 30);
                return (Math.Cos(ang), Math.Sin(ang));
            })
            .ToArray();

        // Возвращает угловой сектор [from,to] (радианы), покрывающий ВСЕ 6 вершин гекса.
        // Углы "развёрнуты" относительно угла на центр, чтобы избежать разрыва на +/-pi.
        private static (double from, double to) HexAngularBounds(HexCell src, HexCell cell, double size)
        {
            var (sx, sy) = AxialToPixel(src.Q, src.R, size);
            var (cx, cy) = AxialToPixel(cell.Q, cell.R, size);

            // угол на центр — опорный
            double baseAng = Math.Atan2(cy - sy, cx - sx);

            // считаем углы до 6 вершин (центр + offset*size)
            var anglesShifted = new double[6];
            for (int i = 0; i < 6; i++)
            {
                var (ox, oy) = CornerOffsetsUnit[i];
                double vx = (cx - sx) + ox * size;
                double vy = (cy - sy) + oy * size;

                double ang = Math.Atan2(vy, vx);
                // «Разворачиваем» вокруг baseAng → углы близки к 0
                anglesShifted[i] = UnwrapAngle(ang - baseAng);
            }

            Array.Sort(anglesShifted);
            double fromShift = anglesShifted.First();
            double toShift = anglesShifted.Last();

            // возвращаем в абсолютную систему
            double from = WrapAngle(baseAng + fromShift);
            double to   = WrapAngle(baseAng + toShift);

            // Приводим к форме «линейного интервала»: переносим при необходимости,
            // чтобы from <= to в одной «ветке».
            if (to < from) to += 2 * Math.PI;

            return (from, to);
        }

        // ---------- SHADOW SET ----------

        // Добавить тень [from,to] и слить перекрытия
        private static void AddShadow(List<(double from, double to)> shadows, double from, double to)
        {
            // нормализуем так, чтобы from<=to
            if (to < from) (from, to) = (to, from);

            // сливаем с существующими
            shadows.Add((from, to));
            shadows.Sort((a, b) => a.from.CompareTo(b.from));

            var merged = new List<(double from, double to)>();
            foreach (var s in shadows)
            {
                if (merged.Count == 0) { merged.Add(s); continue; }

                var last = merged[^1];
                // есть ли пересечение/стык
                if (s.from <= last.to)
                    merged[^1] = (last.from, Math.Max(last.to, s.to));
                else
                    merged.Add(s);
            }

            // дополнительно: если интервал уходит за 2π — перенесём и сольём
            // (редко требуется, но на больших сценах полезно)
            if (merged.Count >= 2 && merged[0].from >= Math.PI && merged[^1].to > 2 * Math.PI)
            {
                var first = merged[0];
                var last  = merged[^1];
                if (last.to - 2 * Math.PI >= first.from)
                {
                    // слить края с переносом
                    merged[0] = (first.from, Math.Max(first.to, last.to - 2 * Math.PI));
                    merged.RemoveAt(merged.Count - 1);
                }
            }

            shadows.Clear();
            shadows.AddRange(merged);
        }

        // Полностью ли [from, to] покрыт текущими тенями
        private static bool IsCoveredByShadows(List<(double from, double to)> shadows, double from, double to)
        {
            if (to < from) (from, to) = (to, from);

            // Проверяем покрытие любым интервалом
            foreach (var s in shadows)
            {
                // допускаем небольшой эпсилон на числовые ошибки
                const double eps = 1e-9;
                if (from + eps >= s.from && to - eps <= s.to)
                    return true;
            }
            return false;
        }

        // ---------- ANGLE HELPERS ----------

        // Привести угол к [-pi, pi)
        private static double WrapAngle(double a)
        {
            while (a >= Math.PI)  a -= 2 * Math.PI;
            while (a < -Math.PI)  a += 2 * Math.PI;
            return a;
        }

        // Развернуть угол около 0 (для сравнений вокруг baseAng)
        private static double UnwrapAngle(double a)
        {
            a = WrapAngle(a);
            // Убедимся, что значения распределены «компактно» около 0
            return a;
        }
    }
}
