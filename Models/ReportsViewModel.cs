namespace cateringflow.Models;

public class PackageStat
{
    public required string PackageName { get; set; }
    public int Count { get; set; }
    public decimal Revenue { get; set; }
}

public class StatusBreakdown
{
    public required string Status { get; set; }
    public int Count { get; set; }
    public decimal TotalAmount { get; set; }
}

public class MethodStat
{
    public required string Method { get; set; }
    public int Count { get; set; }
    public decimal TotalAmount { get; set; }
}

public class ReportsViewModel
{
    public List<KpiCardModel> ReportKpis { get; set; } = new();

    public List<MonthlyRevenuePoint> RevenueTrend { get; set; } = new();

    public List<EventTypeCount> EventsByType { get; set; } = new();

    public List<PackageStat> TopPackages { get; set; } = new();

    public List<StatusBreakdown> InvoiceBuckets { get; set; } = new();

    public List<MethodStat> PaymentMethods { get; set; } = new();

    public int TotalEvents { get; set; }

    public int CompletedEvents { get; set; }

    public int CancelledEvents { get; set; }

    public int PendingQuotations { get; set; }

    public int LowStockItems { get; set; }

    public decimal CurrentMonthRevenue { get; set; }

    public decimal PreviousMonthRevenue { get; set; }

    public decimal RevenueChangePercent { get; set; }
}