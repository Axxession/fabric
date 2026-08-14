using System.Reflection;
using Elsa.Workflows.UIHints.Dropdown;
using Fabric.Server.Printing.Domain;
using Fabric.Server.Printing.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Automation.Kiosk.Providers;

public sealed class CardPrintDesignProvider(PrintingDbContext printingDbContext) : DropDownOptionsProviderBase
{
    protected override bool RefreshOnChange { get; } = true;

    protected override async ValueTask<ICollection<SelectListItem>> GetItemsAsync(PropertyInfo propertyInfo, object? context, CancellationToken cancellationToken)
    {
        if (context is null)
            return [];

        List<SelectListItem> items = await printingDbContext.PrintDesigns
            .AsNoTracking()
            .Where(design => design.SurfaceKind == PrintSurfaceKind.Card)
            .OrderBy(design => design.Name)
            .ThenByDescending(design => design.Version)
            .Select(design => new SelectListItem($"{design.Name} v{design.Version}", design.Id.ToString()))
            .ToListAsync(cancellationToken);

        return items;
    }
}
