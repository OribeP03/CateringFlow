using cateringflow.Models;

namespace cateringflow.Services;

public static class StatCardOrdering
{
    public static List<KpiCardModel> OrderIncremental(IEnumerable<KpiCardModel> cards)
    {
        ArgumentNullException.ThrowIfNull(cards);
        return cards.OrderBy(c => c.SortValue).ToList();
    }
}