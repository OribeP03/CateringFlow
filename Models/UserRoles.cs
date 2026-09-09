// Models/UserRoles.cs
namespace cateringflow.Models;

public static class UserRoles
{
    public const string SuperAdmin = "Super Admin";
    public const string SalesCrm = "Sales / CRM Staff";
    public const string EventCoordinator = "Event Coordinator";
    public const string InventoryStaff = "Inventory Staff";
    public const string KitchenManager = "Kitchen Manager";
    public const string FinanceStaff = "Finance Staff";
    public const string StaffCrew = "Staff / Crew";
    public const string Customer = "Customer";

    public static readonly string[] AllRoles = new[]
    {
        SuperAdmin,
        SalesCrm,
        EventCoordinator,
        InventoryStaff,
        KitchenManager,
        FinanceStaff,
        StaffCrew,
        Customer
    };
}
