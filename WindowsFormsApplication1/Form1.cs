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
        Macro macro;
        MacroRecoder rec = new MacroRecoder();
        BackgroundWorker bw = new BackgroundWorker();
        public Form1()
        {
            InitializeComponent();

            Macro.CommandEvent += Macro_CommandEvent;

            bw.WorkerSupportsCancellation = true;
            bw.DoWork += bw_DoWork;
            bw.RunWorkerCompleted += bw_RunWorkerCompleted;


            btnStart.Enabled = true;
            btnEnd.Enabled = false;
            btnDoMacro.Enabled = true;
            btnStopMacro.Enabled = false;


        }

        void Macro_CommandEvent(object sender, CommandEventArgs e)
        {
            Action action = () =>
            {
                textBox2.Text += e.command + Environment.NewLine;
                textBox2.SelectionStart = textBox2.TextLength - 1;
                textBox2.ScrollToCaret();
            };
            if (this.InvokeRequired)
                this.Invoke(action);
            else
                action();
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            btnStart.Enabled = false;
            btnEnd.Enabled = true;
            textBox1.Text = "";
            rec.Start();
        }

        private void btnEnd_Click(object sender, EventArgs e)
        {

            var recodedCommands = rec.End();

            int lineNum = -1;
            foreach (var co in recodedCommands)
            {
                lineNum++;
                if (lineNum + 4 >= recodedCommands.Count())//最後の4行は記録終了ボタンを押す時のものだから無視する。
                    continue;
                textBox1.Text += co.ToString() + Environment.NewLine;
            }

            btnStart.Enabled = true;
            btnEnd.Enabled = false;
        }
        
        IEnumerable<ICommand> bw_commands;
        private void btnDoMacro_Click(object sender, EventArgs e)
        {
            btnStart.Enabled = false;
            btnEnd.Enabled = false;
            btnDoMacro.Enabled = false;
            btnStopMacro.Enabled = true;

            bw_commands = Macro.TextParser(textBox1.Lines);

            bw.RunWorkerAsync();
        }

        void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Completed();
        }

        void bw_DoWork(object sender, DoWorkEventArgs e)
        {
            macro = new Macro(bw_commands);
            macro.Start();
        }

        private void btnStopMacro_Click(object sender, EventArgs e)
        {
            bw.CancelAsync();
            Completed();
        }
        private void Completed()
        {
            btnStart.Enabled = true;
            btnEnd.Enabled = false;
            btnDoMacro.Enabled = true;
            btnStopMacro.Enabled = false;
        }
    }
}
