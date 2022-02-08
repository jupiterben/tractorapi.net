using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;

namespace tractor.api
{
    public static class trutil
    {
        public static string _localhost = null;

        public static Dictionary<string, object> MergeDictData(Dictionary<string, object> data, Dictionary<string, object> update)
        {
            var ret = new Dictionary<string, object>();
            foreach(var k in data.Keys)
            {
                ret[k] = data[k];
            }
            foreach(var k in update.Keys)
            {
                ret[k] = data[k];
            }
            return ret;
        }

        public static string readUrl(string url)
        {
            WebClient client = new WebClient();
            // Add a user agent header in case the 
            // requested URI contains a query.
            client.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");
            Stream data = client.OpenRead(url);
            StreamReader reader = new StreamReader(data);
            string s = reader.ReadToEnd();
            Console.WriteLine(s);
            data.Close();
            reader.Close();
            return s;
        }

        public static string getlocalhost()
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
            string foreground;
            string background;
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

            public TerminalColor(string fg, string bg = null)
            {
                if (!this.colorcodes["fg"].ContainsKey(fg))
                {
                    throw new UnknownTerminalColor($"'{fg}' is an unknown foreground color, must be one of {this.colorcodes["fg"].Keys}");
                }
                this.foreground = fg;
                if (bg != null && !this.colorcodes["bg"].ContainsKey(bg))
                {
                    throw new UnknownTerminalColor($"'{bg}' is an unknown background color, must be one of {this.colorcodes["bg"].Keys}");
                }
                this.background = bg;
            }

            // Return a properly formatted escape sequence that can be
            //         interpretted by a terminal.
            public virtual object getEscSeq()
            {
                var fg = this.colorcodes["fg"][this.foreground].ToString("D2");
                var seq = $"\x1b[{fg}";
                if (this.background != null)
                {
                    var bg = this.colorcodes["bg"][this.background].ToString("D2");
                    seq += $";{bg}";
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
            public virtual string colorStr(string text)
            {
                return this.getEscSeq() + text + this.reset();
            }
        }

        public static Dictionary<string, TerminalColor> LogColors = new Dictionary<string, TerminalColor> {
            {"yellow",new TerminalColor("yellow")},
            {"red",new TerminalColor("red")},
            {"blue",new TerminalColor("blue")},
            {"white",new TerminalColor("white")},
            {"cyan",new TerminalColor("cyan")}};

        // Appends a time stamp and '==>' to a string before printing
        //     to stdout.
        public static void log(string msg, System.IO.Stream outfile = null, string color = null)
        {
            if (outfile == null)
            {
                outfile = Console.OpenStandardOutput();
            }
            if (color!= null && LogColors.ContainsKey(color))
            {
                var terminalColor = LogColors[color];
                msg = terminalColor.colorStr(msg);
            }
            try
            {
                Console.WriteLine(DateTime.Now + " ==> " + msg);
                outfile.Flush();
            }
            catch
            {
            }
        }

        public static void logWarning(object msg)
        {
            log("WARNING: " + msg, color: "yellow");
        }

        public static void logError(object msg)
        {
            log("ERROR: " + msg, color: "red");
        }
    }
}