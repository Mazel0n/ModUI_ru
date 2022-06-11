### Adding an Icon

!> ModUI will automatically detect if you have set an Icon or not

- remove the File extension of your desired Icon
- add the File to your Mod Resources [Click Me if you don't know what to do](https://fedearre.github.io/my-garage-modding-docs/#/first-steps/asset-bundles)

### Example Icon Assignment

```csharp
using ModUI;

public class ModUIExampleMod : Mod
{
    /*
     * Your Informations here...
     */
    
    public override byte[] Icon => Properties.Resources.yourIconFileName;
}
```