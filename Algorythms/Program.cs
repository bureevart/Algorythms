using Algorythms;

class Program
{
    static void Main()
    {
        //Ex1();
        Ex2();
    }

    public static void Ex1()
    {
        // Пример из задания: [{1:1, 2:5}, {0:1, 5:2}, {0:5}, {}, {1:2}]
        var cells = new List<HexCell>
        {
            new() { Index = 0, Q = 0, R = 0, Connections = new() {{1,1},{2,5}} },
            new() { Index = 1, Q = 1, R = 0, Connections = new() {{0,1},{5,2}} },
            new() { Index = 2, Q = 0, R = 1, Connections = new() {{0,5}} },
            new() { Index = 3, Q = 1, R = 1, Connections = new() { } },
            new() { Index = 4, Q = 2, R = 1, Connections = new() {{1,2}} },
            new() { Index = 5, Q = 2, R = 0, Connections = new() { } },
        };

        int start = 0;
        int goal  = 5;

        var result = PathFinding.FindPath(cells, start, goal, useAStar: true);

        if (result.Path == null)
        {
            Console.WriteLine("Путь не найден.");
            return;
        }

        Console.WriteLine("Путь: " + string.Join(" -> ", result.Path));
        Console.WriteLine("Стоимость: " + result.TotalCost);
    }

    public static void Ex2()
    {
        var map = new HexMapVisualizer(radius: 10, viewRadius: 10);

        while (true)
        {
            map.Draw();
            Console.WriteLine("Commands:");
            Console.WriteLine(" set q r h    -> change height of cell (q,r)");
            Console.WriteLine(" src q r      -> move source");
            Console.WriteLine(" view n       -> change view radius");
            Console.WriteLine(" draw         -> redraw");
            Console.WriteLine(" exit         -> quit");
            Console.Write("> ");

            var cmd = Console.ReadLine();
            if (cmd == null) continue;

            var parts = cmd.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) continue;

            if (parts[0] == "exit") break;
            if (parts[0] == "draw") continue;

            if (parts[0] == "view" && parts.Length == 2 &&
                int.TryParse(parts[1], out int vr))
            {
                map.SetViewRadius(vr);
                continue;
            }

            if (parts[0] == "set" && parts.Length == 4 &&
                int.TryParse(parts[1], out int q) &&
                int.TryParse(parts[2], out int r) &&
                int.TryParse(parts[3], out int h))
            {
                if (!map.SetHeight(q, r, h))
                    Console.WriteLine("Cell not found");
                continue;
            }

            if (parts[0] == "src" && parts.Length == 3 &&
                int.TryParse(parts[1], out q) &&
                int.TryParse(parts[2], out r))
            {
                if (!map.MoveSource(q, r))
                    Console.WriteLine("Cell not found");
                continue;
            }
        }
    }
}