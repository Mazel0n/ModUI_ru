using ModUI.Assets;
using ModUI.Settings;
using ModUI.Keybinds;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using static ModUI.Settings.ModSettings;
using static ModUI.Keybinds.ModKeybinds;
using UnityEngine.SceneManagement;
using System.Linq;

namespace ModUI.Internals
{
    [ModRefuseDisable]
    public class _ModUI : Mod, IModDescription, IModSettings, IModKeybinds
    {
        public override string ID => "ModUI";
        public override string Name => "Mod UserInterface";
        public override string Author => "BrennFuchS";
        public override string Version => version;
        public string Description => "User Interface for Mods!";
        public override byte[] Icon => Properties.Resources.ModUI_Logo;

        Keybind openMenu;
        Keybind openConsole;

        Toggle consoleWarnOpen;
        Toggle consoleErrorOpen;
        Toggle consoleIncludeDebugLog;
        Toggle consoleIncludeCallingMethod;
        Slider consoleWidth;
        Slider consoleHeight;

        public bool UseAssetsFolder => false;

        internal static GameObject ui;
        bool loaded = false;

        string modsFolder;

        internal static string assetsPath;
        internal static string settingsPath;
        internal static string referencesPath;
        internal readonly static string version;

        internal UnityEngine.UI.Image openBtn;
        internal int modCount;

        internal GameObject target;

        static _ModUI()
        {
            var reference = Assembly.GetExecutingAssembly().GetName();
            if (reference.Version.Build == 0)
                version = $"{reference.Version.Major}.{reference.Version.Minor}";
            else
            {
                if (reference.Version.Revision == 0)
                    version = $"{reference.Version.Major}.{reference.Version.Minor}.{reference.Version.Build}";
                else
                    version = $"{reference.Version.Major}.{reference.Version.Minor}.{reference.Version.Build}.{reference.Version.Revision}";
            }
        }

        List<Assembly> References = new List<Assembly>();
        internal static Texture2D defaultIcon;

        void SetFolderIcon(string folderPath, byte[] ico, string infoTip)
        {
            string iniText =
                "[.ShellClassInfo]"  + Environment.NewLine +
                "IconFile={0}"       + Environment.NewLine +
                "IconIndex=0"        + Environment.NewLine +
                "InfoTip={1}"        + Environment.NewLine +
                "IconResource={0},0" + Environment.NewLine;

            var icoPath = Path.Combine(folderPath, "folderIcon.ico");
            if (File.Exists(icoPath)) File.Delete(icoPath);
            File.WriteAllBytes(icoPath, ico);
            File.SetAttributes(icoPath, FileAttributes.Hidden);

            var iniPath = Path.Combine(folderPath, "desktop.ini");
            if (File.Exists(iniPath)) File.Delete(iniPath);
            File.WriteAllText(iniPath, string.Format(iniText, icoPath, infoTip));
            File.SetAttributes(iniPath, FileAttributes.Hidden | FileAttributes.System);

            File.SetAttributes(folderPath, FileAttributes.System);
        }

        public override void OnMenuLoad()
        {
            target = GameObject.Find("UIController/MainMenu_Canvas/MenuDefaultButtons_Canvas");
            GameObject.Find("UIController").transform.Find("MainMenu_Static_Canvas/Static_UI/VersionNumber").localPosition = new Vector3(-781.3f, -495.2f, 0f);

            if (loaded) return;
            loaded = true;

            modsFolder = Path.GetFullPath(Path.Combine("Mods", ""));
            assetsPath = Path.Combine(modsFolder, "Assets");
            settingsPath = Path.Combine(modsFolder, "Settings");
            referencesPath = Path.Combine(modsFolder, "References");

            if (!Directory.Exists(assetsPath)) Directory.CreateDirectory(assetsPath);
            if (!Directory.Exists(settingsPath)) Directory.CreateDirectory(settingsPath);
            if (!Directory.Exists(referencesPath)) Directory.CreateDirectory(referencesPath);

            var references = Directory.GetFiles(referencesPath, "*.dll");

            foreach (var reference in references)
            {
                References.Add(Assembly.LoadFile(reference));
            }

            var ab = AssetBundle.LoadFromMemory(Properties.Resources.modui);

            var prefab = ab.LoadAsset<GameObject>("ModUICanvas.prefab");
            ui = GameObject.Instantiate<GameObject>(prefab);
            GameObject.DontDestroyOnLoad(ui);

            var sph = ab.LoadAsset<SettingsPrefabHolder>("ModUI_SettingsPrefabHolder");

            prefabHolders.Add(sph);

            ab.Unload(false);

            #region ModSettings, ModAssets, ModKeybinds Initialization
            var mods = ModLoader.mods;
            modCount = mods.Count;

            foreach (var mod in mods)
            {
                if (ModSettings.modSettings.ContainsKey(mod)) continue;
                var modSettings = new ModSettings();
                modSettings.optionsFolderPath = Path.Combine(settingsPath, mod.ID);
                modSettings.mod = mod;
                ModSettings.modSettings.Add(mod, modSettings);

                if (!Directory.Exists(modSettings.optionsFolderPath)) Directory.CreateDirectory(modSettings.optionsFolderPath);

                ModSettings.allowCreating = false;
                if (mod is IModSettings)
                {
                    ModSettings.allowCreating = true;

                    var mod1 = (IModSettings)mod;

                    modSettings.AddHeader("Settings");

                    mod1.CreateModSettings(modSettings);

                    modSettings.defaultsButton = modSettings.AddButton("reset all Settings to default", () => { modSettings.LoadDefaults(); });

                    modSettings.LoadAction = mod1.ModSettingsLoaded;
                    modSettings.LoadSettings();
                }
            }

            foreach (var mod in mods)
            {
                ModKeybinds.allowCreating = false;
                if (mod is IModKeybinds)
                {
                    var modKeybinds = new ModKeybinds();
                    modKeybinds.optionsFolderPath = Path.Combine(settingsPath, mod.ID);
                    modKeybinds.mod = mod;
                    ModKeybinds.modKeybinds.Add(mod, modKeybinds);

                    ModKeybinds.allowCreating = true;

                    var mod1 = (IModKeybinds)mod;

                    modKeybinds.AddHeader("Keybinds");

                    mod1.CreateModKeybinds(modKeybinds);

                    modKeybinds.defaultsButton = modKeybinds.AddButton("reset all Keybinds to default", () => { modKeybinds.LoadDefaults(); });

                    modKeybinds.LoadKeybinds();
                }
            }
            
            foreach (var mod in mods)
            {
                if (mod is IModAssets)
                {
                    var mod1 = (IModAssets)mod;

                    if (!mod1.UseAssetsFolder) continue;
                    if (!Directory.Exists(Path.Combine(assetsPath, mod.ID))) Directory.CreateDirectory(Path.Combine(assetsPath, mod.ID));
                }
            }
            #endregion

            ui.transform.SetAsLastSibling();

            var icon = new Texture2D(2, 2);
            icon.LoadImage(Properties.Resources.Mod_Icon);
            icon.filterMode = FilterMode.Trilinear;
            icon.wrapMode = TextureWrapMode.Clamp;
            icon.Apply();

            _ModUI.defaultIcon = icon;

            SetFolderIcon(modsFolder, Properties.Resources.ModsFolderIcon, "My Garage Mods Folder");
            SetFolderIcon(assetsPath, Properties.Resources.assets, "Assets used by the Mods");
            SetFolderIcon(settingsPath, Properties.Resources.settings, "Mod Settings");
            SetFolderIcon(referencesPath, Properties.Resources.library, "Libraries used by the Mods");
        }

        public override void MenuUpdate() => Update();
        public override void Update()
        {
            if (target)
            {
                if (ModUIController.instance.OpenButton.activeSelf != target.activeSelf) ModUIController.instance.OpenButton.SetActive(target.activeSelf);
            }
            else
            {
                if (ModUIController.instance.OpenButton.activeSelf != false) ModUIController.instance.OpenButton.SetActive(false);
            }

            if (openBtn != null) openBtn.color = HSBColor.ToColor(new HSBColor(Mathf.PingPong(Time.time * (modCount / 50f), 1), 1, 1f / 3f * 2f));
            else openBtn = ModUIController.instance.OpenButton.GetComponent<UnityEngine.UI.Image>();

            if (openMenu.GetKeybindUp()) ModUIController.instance.ToggleUI();
            if (openConsole.GetKeybindUp()) ModConsole.instance.ToggleConsole();
        }
        public override void OnLoad()
        {
            target = Resources.FindObjectsOfTypeAll<GameObject>().First(x => x.name == "EscMenu");
            //loaded = false;
        }

        public void CreateModSettings(ModSettings modSettings)
        {
            consoleWarnOpen = modSettings.AddToggle("Toggle Console on Warning", "toggleConsoleWarning", false, (bool value) => 
            { ModConsole.openOnWarning = value; });
            consoleErrorOpen = modSettings.AddToggle("Toggle Console on Error", "toggleConsoleError", true, (bool value) => 
            { ModConsole.openOnError = value; });
            consoleWidth = modSettings.AddSlider("Console Width", "widthConsole", 520f, Screen.width / 6f, Screen.width / 3f * 1.75f, (float value) => 
            {
                var delta = ModConsole.instance.rectWindow.sizeDelta;
                delta.x = value;
                ModConsole.size = delta;
            });
            consoleHeight = modSettings.AddSlider("Console Height", "heightConsole", 280f, Screen.height / 8f, Screen.height -30f, (float value) =>
            {
                var delta = ModConsole.instance.rectWindow.sizeDelta;
                delta.y = value;
                ModConsole.size = delta;
            });
            consoleIncludeDebugLog = modSettings.AddToggle("Console Include Debug Log", "toggleConsoleIncludeDebugLog", false, (bool value) =>
            { ModConsole.includeDebugLog = value; });
            consoleIncludeCallingMethod = modSettings.AddToggle("Console Include Calling Method", "toggleConsoleCallingMethod", false, (bool value) =>
            { ModConsole.includeCallingMethod = value; });
        }

        public void ModSettingsLoaded()
        {
            ModConsole.openOnWarning = consoleWarnOpen.Value;
            ModConsole.openOnError = consoleErrorOpen.Value;
            var size = new Vector2(consoleWidth.Value, consoleHeight.Value);
            size.x = Mathf.Clamp(size.x, Screen.width / 6f, Screen.width / 3f * 2f);
            size.y = Mathf.Clamp(size.y, Screen.height / 8f, Screen.height / 4f * 3f);
            ModConsole.size = size;
            ModConsole.includeDebugLog = consoleIncludeDebugLog.Value;
            ModConsole.includeCallingMethod= consoleIncludeCallingMethod.Value;
        }

        public void CreateModKeybinds(ModKeybinds modKeybinds)
        {
            openMenu = modKeybinds.AddKeybind("openModMenu", "Open ModUI", KeyCode.M, KeyCode.LeftControl);
            openConsole = modKeybinds.AddKeybind("openModConsole", "Toggle Console", KeyCode.BackQuote);
        }
    }
}
