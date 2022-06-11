### Adding a Description

- Add the `IModDescription` interface to your Mod class

### Example Description Usage

```csharp
using ModUI;

public class ModUIExampleMod : Mod, IModDescription
{
    /*
     * Your Informations here...
     */
    
    public string Description => "ModUI Example Mod Description!";
}
```