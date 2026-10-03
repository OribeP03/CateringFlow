using cateringflow.Models;
using cateringflow.Services;
using Xunit;

namespace CateringFlow.Tests;

public class KpiCardLayoutTests
{
    [Fact]
    public void KpiCardModel_DefaultsValuePositionToRight()
    {
        var card = new KpiCardModel { Label = "Total Revenue", Value = "4.52M" };

        Assert.Equal(KpiValuePosition.Right, card.ValuePosition);
    }

    [Fact]
    public void GetValuePositionClass_Right_ReturnsRightClass()
    {
        var card = new KpiCardModel { Label = "L", Value = "1" };
        card.ValuePosition = KpiValuePosition.Right;

        Assert.Equal("kpi-card--value-right", card.GetValuePositionClass());
    }

    [Fact]
    public void GetValuePositionClass_Left_ReturnsLeftClass()
    {
        var card = new KpiCardModel { Label = "L", Value = "1" };
        card.ValuePosition = KpiValuePosition.Left;

        Assert.Equal("kpi-card--value-left", card.GetValuePositionClass());
    }

    [Fact]
    public void OrderIncremental_StillAscending_WithValuePositionSet()
    {
        var cards = new List<KpiCardModel>
        {
            new() { Label = "A", Value = "6", SortValue = 6, ValuePosition = KpiValuePosition.Right },
            new() { Label = "B", Value = "1", SortValue = 1, ValuePosition = KpiValuePosition.Right },
            new() { Label = "C", Value = "3", SortValue = 3, ValuePosition = KpiValuePosition.Right }
        };

        var ordered = StatCardOrdering.OrderIncremental(cards);

        Assert.Collection(ordered,
            c => Assert.Equal("B", c.Label),
            c => Assert.Equal("C", c.Label),
            c => Assert.Equal("A", c.Label));
    }

    [Fact]
    public void OrderIncremental_TiesStayStable_WithValuePositionSet()
    {
        var cards = new List<KpiCardModel>
        {
            new() { Label = "First", Value = "1", SortValue = 1, ValuePosition = KpiValuePosition.Right },
            new() { Label = "Second", Value = "1", SortValue = 1, ValuePosition = KpiValuePosition.Right }
        };

        var ordered = StatCardOrdering.OrderIncremental(cards);

        Assert.Collection(ordered,
            c => Assert.Equal("First", c.Label),
            c => Assert.Equal("Second", c.Label));
    }
}