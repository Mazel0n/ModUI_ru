### Adding & Using Keybinds

!> You shall not add any Keybinds outside of the `CreateModKeybinds` method!

- Add `using ModUI.Keybinds;` and `using static ModUI.Keybinds.ModKeybinds;` to the Top of your .cs File aswell
- Add the `IModKeybinds` interface to your Mod class
- Add the `CreateModKeybinds` method to your Mod class

### Example Keybinds Creation & Usage

```csharp
using ModUI;
using ModUI.Keybinds;
using static ModUI.Keybinds.ModKeybinds;

public class ModUIExampleMod : Mod, IModKeybinds
{
    /*
     * Your Informations here...
     */
    
    Keybind exampleKeybind;

    public void CreateModKeybinds(ModKeybinds modKeybinds)
    {
        exampleKeybind = modKeybinds.AddKeybind("exampleKeybind", "Example Keybind", KeyCode.M, KeyCode.LeftControl);
    }

    public override void Update()
    {
        if (exampleKeybind.GetKeybindDown()) ModConsole.Log("Keybind Down!");
        if (exampleKeybind.GetKeybindUp()) ModConsole.Log("Keybind Up!");
        if (exampleKeybind.GetKeybind()) ModConsole.Log("Keybind!");
    }
}
```