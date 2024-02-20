using System;
using System.Collections.Generic;
using Flow.Launcher.Plugin;
using System.Threading;
using System.Runtime.InteropServices;
namespace Flow.Launcher.Plugin.WinHotkey
{
    public class Win : IPlugin
    {
        private PluginInitContext _context;
        private static bool isCallbackInProgress = false;


        public void Init(PluginInitContext context)
        {
            _context = context;
            _context.API.RegisterGlobalKeyboardCallback(KeyboardCallback);

        }

        public List<Result> Query(Query query)
        {
            return new List<Result>();
        }

        bool KeyboardCallback(int keyCode, int additionalInfo, SpecialKeyState keyState)
        {
            if (_context.CurrentPluginMetadata.Disabled)
            {
                return true;
            }
            if (isCallbackInProgress)
            {
                return true;
            }

            if (additionalInfo == 91)
            {
                isCallbackInProgress = true;
                KeyboardSimulator.SimulateAltSpace();

                // Reset the flag after a delay to allow for debouncing
                ThreadPool.QueueUserWorkItem(state =>
                {
                    Thread.Sleep(300);
                    isCallbackInProgress = false;
                });

                return false;
            }

            return true;
            
        }
        public void Dispose()
        {
            _context.API.RemoveGlobalKeyboardCallback(KeyboardCallback);
        }
    }

    public class KeyboardSimulator
    {
        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [StructLayout(LayoutKind.Sequential)]
        public struct INPUT
        {
            public int type;
            public InputUnion U;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct InputUnion
        {
            [FieldOffset(0)]
            public MOUSEINPUT mi;
            [FieldOffset(0)]
            public KEYBDINPUT ki;
            [FieldOffset(0)]
            public HARDWAREINPUT hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct HARDWAREINPUT
        {
            public uint uMsg;
            public ushort wParamL;
            public ushort wParamH;
        }

        private const ushort VK_MENU = 0x12;
        private const ushort VK_SPACE = 0x20;
        private const uint KEYEVENTF_KEYDOWN = 0x0000;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const int INPUT_KEYBOARD = 1;

        public static void SimulateAltSpace()
        {
            INPUT[] inputs = new INPUT[4];

            inputs[0].type = INPUT_KEYBOARD;
            inputs[0].U.ki.wVk = VK_MENU;

            inputs[1].type = INPUT_KEYBOARD;
            inputs[1].U.ki.wVk = VK_SPACE;

            inputs[2].type = INPUT_KEYBOARD;
            inputs[2].U.ki.wVk = VK_SPACE;
            inputs[2].U.ki.dwFlags = KEYEVENTF_KEYUP;

            inputs[3].type = INPUT_KEYBOARD;
            inputs[3].U.ki.wVk = VK_MENU;
            inputs[3].U.ki.dwFlags = KEYEVENTF_KEYUP;

            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
        }
    }
}