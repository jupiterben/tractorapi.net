using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class Helper
{
    public static string GetEnvVar(string name, string defaultValue)
    {
        try
        {
            return Environment.GetEnvironmentVariable("TRACTOR_ENGINE");
        }
        catch (Exception)
        {
            return defaultValue;
        }
    }

    public static string DEFAULT_ENGINE = "tractor-l:80";
    public static int DEFAULT_ENGINE_PORT = 80;
    public static string DEFAULT_USER = "root";
    public static string DEFAULT_PASSWORD = null;
    // Return (hostname, port) for the given engine string.  Defaults to port 80 if none specified.
    public static Tuple<string, int> hostnamePortForEngine(string engineName)
    {
        var parts = engineName.Split(':');
        if (parts.Count() == 1)
        {
            return Tuple.Create(engineName, DEFAULT_ENGINE_PORT);
        }
        else
        {
            int port;
            if (!int.TryParse(parts[1], out port))
            {
                throw new EngineError(String.Format("'{0}' must be a numeric value for port.", parts[1]));
            }
            return Tuple.Create(parts[0], Convert.ToInt32(parts[1]));
        }
    }

    // Return path to tractor preferences directory in standard renderman location.
    public static string rendermanPrefsDir()
    {
        string prefsDir;
        OperatingSystem os = Environment.OSVersion;
        PlatformID pid = os.Platform;
        if (pid == PlatformID.MacOSX)
        {
            var homeDir = Helper.GetEnvVar("HOME", "/tmp");
            prefsDir = Path.Combine(homeDir, "Library", "Preferences", "Pixar", "Tractor");
        }
        else if (pid == PlatformID.Win32Windows)
        {
            var appDir = Helper.GetEnvVar("APPDATA", "/tmp");
            prefsDir = Path.Combine(appDir, "Pixar", "Tractor");
        }
        else
        {
            var homeDir = Helper.GetEnvVar("HOME", "/tmp");
            prefsDir = Path.Combine(homeDir, ".pixarPrefs", "Tractor");
        }
        return prefsDir;
    }

    // Helper function for creating a session filename.  Parameters help to specify something
    //     that is unique across multiple engines, apps, and users.  If no baseDir is specified,
    //     the standard renderman preferenes directory is used.
    public static string sessionFilename(
        string app,
        string engineHostname,
        int port,
        string clientHostname,
        string user,
        string baseDir = null)
    {
        baseDir = baseDir ?? rendermanPrefsDir();
        var sessionDir = Path.Combine(baseDir, "sites", $"{engineHostname}@{port}");
        return Path.Combine(sessionDir, $"{app}.{clientHostname}.{user}.session");
    }
}


public class EngineError : Exception
{
    public EngineError(string msg) : base(msg) { }
}

// Base class for EngineClient exceptions.
public class EngineClientError : Exception
{
    public EngineClientError(string msg) : base(msg) { }
}

// Raised when an attempt has been made to modify an invalid connection parameter.
public class InvalidParamError : EngineClientError
{
    public InvalidParamError(string msg) : base(msg) { }
}

// Raised when a password is required to establish a session.
public class PasswordRequired : EngineClientError
{
    public PasswordRequired(string msg) : base(msg) { }
}

// Raised when there is a problem opening a connection with the engine.
public class OpenConnError : EngineClientError
{
    public OpenConnError(string msg) : base(msg) { }
}

// Raised when there is a problem creating the directory for the session file..
public class CreateSessionDirectoryError
    : EngineClientError
{
    public CreateSessionDirectoryError(string msg) : base(msg) { }
}

// Raised when there is a problem writing the session file..
public class CreateSessionFileError
    : EngineClientError
{
    public CreateSessionFileError(string msg) : base(msg) { }
}

// Raised when there is a problem loggin in to the engine.
public class LoginError : EngineClientError
{
    public LoginError(string msg) : base(msg) { }
}

// Raised when the engine returns a non-zero return code.
public class TransactionError : EngineClientError
{
    public TransactionError(string msg) : base(msg) { }
}

// Raised when there is a postgres error executing arbitrary SQL using the
//     EngineClient.dbexec() function.
public class DBExecError : EngineClientError
{
    public DBExecError(string msg) : base(msg) { }
}

// public class DictObj
// {
//     public DictObj(Hashtable kwargs)
//     {
//         foreach (var _tup_1 in kwargs.items())
//         {
//             var key = _tup_1.Item1;
//             var val = _tup_1.Item2;
//             setattr(this, key, val);
//         }
//     }
// }

// This class is used to manage connections with the engine.

namespace tractor.api
{
    using Newtonsoft.Json;
    using System.Net.Http;
    using re = System.Text.RegularExpressions.Regex;
    using os = System.OperatingSystem;

    public interface ILogger
    {
        void debug(string msg);
        void Login(string user, string password);
    }

    public class TrHttpRPC
    {
        public TrHttpRPC(string hostname, int port, object apphdrs, double timeout) { }
        public bool PasswordRequired() { return false; }
        public object Login(string user, string password) { return null; }
        public Tuple<int, object> Transaction(string url, object payload, string translation, object headers) { return null; }
    }

    public class EngineClient
    {
        public string QUEUE = "queue";

        public string MONITOR = "monitor";

        public string CONTROL = "ctrl";

        public string BTRACK = "btrack";

        public string SPOOL = "spool";

        public string TASK = "task";

        public string CONFIG = "config";

        public string DB = "db";

        public List<string> VALID_PARAMETERS = new List<string>() { "hostname", "port", "user", "password", "debug", "newSession", "sessionFilename" };

        public string LIMITS_CONFIG_FILENAME = "limits.config";

        public string CREWS_CONFIG_FILENAME = "crews.config";

        public string BLADE_CONFIG_FILENAME = "blade.config";

        public string TRACTOR_CONFIG_FILENAME = "tractor.config";

        public string SPOOL_VERSION = "2.0";

        string hostname;
        int port;
        string user;
        string password;
        bool debug;
        ILogger logger;
        bool newSession;
        Dictionary<string, string> lmthdr;
        string sessionFilename;
        object tsid;
        TrHttpRPC conn;
        //HttpClient conn;

        public static void setattr(object obj, string key, object value)
        {
            var propertyInfo = obj.GetType().GetProperty(key, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            propertyInfo.SetValue(obj, value, null);
        }

        public static object getattr(object obj, string key, object defaultValue)
        {
            var propertyInfo = obj.GetType().GetProperty(key, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            return propertyInfo.GetValue(obj);
        }

        public EngineClient(
            string hostname = null,
            int? port = null,
            string user = null,
            string password = null,
            string sessionFilename = null,
            bool debug = false,
            ILogger logger = null)
        {
            // connection parameters
            var _tup_1 = Helper.hostnamePortForEngine(Helper.GetEnvVar("TRACTOR_ENGINE", Helper.DEFAULT_ENGINE));
            var fallbackHostname = _tup_1.Item1;
            var fallbackPort = _tup_1.Item2;
            this.hostname = hostname ?? fallbackHostname;
            this.port = port ?? fallbackPort;
            this.user = user ?? Helper.GetEnvVar("USER", Helper.DEFAULT_USER);
            this.password = password ?? Helper.GetEnvVar("TRACTOR_PASSWORD", Helper.DEFAULT_PASSWORD);
            this.debug = debug && bool.Parse(Helper.GetEnvVar("TRACTOR_DEBUG", "false"));
            this.logger = logger;
            // create descriptive headers for readability purposes in server logs
            var appName = "EngineClient";
            var appVersion = "1.0";
            var appDate = "app date";
            this.lmthdr = new Dictionary<string, string>
        {
            {"User-Agent",  $"Pixar-${appName}/{appVersion} ({appDate})"},
            {"X-Tractor-Blade",  "0"}
        };
            // gets set to True in setParam() for open() to explicitly open new connection
            this.newSession = false;
            // gets set for open() to read/write session file
            this.sessionFilename = sessionFilename;
            // session id with engine
            this.tsid = null;
            // TrHttpRPC connection with engine
            this.conn = null;
        }

        public virtual Dictionary<string, object> xheaders()
        {
            // dynamically generate xheaders so that it can adapt to a reconfiguration of the hostname or port
            return new Dictionary<string, object> {
                    {"Host",$"{this.hostname}:{this.port}"},
                    {"Cookie",$"TractorUser={this.user}"}
        };
        }

        // Set one or more connection parameters: hostname, port, user, password, and debug.
        public virtual void setParam(Hashtable kw)
        {
            // if engine is specified, replace the class engine client object
            foreach (DictionaryEntry _tup_1 in kw)
            {
                var key = _tup_1.Key as string;
                var value = _tup_1.Value;
                if (!this.VALID_PARAMETERS.Contains(key))
                {
                    throw new InvalidParamError($"{key.ToString()} is not a valid parameter.  Must be in {this.VALID_PARAMETERS.ToString()}.");
                }
                setattr(this, key, value);
            }
        }

        // Return True if a connection is considered to have been established.
        public virtual bool isOpen()
        {
            // a known session id is considered to represent an established connection
            return this.tsid != null;
        }

        // Display message when running in debug mode.
        public virtual void dprint(string msg)
        {
            if (this.debug)
            {
                msg = $"[{this.hostname}:{this.port}] {msg}";
                if (this.logger != null)
                {
                    this.logger.debug(msg);
                }
                else
                {
                    trutil.log(msg);
                }
            }
        }

        // Display url when running in debug mode.
        public virtual void dprintUrl(string url)
        {
            if (this.debug)
            {
                var msg = $"[{hostname}:{port}] http://{hostname}:{port}/Tractor/{url}";
                if (this.logger != null)
                {
                    this.logger.debug(msg);
                }
                else
                {
                    trutil.log(msg);
                }
            }
        }

        // Return the path to the preferences directory for a client with this engine.
        public virtual string prefsDir()
        {
            var engineID = $"{this.hostname}@{this.port}";
            return Path.Combine(Helper.rendermanPrefsDir(), "sites", engineID);
        }

        // Return True if prior session can be used to communicate with engine.
        public virtual bool canReuseSession()
        {
            this.dprint("test if session can be reused");
            object sessionInfo = null;
            if (this.sessionFilename == null || !File.Exists(this.sessionFilename))
            {
                return false;
            }
            try
            {
                var f = File.ReadAllText(this.sessionFilename);
                sessionInfo = JsonConvert.DeserializeObject(f);
            }
            catch (Exception err)
            {
                trutil.logWarning($"problem reading session file: {err.Message}");
                return false;
            }
            var tsid = getattr(sessionInfo, "tsid", null);
            // test session id
            try
            {
                this._transaction(this.CONTROL, new Dictionary<string, object> {
                        {"q","status"},
                        {"tsid",tsid}
                }
                , skipLogin: true);
                return true;
            }
            catch (EngineClientError err)
            {
                this.dprint($"cannot reuse session: {err.Message}");
                return false;
            }
        }

        // Returns True if the engine is using passwords for authentication.
        public virtual bool usesPasswords()
        {
            if (this.conn.PasswordRequired())
            {
                this.dprint("the engine has passwords enabled");
                return true;
            }
            else
            {
                this.dprint("the engine has passwords disabled");
                return false;
            }
        }

        // Returns True if a the existing session cannot be reused and
        //         passwords are enabled and a password has not been specified.
        //         
        public virtual object needsPassword()
        {
            this.dprint("test if a password needs to be specified");
            if (this.conn == null)
            {
                this.conn = new TrHttpRPC(this.hostname, port: this.port, apphdrs: this.lmthdr, timeout: 3600);
            }
            if (this.canReuseSession() || !this.usesPasswords() || this.password != null)
            {
                this.dprint("password is not needed or has already been specified");
                return false;
            }
            else
            {
                this.dprint("password must be specified");
                return true;
            }
        }

        // Establish connection with engine.  If self.newSession is True,
        //         or a session has not already been established, then a new
        //         session will be created.  If self.sessionFilename is set, then the
        //         file will be tested to see if it stores a valid session id;
        //         if it is not valid, a new session will be established.  If
        //         a new session is established, whether due to self.newSession
        //         being True or the session id in the session file being invalid,
        //         the new session id will be written to file if self.sessionFilename
        //         has been set.
        //         
        public virtual void open()
        {
            this.dprint(String.Format("open(), self.newSession=%s, self.sessionFilename=%s", this.newSession.ToString(), this.sessionFilename.ToString()));
            if (!this.newSession && this.conn != null && this.isOpen())
            {
                // only reuse the existing connection if the client setting 
                // is not explicitly requreing a new session,
                // and there is an existing TrHttpRPC object to manage communication
                // and a tsid has been obtained for this client.
                this.dprint("session already established");
                return;
            }
            this.conn = new TrHttpRPC(this.hostname, port: this.port, apphdrs: this.lmthdr, timeout: 3600);
            if (this.password != null)
            {
                if (!this.newSession && this.canReuseSession())
                {
                    this.dprint("reuse engine connection");
                    return;
                }
                if (this.conn.PasswordRequired())
                {
                    this.dprint(String.Format("Password required for %s@%s:%d ", this.user, this.hostname, this.port));
                    throw new PasswordRequired(String.Format("Password required for %s@%s:%d ", this.user, this.hostname, this.port));
                }
            }
            this.dprint("open engine connection");
            object response = null;
            try
            {
                response = this.conn.Login(this.user, this.password);
            }
            catch (Exception err)
            {
                this.dprint($"Login() failed: {err.Message}");
                this.tsid = null;
                string msg;
                if (re.IsMatch("login as '.*' failed", err.Message))
                {
                    msg = $"Unable to log in as user {this.user} on engine {this.hostname}:{this.port}.";
                }
                else
                {
                    msg = $"Engine on {this.hostname}:{this.port} is not reachable.";
                }
                throw new OpenConnError(msg);
            }
            this.tsid = getattr(response, "tsid", null);
            if (this.tsid == null)
            {
                string msg = $"Error logging in as user {this.user} on engine {this.hostname}:{this.port}";
                this.dprint(msg);
                throw new LoginError(msg);
            }
            // save tsid to session file for future reuse
            if (this.sessionFilename != null)
            {
                this.writeSessionFile();
            }
        }

        // Write the session file, creating the directory if necessary.
        public virtual void writeSessionFile()
        {
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(this.sessionFilename));
            try
            {
                File.WriteAllText(this.sessionFilename, $"{{\"tsid\": \"{this.tsid}\"}}\n");
            }
            catch (Exception err)
            {
                var msg = String.Format("problem writing session file '%s': %s", this.sessionFilename.ToString(), err.ToString());
                this.dprint(msg);
                throw new CreateSessionFileError(msg);
            }
            this.dprint(String.Format("wrote session file %s", this.sessionFilename));
        }

        // Build a URL.
        public virtual string constructURL(string queryType, Dictionary<string, object> keyValuePairs)
        {
            var parts = new List<string>();
            foreach (var _tup_1 in keyValuePairs)
            {
                var key = _tup_1.Key;
                var value = _tup_1.Value;
                if (value is List<object>)
                {
                    var list = value as List<object>;
                    // this will automatically change lists into comma-separated values. e.g. [1,3,5] => '1,3,5'
                    value = string.Join(",", list.Select(item => item.ToString()).ToList());
                }

                var urlEncode = System.Web.HttpUtility.UrlEncode(value as string);
                parts.Add($"{key}={urlEncode}");
            }
            if (this.tsid != null)
            {
                parts.Add($"tsid={this.tsid}");
            }
            return queryType + "?" + string.Join("&", parts);
        }

        // Extract the important part of a traceback.
        public virtual string _shortenTraceback(string msg)
        {
            // extract the error message displayed on the line after the line containing "raise" 
            var matches = re.Matches(@"\n\s*raise .*\n(\w+\:.*)\n", msg);
            if (matches.Count > 0)
            {
                // choose the last exception displayed (with [-1]) and the first element [0] has the full message
                return matches[-1].Value;
            }
            // sometimes there is no raise line, but there is a CONTEXT line afterwards
            matches = re.Matches(@"\n(\w+\:.*)\n\nCONTEXT\:", msg);
            if (matches.Count > 0)
            {
                // choose the last exception displayed (with [-1]) and the first element [0] has the full message
                return matches[-1].Value;
            }
            return msg;
        }

        // Send URL to engine, parse and return engine's response.
        public virtual object _transaction(
            string urltype,
            Dictionary<string, object> attrs,
            object payload = null,
            string translation = "JSON",
            Dictionary<string, object> headers = null,
            bool skipLogin = false)
        {
            // support lazy opening of connection
            if (skipLogin)
            {
                // login is skipped for spooling, so there may not be a TrHttpRPC object yet
                if (this.conn == null)
                {
                    this.conn = new TrHttpRPC(this.hostname, port: this.port, apphdrs: this.lmthdr, timeout: 3600);
                }
            }
            else if (!this.isOpen())
            {
                this.open();
            }
            var url = this.constructURL(urltype, attrs);
            this.dprintUrl(url);
            headers = trutil.MergeDictData(headers, this.xheaders());

            var _tup_1 = this.conn.Transaction(url, payload, translation, headers);
            var rcode = _tup_1.Item1;
            var data = _tup_1.Item2;
            if (rcode != 0)
            {
                string msg;
                try
                {
                    object datadict = null;//ast.literal_eval(data.ToString());
                    var err = getattr(datadict, "msg", $"unknown message: {data.ToString()}");
                    if (this.debug)
                    {
                        var rc = getattr(datadict, "rc", "unknown rc");
                        msg = $"[{this.hostname}:{this.port}] error {rc}: {err}";
                    }
                    else
                    {
                        msg = this._shortenTraceback(err.ToString());
                    }
                }
                catch (Exception)
                {
                    msg = data.ToString();
                }
                throw new TransactionError(msg);
            }
            return data;
        }

        // Fetch the next subscription message. This is a blocking call, and it is unknown
        //         how long the engine may take to respond.
        public virtual object subscribe(object jids = null)
        {
            jids = jids ?? new List<int> { 0 };
            var attrs = new Dictionary<string, object> {
                    {"q","subscribe"},
                    {"jids",jids}
            };
            var result = this._transaction(this.MONITOR, attrs);
            return result;
        }

        // Execute an arbitrary SQL statement on the postgres server, using the engine as a proxy.
        //         The result will be a dictionary, with one entry being a JSON encoded list of the
        //         result rows.
        public virtual List<string> dbexec(string sql)
        {
            
            this.dprint($"sql = {sql}");
            var result = this._transaction(this.DB, new Dictionary<string, object> {
                {"q",sql}
            });
            // an error could be reported through either:
            //  rc: for psql client errors
            //  rows: for tractorselect traceback errors, such as for syntax errors in search clause 
            var rc = getattr(result, "rc", 1);
            var rows = getattr(result, "rows", null);
            bool isError = !(rows is List<string>);
            this.dprint($"rc={rc}, isError={isError}");
            string err;
            if (rc!=null)
            {
                err = getattr(result, "msg", null) as string ?? $"postgres server did not specify an error message for dbexec({sql})";
            }
            else if (isError)
            {
                err = rows.ToString();
            }
            else
            {
                err = null;
            }
            if (err!=null)
            {
                if (this.debug)
                {
                    // return full stack trace from server
                    err = "error message from postgres server:\n" + "---------- begin error ----------\n" + err + "----------- end error -----------";
                }
                else
                {
                    // just set the message to a exception if one existed
                    //err = self._shortenTraceback(err)
                    err = err.ToString();//.strip()
                    var errLines = err.Split('\n');
                    err = errLines[errLines.Count()-1];
                }
                throw new DBExecError(err);
            }
            return rows as List<string>;
        }

        // Select items from the specified table, using the given natural language where clause.
        public virtual object select(
            string tableName,
            string where,
            List<string> columns = null,
            List<string> sortby = null,
            int? limit = null,
            bool archive = false,
            object aliases = null)
        {
            var whereStr = where.Replace("'", "''");
            var colStr = columns == null ? "" : string.Join(",", columns);
            var sortStr = sortby == null ? "" : string.Join(",", sortby);
            var limitStr = limit == null ? "NULL" : limit.ToString();
            var archStr = archive ? "t" : "f";
            var aliasStr = aliases.ToString().Replace("'", "''");
            var sql = $"tractorselect('{tableName}', '{whereStr}', '{colStr}', '{sortStr}', {limitStr}, '{archStr}', '{aliasStr}')";
            var rows = this.dbexec(sql);
            return rows;
//             var attrs = new Dictionary<string, object> {
//                     {"q","select"},
//                     {"table",tableName},
//                     {"where",where},
//                     {"columns",colStr},
//                     {"orderby",sortStr},
//                     {"limit",limit.ToString()}
//         };
//             var result = this._transaction(this.MONITOR, attrs);
//             // result is a dictionary with a "rows" entry that is a list of key/value pairs
//             return result;
        }

        // Set a job's attribute to the specified value.
        public virtual void _setAttributeJob(object jid, string attribute, object value, Hashtable kwargs = null)
        {
            var attrs = new Dictionary<string, object> {
            {"q","jattr"},
            {"jid",jid},
            {"set_" + attribute,value}
        };
            // add all non-None values to URL query parameters
            foreach (DictionaryEntry _tup_1 in kwargs)
            {
                if (_tup_1.Value != null)
                {
                    attrs[_tup_1.Key as string] = _tup_1.Value;
                }
            }
            this._transaction(this.QUEUE, attrs);
        }

        // Set a command's attribute to the specified value.
        public virtual void _setAttributeCommand(object jid, object cid, object attribute, object value)
        {
            var attrs = new Dictionary<string, object> {
                {"q","cattr"},
                {"jid",jid},
                {"cid",cid},
                {"set_" + attribute,value}
            };
            this._transaction(this.QUEUE, attrs);
        }

        // Set a blade's attribute to the specified value.
        public virtual void _setAttributeBlade(object bladeName, object ipaddr, string attribute, object value)
        {
            var bladeId = $"{bladeName}/{ipaddr}";
            var attrs = new Dictionary<string, object> {
                {"q","battribute"},
                {"b",bladeId},
                {attribute,value}
            };
            this._transaction(this.CONTROL, attrs);
        }

        // Set a job's priority.
        public virtual void setJobPriority(object jid, object priority)
        {
            this._setAttributeJob(jid, "priority", priority);
        }

        // Set a job's crew list.
        public virtual void setJobCrews(object jid, object crews)
        {
            this._setAttributeJob(jid, "crews", string.Join(",", crews));
        }

        // Set a job's attribute to the specified value.
        public virtual void setJobAttribute(object jid, string key, object value)
        {
            if (value is List<object>)
            {
                var list = value as List<object>;
                value = string.Join(",", list.Select(item => item.ToString()).ToList());
            }
            this._setAttributeJob(jid, key, value);
        }

        // Pause a job.
        public virtual void pauseJob(object jid)
        {
            this._setAttributeJob(jid, "pause", 1);
        }

        // Unpause a job.
        public virtual void unpauseJob(object jid)
        {
            this._setAttributeJob(jid, "pause", 0);
        }

        // Lock a job.
        public virtual void lockJob(object jid, object note = null)
        {
            // TODO: specify note
            //this._setAttributeJob(jid, "lock", 1, note: note);
        }

        // Unlock a job.
        public virtual void unlockJob(object jid)
        {
            // TODO: specify unlocking user
            this._setAttributeJob(jid, "lock", 0);
        }

        // Interrupt a job.
        public virtual void interruptJob(object jid)
        {
            var attrs = new Dictionary<string, object> {
                    {"q","jinterrupt"},
                    {"jid",jid}
            };
            this._transaction(this.QUEUE, attrs);
        }

        // Restart a job.
        public virtual void restartJob(object jid)
        {
            var attrs = new Dictionary<string, object> {
                    {"q","jrestart"},
                    {"jid",jid}
            };
            this._transaction(this.QUEUE, attrs);
        }

        // Retry all active tasks of a job.
        public virtual void retryAllActiveInJob(object jid)
        {
            var attrs = new Dictionary<string, object> {
                    {"q","jretry"},
                    {"tsubset","active"},
                    {"jid",jid}
            };
            this._transaction(this.QUEUE, attrs);
        }

        // Retry all errored tasks of a job.
        public virtual void retryAllErrorsInJob(object jid)
        {
            var attrs = new Dictionary<string, object> {
                    {"q","jretry"},
                    {"tsubset","error"},
                    {"jid",jid}
            };
            this._transaction(this.QUEUE, attrs);
        }

        // Skip all errored tasks of a job.
        public virtual void skipAllErrorsInJob(object jid)
        {
            var attrs = new Dictionary<string, object> {
                    {"q","tskip"},
                    {"tsubset","error"},
                    {"jid",jid}
            };
            this._transaction(this.QUEUE, attrs);
        }

        // Set delay time of a job.
        public virtual void delayJob(string jid, object delayTime)
        {
            this.setJobAttribute(jid, "afterTime", delayTime.ToString());
        }

        // Clear delay time of a job.
        public virtual void undelayJob(object jid)
        {
            this.setJobAttribute(jid, "afterTime", "0");
        }

        // Delete a job.
        public virtual void deleteJob(object jid)
        {
            var attrs = new Dictionary<string, object> {
                    {"q","jretire"},
                    {"jid",jid}
            };
            this._transaction(this.QUEUE, attrs);
        }

        // Un-delete a job.
        public virtual void undeleteJob(object jid)
        {
            var attrs = new Dictionary<string, object> {
                    {"q","jrestore"},
                    {"jid",jid}
            };
            this._transaction(this.QUEUE, attrs);
        }

        // Fetch SQL dump of job.
        public virtual string getJobDump(int jid, string fmt = "JSON")
        {
            var result = this.dbexec($"TractorJobDump({jid}, '{fmt}')");
            if (result.Count > 0)
            {
                return result[0];
            }
            else
            {
                return "";
            }
        }

        // Retry a task.
        public virtual void retryTask(object jid, object tid)
        {
            var attrs = new Dictionary<string, object> {
                    {"q","tretry"},
                    {"jid",jid},
                    {"tid",tid}
            };
            this._transaction(this.QUEUE, attrs);
        }

        // Resume a task.
        public virtual void resumeTask(object jid, object tid)
        {
            var attrs = new Dictionary<string, object> {
                    {"q","tretry"},
                    {"recover",1},
                    {"jid",jid},
                    {"tid",tid}
            };
            this._transaction(this.QUEUE, attrs);
        }

        // Kill a task.
        public virtual void killTask(string jid, object tid)
        {
            var attrs = new Dictionary<string, object> {
                    {"q","jinterrupt"},
                    {"jid",jid},
                    {"tid",tid}
            };
            this._transaction(this.QUEUE, attrs);
        }

        // Skip a task.
        public virtual void skipTask(string jid, object tid)
        {
            var attrs = new Dictionary<string, object> {
                    {"q","tskip"},
                    {"jid",jid},
                    { "tid",tid}
            };
            this._transaction(this.QUEUE, attrs);
        }

        // Set a command's attribute to the specified value.
        public virtual void setCommandAttribute(object jid, object cid, object key, object value)
        {
            if (value is List<object>)
            {
                var list = value as List<object>;
                value = string.Join(",", list.Select(item=>item.ToString()).ToList());
            }
            this._setAttributeCommand(jid, cid, key, value);
        }

        // Return the command details for a task.
        public virtual object getTaskCommands(object jid, object tid)
        {
            var attrs = new Dictionary<string, object> {
                    {"q","taskdetails"},
                    {"jid",jid},
                    {"tid",tid}};
            var result = this._transaction(this.MONITOR, attrs) as Dictionary<string , object>;
            if (!result.ContainsKey("cmds"))
            {
                return null;
            }
            var lines = new List<object>();
            var cmds = result["cmds"] as List<object>;
            // formats = [
            //     Formats.IntegerFormat("cid", width=4),
            //     Formats.StringFormat("state", width=8),
            //     Formats.StringFormat("service", width=12),
            //     Formats.StringFormat("tags", width=12),
            //     Formats.StringFormat("type", width=4),
            //     Formats.TimeFormat("t0", header="start", width=11),
            //     Formats.TimeFormat("t1", header="stop", width=11),
            //     Formats.ListFormat("argv", header="command")
            //     ]
            // cmdFormatter = Formats.Formatter(formats)
            var headings = new List<string> {
                    "cid",
                    "state",
                    "service",
                    "tags",
                    "type",
                    "start",
                    "stop",
                    "argv"
                };
            lines.Add(string.Join(" ", headings));
            lines.Add(string.Join(" ", headings.Select(h => new string('=', h.Count()).ToList())));
            foreach (var cmd in cmds)
            {
                var cmdObj = cmd as Dictionary<string, object>;// DictObj(cmd);
                var line = $"{cmdObj["cid"]} {cmdObj["stat"]} {cmdObj["service"]} {cmdObj["tags"]} {cmdObj["type"]} {cmdObj["t0"]} {cmdObj["t1"]} {cmdObj["argv"]}";
                lines.Add(line);
            }
            return string.Join("\n",lines);
        }

        // Return the command logs for a task.
        public virtual object getTaskLog(object jid, object tid, object owner = null)
        {
            
            var attrs = new Dictionary<string, object> {
                    {"q","tasklogs"},
                    {"jid",jid},
                    {"tid",tid}
            };
            if (owner!=null)
            {
                attrs["owner"] = owner;
            }
            var logInfo = this._transaction(this.MONITOR, attrs) as Dictionary<string, object>;
            var logLines = new List<string>();
            if (!logInfo.ContainsKey("LoggingRedirect"))
            {
                return "";
            }
            var logURIs = logInfo["LoggingRedirect"] as List<string>;

            foreach (var logURI in logURIs)
            {
                string fullURI;
                if (logURI.StartsWith("http://"))
                {
                    fullURI = logURI;
                }
                else
                {
                    fullURI = $"http://{this.hostname}:{this.port}{logURI}";
                }
                // fetch the log
                string logResult = null;
                try
                {
                    logResult = trutil.readUrl(fullURI);
                }
                catch (Exception err)
                {
                    logResult = String.Format("Exception received in EngineClient while fetching log: %s", err.Message);
                }
                logLines.Add(logResult);// .decode("utf-8"));
            }
            return string.Join("",logLines);
        }

        // Return the job description in JSON format.
        public virtual object fetchJobsAsJSON(string filterName = null)
        {
            var attrs = new Dictionary<string, object> {
                    {"q","jobs"}
            };
            if (filterName != null)
            {
                // this additional level of escaping is for file-naming safety when the filter is stored in the file system
                // arguably such safety should happen on the server, not the client
                attrs["filter"] = System.Web.HttpUtility.UrlEncode(filterName + ".joblist");
            }
            var jobInfo = this._transaction(this.MONITOR, attrs);
            return jobInfo;
        }

        // Return the job information in JSON format.
        public virtual object fetchJobDetails(object jid, bool graph = true, bool notes = true)
        {
            var attrs = new Dictionary<string, object> {
                    {"q","jobdetails"},
                    {"jid",jid},
                    {"graph",Convert.ToInt32(graph)},
                    {"notes",Convert.ToInt32(notes)},
                    {"flat",1}
            };
            var jobDetails = this._transaction(this.MONITOR, attrs);
            return jobDetails;
        }

        // Return the status of all blades in JSON format.
        public virtual object fetchBladesAsJSON(string filterName = null)
        {
            var attrs = new Dictionary<string, object> {
                    {"q","blades"}
            };
            if (filterName != null)
            {
                attrs["filter"] = filterName + ".bladelist";
            }
            var bladeInfo = this._transaction(this.MONITOR, attrs);
            return bladeInfo;
        }

        // Nimby a bade.
        public virtual void nimbyBlade(string bladeName, object ipaddr, object allow = null)
        {
            this._setAttributeBlade(bladeName, ipaddr, "nimby", allow ?? 1);
        }

        // Unnimby a bade.
        public virtual void unnimbyBlade(object bladeName, object ipaddr)
        {
            this._setAttributeBlade(bladeName, ipaddr, "nimby", 0);
        }

        // Return the tracer output for a blade.
        public virtual object traceBlade(object bladeName, object ipaddr)
        {
            var bladeId = String.Format("%s/%s", bladeName, ipaddr);
            var attrs = new Dictionary<string, object> {
                    {"q","tracer"},
                    {"t",bladeId},
                    {"fmt","plain"}
            };
            var trace = this._transaction(this.CONTROL, attrs, translation: null) ?? "";
            return trace;
        }

        // Retry active tasks on a blade.
        public virtual void ejectBlade(object bladeName, object ipaddr)
        {
            var bladeId = String.Format("%s/%s", bladeName, ipaddr);
            var attrs = new Dictionary<string, object> {
                    {"q","ejectall"},
                    {"blade",bladeId}
            };
            this._transaction(this.QUEUE, attrs);
        }

        // Remove blade entry from database.
        public virtual void delistBlade(object bladeName, object ipaddr)
        {
            var bladeId = String.Format("%s/%s", bladeName, ipaddr);
            var attrs = new Dictionary<string, object> {
                    {"q","delist"},
                    {"id",bladeId}
            };
            this._transaction(this.BTRACK, attrs);
        }

        // Cause the engine the reload the limits.config file.
        public virtual void reloadLimitsConfig()
        {
            var attrs = new Dictionary<string, object> {
                    {"q","reconfigure"},
                    {"file",this.LIMITS_CONFIG_FILENAME}
            };
            this._transaction(this.CONTROL, attrs);
        }

        // Cause the engine the reload the crews.config file.
        public virtual void reloadCrewsConfig()
        {
            var attrs = new Dictionary<string, object> {
                    {"q","reconfigure"},
                    {"file",this.CREWS_CONFIG_FILENAME}
            };
            this._transaction(this.CONTROL, attrs);
        }

        // Cause the engine the reload the blade.config file.
        public virtual void reloadBladeConfig()
        {
            var attrs = new Dictionary<string, object> {
                    {"q","reconfigure"},
                    {"file",this.BLADE_CONFIG_FILENAME}
            };
            this._transaction(this.CONTROL, attrs);
        }

        // Cause the engine the reload the tractor.config file.
        public virtual void reloadTractorConfig()
        {
            var attrs = new Dictionary<string, object> {
                    {"q","reconfigure"},
                    {"file",this.TRACTOR_CONFIG_FILENAME}
            };
            this._transaction(this.CONTROL, attrs);
        }

        // Cause the engine the reload all config files.
        public virtual void reloadAllConfigs()
        {
            var attrs = new Dictionary<string, object> {
                    {"q","reconfigure"}
            };
            this._transaction(this.CONTROL, attrs);
        }

        // Return the engine's current queue statistics.
        public virtual object queueStats()
        {
            var attrs = new Dictionary<string, object> {
                    {"q","status"},
                    {"qlen","1"},
                    {"enumq","1"}
            };
            return this._transaction(this.CONTROL, attrs);
        }

        // Perform simple communication with engine to verify the session is valid.
        public virtual object ping()
        {
            var attrs = new Dictionary<string, object> {
                    {"q","status"}
            };
            return this._transaction(this.CONTROL, attrs);
        }

        // Signal engine to reestablish its connections with its database server.
        public virtual object dbReconnect()
        {
            var attrs = new Dictionary<string, object> {
                    {"q","dbreconnect"}
            };
            return this._transaction(this.CONTROL, attrs);
        }

        // Request next command to run from engine.  Would be used by a remote execution server like tractor-blade.
        public virtual object nextCmd(bool probe = false, Dictionary<string, object> argv = null)
        {
            var attrs = new Dictionary<string, object> {
                { "q", "nextcmd" }
            };
            if (probe)
            {
                attrs["onlyprobe"] = 1;
            }
            if (argv != null)
            {
                foreach(var k in argv.Keys)
                {
                    attrs[k] = argv[k];
                }
            }
            try
            {
                var result = this._transaction(this.TASK, attrs);
                return result;
            }
            catch (TransactionError err)
            {
                var msg = err.ToString();
                if (msg.Contains("error 404:") || msg.Contains("job queue is empty") || msg.Contains("no dispatchable tasks") || msg.Contains("no remaining queueable commands"))
                {
                    return new List<object>();
                }
                else
                {
                    throw;
                }
            }
        }

        // Retrieve a specified config file. e.g. getConfig("blade.config")
        public virtual object getConfig(object filename)
        {
            var attrs = new Dictionary<string, object> {
                    {"q","get"},
                    {"file",filename}};
            return this._transaction(this.CONFIG, attrs);
        }

        // Spool the given job data.
        public virtual object spool(
            object jobData,
            string hostname = null,
            string filename = null,
            string owner = null,
            string format = null,
            bool skipLogin = false,
            bool block = false)
        {
            hostname = hostname ?? trutil.getlocalhost();
            owner = owner ?? this.user;//|| getpass.getuser();
            var cwd = System.IO.Directory.GetCurrentDirectory().Replace("\\", "/");
            filename = filename ?? "no filename specified";
            var attrs = new Dictionary<string, object> {
                    {"spvers",this.SPOOL_VERSION},
                    {"hnm",hostname},
                    {"jobOwner",owner},
                    {"jobFile",filename},
                    {"cwd",cwd}};
            if (block)
            {
                attrs["blocking"] = "spool";
            }
            var contentType = "application/tractor-spool";
            if (format == "JSON")
            {
                contentType += "-json";
            }
            var headers = new Dictionary<string, object> {
                    {"Content-Type",contentType}
            };
            return this._transaction(this.SPOOL, attrs, payload: jobData, translation: null, headers: headers, skipLogin: skipLogin);
        }

        // Close the connection with the engine by logging out and invalidating the session id.
        public virtual void close()
        {
            if (this.tsid == null)
            {
                // if there's no session id, then there's nothing to close
                this.dprint("no session id established.  connection considered closed.");
                return;
            }
            this.dprint("close engine connection");
            var attrs = new Dictionary<string, object> {
                    {
                        "q",
                        "logout"},
                    {
                        "user",
                        this.user}};
            this._transaction(this.MONITOR, attrs, translation: "logout");
            // tsid is used by isOpen() method, so clear it since connection is now closed
            this.tsid = null;
        }
    }
}