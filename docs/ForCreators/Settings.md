### Adding Settings

!> You shall not add any Settings outside of the `CreateModSettings` method!

- Add `using ModUI.Settings;` and `using static ModUI.Settings.ModSettings;` to the Top of your .cs File aswell
- Add the `IModSettings` interface to your Mod class
- Add the `CreateModSettings` and `ModSettingsLoaded` methods to your Mod class

For all the Types of Settings, check out [this](API/ModUI/ModSettings/Settings.md)

### Example Settings Creation

```csharp
using ModUI;
using ModUI.Settings;
using static ModUI.Settings.ModSettings;

public class ModUIExampleMod : Mod, IModSettings
{
    /*
     * Your Informations here...
     */
    
    Toggle exampleToggle;

    public void CreateModSettings(ModSettings modSettings)
    {
        exampleToggle = modSettings.AddToggle("Test Toggle", "testToggle", false, OnChanged);
    }

    void OnChanged(bool value)
    {
        ModConsole.Log($"Example Toggle Value Changed: {exampleToggle.Value}");
    }

    public void ModSettingsLoaded()
    {
        ModConsole.Log("Loaded Mod Settings!");
    }
}
```