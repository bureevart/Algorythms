// Program.cs
// .NET 6+ (из-за PriorityQueue). Если у тебя .NET 5 и ниже — скажи, дам минимальную реализацию очереди.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Algorythms
{
    /// <summary>
    /// Узел гекс-сетки.
    /// Index – порядок в списке, Q,R – axial координаты,
    /// Connections – сосед -> стоимость перехода
    /// </summary>
    public class HexCell
    {
        public int Index { get; set; }
        public int Q { get; set; }
        public int R { get; set; }
        public Dictionary<int, double> Connections { get; set; } = new();
    }

    /// <summary>
    /// Результат: путь и итоговая стоимость.
    /// Если Path = null → пути нет.
    /// </summary>
    public record PathResult(List<int>? Path, double TotalCost);

    public static class PathFinding
    {
        /// <summary>
        /// Поиск пути A* или Дейкстрой.
        /// useAStar = true → A*
        /// useAStar = false → Дейкстра
        /// </summary>
        public static PathResult FindPath(
            List<HexCell> cells,
            int startIndex,
            int goalIndex,
            bool useAStar = true)
        {
            var start = cells[startIndex];
            var goal  = cells[goalIndex];

            // gScore[v] = лучшая известная стоимость добраться до v
            var gScore = cells.ToDictionary(c => c.Index, _ => double.PositiveInfinity);
            gScore[startIndex] = 0;

            // fScore[v] = gScore + эвристика
            var fScore = cells.ToDictionary(c => c.Index, _ => double.PositiveInfinity);
            fScore[startIndex] = useAStar ? HexDistance(start, goal) : 0;

            // cameFrom[v] = откуда мы пришли в v
            var cameFrom = new Dictionary<int, int>();

            var open = new PriorityQueue<int, double>();
            open.Enqueue(startIndex, fScore[startIndex]);

            while (open.TryDequeue(out int current, out _))
            {
                // Если дошли – восстанавливаем путь
                if (current == goalIndex)
                {
                    var path = ReconstructPath(cameFrom, current);
                    var cost = ComputeTotalCost(cells, path);
                    return new PathResult(path, cost);
                }

                var cell = cells[current];

                // Релаксируем все ребра current -> neighbor
                foreach (var (neighbor, weight) in cell.Connections)
                {
                    double tentative = gScore[current] + weight;
                    if (tentative < gScore[neighbor])
                    {
                        cameFrom[neighbor] = current;
                        gScore[neighbor] = tentative;

                        double h = useAStar ? HexDistance(cells[neighbor], goal) : 0;
                        fScore[neighbor] = tentative + h;

                        open.Enqueue(neighbor, fScore[neighbor]);
                    }
                }
            }

            // Путь отсутствует
            return new PathResult(null, double.PositiveInfinity);
        }

        /// <summary>
        /// Эвристика A* для axial-координат (hex distance)
        /// </summary>
        public static int HexDistance(HexCell a, HexCell b)
        {
            int ax = a.Q;
            int az = a.R;
            int ay = -ax - az;

            int bx = b.Q;
            int bz = b.R;
            int by = -bx - bz;

            return Math.Max(Math.Abs(ax - bx),
                   Math.Max(Math.Abs(ay - by), Math.Abs(az - bz)));
        }

        private static List<int> ReconstructPath(Dictionary<int, int> cameFrom, int current)
        {
            var path = new List<int> { current };
            while (cameFrom.TryGetValue(current, out int prev))
            {
                current = prev;
                path.Add(current);
            }
            path.Reverse();
            return path;
        }

        private static double ComputeTotalCost(List<HexCell> cells, List<int>? path)
        {
            if (path == null || path.Count < 2) return double.PositiveInfinity;

            double sum = 0;
            for (int i = 0; i < path.Count - 1; i++)
            {
                sum += cells[path[i]].Connections[path[i + 1]];
            }
            return sum;
        }
    }
}
