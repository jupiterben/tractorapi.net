using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

namespace tractor.api
{
    public static class TrUtil
    {
        public static object _localhost = null;

        public static object getlocalhost()
        {
            if (_localhost == null)
            {
                _localhost = GetLocalIP();
            }
            return _localhost;
        }

        public static string GetLocalIP()
        {
            string hostName = Dns.GetHostName(); // Retrieve the Name of HOST
            // Get the IP
            var addressList = Dns.GetHostEntry(hostName).AddressList;
            if (addressList.Count() > 0)
            {
                return addressList[0].MapToIPv4().ToString();
            }
            return "";
        }

        public class UnknownTerminalColor: Exception
        {
            public UnknownTerminalColor(string msg) : base(msg)
            {
            }
        }

        // Simple class to support Terminal text coloring.
        public class TerminalColor
        {
            public Dictionary<string, Dictionary<string, int>> colorcodes = new Dictionary<string, Dictionary<string, int>> {
                {
                    "bg",
                    new Dictionary<string, int> {
                        {
                            "black",
                            40},
                        {
                            "red",
                            41},
                        {
                            "green",
                            42},
                        {
                            "yellow",
                            43},
                        {
                            "blue",
                            44},
                        {
                            "magenta",
                            45},
                        {
                            "cyan",
                            46},
                        {
                            "white",
                            47}}},
                {
                    "fg",
                    new Dictionary<string, int> {
                        {
                            "black",
                            30},
                        {
                            "red",
                            31},
                        {
                            "green",
                            32},
                        {
                            "yellow",
                            33},
                        {
                            "blue",
                            34},
                        {
                            "magenta",
                            35},
                        {
                            "cyan",
                            36},
                        {
                            "white",
                            37}}}};

            public TerminalColor(string fg, object bg = null)
            {
                if (!this.colorcodes["fg"].ContainsKey(fg))
                {
                    throw new UnknownTerminalColor($"'{fg}' is an unknown foreground color, must be one of {this.colorcodes["fg"].Keys}");
                }
                this.foreground = fg;
                if (bg && !this.colorcodes["bg"].Contains(bg))
                {
                    throw new UnknownTerminalColor(String.Format("'%s' is an unknown background color, must be one of %s", bg, this.colorcodes["bg"].keys()));
                }
                this.background = bg;
            }

            // Return a properly formatted escape sequence that can be
            //         interpretted by a terminal.
            public virtual object getEscSeq()
            {
                var seq = String.Format("\x1b[%.2d", this.colorcodes["fg"][this.foreground]);
                if (this.background)
                {
                    seq += String.Format(";%.2d", this.colorcodes["bg"][this.background]);
                }
                seq += "m";
                return seq;
            }

            // Reset the color back to default settings.
            public virtual object reset()
            {
                return "\x1b[0m";
            }

            // Color a string of text this color.
            public virtual object colorStr(object text)
            {
                return this.getEscSeq() + text + this.reset();
            }

            // Tests equality with other TerminalColors.
            public virtual object @__eq__(object color)
            {
                return this.foreground == color.foreground && this.background == color.background;
            }
        }

        public static object LogColors = new Dictionary<object, object> {
            {
                "yellow",
                TerminalColor("yellow")},
            {
                "red",
                TerminalColor("red")},
            {
                "blue",
                TerminalColor("blue")},
            {
                "white",
                TerminalColor("white")},
            {
                "cyan",
                TerminalColor("cyan")}};

        // Appends a time stamp and '==>' to a string before printing
        //     to stdout.
        public static object log(object msg, object outfile = null, object color = null)
        {
            if (!outfile)
            {
                outfile = sys.stdout;
            }
            if (color && LogColors.Contains(color))
            {
                var terminalColor = LogColors[color];
                msg = terminalColor.colorStr(msg);
            }
            try
            {
                Console.WriteLine(time.ctime() + " ==> " + msg, file: outfile);
                outfile.flush();
            }
            catch
            {
            }
        }

        public static object logWarning(object msg)
        {
            log("WARNING: " + msg, color: "yellow");
        }

        public static object logError(object msg)
        {
            log("ERROR: " + msg, color: "red");
        }
    }
}