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
            AddWait();
            _commands.Add(new Mouse(e.Button, DeviceInputApi.ActionType.Up, e.X, e.Y));

        }

        void mouseHook_MouseWheel(object sender, MouseEventArgs e)
        {
//            Console.WriteLine("MouseWheel {0}", e.Delta);
            AddWait();
            _commands.Add(new Mouse(DeviceInputApi.MouseWheelType.Vertical, e.Delta / 120));

        }

        void mouseHook_MouseDown(object sender, MouseEventArgs e)
        {
//            Console.WriteLine("MouseDown ({0}, {1})", e.X, e.Y);
            AddWait();            
            _commands.Add(new Mouse(e.Button, DeviceInputApi.ActionType.Down, e.X, e.Y));

        }

        void mouseHook_MouseDClick(object sender, MouseEventArgs e)
        {
            Console.WriteLine(e.Button.ToString());
        }
        private void btnStart_Click(object sender, EventArgs e)
        {
            textBox1.Text = "";
            mouseHook.Regist();
            sw.Start();
        }

        private void btnEnd_Click(object sender, EventArgs e)
        {
            mouseHook.Unregist();
            sw.Stop();            

            int lineNum = -1;
            foreach (var co in _commands)
            {
                lineNum++;
                if (lineNum + 4 >= _commands.Count)//最後の4行は記録終了ボタンを押す時のものだから無視する。
                    continue;
                textBox1.Text += co.ToString() + Environment.NewLine;
            }
            _commands.Clear();
        }

        private async void btnDoMacro_Click(object sender, EventArgs e)
        {
            btnStart.Enabled = false;
            btnEnd.Enabled = false;
            btnDoMacro.Enabled = false;

            var _commands = Macro.TextParser(textBox1.Lines);
            macro = new Macro(_commands);
            await Task.Delay(1000);
            await Task.Run(() =>
            {
                macro.Start();
            });

            btnStart.Enabled = true;
            btnEnd.Enabled = true;
            btnDoMacro.Enabled = true;
        }
    }
}
