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

        protected const int WM_NCMOUSEMOVE = 0x00A0;
        protected const int WM_NCLBUTTONDOWN = 0x00A1;
        protected const int WM_NCLBUTTONUP = 0x00A2;
        protected const int WM_NCLBUTTONDBLCLK = 0x00A3;
        protected const int WM_NCRBUTTONDOWN = 0x00A4;
        protected const int WM_NCRBUTTONUP = 0x00A5;
        protected const int WM_NCRBUTTONDBLCLK = 0x00A6;
        protected const int WM_NCMBUTTONDOWN = 0x00A7;
        protected const int WM_NCMBUTTONUP = 0x00A8;
        protected const int WM_NCMBUTTONDBLCLK = 0x00A9;
        protected const int WM_NCXBUTTONDOWN = 0x00AB;
        protected const int WM_NCXBUTTONUP = 0x00AC;
        protected const int WM_NCXBUTTONDBLCLK = 0x00AD;

        protected const int WM_MOUSEMOVE = 0x0200;
        protected const int WM_LBUTTONDOWN = 0x0201;
        protected const int WM_LBUTTONUP = 0x0202;
        protected const int WM_LBUTTONDBLCLK = 0x0203;
        protected const int WM_RBUTTONDOWN = 0x0204;
        protected const int WM_RBUTTONUP = 0x0205;
        protected const int WM_RBUTTONDBLCLK = 0x0206;
        protected const int WM_MBUTTONDOWN = 0x0207;
        protected const int WM_MBUTTONUP = 0x0208;
        protected const int WM_MBUTTONDBLCLK = 0x0209;
        protected const int WM_MOUSEWHEEL = 0x020A;
        protected const int WM_XBUTTONDOWN = 0x020B;
        protected const int WM_XBUTTONUP = 0x020C;
        protected const int WM_XBUTTONDBLCLK = 0x020D;
        protected const int WM_MOUSEHWHEEL = 0x020E;

        private delegate int HookProc(int nCode, IntPtr wParam, IntPtr lParam);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="idHook"></param>
        /// <param name="lpfn"></param>
        /// <param name="hInstance"></param>
        /// <param name="threadId">フックしたいスレッドのID。0だと全て。</param>
        /// <returns>If the function succeeds, the return value is the handle to the hook procedure</returns>
        [DllImport("user32.dll", EntryPoint = "SetWindowsHookEx", SetLastError = true, CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern int _setWindowsHookEx(int idHook, HookProc lpfn,
        IntPtr hInstance, int threadId);

        [DllImport("user32.dll", EntryPoint = "UnhookWindowsHookEx", CharSet = CharSet.Auto,
         CallingConvention = CallingConvention.StdCall)]
        private static extern bool _unhookWindowsHookEx(int idHook);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="idHook">This parameter is ignored.</param>
        /// <param name="nCode"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <returns></returns>
        [DllImport("user32.dll", EntryPoint = "CallNextHookEx", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern int CallNextHookEx(int idHook, int nCode,
        IntPtr wParam, IntPtr lParam);


        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        int procHandle = 0;
        public bool IsListening { get { return (procHandle != 0); } }
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
                    throw new Win32Exception(Marshal.GetLastWin32Error());
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
        const int HC_ACTION = 0;
        protected abstract void Hook(IntPtr wParam, IntPtr lParam);
        private int HookProcedure(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode < 0)
            {
                //If nCode is less than zero, the hook procedure must pass the message to the CallNextHookEx function 
                //without further processing and should return the value returned by CallNextHookEx. 
                return CallNextHookEx(0, nCode, wParam, lParam);
            }
            else
            {
                Hook(wParam, lParam);
                return CallNextHookEx(0, nCode, wParam, lParam);
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
        protected override void Hook(IntPtr wParam, IntPtr lParam)
        {
            //System.Windows.Forms.Keysを使うと、ShiftやControl等のLとRがあるキーを区別しない。

            //wParam=WM_KEYDOWN, WM_KEYUP, WM_SYSKEYDOWN, or WM_SYSKEYUP            
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
        }
    }
    public sealed class GlobalMouseHook : DeviceHook
    {
        public event MouseEventHandler MouseMove;
        public event MouseEventHandler MouseDown;
        public event MouseEventHandler MouseUp;
        //        public event MouseEventHandler MouseClick;
        public event MouseEventHandler MouseDClick;
        public event MouseEventHandler MouseWheel;
        public event MouseEventHandler MouseHWheel;


        [StructLayout(LayoutKind.Sequential)]
        private class POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private class MouseHookStruct
        {
            public POINT pt;
            public int hwnd;
            public int wHitTestCode;
            public int dwExtraInfo;
        }
        [StructLayout(LayoutKind.Sequential)]
        struct MouseHookStructEx
        {

            public MouseHookStruct mouseHookStruct;
            public int MouseData;
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public int mouseData;
            public int flags;
            public int time;
            public UIntPtr dwExtraInfo;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns>milliseconds</returns>
        [DllImport("user32.dll")]
        private static extern uint GetDoubleClickTime();
        private System.Timers.Timer timer;
        public void Regist()
        {
            SetWindowsHookEx(WH_MOUSE_LL);
            timer = new System.Timers.Timer
            {
                Interval = GetDoubleClickTime(),
                AutoReset = false,
            };
            timer.Elapsed += timer_Elapsed;
            MouseDown += MouseHook_MouseDown;
        }

        void MouseHook_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button.Equals(lastClickedButton))
            {
                OnEvent(MouseDClick, e.Button, 2, e.X, e.Y, e.Delta);
                lastClickedButton = MouseButtons.None;
            }
            else
            {
                timer.Stop();
                timer.Start();
                lastClickedButton = e.Button;
            }
        }

        void timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            lastClickedButton = MouseButtons.None;
        }
        public void Unregist()
        {
            UnhookWindowsHookEx();
        }
        private void OnEvent(MouseEventHandler e, MouseButtons buttons, int clicks, int x, int y, int delta)
        {
            var args = new MouseEventArgs(buttons, clicks, x, y, delta);
            if (e != null)
                e(this, args);
        }
        private int lastBDownX;
        private int lastBDownY;
        private MouseButtons lastClickedButton = MouseButtons.None;
        const int XBUTTON1 = 1;
        const int XBUTTON2 = 2;
        protected override void Hook(IntPtr wParam, IntPtr lParam)
        {
            var mouseHookStructEx = (MouseHookStructEx)Marshal.PtrToStructure(lParam, typeof(MouseHookStructEx));
            var mouseHookStruct = mouseHookStructEx.mouseHookStruct;
            var msLlHookStruct = (MSLLHOOKSTRUCT)Marshal.PtrToStructure((IntPtr)lParam, typeof(MSLLHOOKSTRUCT));
            var currentX = mouseHookStruct.pt.x;
            var currentY = mouseHookStruct.pt.y;

            switch ((int)wParam)
            {
                case WM_MOUSEMOVE:
                    OnEvent(MouseMove, MouseButtons.None, 0, currentX, currentY, 0);
                    break;
                case WM_LBUTTONDOWN:
                    lastBDownX = currentX;
                    lastBDownY = currentY;
                    OnEvent(MouseDown, MouseButtons.Left, 1, currentX, currentY, 0);
                    break;
                case WM_LBUTTONUP:
                    OnEvent(MouseUp, MouseButtons.Left, 1, currentX, currentY, 0);
                    break;
                case WM_RBUTTONDOWN:
                    lastBDownX = currentX;
                    lastBDownY = currentY;
                    OnEvent(MouseDown, MouseButtons.Right, 1, currentX, currentY, 0);
                    break;
                case WM_RBUTTONUP:
                    OnEvent(MouseUp, MouseButtons.Right, 1, currentX, currentY, 0);
                    break;
                case WM_MBUTTONDOWN:
                    lastBDownX = currentX;
                    lastBDownY = currentY;
                    OnEvent(MouseDown, MouseButtons.Middle, 1, currentX, currentY, 0);
                    break;
                case WM_MBUTTONUP:
                    OnEvent(MouseUp, MouseButtons.Middle, 1, currentX, currentY, 0);
                    break;
                case WM_MOUSEWHEEL:
                    {
                        var delta = (short)((msLlHookStruct.mouseData >> 16) & 0xFFFF);
                        OnEvent(MouseWheel, MouseButtons.None, 0, currentX, currentY, delta);
                    }
                    break;
                case WM_MOUSEHWHEEL:
                    {
                        var delta = (short)((msLlHookStruct.mouseData >> 16) & 0xFFFF);
                        OnEvent(MouseHWheel, MouseButtons.None, 0, currentX, currentY, delta);
                    }
                    break;
                case WM_XBUTTONDOWN:
                    {
                        lastBDownX = currentX;
                        lastBDownY = currentY;
                        var x = (msLlHookStruct.mouseData >> 16);
                        if (x == XBUTTON1)
                            OnEvent(MouseDown, MouseButtons.XButton1, 1, currentX, currentY, 0);
                        else if (x == XBUTTON2)
                            OnEvent(MouseDown, MouseButtons.XButton2, 1, currentX, currentY, 0);
                    }
                    break;
                case WM_XBUTTONUP:
                    {
                        var x = (msLlHookStruct.mouseData >> 16);
                        if (x == XBUTTON1)
                            OnEvent(MouseUp, MouseButtons.XButton1, 1, currentX, currentY, 0);
                        else if (x == XBUTTON2)
                            OnEvent(MouseUp, MouseButtons.XButton2, 1, currentX, currentY, 0);
                    }
                    break;
                default:
                    Debug.WriteLine("unknown wParam:" + (int)wParam);
                    break;
            }
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
        public void Remove(KeyEventType type, Keys key)
        {
            listeningKeys.Remove(new KeyValuePair<KeyEventType, Keys>(type, key));
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
