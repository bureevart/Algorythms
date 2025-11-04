namespace Algorythms;

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
    public int Height { get; set; }
    public Dictionary<int, double> Connections { get; set; } = new();
    public override string ToString() => $"#{Index} ({Q},{R}) H={Height}";
}