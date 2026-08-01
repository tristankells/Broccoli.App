using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Storage;

namespace Broccoli.Avalonia.Slices.Planning;

public class MacroTargetService : IMacroTargetService
{
    private const string SettingsRowId = "default";

    public List<MacroTarget> GetAll()
    {
        using var context = BroccoliDbContext.CreateForApp();
        return context.MacroTargets
            .OrderBy(t => t.CreatedAt)
            .ToList();
    }

    public MacroTarget Add(MacroTarget target)
    {
        target.Id = Guid.NewGuid().ToString();
        target.CreatedAt = DateTime.UtcNow;
        target.UpdatedAt = DateTime.UtcNow;

        using var context = BroccoliDbContext.CreateForApp();
        context.MacroTargets.Add(target);
        context.SaveChanges();
        return target;
    }

    public MacroTarget Update(MacroTarget target)
    {
        target.UpdatedAt = DateTime.UtcNow;

        using var context = BroccoliDbContext.CreateForApp();
        context.MacroTargets.Update(target);
        context.SaveChanges();
        return target;
    }

    public void Delete(string id)
    {
        using var context = BroccoliDbContext.CreateForApp();
        var target = context.MacroTargets.Find(id);
        if (target is not null)
        {
            context.MacroTargets.Remove(target);
            context.SaveChanges();
        }
    }

    public MacroTargetSettings GetSettings()
    {
        using var context = BroccoliDbContext.CreateForApp();
        var settings = context.MacroTargetSettings.Find(SettingsRowId);
        if (settings is not null) return settings;

        return new MacroTargetSettings { Id = SettingsRowId };
    }

    public MacroTargetSettings SaveSettings(MacroTargetSettings settings)
    {
        settings.Id = SettingsRowId;
        settings.UpdatedAt = DateTime.UtcNow;

        using var context = BroccoliDbContext.CreateForApp();
        var existing = context.MacroTargetSettings.Find(SettingsRowId);
        if (existing is not null)
        {
            context.Entry(existing).CurrentValues.SetValues(settings);
        }
        else
        {
            context.MacroTargetSettings.Add(settings);
        }
        context.SaveChanges();
        return settings;
    }
}
