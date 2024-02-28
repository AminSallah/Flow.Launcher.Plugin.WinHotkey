
using System.Collections.Generic;
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

        public List<Result> Query(Query query)
        {
            return new List<Result>();
        }
        public void Hook()
        {
            if (!_context.CurrentPluginMetadata.Disabled)
            {
                string Timeout = _settings.Timeout;
                string Script = $@"#Persistent
                return

                ~LWin::
                    Send, {{Blind}}{{VKFF}}
                    KeyboardStartTime := A_TickCount ; Record the start time
                    KeyWait, LWin ; Wait for the Left Windows key to be released

                    ; Calculate the time elapsed
                    ElapsedTime := A_TickCount - KeyboardStartTime
                    if (ElapsedTime < {Timeout}) ; Time between press and release is less than 200 milliseconds
                    {{
                        ; Simulate Alt+Space
                        Send, !{{Space}}
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
