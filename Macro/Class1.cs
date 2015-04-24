using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.IO;
using ryu_s.DeviceHook;
using System.Text.RegularExpressions;
namespace ryu_s.Macro
{
    public interface ICommand
    {
        void DoWork();
    }

    public class Wait : ICommand
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
    }
    public class Mouse : ICommand
    {
        string s = "";
        public class MouseAction
        {
            public MouseAction(MouseButtons button, DeviceInputApi.ActionType type, int x, int y, bool abusolutePos = true)
            {
            }
        }
        bool bMove = false;
        DeviceInputApi.MouseMoveType _moveType;
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
            Console.WriteLine(_button.ToString());
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
    }
    public class Keyboard : ICommand
    {
        public Keyboard(int vKey, DeviceInputApi.ActionType type)
        {
        }
        public void DoWork()
        {

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
    }
    public class ParseError : Nop
    {
        string _str;
        public ParseError(string str)
        {
            _str = str;
        }
        public override string ToString()
        {
            return string.Format("ParseError({0})", _str);
        }
    }
    public class CommandFile : ICommand
    {
        string _filePath;
        public CommandFile(string filePath)
        {
            _filePath = filePath;
        }
        public void DoWork()
        {
            string s = "";
            using (var sr = new StreamReader(_filePath))
            {
                s = sr.ReadToEnd();
            }
            var lines = s.Split(new [] { Environment.NewLine }, StringSplitOptions.None);
            var commands = Macro.TextParser(lines);
            foreach (var command in commands)
            {
                command.DoWork();
            }
        }
        public override string ToString()
        {
            return string.Format("COMMANDFILE {0}");
        }
    }
    public class CommandTimes : ICommand
    {
        ICommand _command;
        long n;
        public CommandTimes(ICommand command, long times)
        {
            _command = command;
            n = times;
        }
        public void DoWork()
        {
            for (int i = 0; i < n; i++)
            {
                _command.DoWork();
            }
        }
    }
    public class Comment : Nop
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
            if (string.IsNullOrWhiteSpace(line))
            {
                return new Nop();
            }
            var match000 = Regex.Match(line, "^//(?<comment>.*)$");
            if (match000.Success)
            {
                var comment = match000.Groups["comment"].Value;
                return new Comment(comment);
            }
            var match00 = Regex.Match(line, "^(?<times>[0-9]+)$");
            if (match00.Success)
            {
                var times = long.Parse(match00.Groups["times"].Value);
                return new CommandTimes(lastCommand, times);
            }
            var match0 = Regex.Match(line, "^WAIT (?<time>[0-9]+)$", RegexOptions.IgnoreCase);
            if (match0.Success)
            {
                var milli = int.Parse(match0.Groups["time"].Value);
                return new Wait(milli);
            }
            var match1 = Regex.Match(line, "^MOUSE_(?<button>[a-zA-Z]+)_(?<type>[a-zA-Z]+) (?<x>[-0-9]+) (?<y>[-0-9]+)$");
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
            var match3 = Regex.Match(line, "^COMMANDFILE (?<path>.+)$", RegexOptions.IgnoreCase);
            if (match3.Success)
            {
                var path = match3.Groups["path"].Value;
                return new CommandFile(path);
            }
        ERROR:
            return new ParseError(line);
        }
        public void Start()
        {
            foreach (var command in _commands)
            {
                command.DoWork();
            }
        }
    }
}
