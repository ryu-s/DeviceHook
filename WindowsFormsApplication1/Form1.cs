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
namespace WindowsFormsApplication1
{
    public partial class Form1 : Form
    {
        GlobalMouseHook mouseHook = new GlobalMouseHook();
        public Form1()
        {
            InitializeComponent();
            mouseHook.Regist();
            mouseHook.MouseDClick += mouseHook_MouseDClick;

        }

        void mouseHook_MouseDClick(object sender, MouseEventArgs e)
        {
            Console.WriteLine(e.Button.ToString());
        }
    }
}
