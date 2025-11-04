using Algorythms;

class Program
{
    static void Main()
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
}