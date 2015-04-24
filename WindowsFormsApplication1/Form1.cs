using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ryu_s.DeviceHook;
using ryu_s.Macro;
using System.Text.RegularExpressions;
namespace WindowsFormsApplication1
{
    public partial class Form1 : Form
    {
        GlobalMouseHook mouseHook = new GlobalMouseHook();
        Macro macro;
        List<ICommand> _commands = new List<ICommand>();
        public Form1()
        {
            InitializeComponent();

            mouseHook.MouseDClick += mouseHook_MouseDClick;
            mouseHook.MouseDown += mouseHook_MouseDown;
            mouseHook.MouseUp += mouseHook_MouseUp;
            mouseHook.MouseWheel += mouseHook_MouseWheel;
        }
        System.Timers.Timer timer = new System.Timers.Timer();
        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        private void AddWait()
        {
            var milliSec = sw.ElapsedMilliseconds;
            _commands.Add(new Wait((int)milliSec));
            sw.Reset();
            sw.Start();
        }
        void mouseHook_MouseUp(object sender, MouseEventArgs e)
        {
            
            _commands.Add(new Mouse(e.Button, DeviceInputApi.ActionType.Up, e.X, e.Y));
            AddWait();
        }

        void mouseHook_MouseWheel(object sender, MouseEventArgs e)
        {
            Console.WriteLine("MouseWheel {0}", e.Delta);
            
            _commands.Add(new Mouse(DeviceInputApi.MouseWheelType.Vertical, e.Delta / 120));
            AddWait();
        }

        void mouseHook_MouseDown(object sender, MouseEventArgs e)
        {
            Console.WriteLine("MouseDown ({0}, {1})", e.X, e.Y);
            
            _commands.Add(new Mouse(e.Button, DeviceInputApi.ActionType.Down, e.X, e.Y));
            AddWait();
        }

        void mouseHook_MouseDClick(object sender, MouseEventArgs e)
        {
            Console.WriteLine(e.Button.ToString());
        }
        bool first = true;
        private void btnStart_Click(object sender, EventArgs e)
        {
            textBox1.Text = "";
            mouseHook.Regist();

//            btnStart.Enabled = false;
//            var list = new List<ICommand>();
//            list.Add(new Mouse(MouseButtons.Left, DeviceInputApi.ActionType.Down, 1709, 304));
//            list.Add(new Mouse(MouseButtons.Left, DeviceInputApi.ActionType.Up, 1709, 304));
//            list.Add(new Mouse(DeviceInputApi.MouseWheelType.Vertical, -17));
//            list.Add(new Wait(5000));
//            list.Add(new Mouse(MouseButtons.Left, DeviceInputApi.ActionType.Down, 1587, 423));
//            list.Add(new Mouse(MouseButtons.Left, DeviceInputApi.ActionType.Up, 1587, 423));
//            list.Add(new Wait(10000));
//            list.Add(new Mouse(MouseButtons.Left, DeviceInputApi.ActionType.Down, 1595, 550));
//            list.Add(new Mouse(MouseButtons.Left, DeviceInputApi.ActionType.Up, 1595, 550));
//            list.Add(new Wait(5000));
//            list.Add(new Mouse(DeviceInputApi.MouseWheelType.Vertical, -28));
//            list.Add(new Mouse(MouseButtons.Left, DeviceInputApi.ActionType.Down, 1595, 512));
//            list.Add(new Mouse(MouseButtons.Left, DeviceInputApi.ActionType.Up, 1595, 512));
////            list.Add(new Wait(5000));
////            list.Add(new Mouse(DeviceInputApi.MouseWheelType.Vertical, -7));
//            list.Add(new Wait(5000));
//            list.Add(new Mouse(MouseButtons.Left, DeviceInputApi.ActionType.Down, 1595, 464));
//            list.Add(new Mouse(MouseButtons.Left, DeviceInputApi.ActionType.Up, 1595, 464));
//            list.Add(new Wait(5000));
//            list.Add(new Mouse(MouseButtons.Left, DeviceInputApi.ActionType.Down, 1593, 396));
//            list.Add(new Mouse(MouseButtons.Left, DeviceInputApi.ActionType.Up, 1593, 396));
//            list.Add(new Wait(5000));
//            list.Add(new Mouse(MouseButtons.Left, DeviceInputApi.ActionType.Down, 1609, 542));
//            list.Add(new Mouse(MouseButtons.Left, DeviceInputApi.ActionType.Up, 1609, 542));
//            list.Add(new Wait(5000));
//            list.Add(new Mouse(MouseButtons.Left, DeviceInputApi.ActionType.Down, 1609, 542));
//            list.Add(new Mouse(MouseButtons.Left, DeviceInputApi.ActionType.Up, 1609, 542));
//            list.Add(new Wait(5000));
//            list.Add(new Mouse(MouseButtons.Left, DeviceInputApi.ActionType.Down, 1623, 602));
//            list.Add(new Mouse(MouseButtons.Left, DeviceInputApi.ActionType.Up, 1623, 602));
//            list.Add(new Wait(5000));
//            list.Add(new Mouse(MouseButtons.Left, DeviceInputApi.ActionType.Down, 1623, 602));
//            list.Add(new Mouse(MouseButtons.Left, DeviceInputApi.ActionType.Up, 1623, 602));
//            list.Add(new Wait(5000));
//            list.Add(new Mouse(MouseButtons.Left, DeviceInputApi.ActionType.Down, 1623, 602));
//            list.Add(new Mouse(MouseButtons.Left, DeviceInputApi.ActionType.Up, 1623, 602));
//            list.Add(new Wait(5000));
//            list.Add(new Mouse(MouseButtons.Left, DeviceInputApi.ActionType.Down, 1623, 602));
//            list.Add(new Mouse(MouseButtons.Left, DeviceInputApi.ActionType.Up, 1623, 602)); 
//            list.Add(new Wait(5000));
//            list.Add(new Mouse(MouseButtons.Left, DeviceInputApi.ActionType.Down, 1623, 602));
//            list.Add(new Mouse(MouseButtons.Left, DeviceInputApi.ActionType.Up, 1623, 602));

//            macro = new Macro(list);
//            macro.Start();
            //await Task.Delay(2000);
            //btnStart.Enabled = true;
        }

        private void btnEnd_Click(object sender, EventArgs e)
        {
            mouseHook.Unregist();
            sw.Stop();            

            int lineNum = -1;
            foreach (var co in _commands)
            {
                lineNum++;
                //if (lineNum < 2)
                //    continue;
                textBox1.Text += co.ToString() + Environment.NewLine;
            }
            _commands.Clear();
        }
        private ICommand TextParser(string line)
        {
            var match0 = Regex.Match(line, "WAIT (?<time>[0-9]+)", RegexOptions.IgnoreCase);
            if (match0.Success)
            {
                var milli = int.Parse(match0.Groups["time"].Value);
                return new Wait(milli);
            }
            var match1 = Regex.Match(line, "MOUSE_(?<button>[a-zA-Z]+)_(?<type>[a-zA-Z]+) (?<x>[0-9]+) (?<y>[0-9]+)");
            if (match1.Success)
            {
                var buttonStr = match1.Groups["button"].Value;
                var button = MouseButtons.None;
                switch (buttonStr.ToUpper())
                {
                    case "LEFT":
                        button = System.Windows.Forms.MouseButtons.Left;
                        break;
                    case "RIGHT":
                        button = System.Windows.Forms.MouseButtons.Right;
                        break;
                    case "MIDDLE":
                        button = System.Windows.Forms.MouseButtons.Middle;
                        break;
                    case "XBUTTON1":
                        button = System.Windows.Forms.MouseButtons.XButton1;
                        break;
                    case "XBUTTON2":
                        button = System.Windows.Forms.MouseButtons.XButton2;
                        break;
                    default:
                        goto ERROR;
                }
                var typeStr = match1.Groups["type"].Value;
                var type = DeviceInputApi.ActionType.Up;
                switch (typeStr.ToUpper())
                {
                    case "DOWN":
                        type = DeviceInputApi.ActionType.Down;
                        break;
                    case "UP":
                        type = DeviceInputApi.ActionType.Up;
                        break;
                    default:
                        goto ERROR;
                }
                var x = int.Parse(match1.Groups["x"].Value);
                var y = int.Parse(match1.Groups["y"].Value);
                return new Mouse(button, type, x, y);
            }
            var match2 = Regex.Match(line, "MOUSE_WHEEL (?<amount>[-0-9]+)");
            if (match2.Success)
            {
                var amount = int.Parse(match2.Groups["amount"].Value);
                return new Mouse(DeviceInputApi.MouseWheelType.Vertical, amount);
            }
ERROR:
            return new Nop();
        }

        private void btnDoMacro_Click(object sender, EventArgs e)
        {
            var _commands = new List<ICommand>();
            foreach (var line in textBox1.Lines)
            {
                _commands.Add(TextParser(line));
            }
            DoMacro(_commands);
        }

        private void DoMacro(IEnumerable<ICommand> commands)
        {
            macro = new Macro(commands);
            macro.Start();
        }
    }
}
