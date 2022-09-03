using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using ModUI.Internals;
using System.IO;
using Newtonsoft.Json;

namespace ModUI.Settings
{
    [CreateAssetMenu(fileName = "ModUI_SettingsPrefabHolder.asset", menuName = "ModUI/Create SettingsPrefabHolder")]
    public class SettingsPrefabHolder : ScriptableObject
    {
        public List<GameObject> prefabs;
    }

    public class SettingSaveTargetTypeAttribute : Attribute
    {
        internal Type type;
        public SettingSaveTargetTypeAttribute(Type type)
        {
            this.type = type;
        }
    }
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class IModSetttingsExpansion
    {
        public static string GetModSettingsFolder(this IModSettings modSettings)
        {
            if (!(modSettings is Mod)) throw new Exception("GetModSettingsFolder is for Mods only!");
            var mod = (Mod)modSettings;

            return Path.Combine(_ModUI.settingsPath, mod.ID);
        }
    }
    public interface IModSettings
    {
        void CreateModSettings(ModSettings modSettings);
        void ModSettingsLoaded();
    }

    public class ModSettings
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        public class Save
        {
            public struct Value
            {
                public string ID { get; set; }
                public object value { get; set; }

                public Value(ModSetting setting)
                {
                    ID = setting.ID;
                    value = setting.value;
                }
            }
            public bool enabled { get; set; }
            public List<Value> modSettings { get; set; } = new List<Value>();
        }

        internal Action LoadAction;
        internal void SaveSettings()
        {
            var save = new Save();

            save.enabled = mod.enabled;

            for (var i = 0; i < settingsElements.Count; i++)
            {
                var element = settingsElements[i];
                if (element != null && element is ModSetting) save.modSettings.Add(new Save.Value((ModSetting)element));
            }

            var config = new JsonSerializerSettings();
            config.Formatting = Formatting.Indented;
            config.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            var text = JsonConvert.SerializeObject(save, config);
            File.WriteAllText(Path.Combine(optionsFolderPath, "settings.json"), text);
        }
        internal void LoadSettings()
        {
            if (!File.Exists(Path.Combine(optionsFolderPath, "settings.json")))
            {
                for (var i = 0; i < settingsElements.Count; i++)
                {
                    var element = settingsElements[i];
                    if (element != null && element is ModSetting)
                    {
                        var modSetting = (ModSetting)element;
                        modSetting.value = modSetting.defaultValue;
                    }
                }

                LoadAction?.Invoke();
                return;
            }
            var text = File.ReadAllText(Path.Combine(optionsFolderPath, "settings.json"));
            dynamic save = JsonConvert.DeserializeObject<Save>(text);

            var modRefuseDisable = Attribute.GetCustomAttribute(mod.GetType(), typeof(ModRefuseDisable));

            mod.enabled = save.enabled || modRefuseDisable != null;

            for (var i = 0; i < settingsElements.Count; i++)
            {
                var element = settingsElements[i];
                if (element != null && element is ModSetting)
                {
                    var modSetting = (ModSetting)element;
                    for (var o = 0; o < save.modSettings.Count; o++)
                        if (modSetting.ID == save.modSettings[o].ID)
                        {
                            var attribute = (SettingSaveTargetTypeAttribute)Attribute.GetCustomAttribute(modSetting.GetType(), typeof(SettingSaveTargetTypeAttribute));
                            if (attribute != null) modSetting.value = JsonConvert.DeserializeObject(save.modSettings[o].value.ToString(), attribute.type);
                            else modSetting.value = save.modSettings[o].value;
                            if (modSetting.value == null) modSetting.value = modSetting.defaultValue;
                        }
                }
            }

            LoadAction?.Invoke();
        }
        public void LoadDefaults()
        {
            for (var i = 0; i < settingsElements.Count; i++)
            {
                var element = settingsElements[i];

                if (!(element is ModSetting)) continue;

                var modSetting = (ModSetting)element;

                modSetting.value = modSetting.defaultValue;
            }

            if (ModUIController.instance.settingsContainer.childCount > 0)
                for (var i = 0; i < ModUIController.instance.settingsContainer.childCount; i++)
                    GameObject.Destroy(ModUIController.instance.settingsContainer.GetChild(i).gameObject);
            if (ModUIController.history.Peek().menu == ModUIController.MenuType.Settings) ModUIController.instance.CreateSettingsMenu(mod);

            LoadAction?.Invoke();
        }

        internal Mod mod;
        internal string optionsFolderPath;

        internal static bool allowCreating = false;
        internal ModSettings() { }

        public void AddToSettingsList<T>(T element) where T : IModSettingsElement
        {
            if (!allowCreating) throw new Exception("ModUI.ModSettings: please create your Settings in the 'CreateModSettings' method!");
            settingsElements.Add(element);
        }

        public Header AddHeader(string text, bool open = true)
        {
            var header = new Header();
            header.Text = text;
            header.Active = open;
            AddToSettingsList(header);

            return header;
        }
        public Label AddLabel(string text)
        {
            var label = new Label();
            label.Text = text;
            AddToSettingsList(label);

            return label;
        }
        public Space AddSpace(float height = 35.41f)
        {
            var space = new Space();
            space.Amount = height;
            AddToSettingsList(space);

            return space;
        }
        public Button AddButton(string text, Action action)
        {
            var button = new Button();
            button.Text = text;
            button.OnClick += () => { action?.Invoke(); };
            AddToSettingsList(button);

            return button;
        }

        public Toggle AddToggle(string name, string ID, bool value, Action<bool> onValueChanged = null)
        {
            var field = new Toggle(name, ID, value, onValueChanged);
            AddToSettingsList(field);

            return field;
        }
        public RadioButtons AddRadioButtons(string name, string ID, int value, Action<int> onValueChanged = null, params string[] options)
        {
            var field = new RadioButtons(name, ID, value, options.ToList(), onValueChanged);
            AddToSettingsList(field);

            return field;
        }
        public Slider AddSlider(string name, string ID, float value, float minValue, float maxValue, Action<float> onValueChanged = null)
        {
            var field = new Slider(name, ID, value, minValue, maxValue, onValueChanged);
            AddToSettingsList(field);

            return field;
        }
        public SliderInt AddSliderInt(string name, string ID, int value, int minValue, int maxValue, Action<int> onValueChanged = null)
        {
            var field = new SliderInt(name, ID, value, minValue, maxValue, onValueChanged);
            AddToSettingsList(field);

            return field;
        }
        public Dropdown AddDropdown(string name, string ID, int value, Action<int> onValueChanged = null, params Dropdown.Option[] options)
        {
            var field = new Dropdown(name, ID, value, onValueChanged, options.ToList());
            AddToSettingsList(field);

            return field;
        }

        public DoubleField AddDoubleField(string name, string ID, double value, Action<double> onValueChanged = null)
        {
            var field = new DoubleField(name, ID, value, onValueChanged);
            AddToSettingsList(field);

            return field;
        }
        public FloatField AddFloatField(string name, string ID, float value, Action<float> onValueChanged = null)
        {
            var field = new FloatField(name, ID, value, onValueChanged);
            AddToSettingsList(field);

            return field;
        }
        public IntField AddIntField(string name, string ID, int value, Action<int> onValueChanged = null)
        {
            var field = new IntField(name, ID, value, onValueChanged);
            AddToSettingsList(field);

            return field;
        }
        public LongField AddLongField(string name, string ID, long value, Action<long> onValueChanged = null)
        {
            var field = new LongField(name, ID, value, onValueChanged);
            AddToSettingsList(field);

            return field;
        }
        public PasswordField AddPasswordField(string name, string ID, string value, Action<string> onValueChanged = null)
        {
            var field = new PasswordField(name, ID, value, onValueChanged);
            AddToSettingsList(field);

            return field;
        }
        public TextField AddTextField(string name, string ID, string value, Action<string> onValueChanged = null)
        {
            var field = new TextField(name, ID, value, onValueChanged);
            AddToSettingsList(field);

            return field;
        }
        public Vector2Field AddVector2Field(string name, string ID, Vector2 value, Action<Vector2> onValueChanged = null)
        {
            var field = new Vector2Field(name, ID, value, onValueChanged);
            AddToSettingsList(field);

            return field;
        }
        public Vector3Field AddVector3Field(string name, string ID, Vector3 value, Action<Vector3> onValueChanged = null)
        {
            var field = new Vector3Field(name, ID, value, onValueChanged);
            AddToSettingsList(field);

            return field;
        }
        public Vector4Field AddVector4Field(string name, string ID, Vector4 value, Action<Vector4> onValueChanged = null)
        {
            var field = new Vector4Field(name, ID, value, onValueChanged);
            AddToSettingsList(field);

            return field;
        }
        public RectField AddRectField(string name, string ID, Rect value, Action<Rect> onValueChanged = null)
        {
            var field = new RectField(name, ID, value, onValueChanged);
            AddToSettingsList(field);

            return field;
        }

        public static List<SettingsPrefabHolder> prefabHolders = new List<SettingsPrefabHolder>();

        /// <param name="name">Case Sensitive!</param>
        public static GameObject GetPrefab(string name)
        {
            GameObject prefab = null;
            for (var i = 0; i < prefabHolders.Count; i++)
            {
                for (var o = 0; o < prefabHolders[i].prefabs.Count; o++)
                {
                    if (name == prefabHolders[i].prefabs[o].name) prefab = prefabHolders[i].prefabs[o];
                }
            }
            if (prefab == null) throw new Exception($"ModUI.ModSettings : SettingsPrefab {name} Missing!");

            return prefab;
        }

        internal static Dictionary<Mod, ModSettings> modSettings = new Dictionary<Mod, ModSettings>();

        internal List<IModSettingsElement> settingsElements = new List<IModSettingsElement>();

        public interface IModSettingsElement { [EditorBrowsable(EditorBrowsableState.Never)]GameObject Create(object info); }
        public abstract class ModSetting
        {
            public string Name { get; }
            public string ID { get; internal set; }
            internal object value { get; set; }
            internal object defaultValue { get; set; }

            public ModSetting (string name, string ID, object defaultValue)
            {
                Name = name;
                this.ID = ID;
                this.defaultValue = defaultValue;
                value = defaultValue;
            }
        }

        public class Header : IModSettingsElement
        {
            public string Text;
            public bool Active { get; internal set; } = true;

            [EditorBrowsable(EditorBrowsableState.Never)]
            public GameObject Create(object info)
            {
                var prefab = GetPrefab("Header");
                var go = GameObject.Instantiate<GameObject>(prefab);
                go.GetComponent<_Header>().header = (Header)info;

                return go;
            }
        }
        public class Label : IModSettingsElement
        {
            public string Text;

            [EditorBrowsable(EditorBrowsableState.Never)]
            public GameObject Create(object info)
            {
                var prefab = GetPrefab("Label");
                var go = GameObject.Instantiate<GameObject>(prefab);
                go.GetComponent<_Label>().label = (Label)info;

                return go;
            }

        }
        public class Space : IModSettingsElement
        {
            public float Amount;

            [EditorBrowsable(EditorBrowsableState.Never)]
            public GameObject Create(object info)
            {
                var prefab = GetPrefab("Space");
                var go = GameObject.Instantiate<GameObject>(prefab);
                go.GetComponent<_Space>().space = (Space)info;

                return go;
            }
        }
        public class Button : IModSettingsElement
        {
            public string Text;
            public Action OnClick;

            [EditorBrowsable(EditorBrowsableState.Never)]
            public GameObject Create(object info)
            {
                var prefab = GetPrefab("Button");
                var go = GameObject.Instantiate<GameObject>(prefab);
                go.GetComponent<_Button>().button = (Button)info;

                return go;
            }
        }

        public class Toggle : ModSetting, IModSettingsElement
        {
            public bool Value
            {
                get
                {
                    if (value == null) return bool.Parse(defaultValue.ToString()); ;
                    return bool.Parse(value.ToString());
                }
            }
            public Action<bool> OnValueChanged;
            public Toggle(string name, string ID, bool defaultValue, Action<bool> onValueChanged) : base(name, ID, defaultValue) { OnValueChanged = onValueChanged; }

            [EditorBrowsable(EditorBrowsableState.Never)]
            public GameObject Create(object info)
            {
                var prefab = GetPrefab("Toggle");
                var go = GameObject.Instantiate<GameObject>(prefab);
                go.GetComponent<_Toggle>().toggle = (Toggle)info;

                return go;
            }
        }
        public class RadioButtons : ModSetting, IModSettingsElement
        {
            public List<string> Options { get; internal set; } = new List<string>();

            public int Value
            {
                get
                {
                    if (value == null) return int.Parse(defaultValue.ToString());
                    return int.Parse(value.ToString());
                }
            }
            public Action<int> OnValueChanged;
            public RadioButtons(string name, string ID, int defaultValue, List<string> options, Action<int> onValueChanged) : base(name, ID, defaultValue) { Options = options; OnValueChanged = onValueChanged; }

            [EditorBrowsable(EditorBrowsableState.Never)]
            public GameObject Create(object info)
            {
                var prefab = GetPrefab("RadioButtons");
                var go = GameObject.Instantiate<GameObject>(prefab);
                go.GetComponent<_RadioButtons>().radioButtons = (RadioButtons)info;

                return go;
            }
        }
        public class Slider : ModSetting, IModSettingsElement
        {
            public float Value
            {
                get
                {
                    if (value == null) return float.Parse(defaultValue.ToString());
                    return float.Parse(value.ToString());
                }
            }
            public float MinValue { get; internal set; }
            public float MaxValue { get; internal set; }

            public Action<float> OnValueChanged;
            public Slider(string name, string ID, float defaultValue, float minValue, float maxValue, Action<float> onValueChanged) : base(name, ID, defaultValue) { OnValueChanged = onValueChanged; MinValue = minValue; MaxValue = maxValue; }

            [EditorBrowsable(EditorBrowsableState.Never)]
            public GameObject Create(object info)
            {
                var prefab = GetPrefab("Slider");
                var go = GameObject.Instantiate<GameObject>(prefab);
                go.GetComponent<_Slider>().slider = (Slider)info;

                return go;
            }
        }
        public class SliderInt : ModSetting, IModSettingsElement
        {
            public int Value
            {
                get
                {
                    if (value == null) return int.Parse(defaultValue.ToString());
                    return int.Parse(value.ToString());
                }
            }
            public int MinValue { get; internal set; }
            public int MaxValue { get; internal set; }

            public Action<int> OnValueChanged;
            public SliderInt(string name, string ID, int defaultValue, int minValue, int maxValue, Action<int> onValueChanged) : base(name, ID, defaultValue) { OnValueChanged = onValueChanged; MinValue = minValue; MaxValue = maxValue; }

            [EditorBrowsable(EditorBrowsableState.Never)]
            public GameObject Create(object info)
            {
                var prefab = GetPrefab("SliderInt");
                var go = GameObject.Instantiate<GameObject>(prefab);
                go.GetComponent<_SliderInt>().slider = (SliderInt)info;

                return go;
            }
        }
        public class Dropdown : ModSetting, IModSettingsElement
        {
            public struct Option
            {
                public string Text;
                public Sprite Image;

                private Option(string text, Sprite image)
                {
                    Text = text;
                    Image = image;
                }

                public static Option Create(string text, Sprite image = null) => new Option(text, image);
                public static Option Create(string text, Texture2D texture, Rect rect, Vector2 pivot, float pixelsPerUnit, uint extrude, SpriteMeshType meshType, Vector4 border, bool generateFallbackPhysicsShape) => Create(text, Sprite.Create(texture, rect, pivot, pixelsPerUnit, extrude, meshType, border, generateFallbackPhysicsShape));
                public static Option Create(string text, Texture2D texture, Rect rect, Vector2 pivot, float pixelsPerUnit, uint extrude, SpriteMeshType meshType, Vector4 border) => Create(text, texture, rect, pivot, pixelsPerUnit, extrude, meshType, border, generateFallbackPhysicsShape: false);
                public static Option Create(string text, Texture2D texture, Rect rect, Vector2 pivot, float pixelsPerUnit, uint extrude, SpriteMeshType meshType) => Create(text, texture, rect, pivot, pixelsPerUnit, extrude, meshType, Vector4.zero);
                public static Option Create(string text, Texture2D texture, Rect rect, Vector2 pivot, float pixelsPerUnit, uint extrude) => Create(text, texture, rect, pivot, pixelsPerUnit, extrude, SpriteMeshType.Tight);
                public static Option Create(string text, Texture2D texture, Rect rect, Vector2 pivot, float pixelsPerUnit) => Create(text, texture, rect, pivot, pixelsPerUnit, 0u);
                public static Option Create(string text, Texture2D texture, Rect rect, Vector2 pivot) => Create(text, texture, rect, pivot, 100f);
            }
            public List<Option> Options;

            public int Value
            {
                get
                {
                    if (value == null) return int.Parse(defaultValue.ToString());
                    return int.Parse(value.ToString());
                }
            }

            public Action<int> OnValueChanged;
            public Dropdown(string name, string ID, int defaultValue, Action<int> onValueChanged, List<Option> options) : base(name, ID, defaultValue) { OnValueChanged = onValueChanged; Options = options; }

            [EditorBrowsable(EditorBrowsableState.Never)]
            public GameObject Create(object info)
            {
                var prefab = GetPrefab("Dropdown");
                var go = GameObject.Instantiate<GameObject>(prefab);
                go.GetComponent<_Dropdown>().dropdown = (Dropdown)info;

                return go;
            }
        }

        public class DoubleField : ModSetting, IModSettingsElement
        {
            public double Value { 
                get {
                    var result = 0.0;
                    if (value == null) return (double)defaultValue;
                    if (double.TryParse(value.ToString(), out result)) return result;
                    return (double)defaultValue;
                }
            }
            public Action<double> OnValueChanged;
            public DoubleField(string name, string ID, double defaultValue, Action<double> onValueChanged) : base(name, ID, defaultValue) { OnValueChanged = onValueChanged; }

            [EditorBrowsable(EditorBrowsableState.Never)]
            public GameObject Create(object info)
            {
                var prefab = GetPrefab("DoubleField");
                var go = GameObject.Instantiate<GameObject>(prefab);
                go.GetComponent<_DoubleField>().doubleField = (DoubleField)info;

                return go;
            }
        }
        public class FloatField : ModSetting, IModSettingsElement
        {
            public float Value
            {
                get
                {
                    var result = 0.0f;
                    if (value == null) return (float)defaultValue;
                    if (float.TryParse(value.ToString(), out result)) return result;
                    return (float)defaultValue;
                }
            }
            public Action<float> OnValueChanged;
            public FloatField(string name, string ID, float defaultValue, Action<float> onValueChanged) : base(name, ID, defaultValue) { OnValueChanged = onValueChanged; }

            [EditorBrowsable(EditorBrowsableState.Never)]
            public GameObject Create(object info)
            {
                var prefab = GetPrefab("FloatField");
                var go = GameObject.Instantiate<GameObject>(prefab);
                go.GetComponent<_FloatField>().floatField = (FloatField)info;

                return go;
            }
        }
        public class IntField : ModSetting, IModSettingsElement
        {
            public int Value
            {
                get
                {
                    var result = 0;
                    if (value == null) return (int)defaultValue;
                    if (int.TryParse(value.ToString(), out result)) return result;
                    return (int)defaultValue;
                }
            }
            public Action<int> OnValueChanged;
            public IntField(string name, string ID, int defaultValue, Action<int> onValueChanged) : base(name, ID, defaultValue) { OnValueChanged = onValueChanged; }

            [EditorBrowsable(EditorBrowsableState.Never)]
            public GameObject Create(object info)
            {
                var prefab = GetPrefab("IntField");
                var go = GameObject.Instantiate<GameObject>(prefab);
                go.GetComponent<_IntField>().intField = (IntField)info;

                return go;
            }
        }
        public class LongField : ModSetting, IModSettingsElement
        {
            public long Value
            {
                get
                {
                    long result = 0;
                    if (value == null) return (long)defaultValue;
                    if (long.TryParse(value.ToString(), out result)) return result;
                    return (long)defaultValue;
                }
            }
            public Action<long> OnValueChanged;
            public LongField(string name, string ID, long defaultValue, Action<long> onValueChanged) : base(name, ID, defaultValue) { OnValueChanged = onValueChanged; }

            [EditorBrowsable(EditorBrowsableState.Never)]
            public GameObject Create(object info)
            {
                var prefab = GetPrefab("LongField");
                var go = GameObject.Instantiate<GameObject>(prefab);
                go.GetComponent<_LongField>().longField = (LongField)info;

                return go;
            }
        }
        public class PasswordField : ModSetting, IModSettingsElement
        {
            public string Value
            {
                get
                {
                    if (value == null) return defaultValue.ToString();
                    return value.ToString();
                }
            }
            public Action<string> OnValueChanged;
            public PasswordField(string name, string ID, string defaultValue, Action<string> onValueChanged) : base(name, ID, defaultValue) 
            { 
                OnValueChanged = onValueChanged; 
                this.defaultValue = Encoding.UTF8.GetBytes(defaultValue);
                this.value = Encoding.UTF8.GetBytes(defaultValue);
            }

            [EditorBrowsable(EditorBrowsableState.Never)]
            public GameObject Create(object info)
            {
                var prefab = GetPrefab("PasswordField");
                var go = GameObject.Instantiate<GameObject>(prefab);
                go.GetComponent<_PasswordField>().passwordField = (PasswordField)info;

                return go;
            }
        }
        public class TextField : ModSetting, IModSettingsElement
        {
            public string Value
            {
                get
                {
                    if (value == null) return (string)defaultValue;
                    return (string)value;
                }
            }
            public Action<string> OnValueChanged;
            public TextField(string name, string ID, string defaultValue, Action<string> onValueChanged) : base(name, ID, defaultValue) { OnValueChanged = onValueChanged; }

            [EditorBrowsable(EditorBrowsableState.Never)]
            public GameObject Create(object info)
            {
                var prefab = GetPrefab("TextField");
                var go = GameObject.Instantiate<GameObject>(prefab);
                go.GetComponent<_TextField>().textField = (TextField)info;

                return go;
            }
        }
        [SettingSaveTargetType(typeof(Vector2))] public class Vector2Field : ModSetting, IModSettingsElement
        {
            public Vector2 Value
            {
                get
                {
                    if (value == null) return (Vector2)defaultValue;
                    return (Vector2)value;
                }
            }
            public Action<Vector2> OnValueChanged;
            public Vector2Field(string name, string ID, Vector2 defaultValue, Action<Vector2> onValueChanged) : base(name, ID, defaultValue) { OnValueChanged = onValueChanged; }

            [EditorBrowsable(EditorBrowsableState.Never)]
            public GameObject Create(object info)
            {
                var prefab = GetPrefab("Vector2Field");
                var go = GameObject.Instantiate<GameObject>(prefab);
                go.GetComponent<_Vector2Field>().vector2Field = (Vector2Field)info;

                return go;
            }
        }
        [SettingSaveTargetType(typeof(Vector3))] public class Vector3Field : ModSetting, IModSettingsElement
        {
            public Vector3 Value
            {
                get
                {
                    if (value == null) return (Vector3)defaultValue;
                    return (Vector3)value;
                }
            }
            public Action<Vector3> OnValueChanged;
            public Vector3Field(string name, string ID, Vector3 defaultValue, Action<Vector3> onValueChanged) : base(name, ID, defaultValue) { OnValueChanged = onValueChanged; }

            [EditorBrowsable(EditorBrowsableState.Never)]
            public GameObject Create(object info)
            {
                var prefab = GetPrefab("Vector3Field");
                var go = GameObject.Instantiate<GameObject>(prefab);
                go.GetComponent<_Vector3Field>().vector3Field = (Vector3Field)info;

                return go;
            }
        }
        [SettingSaveTargetType(typeof(Vector4))] public class Vector4Field : ModSetting, IModSettingsElement
        {
            public Vector4 Value
            {
                get
                {
                    if (value == null) return (Vector4)defaultValue;
                    return (Vector4)value;
                }
            }
            public Action<Vector4> OnValueChanged;
            public Vector4Field(string name, string ID, Vector4 defaultValue, Action<Vector4> onValueChanged) : base(name, ID, defaultValue) { OnValueChanged = onValueChanged; }

            [EditorBrowsable(EditorBrowsableState.Never)]
            public GameObject Create(object info)
            {
                var prefab = GetPrefab("Vector4Field");
                var go = GameObject.Instantiate<GameObject>(prefab);
                go.GetComponent<_Vector4Field>().vector4Field = (Vector4Field)info;

                return go;
            }
        }
        [SettingSaveTargetType(typeof(Rect))] public class RectField : ModSetting, IModSettingsElement
        {
            public Rect Value
            {
                get
                {
                    if (value == null) return (Rect)defaultValue;
                    return (Rect)value;
                }
            }
            public Action<Rect> OnValueChanged;
            public RectField(string name, string ID, Rect defaultValue, Action<Rect> onValueChanged) : base(name, ID, defaultValue) { OnValueChanged = onValueChanged; }

            [EditorBrowsable(EditorBrowsableState.Never)]
            public GameObject Create(object info)
            {
                var prefab = GetPrefab("RectField");
                var go = GameObject.Instantiate<GameObject>(prefab);
                go.GetComponent<_RectField>().rectField = (RectField)info;

                return go;
            }
        }

        internal _Header currentHeader = null;
        internal Button defaultsButton = null;
    }

    internal class _Header : MonoBehaviour
    {
        public ModSettings.Header header;
        public TMPro.TextMeshProUGUI text;
        public Toggle toggle;
        public RectTransform arrow;
        public Vector3 rotOn = Vector3.zero;
        public Vector3 rotOff = Vector3.forward * 180f;
        internal List<GameObject> children = new List<GameObject>();

        void Start()
        {
            toggle.isOn = header.Active;
            ChangeActive(header.Active);
            toggle.onValueChanged.AddListener(
                (bool value) => {
                    header.Active = value;

                    ChangeActive(value);
                }
            );
        }

        void ChangeActive(bool value)
        {
            for (var i = 0; i < children.Count; i++)
            {
                var child = children[i];

                child.SetActive(value);
            }
        }

        void FixedUpdate()
        {
            if (text.text != header.Text) text.text = header.Text;
            if (arrow.localEulerAngles != (header.Active ? rotOn : rotOff))
                arrow.localEulerAngles = header.Active ? rotOn : rotOff;
        }
    }
    internal class _Label : MonoBehaviour
    {
        public ModSettings.Label label;
        public TMPro.TextMeshProUGUI text;

        void FixedUpdate()
        {
            if (text.text != label.Text) text.text = label.Text;
        }
    }
    internal class _Space : MonoBehaviour
    {
        public ModSettings.Space space;
        public RectTransform spacer;

        void FixedUpdate()
        {
            if (spacer.sizeDelta.y != space.Amount)
            {
                var sizeDelta = spacer.sizeDelta;
                sizeDelta.y = space.Amount;
                spacer.sizeDelta = sizeDelta;
            }
        }
    }
    internal class _Button : MonoBehaviour
    {
        public ModSettings.Button button;
        public Button btn;
        public TMPro.TextMeshProUGUI text;

        public void OnClick()
        {
            button.OnClick?.Invoke();
        }

        void FixedUpdate()
        {
            if (text.text != button.Text) text.text = button.Text;
        }
    }

    internal class _Toggle : MonoBehaviour
    {
        public ModSettings.Toggle toggle;
        public TMPro.TextMeshProUGUI label;
        public Toggle _toggle;

        void Start()
        {
            _toggle.isOn = toggle.Value;
            _toggle.onValueChanged.AddListener((bool value) => { toggle.value = value; toggle.OnValueChanged?.Invoke(value); });
        }

        void FixedUpdate()
        {
            if (label.text != toggle.Name) label.text = toggle.Name;
        }
    }
    internal class _RadioButton : MonoBehaviour
    {
        public int ID;
        public TMPro.TextMeshProUGUI label;
        public Toggle toggle;
    }
    internal class _RadioButtons : MonoBehaviour
    {
        internal List<string> Options = new List<string>();
        public ModSettings.RadioButtons radioButtons;
        public TMPro.TextMeshProUGUI label;
        public ToggleGroup _toggleGroup;

        public RectTransform holder, labelRect, thisRect;
        ContentSizeFitter sizeFitter;
        public List<_RadioButton> _radioButtons = new List<_RadioButton>();
        public GameObject radioButtonPrefab;

        void Start()
        {
            sizeFitter = holder.GetComponent<ContentSizeFitter>();
            sizeFitter.enabled = false;
            sizeFitter.enabled = true;
        }

        void OnValueChanged(int value)
        {
            radioButtons.value = value;
            radioButtons.OnValueChanged?.Invoke(value);
        }

        void FixedUpdate()
        {
            var delta = thisRect.sizeDelta;
            delta.y = holder.sizeDelta.y + labelRect.sizeDelta.y;
            thisRect.sizeDelta = delta;

            if (label.text != radioButtons.Name + ":") label.text = radioButtons.Name + ":";
            if (Options != radioButtons.Options)
            {
                if (_radioButtons.Count > 0)
                {
                    for (var i = 0; i < _radioButtons.Count; i++)
                    {
                        var radioButton = _radioButtons[i];
                        Destroy(radioButton.gameObject);
                    }
                }
                _radioButtons = new List<_RadioButton>();

                Options = radioButtons.Options;
                for (var i = 0; i < Options.Count; i++)
                {
                    var go = Instantiate<GameObject>(radioButtonPrefab);
                    var radio = go.GetComponent<_RadioButton>();

                    radio.ID = i;
                    radio.label.text = Options[i];
                    radio.toggle.group = _toggleGroup;
                    radio.toggle.isOn = false;
                    radio.toggle.onValueChanged.AddListener((bool value) => { if (value) OnValueChanged(radio.ID); });

                    radio.transform.SetParent(holder.transform, false);
                    _radioButtons.Add(radio);
                }

                for (var i = 0; i < _radioButtons.Count; i++)
                {
                    _radioButtons[i].toggle.isOn = radioButtons.Value == i; ;
                }
            }
        }
    }
    internal class _Slider : MonoBehaviour
    {
        public ModSettings.Slider slider;
        public TMPro.TextMeshProUGUI label;
        public Slider _slider;
        public TMPro.TMP_InputField input;

        void Start()
        {
            _slider.minValue = slider.MinValue;
            _slider.maxValue = slider.MaxValue;
            _slider.value = slider.Value;
            _slider.onValueChanged.AddListener((float value) => { input.text = value.ToString(); slider.value = value; slider.OnValueChanged?.Invoke(value); });

            input.text = slider.Value.ToString();
            input.onEndEdit.AddListener((string value) => { var val = float.Parse(value.ToString()); slider.value = val; _slider.value = val; slider.OnValueChanged?.Invoke(val); });
        }

        void FixedUpdate()
        {
            if (label.text != slider.Name + ":") label.text = slider.Name + ":";
        }
    }
    internal class _SliderInt : MonoBehaviour
    {
        public ModSettings.SliderInt slider;
        public TMPro.TextMeshProUGUI label;
        public Slider _slider;
        public TMPro.TMP_InputField input;

        void Start()
        {
            _slider.minValue = slider.MinValue;
            _slider.maxValue = slider.MaxValue;
            _slider.value = slider.Value;
            _slider.onValueChanged.AddListener((float value) => { var val = Mathf.RoundToInt(value); input.text = val.ToString(); slider.value = val; slider.OnValueChanged?.Invoke(val); });

            input.text = slider.Value.ToString();
            input.onEndEdit.AddListener((string value) => { var val = Mathf.RoundToInt(float.Parse(value.ToString())); _slider.value = val; slider.OnValueChanged?.Invoke(val); });
        }

        void FixedUpdate()
        {
            if (label.text != slider.Name + ":") label.text = slider.Name + ":";
        }
    }
    internal class _Dropdown : MonoBehaviour
    {
        public List<ModSettings.Dropdown.Option> options;
        public ModSettings.Dropdown dropdown;
        public TMPro.TextMeshProUGUI label;
        public TMPro.TMP_Dropdown _dropdown;

        void Start()
        {
            _dropdown.value = dropdown.Value;
            _dropdown.onValueChanged.AddListener((int value) => { dropdown.value = value; dropdown.OnValueChanged?.Invoke(value); });
        }

        void FixedUpdate()
        {
            if (label.text != dropdown.Name + ":") label.text = dropdown.Name + ":";
            if (options != dropdown.Options)
            {
                options = dropdown.Options;

                _dropdown.ClearOptions();
                var tmpDropdownOptions = new List<TMPro.TMP_Dropdown.OptionData>();

                foreach (var option in options)
                {
                    var tmpDropdownOption = new TMPro.TMP_Dropdown.OptionData();
                    tmpDropdownOption.text = option.Text;
                    tmpDropdownOption.image = option.Image;

                    tmpDropdownOptions.Add(tmpDropdownOption);
                }

                _dropdown.AddOptions(tmpDropdownOptions);
                _dropdown.value = dropdown.Value;
            }
        }
    }

    internal class _DoubleField : MonoBehaviour
    {
        public ModSettings.DoubleField doubleField;
        public TMPro.TextMeshProUGUI label;
        public TMPro.TMP_InputField field;

        void Start()
        {
            field.text = doubleField.Value.ToString();

            field.onValueChanged.AddListener((string value) => { doubleField.value = value; });
            field.onEndEdit.AddListener((string value) => { doubleField.value = value; doubleField.OnValueChanged?.Invoke(double.Parse(value)); });
        }

        void FixedUpdate()
        {
            if (label.text != doubleField.Name + ":") label.text = doubleField.Name + ":";
        }
    }
    internal class _FloatField : MonoBehaviour
    {
        public ModSettings.FloatField floatField;
        public TMPro.TextMeshProUGUI label;
        public TMPro.TMP_InputField field;

        void Start()
        {
            field.text = floatField.Value.ToString();

            field.onValueChanged.AddListener((string value) => { floatField.value = value; });
            field.onEndEdit.AddListener((string value) => { floatField.value = value; floatField.OnValueChanged?.Invoke(float.Parse(value)); });
        }

        void FixedUpdate()
        {
            if (label.text != floatField.Name + ":") label.text = floatField.Name + ":";
        }
    }
    internal class _IntField : MonoBehaviour
    {
        public ModSettings.IntField intField;
        public TMPro.TextMeshProUGUI label;
        public TMPro.TMP_InputField field;

        void Start()
        {
            field.text = intField.Value.ToString();

            field.onValueChanged.AddListener((string value) => { intField.value = value; });
            field.onEndEdit.AddListener((string value) => { intField.value = value; intField.OnValueChanged?.Invoke(int.Parse(value)); });
        }

        void FixedUpdate()
        {
            if (label.text != intField.Name + ":") label.text = intField.Name + ":";
        }
    }
    internal class _LongField : MonoBehaviour
    {
        public ModSettings.LongField longField;
        public TMPro.TextMeshProUGUI label;
        public TMPro.TMP_InputField field;

        void Start()
        {
            field.text = longField.Value.ToString();

            field.onValueChanged.AddListener((string value) => { longField.value = value; });
            field.onEndEdit.AddListener((string value) => { longField.value = value; longField.OnValueChanged?.Invoke(long.Parse(value)); });
        }

        void FixedUpdate()
        {
            if (label.text != longField.Name + ":") label.text = longField.Name + ":";
        }
    }
    internal class _PasswordField : MonoBehaviour
    {
        public ModSettings.PasswordField passwordField;
        public TMPro.TextMeshProUGUI label;
        public TMPro.TMP_InputField field;

        void Start()
        {
            field.text = passwordField.Value.ToString();

            field.onValueChanged.AddListener((string value) => { passwordField.value = value; });
            field.onEndEdit.AddListener((string value) => { passwordField.value = value; passwordField.OnValueChanged?.Invoke(value); });
        }

        void FixedUpdate()
        {
            if (label.text != passwordField.Name + ":") label.text = passwordField.Name + ":";
        }
    }
    internal class _TextField : MonoBehaviour
    {
        public ModSettings.TextField textField;
        public TMPro.TextMeshProUGUI label;
        public TMPro.TMP_InputField field;

        void Start()
        {
            field.text = textField.Value.ToString();

            field.onValueChanged.AddListener((string value) => { textField.value = value; });
            field.onEndEdit.AddListener((string value) => { textField.value = value; textField.OnValueChanged?.Invoke(value); });
        }

        void FixedUpdate()
        {
            if (label.text != textField.Name + ":") label.text = textField.Name + ":";
        }
    }
    internal class _Vector2Field : MonoBehaviour
    {
        public ModSettings.Vector2Field vector2Field;
        public TMPro.TextMeshProUGUI label;
        public TMPro.TMP_InputField x;
        public TMPro.TMP_InputField y;

        void Start()
        {
            x.text = vector2Field.Value.x.ToString();
            y.text = vector2Field.Value.y.ToString();

            x.onValueChanged.AddListener(OnValueChanged);
            x.onEndEdit.AddListener(OnEndEdit);

            y.onValueChanged.AddListener(OnValueChanged);
            y.onEndEdit.AddListener(OnEndEdit);
        }

        public void OnValueChanged(string value)
        {
            var vector = new Vector2();

            vector.x = float.Parse(x.text);
            vector.y = float.Parse(y.text);

            vector2Field.value = vector;
        }

        public void OnEndEdit(string value)
        {
            OnValueChanged(value);
            vector2Field.OnValueChanged?.Invoke(vector2Field.Value);
        }

        void FixedUpdate()
        {
            if (label.text != vector2Field.Name + ":") label.text = vector2Field.Name + ":";
        }
    }
    internal class _Vector3Field : MonoBehaviour
    {
        public ModSettings.Vector3Field vector3Field;
        public TMPro.TextMeshProUGUI label;
        public TMPro.TMP_InputField x;
        public TMPro.TMP_InputField y;
        public TMPro.TMP_InputField z;

        void Start()
        {
            x.text = vector3Field.Value.x.ToString();
            y.text = vector3Field.Value.y.ToString();
            z.text = vector3Field.Value.z.ToString();

            x.onValueChanged.AddListener(OnValueChanged);
            x.onEndEdit.AddListener(OnEndEdit);

            y.onValueChanged.AddListener(OnValueChanged);
            y.onEndEdit.AddListener(OnEndEdit);

            z.onValueChanged.AddListener(OnValueChanged);
            z.onEndEdit.AddListener(OnEndEdit);
        }

        public void OnValueChanged(string value)
        {
            var vector = new Vector3();

            vector.x = float.Parse(x.text);
            vector.y = float.Parse(y.text);
            vector.z = float.Parse(z.text);

            vector3Field.value = vector;
        }

        public void OnEndEdit(string value)
        {
            OnValueChanged(value);
            vector3Field.OnValueChanged?.Invoke(vector3Field.Value);
        }

        void FixedUpdate()
        {
            if (label.text != vector3Field.Name + ":") label.text = vector3Field.Name + ":";
        }
    }
    internal class _Vector4Field : MonoBehaviour
    {
        public ModSettings.Vector4Field vector4Field;
        public TMPro.TextMeshProUGUI label;
        public TMPro.TMP_InputField x;
        public TMPro.TMP_InputField y;
        public TMPro.TMP_InputField z;
        public TMPro.TMP_InputField w;

        void Start()
        {
            x.text = vector4Field.Value.x.ToString();
            y.text = vector4Field.Value.y.ToString();
            z.text = vector4Field.Value.z.ToString();
            w.text = vector4Field.Value.w.ToString();

            x.onValueChanged.AddListener(OnValueChanged);
            x.onEndEdit.AddListener(OnEndEdit);

            y.onValueChanged.AddListener(OnValueChanged);
            y.onEndEdit.AddListener(OnEndEdit);

            z.onValueChanged.AddListener(OnValueChanged);
            z.onEndEdit.AddListener(OnEndEdit);

            w.onValueChanged.AddListener(OnValueChanged);
            w.onEndEdit.AddListener(OnEndEdit);
        }

        public void OnValueChanged(string value)
        {
            var vector = new Vector4();

            vector.x = float.Parse(x.text);
            vector.y = float.Parse(y.text);
            vector.z = float.Parse(z.text);
            vector.w = float.Parse(w.text);

            vector4Field.value = vector;
        }

        public void OnEndEdit(string value)
        {
            OnValueChanged(value);
            vector4Field.OnValueChanged?.Invoke(vector4Field.Value);
        }

        void FixedUpdate()
        {
            if (label.text != vector4Field.Name + ":") label.text = vector4Field.Name + ":";
        }
    }
    internal class _RectField : MonoBehaviour
    {
        public ModSettings.RectField rectField;
        public TMPro.TextMeshProUGUI label;
        public TMPro.TMP_InputField x;
        public TMPro.TMP_InputField y;
        public TMPro.TMP_InputField w;
        public TMPro.TMP_InputField h;

        void Start()
        {
            x.text = rectField.Value.x.ToString();
            y.text = rectField.Value.y.ToString();
            w.text = rectField.Value.width.ToString();
            h.text = rectField.Value.height.ToString();

            x.onValueChanged.AddListener(OnValueChanged);
            x.onEndEdit.AddListener(OnEndEdit);

            y.onValueChanged.AddListener(OnValueChanged);
            y.onEndEdit.AddListener(OnEndEdit);

            w.onValueChanged.AddListener(OnValueChanged);
            w.onEndEdit.AddListener(OnEndEdit);

            h.onValueChanged.AddListener(OnValueChanged);
            h.onEndEdit.AddListener(OnEndEdit);
        }

        public void OnValueChanged(string value)
        {
            var rect = new Rect();

            rect.x = float.Parse(x.text);
            rect.y = float.Parse(y.text);
            rect.width = float.Parse(w.text);
            rect.height = float.Parse(h.text);

            rectField.value = rect;
        }

        public void OnEndEdit(string value)
        {
            OnValueChanged(value);
            rectField.OnValueChanged?.Invoke(rectField.Value);
        }

        void FixedUpdate()
        {
            if (label.text != rectField.Name + ":") label.text = rectField.Name + ":";
        }
    }
}
