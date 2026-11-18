using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

public interface INavigationMenuService
{
    Task<List<NavigationMenuItem>> GetItemsAsync();
    Task SaveItemsAsync(IEnumerable<NavigationMenuItem> items);
    Task ResetToDefaultsAsync();
}
