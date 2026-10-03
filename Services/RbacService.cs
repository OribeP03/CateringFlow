// Services/RbacService.cs
using cateringflow.Models;

namespace cateringflow.Services;

public class RbacService : IRbacService
{
    // Exact mapping requested by the user:
    // Role -> (Page/Resource -> Permission Description)
    private static readonly Dictionary<string, Dictionary<string, string>> RolePermissions = new()
    {
        [UserRoles.SuperAdmin] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Dashboard"] = "Manage",
            ["Calendar"] = "Manage",
            ["Customers"] = "Manage",
            ["Events"] = "Manage",
            ["MenuPackages"] = "Manage",
            ["Inventory"] = "Manage",
            ["Suppliers"] = "Manage",
            ["Staff"] = "Manage",
            ["Quotations"] = "Manage",
            ["Invoices"] = "Manage",
            ["Payments"] = "Manage",
            ["PaymentProofs"] = "Manage",
            ["ProofAdminChat"] = "Manage",
            ["SendProofAdminMessage"] = "Manage",
            ["ApprovePaymentProof"] = "Manage",
            ["RejectPaymentProof"] = "Manage",
            ["CRM"] = "Manage",
            ["Inquiries"] = "Manage",
            ["UpdateInquiryStatus"] = "Manage",
            ["AssignInquiry"] = "Manage",
            ["DeleteInquiry"] = "Manage",
            ["Reports"] = "View / Generate",
            ["ActivityLog"] = "View",
            ["Notifications"] = "Manage",
            ["Settings"] = "Manage"
        },
        [UserRoles.SalesCrm] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Customer Management — Manage; Customer CRM — Manage; Event Booking — Submit / Manage; Quotation & Billing — Quotations
            ["Dashboard"] = "View",
            ["Calendar"] = "View",
            ["Customers"] = "Manage",
            ["CRM"] = "Manage",
            ["Inquiries"] = "Manage",
            ["UpdateInquiryStatus"] = "Manage",
            ["AssignInquiry"] = "Manage",
            ["DeleteInquiry"] = "Manage",
            ["Events"] = "Submit / Manage",
            ["Quotations"] = "Quotations"
        },
        [UserRoles.EventCoordinator] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Event Booking — Submit / Manage; Staff Assignment — Assign / View; Event Scheduling — Manage / View
            ["Dashboard"] = "View",
            ["Calendar"] = "View",
            ["Events"] = "Submit / Manage / View",
            ["Staff"] = "Assign / View"
        },
        [UserRoles.InventoryStaff] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Ingredient Inventory — Manage; Supplier Management — Manage; Reports — View
            ["Dashboard"] = "View",
            ["Inventory"] = "Manage",
            ["Suppliers"] = "Manage",
            ["Reports"] = "View"
        },
        [UserRoles.KitchenManager] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Menu Package Management — Manage; Ingredient Inventory — Manage; Reports — View
            ["Dashboard"] = "View",
            ["MenuPackages"] = "Manage",
            ["Inventory"] = "Manage",
            ["Reports"] = "View"
        },
        [UserRoles.FinanceStaff] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Quotation & Billing — Manage; Reports — View / Generate
            ["Dashboard"] = "View",
            ["Quotations"] = "Manage",
            ["Invoices"] = "Manage",
            ["Payments"] = "Manage",
            ["PaymentProofs"] = "Manage",
            ["ProofAdminChat"] = "Manage",
            ["SendProofAdminMessage"] = "Manage",
            ["ApprovePaymentProof"] = "Manage",
            ["RejectPaymentProof"] = "Manage",
            ["Reports"] = "View / Generate"
        },
        [UserRoles.StaffCrew] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Staff Assignment — View Schedule / Tasks
            ["Dashboard"] = "View",
            ["Staff"] = "View Schedule / Tasks"
        },
        [UserRoles.Customer] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Customers"] = "Profile",
            ["CRM"] = "Interactions",
            ["Events"] = "Submit / View Details"
        }
    };

    public bool HasAccess(string role, string pageOrFeature, out string permissionLevel)
    {
        permissionLevel = "None";
        if (string.IsNullOrEmpty(role))
        {
            role = UserRoles.SuperAdmin; // Default fallback for guest demo
        }

        if (RolePermissions.TryGetValue(role, out var pages))
        {
            if (pages.TryGetValue(pageOrFeature, out var perm))
            {
                permissionLevel = perm;
                return true;
            }
        }
        return false;
    }

    public bool CanAccessPage(string role, string page)
    {
        return HasAccess(role, page, out _);
    }

    public Dictionary<string, string> GetAllowedPages(string role)
    {
        if (string.IsNullOrEmpty(role)) role = UserRoles.SuperAdmin;
        if (RolePermissions.TryGetValue(role, out var pages))
        {
            return pages;
        }
        return new Dictionary<string, string>();
    }
}
