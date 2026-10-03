using cateringflow.Models;
using cateringflow.Services;
using Xunit;

namespace CateringFlow.Tests;

public class StatCardOrderingTests
{
    [Fact]
    public void OrderIncremental_PlacesLargestValueOnTheRight()
    {
        var cards = new List<KpiCardModel>
        {
            new() { Label = "Total", Value = "6", SortValue = 6 },
            new() { Label = "Paid", Value = "3", SortValue = 3 },
            new() { Label = "Partial", Value = "1", SortValue = 1 },
            new() { Label = "Overdue", Value = "1", SortValue = 1 },
            new() { Label = "Unpaid", Value = "1", SortValue = 1 }
        };

        var ordered = StatCardOrdering.OrderIncremental(cards);

        Assert.Equal(5, ordered.Count);
        Assert.Equal(1, ordered[0].SortValue);
        Assert.Equal(6, ordered[^1].SortValue);
        Assert.Equal("Total", ordered[^1].Label);
    }

    [Fact]
    public void OrderIncremental_ValuesAscendLeftToRight()
    {
        var cards = new List<KpiCardModel>
        {
            new() { Label = "A", Value = "10", SortValue = 10 },
            new() { Label = "B", Value = "2", SortValue = 2 },
            new() { Label = "C", Value = "7", SortValue = 7 }
        };

        var ordered = StatCardOrdering.OrderIncremental(cards);

        Assert.Collection(ordered,
            c => Assert.Equal("B", c.Label),
            c => Assert.Equal("C", c.Label),
            c => Assert.Equal("A", c.Label));
    }

    [Fact]
    public void OrderIncremental_PreservesOriginalOrderForTies()
    {
        var cards = new List<KpiCardModel>
        {
            new() { Label = "First", Value = "1", SortValue = 1 },
            new() { Label = "Second", Value = "1", SortValue = 1 },
            new() { Label = "Third", Value = "1", SortValue = 1 }
        };

        var ordered = StatCardOrdering.OrderIncremental(cards);

        Assert.Collection(ordered,
            c => Assert.Equal("First", c.Label),
            c => Assert.Equal("Second", c.Label),
            c => Assert.Equal("Third", c.Label));
    }

    [Fact]
    public void OrderIncremental_EmptyInputReturnsEmptyList()
    {
        var ordered = StatCardOrdering.OrderIncremental(new List<KpiCardModel>());
        Assert.Empty(ordered);
    }

    [Fact]
    public void OrderIncremental_SingleCardReturnsUnchanged()
    {
        var cards = new List<KpiCardModel> { new() { Label = "Solo", Value = "9", SortValue = 9 } };

        var ordered = StatCardOrdering.OrderIncremental(cards);

        var card = Assert.Single(ordered);
        Assert.Equal("Solo", card.Label);
        Assert.Equal("9", card.Value);
    }

    [Fact]
    public void OrderIncremental_NullInputThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => StatCardOrdering.OrderIncremental(null!));
    }
}