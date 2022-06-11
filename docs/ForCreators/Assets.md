### Using Assets

!> You shall not use any of the ModAssets methods anywhere else than on your Mod!

- Add `using ModUI.Assets;` to the Top of your .cs File aswell
- Add the `IModAssets` interface to your Mod class
- Add the `UseAssetsFolder` property to your Mod class

### Example Keybinds Creation & Usage

```csharp
using ModUI;
using ModUI.Assets;

public class ModUIExampleMod : Mod, IModAssets
{
    /*
     * Your Informations here...
     */
    
    public bool UseAssetsFolder => true;

    public override void OnMenuLoad()
    {
        ModAssets.LoadBundle(this, "myBundle.unity3d");
    }
}
```