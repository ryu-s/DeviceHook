using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.IO;
using ryu_s.DeviceHook;
using System.Diagnostics;
using System.Text.RegularExpressions;
namespace ryu_s.Macro
{
    public interface ICommand
    {
        void DoWork();
        //名前が適切ではないかも。
        IEnumerable<ICommand> GetChildren();
    }

    public sealed class Wait : ICommand
    {
        int waitTime;
        public Wait(int milliseconds)
        {
            waitTime = milliseconds;
        }
        public void DoWork()
        {
            Thread.Sleep(waitTime);
        }
        public override string ToString()
        {
            return string.Format("WAIT {0}", waitTime);
        }
        public static ICommand Parse(string line)
        {
            var match0 = Regex.Match(line, "^WAIT (?<time>[0-9]+)$", RegexOptions.IgnoreCase);
            if (match0.Success)
            {
                var milli = int.Parse(match0.Groups["time"].Value);
                return new Wait(milli);
            }
            return new ParseError(line);
        }
        public IEnumerable<ICommand> GetChildren()
        {
            return new List<ICommand>();
        }
    }
    public sealed class Mouse : ICommand
    {
        string s = "";
        public class MouseAction
        {
            public MouseAction(MouseButtons button, DeviceInputApi.ActionType type, int x, int y, bool abusolutePos = true)
            {
            }
        }
        bool bMove = false;
        //        DeviceInputApi.MouseMoveType _moveType;
        public Mouse(int x, int y)
        {
            bMove = true;
            _x = x;
            _y = y;
            s = string.Format("MOUSE_MOVE {0} {1}", _x, _y);
        }
        bool bWheel = false;
        int _amount;
        DeviceInputApi.MouseWheelType _wheelType;
        public Mouse(DeviceInputApi.MouseWheelType wheelType, int amount)
        {
            bWheel = true;
            _amount = amount;
            _wheelType = wheelType;
            s = string.Format("MOUSE_WHEEL {0}", _amount);
        }
        MouseButtons _button;
        DeviceInputApi.ActionType _type;
        int _x;
        int _y;
        public Mouse(MouseButtons button, DeviceInputApi.ActionType type, int x, int y, bool abusolutePos = true)
        {
            _button = button;
            _type = type;
            _x = x;
            _y = y;
            s = string.Format("MOUSE_{0}_{1} {2} {3}", _button.ToString().ToUpper(), _type.ToString().ToUpper(), _x, _y);
        }
        public void DoWork()
        {
            //            Console.WriteLine(_button.ToString());
            if (bMove)
            {
                DeviceInputApi.MoveMouse(_x, _y);
            }
            else if (bWheel)
            {
                DeviceInputApi.MoveMouseWheel(_wheelType, _amount * 120);
            }
            else
            {
                DeviceInputApi.MoveMouse(_x, _y);
                DeviceInputApi.ActionMouseButton(_button, _type, _x, _y);
            }
        }
        public override string ToString()
        {
            return s;
        }
        public IEnumerable<ICommand> GetChildren()
        {
            return new List<ICommand>();
        }

        public static ICommand Parse(string line)
        {
            var match1 = Regex.Match(line, "^MOUSE_(?<button>[a-zA-Z]+)_(?<type>[a-zA-Z]+) (?<x>[-0-9]+) (?<y>[-0-9]+)$", RegexOptions.IgnoreCase);
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
            var match2 = Regex.Match(line, "^MOUSE_WHEEL (?<amount>[-0-9]+)$");
            if (match2.Success)
            {
                var amount = int.Parse(match2.Groups["amount"].Value);
                return new Mouse(DeviceInputApi.MouseWheelType.Vertical, amount);
            }
        ERROR:
            return new ParseError(line);
        }
    }
    public sealed class Keyboard : ICommand
    {
        Keys _key;
        DeviceInputApi.ActionType _type;
        public Keyboard(Keys key, DeviceInputApi.ActionType type)
        {
            _key = key;
            _type = type;
        }
        public void DoWork()
        {
            DeviceInputApi.ActionKeyboard(_key, _type);
        }
        public IEnumerable<ICommand> GetChildren()
        {
            return new List<ICommand>();
        }
        public override string ToString()
        {
            var key = (Keys)((int)_key & 0xFF);//remove modifier keys
            return string.Format("KEY_{0}_{1}", key.ToString(), _type);
        }
        public static ICommand Parse(string line)
        {
            var match = Regex.Match(line, "^KEY_(?<key>.+)_(?<type>[a-zA-Z]+)$", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var keyStr = match.Groups["key"].Value;
                Keys keys;
                var b = Enum.TryParse<Keys>(keyStr, true, out keys);
                if (!b)                
                    goto ERROR;
                var typeStr = match.Groups["type"].Value.ToUpper();
                DeviceInputApi.ActionType type;
                if (typeStr == "DOWN")
                    type = DeviceInputApi.ActionType.Down;
                else if (typeStr == "UP")
                    type = DeviceInputApi.ActionType.Up;
                else
                    goto ERROR;              

                return new Keyboard(keys, type);
            }
            ERROR:
            return new ParseError(line);
        }
    }
    public class Nop : ICommand
    {
        public Nop()
        {
        }
        public void DoWork()
        {
        }
        public override string ToString()
        {
            return "NOP";
        }
        public IEnumerable<ICommand> GetChildren()
        {
            return new List<ICommand>();
        }
        public static ICommand Parse(string line)
        {
            if (line.ToUpper() == "NOP" || string.IsNullOrWhiteSpace(line))
            {
                return new Nop();
            }
            return new ParseError(line);
        }
    }
    public sealed class ParseError : ICommand
    {
        string _str;
        public ParseError(string str)
        {
            _str = str;
        }
        public void DoWork()
        {
        }
        public IEnumerable<ICommand> GetChildren()
        {
            return new List<ICommand>();
        }
        public override string ToString()
        {
            return string.Format("ParseError(\"{0}\")", _str);
        }
    }
    public sealed class CommandFile : ICommand
    {
        string _filePath;
        IEnumerable<ICommand> _commands;
        public CommandFile(string filePath)
        {
            _filePath = filePath;

            string s = "";
            using (var sr = new StreamReader(_filePath))
            {
                s = sr.ReadToEnd();
            }
            var lines = s.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            _commands = Macro.TextParser(lines);
        }
        public void DoWork()
        {
            Macro.DoCommands(_commands);
            //foreach (var command in _commands)
            //{
            //    command.DoWork();
            //}        
        }

        public IEnumerable<ICommand> GetChildren()
        {
            return _commands;
        }

        public override string ToString()
        {
            return string.Format("COMMANDFILE {0}", _filePath);
        }

        public static ICommand Parse(string line)
        {
            var match3 = Regex.Match(line, "^COMMANDFILE (?<path>.+)$", RegexOptions.IgnoreCase);
            if (match3.Success)
            {
                var path = match3.Groups["path"].Value;
                return new CommandFile(path);
            }
            return new ParseError(line);
        }
    }
    public sealed class CommandTimes : ICommand
    {
        //ICommand _command;
        int _times;
        IEnumerable<ICommand> commands;
        public CommandTimes(ICommand command, int times)
        {
            //            _command = command;
            //            n = times;
            _times = times;
            commands = Enumerable.Repeat<ICommand>(command, times);
        }
        public void DoWork()
        {
            //for (int i = 0; i < n; i++)
            //{
            //    _command.DoWork();
            //}
            Macro.DoCommands(commands);
        }

        public IEnumerable<ICommand> GetChildren()
        {
            return commands;
        }

        public static ICommand Parse(string line, ICommand lastCommand)
        {
            var match00 = Regex.Match(line, "^(?<times>[0-9]+)$");
            if (match00.Success)
            {
                var times = int.Parse(match00.Groups["times"].Value);
                return new CommandTimes(lastCommand, times);
            }
            return new ParseError(line);
        }

        public override string ToString()
        {
            return string.Format("{0}", _times);
        }
    }
    public sealed class Comment : Nop
    {
        string _comment;
        public Comment(string comment)
        {
            _comment = comment;
        }
        public override string ToString()
        {
            return string.Format("//{0}", _comment);
        }
        public static new ICommand Parse(string line)
        {
            var match000 = Regex.Match(line, "^//(?<comment>.*)$");
            if (match000.Success)
            {
                var comment = match000.Groups["comment"].Value;
                return new Comment(comment);
            }
            return new ParseError(line);
        }
    }
    public class Macro
    {
        IEnumerable<ICommand> _commands;
        public Macro(IEnumerable<ICommand> commands)
        {
            _commands = commands;
        }
        public static IEnumerable<ICommand> TextParser(IEnumerable<string> multiLines)
        {
            var _commands = new List<ICommand>();
            ICommand lastCommand = new Nop();
            foreach (var line in multiLines)
            {
                var command = Macro.TextParser(line, lastCommand);
                _commands.Add(command);
                lastCommand = command;
            }
            return _commands;
        }
        public static ICommand TextParser(string line, ICommand lastCommand)
        {
            ICommand co;

            co = Nop.Parse(line);
            if (!(co is ParseError)) return co;

            co = Comment.Parse(line);
            if (!(co is ParseError)) return co;

            co = CommandTimes.Parse(line, lastCommand);
            if (!(co is ParseError)) return co;

            co = Wait.Parse(line);
            if (!(co is ParseError)) return co;

            co = Mouse.Parse(line);
            if (!(co is ParseError)) return co;

            co = Keyboard.Parse(line);
            if (!(co is ParseError)) return co;

            co = CommandFile.Parse(line);
            if (!(co is ParseError)) return co;

            return new ParseError(line);
        }
        public void Start()
        {
            DoCommands(_commands);
        }
        public delegate void CommandEventHandler(object sender, CommandEventArgs e);
        public static event CommandEventHandler CommandEvent;
        public static void DoCommands(IEnumerable<ICommand> commands)
        {
            foreach (var command in commands)
            {
                System.Diagnostics.Debug.WriteLine(command.ToString());
                if (CommandEvent != null)
                {
                    var args = new CommandEventArgs();
                    args.command = command.ToString();
                    CommandEvent(null, args);
                }
                command.DoWork();
            }
        }
    }

    public class MacroRecoder
    {
        List<ICommand> commands = new List<ICommand>();
        GlobalMouseHook mouseHook = new GlobalMouseHook();
        GlobalKeyListener keyHook = new GlobalKeyListener();
        public MacroRecoder()
        {
            mouseHook.MouseDown += mouseHook_MouseDown;
            mouseHook.MouseUp += mouseHook_MouseUp;
            mouseHook.MouseDClick += mouseHook_MouseDClick;
            mouseHook.MouseWheel += mouseHook_MouseWheel;
            mouseHook.MouseHWheel += mouseHook_MouseHWheel;

            keyHook.AllKeyDown += keyHook_AllKeyDown;
            keyHook.AllKeyUp += keyHook_AllKeyUp;
        }
        public void Start()
        {
            commands.Clear();

            mouseHook.Regist();
            keyHook.Regist();

            sw.Start();
        }

        void mouseHook_MouseHWheel(object sender, MouseEventArgs e)
        {
            AddWait();
            commands.Add(new Mouse(DeviceInputApi.MouseWheelType.Horizontal, e.Delta / 120));
        }

        void mouseHook_MouseWheel(object sender, MouseEventArgs e)
        {
            AddWait();
            commands.Add(new Mouse(DeviceInputApi.MouseWheelType.Vertical, e.Delta / 120));
        }

        void mouseHook_MouseDClick(object sender, MouseEventArgs e)
        {
            //TODO:
        }

        void mouseHook_MouseUp(object sender, MouseEventArgs e)
        {
            AddWait();
            commands.Add(new Mouse(e.Button, DeviceInputApi.ActionType.Up, e.X, e.Y));
        }

        void mouseHook_MouseDown(object sender, MouseEventArgs e)
        {
            AddWait();
            commands.Add(new Mouse(e.Button, DeviceInputApi.ActionType.Down, e.X, e.Y));
        }

        void keyHook_AllKeyUp(object sender, MyKeyEventArgs e)
        {
            AddWait();
            commands.Add(new Keyboard(e.Key, DeviceInputApi.ActionType.Up));
            Debug.WriteLine(e.Key.ToString());
        }

        void keyHook_AllKeyDown(object sender, MyKeyEventArgs e)
        {
            AddWait();
            commands.Add(new Keyboard(e.Key, DeviceInputApi.ActionType.Down));
            Debug.WriteLine(e.Key.ToString());
        }
        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        private void AddWait()
        {
            var milliSec = sw.ElapsedMilliseconds;
            commands.Add(new Wait((int)milliSec));
            sw.Reset();
            sw.Start();
        }
        public IEnumerable<ICommand> End()
        {
            sw.Stop();
            mouseHook.Unregist();
            keyHook.Unregist();
            return commands;
        }
    }
    public class CommandEventArgs : EventArgs
    {
        public string command;
    }
}
