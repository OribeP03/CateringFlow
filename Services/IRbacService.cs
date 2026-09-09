// Services/IRbacService.cs
namespace cateringflow.Services;

public interface IRbacService
{
    bool HasAccess(string role, string pageOrFeature, out string permissionLevel);
    bool CanAccessPage(string role, string page);
    Dictionary<string, string> GetAllowedPages(string role);
}
