using System;
using System.Collections.Generic;
using System.Linq;

namespace Algorythms
{
    public class HexMapVisualizer
    {
        public List<HexCell> Cells { get; private set; } = new();
        public HexCell Source { get; private set; }

        public int Radius { get; private set; }
        public int ViewRadius { get; private set; }
        public bool InvertRAxis { get; set; } = true;

        public HexMapVisualizer(int radius,  int viewRadius)
        {
            Radius = radius;
            ViewRadius = viewRadius;
            BuildFullHexMap();
            Source = Cells.First(c => c.Q == 0 && c.R == 0);
        }

        /// <summary>
        /// Заселяет все клетки в гекс-радиусе N вокруг (0,0)
        /// </summary>
        private void BuildFullHexMap()
        {
            Cells.Clear();
            int index = 0;

            for (int q = -Radius; q <= Radius; q++)
            {
                for (int r = -Radius; r <= Radius; r++)
                {
                    int s = -q - r;
                    if (Math.Abs(s) <= Radius)
                    {
                        Cells.Add(new HexCell
                        {
                            Index = index++,
                            Q = q,
                            R = r,
                            Height = 1
                        });
                    }
                }
            }
        }

        /// <summary>
        /// Переместить источник
        /// </summary>
        public bool MoveSource(int q, int r)
        {
            var cell = Cells.FirstOrDefault(c => c.Q == q && c.R == r);
            if (cell == null) return false;
            Source = cell;
            return true;
        }

        /// <summary>
        /// Изменить высоту клетки
        /// </summary>
        public bool SetHeight(int q, int r, int h)
        {
            var cell = Cells.FirstOrDefault(c => c.Q == q && c.R == r);
            if (cell == null) return false;
            cell.Height = h;
            return true;
        }
        
        /// <summary>
        /// Изменить радиус видимости
        /// </summary>
        public void SetViewRadius(int vr)
        {
            if (vr < 0)
            {
                Console.WriteLine("View radius must be >= 0");
                return;
            }
    
            ViewRadius = vr;
        }


        /// <summary>
        /// Нарисовать карту в консоль
        /// </summary>
        public void Draw()
        {
            Console.Clear();
            Console.WriteLine($"Hex map radius = {Radius}, Total cells = {Cells.Count}");
            Console.WriteLine($"Source at ({Source.Q},{Source.R}) H={Source.Height}\n");

            var visible = HexFov.ComputeFov(Cells, Source, ViewRadius);
            var vis = new HashSet<int>(visible.Select(c => c.Index));

            // рендерим строки от r = -Radius до +Radius (экранная ось r вверх)
            for (int r = -Radius; r <= Radius; r++)
            {
                int rowLen = Cells.Count(c => c.R == r);
                int maxLen = Cells.Count(c => c.R == 0); // центральная строка

                int indent = (maxLen - rowLen) * 2; // симметрия как в RedBlob
                Console.Write(new string(' ', indent));

                foreach (var cell in Cells.Where(c => c.R == r).OrderBy(c => c.Q))
                {
                    if (cell == Source)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write($"[S{cell.Height}]");
                    }
                    else if (vis.Contains(cell.Index))
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write($"[H{cell.Height}]");
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.Write($"[■{cell.Height}]");
                    }
                    Console.ResetColor();
                }

                Console.WriteLine();
            }

            Console.WriteLine();
        }
    }
}
