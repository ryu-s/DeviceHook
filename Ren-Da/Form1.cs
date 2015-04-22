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
namespace Ren_Da
{
    public partial class Form1 : Form
    {
        GlobalKeyListener keyListenter = new GlobalKeyListener();
        public Form1()
        {
            InitializeComponent();
            keyListenter.Add(KeyboardHook.KeyEventType.Down, Keys.F9);
            keyListenter.Add(KeyboardHook.KeyEventType.Down, Keys.F10);

            keyListenter.KeyDown += keyListenter_KeyDown;

            timer.Interval = 1000;
            timer.Elapsed += (s, e) =>
            {
                DeviceInputApi.MouseClick(DeviceInputApi.MouseClickButtonType.Left);
            };
        }

        void keyListenter_KeyDown(object sender, MyKeyEventArgs e)
        {
            switch (e.Key)
            {
                case Keys.F9:
                    timer.Start();
                    this.BackColor = Color.Red;
                    break;
                case Keys.F10:
                    timer.Stop();
                    this.BackColor = Color.Black;
                    break;
            }
        }
        System.Timers.Timer timer = new System.Timers.Timer();
    }
}
