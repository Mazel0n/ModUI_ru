using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using ModUI.Internals;
using System.IO;
using System.Reflection;
using ModUI.Settings;
using ModUI.Keybinds;

namespace ModUI
{
    internal class ModUIController : MonoBehaviour
    {
        public enum MenuType
        {
            Default,
            Details,
            Settings,
            Keybinds
        }
        public class HistoryInfo
        {
            public GameObject menuObject;
            public MenuType menu;
            public Action menuAction;
            public Action closeAction;

            public HistoryInfo(GameObject menuObj, MenuType menu, Action menuAction, Action closeAction = null)
            {
                menuObject = menuObj;
                this.menu = menu;
                this.menuAction = menuAction;
                this.closeAction = closeAction;
            }
        }

        public static ModUIController instance;
        public GameObject canvas;
        public GameObject OpenButton;
        public GameObject modPrefab;
        public GameObject returnButton;
        public TMPro.TextMeshProUGUI headerText;
        public Transform modContainer;
        public Transform settingsContainer;
        internal static Stack<HistoryInfo> history = new Stack<HistoryInfo>();

        public GameObject modMenu;
        public GameObject settingsMenu;
        public GameObject eventSystem;

        public List<ModInfo> modInfos;
        bool toggle;

        void Start()
        {
            returnButton.SetActive(false);
            instance = this;
            canvas.SetActive(false);
            toggle = false;
            history.Push(new HistoryInfo(modMenu, MenuType.Default, null));

            InitModContainer();
        }

        void InitModContainer()
        {
            modInfos = new List<ModInfo>();
            var mods = ModLoader.mods;
            for (var i = 0; i < mods.Count; i++)
            {
                var modInfoGO = GameObject.Instantiate<GameObject>(modPrefab);
                var modInfo = modInfoGO.GetComponent<ModInfo>();

                modInfo.mod = mods[i];
                modInfo.Setup();
                modInfos.Add(modInfo);

                modInfoGO.transform.SetParent(modContainer, false);
            }
        }
        internal void CreateSettingsMenu(Mod mod)
        {
            var modSettings = ModSettings.modSettings[mod];

            foreach(var setting in modSettings.settingsElements)
            {
                var go = setting.Create(setting);
                go.SetActive(false);
                if (setting is ModSettings.Header) modSettings.currentHeader = go.GetComponent<_Header>();
                else
                {
                    if (setting != modSettings.defaultsButton) modSettings.currentHeader.children.Add(go);
                }
                go.transform.SetParent(settingsContainer);
                go.SetActive(true);
            }
        }
        internal void CreateKeybindsMenu(Mod mod)
        {
            var modKeybinds = ModKeybinds.modKeybinds[mod];

            foreach (var keybind in modKeybinds.keybindsElements)
            {
                var go = keybind.Create(keybind);
                go.SetActive(false);
                if (keybind is ModKeybinds.Header) modKeybinds.currentHeader = go.GetComponent<_Header>();
                else
                {
                    if (keybind != modKeybinds.defaultsButton) modKeybinds.currentHeader.children.Add(go);
                }
                go.transform.SetParent(settingsContainer);
                go.SetActive(true);
            }
        }
        void CreateDetailsMenu(Mod mod)
        {
            ModInfo modInfo = null;
            for (var i = 0; i < modInfos.Count; i++)
            {
                if (modInfos[i].mod.ID == mod.ID) modInfo = modInfos[i]; 
            }

            var modRefuseDisable = Attribute.GetCustomAttribute(mod.GetType(), typeof(ModRefuseDisable));

            if (modRefuseDisable == null)
            {
                var toggle = modInfo.toggle.Create(modInfo.toggle);
                toggle.transform.SetParent(settingsContainer);
            }
            var label = modInfo.label.Create(modInfo.label);
            label.transform.SetParent(settingsContainer);
        }

        public void ToggleUI()
        {
            toggle = !toggle;
            instance.canvas.SetActive(toggle);
            MenuHelper.SetInteractMenu(toggle);
            if (!toggle) OpenMenu(MenuType.Default);
            eventSystem.SetActive(toggle || ModConsole.instance.consoleWindow.activeSelf);
        }
        public static void OpenMenu(MenuType menu, Mod mod = null)
        {
            switch(menu)
            {
                case MenuType.Default:
                    while (history.Peek().menu != MenuType.Default) instance.Return();
                    instance.modMenu.SetActive(true);
                    break;
                case MenuType.Details:
                case MenuType.Settings:
                case MenuType.Keybinds:
                    instance.modMenu.SetActive(false);
                    instance.settingsMenu.SetActive(true);
                    instance.returnButton.SetActive(true);

                    if (menu == MenuType.Details)
                    {
                        var head = instance.headerText.text;
                        instance.headerText.text = $"{mod.Name} Details";
                        history.Push(new HistoryInfo(instance.settingsMenu, menu, () => { instance.CreateDetailsMenu(mod); }, () => { ModSettings.modSettings[mod].SaveSettings(); instance.headerText.text = head; }));
                        instance.CreateDetailsMenu(mod);
                    }
                    if (menu == MenuType.Settings)
                    {
                        var head = instance.headerText.text;
                        instance.headerText.text = $"{mod.Name} Settings";
                        history.Push(new HistoryInfo(instance.settingsMenu, menu, () => { instance.CreateSettingsMenu(mod); }, () => { ModSettings.modSettings[mod].SaveSettings(); instance.headerText.text = head; }));
                        instance.CreateSettingsMenu(mod);
                    }
                    if (menu == MenuType.Keybinds)
                    {
                        var head = instance.headerText.text;
                        instance.headerText.text = $"{mod.Name} Keybinds";
                        history.Push(new HistoryInfo(instance.settingsMenu, menu, () => { instance.CreateKeybindsMenu(mod); }, () => { ModKeybinds.modKeybinds[mod].SaveKeybinds(); instance.headerText.text = head; }));
                        instance.CreateKeybindsMenu(mod);
                    }
                    break;
            }
        }
        public static void CloseMenu(HistoryInfo history)
        {
            if (instance.settingsContainer.childCount > 0)
                for (var i = 0; i < instance.settingsContainer.childCount; i++)
                    GameObject.Destroy(instance.settingsContainer.GetChild(i).gameObject);

            history.closeAction?.Invoke();
            history.menuObject.SetActive(false);
            if (ModUIController.history.Peek().menu == MenuType.Default)
            {
                instance.returnButton.SetActive(false);
            }
        }
        public void Return()
        {
            if (history.Count < 0 || history.Peek().menu == MenuType.Default) return;

            CloseMenu(history.Pop());

            ModUIController.history.Peek().menuAction?.Invoke();
            ModUIController.history.Peek().menuObject.SetActive(true);
        }
    }

    internal class ModInfo : MonoBehaviour
    {
        public Mod mod;
        public Image status;
        public RawImage icon;
        public TMPro.TextMeshProUGUI head, text;
        public Selectable settings, keybinds;
        public ModSettings.Toggle toggle;
        public ModSettings.Label label;

        public void Setup()
        {
            toggle = new ModSettings.Toggle("Enabled", "null", mod.enabled, (bool value) => { mod.enabled = value; });
            label = new ModSettings.Label();

            label.Text = 
                $"ID: <color=#00ffffff>{mod.ID}</color> (ModUI <color=#008080ff>{_ModUI.version}</color>)\n" +
                $"Version: <color=#00ffffff>{mod.Version}</color>\n" +
                $"Author/s: <color=#00ffffff>{mod.Author}</color>";
            status.color = mod.enabled ? Color.green : Color.red;

            var desc = "<i><color=#AEAEAE>no Description provided...</color></i>";
            if (mod is IModDescription)
            {
                desc = ((IModDescription)mod).Description;
            }

            if (mod.Icon != null)
            {
                var icon = new Texture2D(2, 2);
                icon.LoadImage(mod.Icon);
                icon.filterMode = FilterMode.Trilinear;
                icon.wrapMode = TextureWrapMode.Clamp;
                icon.Apply();

                this.icon.texture = icon;
            }
            else icon.texture = _ModUI.defaultIcon;

            head.text = $"{mod.Name}";
            text.text = $"by {mod.Author} ({mod.Version})\n{desc}";
        }

        void Update()
        {
            var col = status.color;
            var tar = mod.enabled ? Color.green : Color.red;
            col.r = Mathf.MoveTowards(col.r, tar.r, 2 * Time.deltaTime);
            col.g = Mathf.MoveTowards(col.g, tar.g, 2 * Time.deltaTime);
            col.b = Mathf.MoveTowards(col.b, tar.b, 2 * Time.deltaTime);
            col.a = Mathf.MoveTowards(col.a, tar.a, 2 * Time.deltaTime);
            status.color = col;

            if (ModSettings.modSettings.ContainsKey(mod))
            {
                if (settings.gameObject.activeSelf != ModSettings.modSettings[mod].settingsElements.Count > 0)
                    settings.gameObject.SetActive(ModSettings.modSettings[mod].settingsElements.Count > 0);
            }
            else
            {
                if (settings.gameObject.activeSelf) settings.gameObject.SetActive(false);
            }
            if (keybinds.gameObject.activeSelf != ModKeybinds.modKeybinds.ContainsKey(mod))
                keybinds.gameObject.SetActive(ModKeybinds.modKeybinds.ContainsKey(mod));
        }

        public void ShowDetails()  => ModUIController.OpenMenu(ModUIController.MenuType.Details , mod);
        public void ShowSettings() => ModUIController.OpenMenu(ModUIController.MenuType.Settings, mod);
        public void ShowKeybinds() => ModUIController.OpenMenu(ModUIController.MenuType.Keybinds, mod);
    }
}