
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Controls;
using AutoHotkey.Interop;
namespace Flow.Launcher.Plugin.WinHotkey
{
    public class WinHotkey : IPlugin, ISettingProvider
    {
        private PluginInitContext _context;
        private static AutoHotkeyEngine _ahk;
        private Settings _settings;

        public void Init(PluginInitContext context)
        {
            _context = context;
            _settings = _context.API.LoadSettingJsonStorage<Settings>();
            _ahk = new AutoHotkeyEngine();
            Hook();
        }

        string MainSettingsPath()
        {
            string SettingsJsonPath = Path.GetDirectoryName(Path.GetDirectoryName(_context.CurrentPluginMetadata.PluginDirectory));
            return Path.Combine(SettingsJsonPath, "Settings", "Settings.json");
        }

        Dictionary<string, JsonElement> LoadSettingsJson()
        {
            string json_data = System.IO.File.ReadAllText(MainSettingsPath());
            return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json_data);
        }

        string GetCurrentHotkey()
        {
            return LoadSettingsJson()["Hotkey"].GetString();
        }

        string GetHotkeyInAhkFormat()
        {
            // Split the shortcut string into individual key parts
            string[] keys = GetCurrentHotkey().Split('+');

            // Convert each key to its AHK format
            for (int i = 0; i < keys.Length; i++)
            {
                keys[i] = keys[i].Trim(); // Remove leading and trailing spaces
                if (keys[i].Length == 1)
                {
                    keys[i] = keys[i].ToLower();
                }
                else if (keys[i].Length == 2 && keys[i].StartsWith("D"))
                {
                    keys[i] = "{" + keys[i].Substring(1) + "}";
                }
                else if (keys[i].StartsWith("Page"))
                {
                    keys[i] = "{" + keys[i].Replace("Page", "Pg") + "}";
                }
                else if (keys[i] == "Next")
                {
                    keys[i] = "{" + "PgDn" + "}";
                }
                else
                {
                    switch (keys[i].ToLower())
                    {
                        case "alt":
                            keys[i] = "!";
                            break;
                        case "ctrl":
                            keys[i] = "^";
                            break;
                        case "shift":
                            keys[i] = "+";
                            break;
                        case "win":
                            keys[i] = "#";
                            break;
                        case "back":
                            keys[i] = "{Backspace}";
                            break;
                        case "oemquestion":
                            keys[i] = "/";
                            break;
                        case "oemplus":
                            keys[i] = "=";
                            break;
                        case "oemminus":
                            keys[i] = "-";
                            break;
                        case "oem5":
                            keys[i] = "\\";
                            break;
                        case "oem6":
                            keys[i] = "]";
                            break;
                        case "oemopenbrackets":
                            keys[i] = "[";
                            break;
                        case "oemperiod":
                            keys[i] = ".";
                            break;
                        case "oemcomma":
                            keys[i] = ",";
                            break;
                        case "oem1":
                            keys[i] = ";";
                            break;
                        case "oemquotes":
                            keys[i] = "'";
                            break;
                        case "divide":
                            keys[i] = "{NumpadDiv}";
                            break;
                        case "multiply":
                            keys[i] = "{NumpadMult}";
                            break;
                        case "subtract":
                            keys[i] = "{NumpadSub}";
                            break;
                        case "add":
                            keys[i] = "{NumpadAdd}";
                            break;
                        default:
                            keys[i] = "{" + keys[i] + "}";
                            break;
                    }
                }
            }

            // Combine the keys back into the AHK format
            string ahkFormat = string.Join("", keys);

            return ahkFormat;
        }

        public List<Result> Query(Query query)
        {
            return new List<Result>();
        }

        public string ReleaseMappedButton()
        {
            string ahkFormat = string.Empty;
            switch (_settings.InterrModifier)
            {
                case "LAlt":
                    ahkFormat = "!";
                    break;
                case "LWin":
                    ahkFormat = "#";
                    break;
                case "LControl":
                    ahkFormat = "^";
                    break;
                case "RWin":
                    ahkFormat = "RWin";
                    break;
                case "RControl":
                    ahkFormat = "RControl";
                    break;
                case "RAlt":
                    ahkFormat = "RAlt";
                    break;
            }
            return ahkFormat;

        }
        public void Hook()
        {
            if (!_context.CurrentPluginMetadata.Disabled)
            {
                string Timeout = _settings.Timeout;
                string Script = $@"#Persistent
                return
                {(_settings.DoubleTap ? "Interr_PriorKey := \"\"\"\" " : "")}
                {(_settings.DoubleTap ? "First_Tap_Time := 0 " : "")}
                ~{_settings.InterrModifier}::
                    Send, {{Blind}}{{VKFF}}
                    KeyboardStartTime := A_TickCount ; Record the start time
                    KeyWait, {_settings.InterrModifier}
                    
                    ; Calculate the time elapsed
                    ElapsedTime := A_TickCount - KeyboardStartTime

                    if (A_PriorKey != ""{_settings.InterrModifier}"")
                    {{
                        {(_settings.DoubleTap ? "Interr_PriorKey := A_PriorKey" : "")}
                        Send, {ReleaseMappedButton()}
                        return
                    }}
                    {(_settings.DoubleTap ? $@"
                    if (Interr_PriorKey != ""{_settings.InterrModifier}"" || (A_TickCount - First_Tap_Time) > 500)
                    {{
                        First_Tap_Time := A_TickCount  ; Set First_Tap_Time to the current tick count
                        Interr_PriorKey := A_PriorKey
                        return
                    }}
                    " : "")}
                    {(_settings.DoubleTap ? $@"
                    if (Interr_PriorKey != ""{_settings.InterrModifier}"" || (A_TickCount - First_Tap_Time) > {_settings.DoubleTapTimeout})
                    {{
                        First_Tap_Time := A_TickCount  ; Set First_Tap_Time to the current tick count
                        Interr_PriorKey := A_PriorKey
                        return
                    }}
                    " : "")}
                    if ({(_settings.DoubleTap ? $"Interr_PriorKey = \"{_settings.InterrModifier}\" && " : "")}ElapsedTime < {Timeout}) ; Time between press and release is less than 200 milliseconds
                    {{

                        ; Get the class of the currently active window
                        WinGetClass, activeWindowClass, A
                        if (activeWindowClass = ""Windows.UI.Core.CoreWindow"" || activeWindowClass = ""Shell_TrayWnd"")
                        {{
                            Send, {{Esc}}
                        }}
                        ; Simulate Alt+Space
                        Send, {GetHotkeyInAhkFormat()}
                        return
                    }}
                return
                ";




                _ahk.ExecRaw(Script);
            }
        }

        public void Unhook()
        {
            _ahk.Terminate();
        }

        public Control CreateSettingPanel()
        {
            return new WinHotkeySettings(_settings);
        }

        public void Dispose()
        {
            Unhook();
        }
    }


    public partial class WinHotkeySettings : UserControl
    {
        private readonly Settings _settings;
        public WinHotkeySettings(Settings settings)
        {
            this.DataContext = settings;
            this.InitializeComponent();
        }
    }

    
    public class Settings
    {
        private string _timeout = "200";
        public string _doubleTapTimeout = "500";
        public string DoubleTapTimeout 
        {
            
            get
            {
                return _doubleTapTimeout;
            }
            set
            {
                if (Convert.ToInt32(value) < 200)
                {
                    _doubleTapTimeout = "200";
                }
                else
                {
                    _doubleTapTimeout = value;
                }
            }

        }
        public bool DoubleTap {get; set;} = false;
        public string InterrModifier {get; set;} = "LWin";

        [JsonIgnore]
        public List<string> Modifiers {get; } = new List<string> {"LWin", "LControl", "LAlt", "RWin", "RControl", "RAlt"};
        public string Timeout
        {
            get
            {
                return _timeout;
            }
            set
            {
                _timeout = value;
            }

        }
    }
}
