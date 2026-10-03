namespace cateringflow.Models;

public class CalendarDay
{
    public DateTime Date { get; set; }

    public bool InMonth { get; set; }

    public bool IsToday { get; set; }

    public List<EventModel> Events { get; set; } = new();
}

public class CalendarViewModel
{
    public int Year { get; set; }

    public int Month { get; set; }

    public int TotalDaysInMonth { get; set; }

    public int WeekdayOffset { get; set; }

    public List<CalendarDay> Days { get; set; } = new();

    public DateTime PrevMonthDate { get; set; }

    public DateTime NextMonthDate { get; set; }

    public int UpcomingCount { get; set; }

    public decimal MonthRevenue { get; set; }

    public string MonthName { get; set; } = string.Empty;
}