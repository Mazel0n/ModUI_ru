using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using ModUI.Internals;
using UnityEngine.Events;
using System.IO;
using Newtonsoft.Json;
using ModUI.Settings;
using System.Collections;

namespace ModUI.Keybinds
{
    public interface IModKeybinds
    {
        void CreateModKeybinds(ModKeybinds modKeybinds);
    }

    public static class KeybindExtension
    {

        public static bool GetKeybind(this ModKeybinds.Keybind kb)
        {
            if (kb == null) return false;
            if (kb.Modifier != KeyCode.None) return Input.GetKey(kb.Key) && Input.GetKey(kb.Modifier);
            return Input.GetKey(kb.Key);
        }
        public static bool GetKeybindDown(this ModKeybinds.Keybind kb)
        {
            if (kb == null) return false;
            if (kb.Modifier != KeyCode.None) return Input.GetKeyDown(kb.Key) && Input.GetKey(kb.Modifier);
            return Input.GetKeyDown(kb.Key);
        }
        public static bool GetKeybindUp(this ModKeybinds.Keybind kb)
        {
            if (kb == null) return false;
            if (kb.Modifier != KeyCode.None) return Input.GetKeyUp(kb.Key) && Input.GetKey(kb.Modifier);
            return Input.GetKeyUp(kb.Key);
        }
    }
    public class ModKeybinds
    {
        internal static bool allowCreating = false;
        internal static Dictionary<Mod, ModKeybinds> modKeybinds = new Dictionary<Mod, ModKeybinds>();
        internal List<ModSettings.IModSettingsElement> keybindsElements = new List<ModSettings.IModSettingsElement>();
        internal Mod mod;
        internal string optionsFolderPath;

        public void AddToKeybindsList<T>(T element) where T : ModSettings.IModSettingsElement
        {
            if (!allowCreating) throw new Exception("ModUI.ModKeybinds: please create your Keybinds in the 'CreateModKeybinds' method!");
            keybindsElements.Add(element);
        }

        internal Button AddButton(string text, Action action)
        {
            var button = new Button();
            button.Text = text;
            button.OnClick += () => { action?.Invoke(); };
            AddToKeybindsList(button);

            return button;
        }
        public Header AddHeader(string text, bool open = true)
        {
            var header = new Header();
            header.Text = text;
            header.Active = open;
            AddToKeybindsList(header);

            return header;
        }
        public Keybind AddKeybind(string ID, string Name, KeyCode Key, KeyCode KeyModifier = KeyCode.None)
        {
            var keybind = new Keybind(ID, Name, Key, KeyModifier);
            AddToKeybindsList(keybind);

            return keybind;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public class Save
        {
            public struct Value
            {
                public string ID;
                public KeyCode Key;
                public KeyCode Modifier;

                public Value (Keybind keybind)
                {
                    ID = keybind.ID;
                    Key = keybind.Key;
                    Modifier = keybind.Modifier;
                }
            }

            public List<Value> Keybinds = new List<Value>();
        }

        public class Keybind : ModSettings.IModSettingsElement
        {
            public string ID { get; }
            public string Name;
            public KeyCode Key { get; internal set; }
            public KeyCode Modifier { get; internal set; }
            internal KeyCode DefaultKey, DefaultModifier;

            internal Keybind (string ID, string Name, KeyCode Key, KeyCode KeyModifier)
            {
                this.ID = ID;
                this.Name = Name;
                this.Key = Key;
                Modifier = KeyModifier;
                DefaultKey = Key;
                DefaultModifier = KeyModifier;
            }

            [EditorBrowsable(EditorBrowsableState.Never)]
            public GameObject Create(object info)
            {
                var prefab = ModSettings.GetPrefab("Keybind");
                var go = GameObject.Instantiate<GameObject>(prefab);
                go.GetComponent<_Keybind>().keybind = (Keybind)info;

                return go;
            }
        }
        public class Header : ModSettings.Header { }
        internal class Button : ModSettings.Button { }

        internal void SaveKeybinds()
        {
            var save = new Save();

            for (var i = 0; i < keybindsElements.Count; i++) if (keybindsElements[i] is Keybind) save.Keybinds.Add(new Save.Value((Keybind)keybindsElements[i]));

            var config = new JsonSerializerSettings();
            config.Formatting = Formatting.Indented;
            config.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            var text = JsonConvert.SerializeObject(save, config);
            File.WriteAllText(Path.Combine(optionsFolderPath, "keybinds.json"), text);
        }
        internal void LoadKeybinds()
        {
            if (!File.Exists(Path.Combine(optionsFolderPath, "keybinds.json"))) return;
            var text = File.ReadAllText(Path.Combine(optionsFolderPath, "keybinds.json"));
            var save = JsonConvert.DeserializeObject<Save>(text);

            for (var i = 0; i < keybindsElements.Count; i++)
            {
                var element = keybindsElements[i];

                if (element != null && element is Keybind)
                {
                    var keybind = (Keybind)element;

                    for (var o = 0; o < save.Keybinds.Count; o++)
                    {
                        if (keybind.ID == save.Keybinds[o].ID)
                        {
                            keybind.Key = save.Keybinds[o].Key;
                            keybind.Modifier = save.Keybinds[o].Modifier;
                        }
                    }
                }
            }
        }
        public void LoadDefaults()
        {
            for (var i = 0; i < keybindsElements.Count; i++)
            {
                var element = keybindsElements[i];

                if (!(element is Keybind)) continue;

                var keybind = (Keybind)element;
                keybind.Key = keybind.DefaultKey;
                keybind.Modifier = keybind.DefaultModifier;
            }

            if (ModUIController.instance.settingsContainer.childCount > 0)
                for (var i = 0; i < ModUIController.instance.settingsContainer.childCount; i++)
                    GameObject.Destroy(ModUIController.instance.settingsContainer.GetChild(i).gameObject);
            ModUIController.instance.CreateKeybindsMenu(mod);
        }

        internal _Header currentHeader = null;
        internal ModSettings.Button defaultsButton = null;
    }

    internal class _Keybind : MonoBehaviour
    {
        public ModKeybinds.Keybind keybind;
        public TMPro.TextMeshProUGUI label, key, modifier;

        public void SetKey() => StartCoroutine(SetKeybind());
        public void SetModifier() => StartCoroutine(SetKeybind(true));

        IEnumerator SetKeybind(bool isModifier = false)
        {
            var rebind = true;
            var newKey = KeyCode.None;

            while (rebind)
            {
                if (Input.anyKeyDown)
                {
                    KeyCode[] keyCodes = (KeyCode[])Enum.GetValues(typeof(KeyCode));
                    for (var i = 0; i < keyCodes.Length; i++)
                    {
                        if (Input.GetKeyDown(keyCodes[i]))
                        {
                            if (keyCodes[i] != KeyCode.Mouse0)
                            {
                                if (keyCodes[i] == KeyCode.Mouse1) newKey = KeyCode.None;
                                else newKey = keyCodes[i];
                                break;
                            }
                        }
                    }

                    if (isModifier) keybind.Modifier = newKey;
                    else keybind.Key = newKey;
                    yield break;
                }

                yield return new WaitForEndOfFrame();
            }

            yield break;
        }

        void FixedUpdate()
        {
            if (label.text != keybind.Name) label.text = keybind.Name;
            if (key.text != keybind.Key.ToString()) key.text = keybind.Key.ToString();
            if (modifier.text != keybind.Modifier.ToString()) modifier.text = keybind.Modifier.ToString();
        }
    }
}