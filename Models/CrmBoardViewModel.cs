namespace cateringflow.Models;

/// <summary>
/// One pipeline column of the CRM board. Leads are never created by hand in a real
/// CRM — they arrive from website inquiries — so a column is just a bucket of
/// inquiry-sourced leads grouped by stage.
/// </summary>
public class CrmStageColumn
{
    public required string Stage { get; init; }
    public required string Label { get; init; }
    public required string CssClass { get; init; }
    public IReadOnlyList<CRMLeadModel> Leads { get; init; } = Array.Empty<CRMLeadModel>();
    public decimal TotalValue => Leads.Sum(l => l.EstimatedValue);
    public int Count => Leads.Count;
}

/// <summary>
/// View model for the live CRM board page (/SuperAdmin/CRM).
/// </summary>
public class CrmBoardViewModel
{
    public const string StageNew = "New";
    public const string StageContacted = "Contacted";
    public const string StageQualified = "Qualified";
    public const string StageProposal = "Proposal";
    public const string StageNegotiation = "Negotiation";
    public const string StageWon = "Won";
    public const string StageLost = "Lost";

    /// <summary>Stages that are still "open" (not won, not lost).</summary>
    public static readonly string[] OpenStages =
    {
        StageNew, StageContacted, StageQualified, StageProposal, StageNegotiation
    };

    /// <summary>Canonical pipeline order, used for the board and the filter dropdown.</summary>
    public static readonly (string Stage, string Label, string CssClass)[] Pipeline =
    {
        (StageNew, "New Inquiry", "col-new"),
        (StageContacted, "Contacted", "col-contacted"),
        (StageQualified, "Qualified", "col-qualified"),
        (StageProposal, "Proposal Sent", "col-quotation"),
        (StageNegotiation, "Negotiation", "col-negotiation"),
        (StageWon, "Converted / Won", "col-converted"),
        (StageLost, "Lost", "col-lost")
    };

    public IReadOnlyList<CrmStageColumn> Columns { get; init; } = Array.Empty<CrmStageColumn>();
    public IPagedResult Leads { get; init; } = default!;
    public int TotalLeads { get; init; }
    public int OpenLeads { get; init; }
    public int WonLeads { get; init; }
    public int LostLeads { get; init; }
    public int NewLeads { get; init; }
    public decimal PipelineValue { get; init; }
    public decimal WonValue { get; init; }
    public string? Search { get; init; }
    public string? StageFilter { get; init; }
    public string? AssigneeFilter { get; init; }

    /// <summary>
    /// Share of leads that converted to Won (0-100, one decimal). Zero when there
    /// are no leads at all so the KPI card never divides by zero.
    /// </summary>
    public double ConversionRate => TotalLeads == 0 ? 0 : Math.Round(WonLeads * 100.0 / TotalLeads, 1);

    /// <summary>Assignees present in the pipeline, for the filter dropdown.</summary>
    public IReadOnlyList<string> Assignees { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Builds the board from the lead set: one column per pipeline stage (always
    /// present, even when empty) plus the paged lead list for the table below.
    /// </summary>
    public static CrmBoardViewModel Build(
        IReadOnlyList<CRMLeadModel> allLeads,
        IPagedResult pagedLeads,
        string? search,
        string? stageFilter,
        string? assigneeFilter)
    {
        var columns = Pipeline
            .Select(p => new CrmStageColumn
            {
                Stage = p.Stage,
                Label = p.Label,
                CssClass = p.CssClass,
                Leads = allLeads
                    .Where(l => NormalizeStage(l.Stage) == p.Stage)
                    .OrderByDescending(l => l.EstimatedValue)
                    .ToList()
            })
            .ToList();

        var won = allLeads.Where(l => NormalizeStage(l.Stage) == StageWon).ToList();
        var lost = allLeads.Where(l => IsLost(l.Stage)).ToList();
        var open = allLeads.Where(l => OpenStages.Contains(NormalizeStage(l.Stage))).ToList();

        return new CrmBoardViewModel
        {
            Columns = columns,
            Leads = pagedLeads,
            TotalLeads = allLeads.Count,
            OpenLeads = open.Count,
            WonLeads = won.Count,
            LostLeads = lost.Count,
            NewLeads = allLeads.Count(l => NormalizeStage(l.Stage) == StageNew),
            PipelineValue = open.Sum(l => l.EstimatedValue),
            WonValue = won.Sum(l => l.EstimatedValue),
            Search = search,
            StageFilter = stageFilter,
            AssigneeFilter = assigneeFilter,
            Assignees = allLeads
                .Select(l => l.AssignedTo)
                .Where(a => !string.IsNullOrWhiteSpace(a))
                .Select(a => a!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(a => a, StringComparer.OrdinalIgnoreCase)
                .ToList()
        };
    }

    /// <summary>Maps legacy/synonym stage values onto the canonical pipeline stage.</summary>
    public static string NormalizeStage(string? stage)
    {
        var value = (stage ?? string.Empty).Trim();
        return value.ToLowerInvariant() switch
        {
            "new" or "new lead" or "new inquiry" => StageNew,
            "contacted" => StageContacted,
            "qualified" => StageQualified,
            "proposal" or "proposal sent" or "quotation" or "quotation sent" => StageProposal,
            "negotiation" => StageNegotiation,
            "won" or "converted" => StageWon,
            _ => IsLost(value) ? StageLost : StageNew
        };
    }

    public static bool IsLost(string? stage)
        => string.Equals(stage?.Trim(), "Lost", StringComparison.OrdinalIgnoreCase)
           || string.Equals(stage?.Trim(), "Locked/Lost", StringComparison.OrdinalIgnoreCase);

    /// <summary>Days since the lead was created — the "aging" signal on a kanban card.</summary>
    public static int DaysWaiting(CRMLeadModel lead, DateTime now)
        => Math.Max(0, (now.Date - lead.CreatedAt.Date).Days);

    public static List<KpiCardModel> BuildKpiCards(CrmBoardViewModel board)
    {
        var cards = new List<KpiCardModel>
        {
            new() { Label = "New Inquiries", Value = board.NewLeads.ToString("N0"), SortValue = board.NewLeads, Icon = "bi bi-inbox", IconPill = "blue", SubNote = board.NewLeads > 0 ? "Waiting for first contact" : "All inquiries contacted", SubNoteClass = board.NewLeads > 0 ? "warning" : "positive" },
            new() { Label = "Open Leads", Value = board.OpenLeads.ToString("N0"), SortValue = board.OpenLeads, Icon = "bi bi-funnel", IconPill = "purple", SubNote = $"{board.TotalLeads} total in pipeline", SubNoteClass = "muted" },
            new() { Label = "Conversion Rate", Value = $"{board.ConversionRate:0.#}%", SortValue = board.ConversionRate, Icon = "bi bi-percent", IconPill = "amber", SubNote = $"{board.WonLeads} won · {board.LostLeads} lost", SubNoteClass = "muted" },
            new() { Label = "Won Value", Value = $"₱{board.WonValue:N0}", SortValue = (double)board.WonValue, Icon = "bi bi-trophy", IconPill = "gold", SubNote = "Converted to bookings", SubNoteClass = "positive" },
            new() { Label = "Pipeline Value", Value = $"₱{board.PipelineValue:N0}", SortValue = (double)board.PipelineValue, Icon = "bi bi-currency-dollar", IconPill = "green", SubNote = "Open opportunities", SubNoteClass = "positive" }
        };

        return Services.StatCardOrdering.OrderIncremental(cards);
    }
}