using Broccoli.Avalonia.Models;

namespace Broccoli.Avalonia.Slices.Planning;

public interface IMacroTargetService
{
    List<MacroTarget> GetAll();

    MacroTarget Add(MacroTarget target);

    MacroTarget Update(MacroTarget target);

    void Delete(string id);

    MacroTargetSettings GetSettings();

    MacroTargetSettings SaveSettings(MacroTargetSettings settings);
}
