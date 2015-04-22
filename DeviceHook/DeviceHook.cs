using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Data;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Forms;
namespace ryu_s.DeviceHook
{
    public abstract class DeviceHook : IDisposable
    {
        protected const int WH_MOUSE = 7;
        internal const int WH_MOUSE_LL = 14;
        internal const int WH_KEYBOARD_LL = 13;
        protected const int WM_KEYDOWN = 0x0100;
        protected const int WM_KEYUP = 0x0101;
        private delegate int HookProc(int nCode, IntPtr wParam, IntPtr lParam);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="idHook"></param>
        /// <param name="lpfn"></param>
        /// <param name="hInstance"></param>
        /// <param name="threadId"></param>
        /// <returns>If the function succeeds, the return value is the handle to the hook procedure</returns>
        [DllImport("user32.dll", EntryPoint = "SetWindowsHookEx", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern int _setWindowsHookEx(int idHook, HookProc lpfn,
        IntPtr hInstance, int threadId);

        [DllImport("user32.dll", EntryPoint = "UnhookWindowsHookEx", CharSet = CharSet.Auto,
         CallingConvention = CallingConvention.StdCall)]
        private static extern bool _unhookWindowsHookEx(int idHook);
        [DllImport("user32.dll", EntryPoint = "CallNextHookEx", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern int _callNextHookEx(int idHook, int nCode,
        IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        int procHandle = 0;
        protected void SetWindowsHookEx(int idHook)
        {
            if (procHandle != 0)
                throw new InvalidOperationException("既に登録済み！");
            using (var process = Process.GetCurrentProcess())
            using (var module = process.MainModule)
            {
                var hModule = GetModuleHandle(module.ModuleName);
                procHandle = _setWindowsHookEx(idHook, new HookProc(HookProcedure), hModule, 0);
                if (procHandle == 0)
                {
                    Debug.WriteLine("fail");
                }
            }
        }
        protected bool UnhookWindowsHookEx()
        {
            if (procHandle == 0)
                return false;
            var b = _unhookWindowsHookEx(procHandle);
            procHandle = 0;
            return b;
        }
        protected abstract void Hook(int nCode, IntPtr wParam, IntPtr lParam);
        private int HookProcedure(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode < 0)
            {
                //何もせずにすぐにCallNextHookExを呼ばないといけない。
                return _callNextHookEx(0, nCode, wParam, lParam);
            }
            else
            {
                Hook(nCode, wParam, lParam);
                return _callNextHookEx(0, nCode, wParam, lParam);
            }
        }
        ~DeviceHook()
        {
            Dispose(false);
        }
        private bool _alreadyDisposed = false;
        protected virtual void Dispose(bool isDisposing)
        {
            if (_alreadyDisposed)
                return;
            if (isDisposing)
            {
                //release managed resourcies
            }
            //release unmanaged resourcies
            UnhookWindowsHookEx();

            _alreadyDisposed = true;
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
    public class KeyboardHook : DeviceHook
    {
        public enum KeyEventType
        {
            Up,
            Down,
        }
        public void Regist()
        {
            SetWindowsHookEx(WH_KEYBOARD_LL);
        }
        public void Unregist()
        {
            UnhookWindowsHookEx();
        }
        bool isPressingAlt = false;
        bool isPressingControl = false;
        bool isPressingShift = false;
        const int vk_lshift = 0xA0;
        const int vk_rshift = 0xA1;
        const int vk_lcontrol = 0xA2;
        const int vk_rcontrol = 0xA3;
        const int vk_lmenu = 0xA4;
        const int vk_rmenu = 0xA5;
        [StructLayout(LayoutKind.Sequential)]
        private class KBDLLHOOKSTRUCT
        {
            public uint vkCode;
            public uint scanCode;
            public KBDLLHOOKSTRUCTFlags flags;
            public uint time;
            public UIntPtr dwExtraInfo;
        }
        [Flags]
        private enum KBDLLHOOKSTRUCTFlags : uint
        {
            LLKHF_EXTENDED = 0x01,
            LLKHF_INJECTED = 0x10,
            LLKHF_ALTDOWN = 0x20,
            LLKHF_UP = 0x80,
        }
        protected virtual void KeyStateChanged(KeyEventType type, Keys key)
        {
        }
        protected override void Hook(int nCode, IntPtr wParam, IntPtr lParam)
        {
            var kb = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));
            var type = KeyEventType.Down;
            if ((int)wParam == WM_KEYDOWN)
            {
                if (kb.vkCode == vk_lshift || kb.vkCode == vk_rshift)
                    isPressingShift = true;
                else if (kb.vkCode == vk_lcontrol || kb.vkCode == vk_rcontrol)
                    isPressingControl = true;
                else if (kb.vkCode == vk_lmenu || kb.vkCode == vk_rmenu)
                    isPressingAlt = true;
//                Console.WriteLine(string.Format("{0}, shift={1}, control={2}, alt={3}", key.ToString(), isPressingShift, isPressingControl, isPressingAlt));
                type = KeyEventType.Down;
            }
            else if ((int)wParam == WM_KEYUP)
            {
                if (kb.vkCode == vk_lshift || kb.vkCode == vk_rshift)
                    isPressingShift = false;
                else if (kb.vkCode == vk_lcontrol || kb.vkCode == vk_rcontrol)
                    isPressingControl = false;
                else if (kb.vkCode == vk_lmenu || kb.vkCode == vk_rmenu)
                    isPressingAlt = false;
                type = KeyEventType.Up;
            }
            else
            {
                Console.WriteLine("KeyboardHook::Hook() unknowncode:" + (int)wParam);
            }
            var key = (Keys)kb.vkCode;
            if (isPressingAlt)
                key &= Keys.Alt;
            if (isPressingControl)
                key &= Keys.Control;
            if (isPressingShift)
                key &= Keys.Shift;
            KeyStateChanged(type, key);

//            Console.WriteLine(nCode);
        }
    }
    public class MouseHook : DeviceHook
    {
        public void Regist()
        {
            SetWindowsHookEx(WH_MOUSE_LL);
        }
        public void Unregist()
        {
            UnhookWindowsHookEx();
        }
        protected override void Hook(int nCode, IntPtr wParam, IntPtr lParam)
        {
            Console.WriteLine(nCode);
        }
    }
    public class MyKeyEventArgs : EventArgs
    {
        public Keys Key { get; private set; }
        public MyKeyEventArgs(Keys key)
        {
            this.Key = key;
        }
    }
    public delegate void KeyEventHandler(object sender, MyKeyEventArgs e);
    public sealed class GlobalKeyListener : KeyboardHook
    {
        public event KeyEventHandler KeyDown;
        public event KeyEventHandler KeyUp;
        List<KeyValuePair<KeyEventType, Keys>> listeningKeys = new List<KeyValuePair<KeyEventType, Keys>>();
        public GlobalKeyListener()
        {
            Regist();
        }
        public void Add(KeyEventType type, Keys key)
        {
            listeningKeys.Add(new KeyValuePair<KeyEventType, Keys>(type, key));
        }
        protected override void KeyStateChanged(KeyboardHook.KeyEventType type, Keys key)
        {
            base.KeyStateChanged(type, key);
            foreach (var checkPair in listeningKeys)
            {
                if (checkPair.Value == key && checkPair.Key == type)
                {
                    var args = new MyKeyEventArgs(key);
                    if (KeyDown != null && type == KeyEventType.Down)
                        KeyDown(this, args);
                    else if (KeyUp != null && type == KeyEventType.Up)
                        KeyUp(this, args);
                    break;
                }
            }
        }
    }
}
