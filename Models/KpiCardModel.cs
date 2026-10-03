namespace cateringflow.Models;

public enum KpiValuePosition
{
    Left,
    Right
}

public class KpiCardModel
{
    public required string Label { get; set; }

    public required string Value { get; set; }

    public double SortValue { get; set; }

    public bool ValueFirst { get; set; }

    public string? LabelClass { get; set; }

    public string? FontSize { get; set; }

    public string? Background { get; set; }

    public string? BorderColor { get; set; }

    public string? Foreground { get; set; }

    public string? Icon { get; set; }

    public string? IconPill { get; set; }

    public string? SubNote { get; set; }

    public string? SubNoteClass { get; set; }

    public KpiValuePosition ValuePosition { get; set; } = KpiValuePosition.Right;

    public string GetValuePositionClass()
        => ValuePosition == KpiValuePosition.Right ? "kpi-card--value-right" : "kpi-card--value-left";
}