using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ModUI
{
    public class ModConsole : MonoBehaviour
    {
        public abstract class Command
        {
            public abstract string Help { get; }
            public abstract bool ShowInHelp { get; }
            public abstract void Run(string[] args);
            public static void Add(string name, Command command) => commands.Add(name.ToLower(), command);
        }

        internal class HelpCommand : Command
        {
            public override string Help => "this Command!";

            public override bool ShowInHelp => true;

            public override void Run(string[] args)
            {
                var help = new List<string>();

                help.Add("<color=#2ECC71><b>available Commands:</b></color>");

                foreach (var kv in commands)
                {
                    if (kv.Value.ShowInHelp) help.Add($"<color=#16A085><b>{kv.Key}</b></color>: {kv.Value.Help}");
                    else continue;
                }

                Log(string.Join("\n", help.ToArray()));
            }
        }
        internal class ClearCommand : Command
        {
            public override string Help => "clears the Console";

            public override bool ShowInHelp => true;

            public override void Run(string[] args)
            {
                ClearConsole();
            }
        }

        internal static Dictionary<string, Command> commands = new Dictionary<string, Command>();
        internal static ModConsole instance;
        [SerializeField] internal TMPro.TextMeshProUGUI log;
        [SerializeField] internal TMPro.TMP_InputField commandField;
        [SerializeField] internal GameObject consoleWindow;
        internal RectTransform rectWindow;
        internal static Vector2 size;

        internal static List<string> bannedBehaviours = new List<string>()
        {
            "GleyTrafficSystem"
        };

        internal static Queue<string> logData = new Queue<string>();
        internal static int maxLogData = 20;

        internal static bool openOnError = true;
        internal static bool openOnWarning = true;

        bool toggle = false;
        static bool writeToLog = false;

        public void ToggleConsole()
        {
            toggle = !toggle;
            consoleWindow.SetActive(toggle);
            ModUIController.instance.eventSystem.SetActive(toggle || ModUIController.instance.canvas.activeSelf);
            MenuHelper.SetInteractMenu(toggle);
        }
        public void Run()
        {
            ExecuteCommand(commandField.text);
            commandField.text = "";
        }

        public static void ExecuteCommand(string command)
        {
            if (command == "") return;

            var args = command.Split(' ').ToList();
            var cmdName = args[0].ToLower();
            if (args.Count > 1) args.RemoveAt(0);

            if (!commands.ContainsKey(args[0])) { LogError($"ModUI.ModConsole: Command '{args[0]}' doesn't exist!"); return; }

            commands[cmdName].Run(args.ToArray());
        }
        public static void ClearConsole()
        {
            logData.Clear();
            instance.UpdateLog();
        }

        void FixedUpdate()
        {
            if (rectWindow.sizeDelta != size) rectWindow.sizeDelta = size;
        }

        void Start()
        {
            instance = this;

            Command.Add("help", new HelpCommand());
            Command.Add("clear", new ClearCommand());
            log.text = "";

            Application.logMessageReceived += (string condition, string stackTrace, LogType type) =>
            {
                for (var i = 0; i < bannedBehaviours.Count; i++)
                    if 
                    (
                        condition.ToLower().Contains(bannedBehaviours[i].ToLower()) ||
                        stackTrace.ToLower().Contains(bannedBehaviours[i].ToLower())
                    ) return;
                var text = $"{condition} {(stackTrace != "" ? "\n" + stackTrace : "")}";
                writeToLog = false;

                switch (type)
                {
                    case LogType.Error:
                    case LogType.Exception:
                        LogError(text);
                        break;
                    case LogType.Assert:
                    case LogType.Log:
                        Log(text);
                        break;
                    case LogType.Warning:
                        LogWarning(text);
                        break;
                }

                writeToLog = true;
            };

            rectWindow = consoleWindow.GetComponent<RectTransform>();
            commandField.onEndEdit.AddListener((string value) => { Run(); });
            consoleWindow.SetActive(false);
        }

        void UpdateLog() => log.text = string.Join("\n", logData.ToArray());

        public static void Log(object message)
        {
            if (writeToLog)
            {
                Console.WriteLine(message);
            }
            logData.Enqueue(message.ToString());
            if (logData.Count > maxLogData) logData.Dequeue();

            instance.UpdateLog();
        }
        public static void LogFormat(string format, params string[] args) => Log(string.Format(format, args));
        public static void LogWarning(object message)
        {
            Log($"<color=yellow>Warning: {message}</color>");
            if (openOnWarning)
            {
                instance.toggle = true;
                instance.consoleWindow.SetActive(true);
            }
        }
        public static void LogWarningFormat(string format, params string[] args) => LogWarning(string.Format(format, args));
        public static void LogError(object message)
        {
            Log($"<color=red>Error: {message}</color>");
            if (openOnError)
            {
                instance.toggle = true;
                instance.consoleWindow.SetActive(true);
            }
        }
        public static void LogErrorFormat(string format, params string[] args) => LogError(string.Format(format, args));
    }
}
