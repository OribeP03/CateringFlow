namespace cateringflow.Models;

public class MonthlyRevenuePoint
{
    public string Month { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class EventTypeCount
{
    public string Type { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class DashboardViewModel
{
    public List<KpiCardModel> KpiCards { get; set; } = new();

    public List<EventModel> UpcomingEvents { get; set; } = new();

    public List<PaymentModel> RecentPayments { get; set; } = new();

    public List<InventoryModel> StockAlerts { get; set; } = new();

    public List<EventModel> RecentActivity { get; set; } = new();

    public List<MonthlyRevenuePoint> MonthlyRevenue { get; set; } = new();

    public List<EventTypeCount> EventsByType { get; set; } = new();
}