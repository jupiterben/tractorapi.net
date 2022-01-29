using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tractor.api
{
    public class TrHttpError : Exception
    {
    }

    public class fake_json
    {
        public fake_json()
        {
            this.fakeJSON = 1;
        }

        // A stand-in for the real json.loads(), using eval() instead.
        public virtual object loads(object jsonstr)
        {
            //
            // NOTE: In general, tractor-blade code should (and does) simply
            // "import json" and proceed from there -- which assumes that
            // the blade itself is running in a python distribution that is
            // new enough to have the json module built in.  However, this
            // one file (TrHttpRPC.py) is sometime used in other contexts in
            // which the json module is not available, hence the need for a
            // workaround.
            //
            // NOTE: python eval() will *fail* on strings ending in CRLF (\r\n),
            // they must be stripped!
            //
            // We add local variables to stand in for the three JSON
            // "native" types that aren't available in python; however,
            // these types aren't expected to appear in tractor data.
            //
            object null = null;
            var @true = true;
            var @false = false;
            return eval(jsonstr.replace("\r", ""));
        }
    }

    public static object json = fake_json();

    public class TrHttpRPC
    {
        public TrHttpRPC(
            string host,
            int port = 80,
            object logger = null,
            object apphdrs = null,
            object urlprefix = "/Tractor/",
            object timeout = 65.0)
        {
            this.port = port;
            this.lastPeerQuad = "0.0.0.0";
            this.engineResolved = false;
            this.resolveFallbackMsgSent = false;
            this.logger = logger;
            this.appheaders = apphdrs;
            this.urlprefix = urlprefix;
            this.timeout = timeout;
            this.passwdRequired = null;
            this.passwordhashfunc = null;
            this.host = host;
            if (type(port) != @int)
            {
                throw TrHttpError(String.Format("port value '%s' is not of type integer", port.ToString()));
            }
            if (port <= 0)
            {
                var _tup_1 = host.partition(":");
                var h = _tup_1.Item1;
                var c = _tup_1.Item2;
                var p = _tup_1.Item3;
                if (p)
                {
                    this.host = h;
                    this.port = Convert.ToInt32(p);
                }
            }
            // embrace and extend errno values
            if (!hasattr(errno, "WSAECONNRESET"))
            {
                errno.WSAECONNRESET = 10054;
            }
            if (!hasattr(errno, "WSAETIMEDOUT"))
            {
                errno.WSAETIMEDOUT = 10060;
            }
            if (!hasattr(errno, "WSAECONNREFUSED"))
            {
                errno.WSAECONNREFUSED = 10061;
            }
        }

        //# --------------------------------- ##
        // 
        //         Make an HTTP request and retrieve the reply from the server.
        //         An implementation using a few high-level methods from the
        //         urllib2 module is also possible, however it is many times
        //         slower than this implementation, and pulls in modules that
        //         are not always available (e.g. when running in maya's python).
        //         
        public virtual object Transaction(
            object tractorverb,
            object formdata,
            object parseCtxName = null,
            object xheaders = new Dictionary<object, object>
            {
            },
            object preAnalyzer = null,
            object postAnalyzer = null)
        {
            object outdata = null;
            var errcode = 0;
            object hsock = null;
            try
            {
                // like:  http://tractor-engine:80/Tractor/task?q=nextcmd&...
                // we use POST when making changes to the destination (REST)
                var req = "POST " + this.urlprefix + tractorverb + " HTTP/1.0\r\n";
                foreach (var h in this.appheaders)
                {
                    req += h + ": " + this.appheaders[h] + "\r\n";
                }
                foreach (var h in xheaders)
                {
                    req += h + ": " + xheaders[h] + "\r\n";
                }
                var t = "";
                if (formdata)
                {
                    t = formdata.strip();
                    t += "\r\n";
                    if (t && !req.Contains("Content-Type: "))
                    {
                        req += "Content-Type: application/x-www-form-urlencoded\r\n";
                    }
                }
                req += String.Format("Content-Length: %d\r\n", t.Count);
                req += "\r\n";
                req += t;
                // error checking?  why be a pessimist?
                // that's why we have exceptions!
                var _tup_1 = this.httpConnect();
                errcode = _tup_1.Item1;
                outdata = _tup_1.Item2;
                hsock = _tup_1.Item3;
                if (hsock)
                {
                    hsock.settimeout(min(55.0, this.timeout));
                    hsock.sendall(req.encode("utf-8"));
                    var _tup_2 = this.collectHttpReply(hsock, parseCtxName);
                    errcode = _tup_2.Item1;
                    outdata = _tup_2.Item2;
                    if (!errcode)
                    {
                        var _tup_3 = this.httpUnpackReply(outdata, parseCtxName, preAnalyzer, postAnalyzer);
                        errcode = _tup_3.Item1;
                        outdata = _tup_3.Item2;
                    }
                }
            }
            catch (OSError)
            {
                errcode = e.errno;
                outdata = new Dictionary<object, object> {
                        {
                            "msg",
                            "http transaction: " + e.strerror}};
            }
            catch (Exception)
            {
                errcode = 1;
                outdata = new Dictionary<object, object> {
                        {
                            "msg",
                            "http transaction: " + e.ToString()}};
            }
            if (parseCtxName && !(outdata is dict))
            {
                outdata = new Dictionary<object, object> {
                        {
                            "msg",
                            outdata}};
            }
            if (!(errcode is int))
            {
                errcode = -1;
            }
            if (hsock)
            {
                try
                {
                    hsock.close();
                }
                catch
                {
                }
            }
            return (errcode, outdata);
        }

        //# --------------------------------- ##
        public virtual object httpConnect()
        {
            object outdata = null;
            var errcode = 0;
            object hsock = null;
            try
            {
                // We can't use a simple socket.create_connection() here because
                // we need to protect this socket from being inherited by all of
                // the subprocesses that tractor-blade launches. Since those *may*
                // be happening in a different thread from this one, we still have
                // a race between the socket creation line and trSetNoInherit line
                // below. Python 3.2+ will finally add support for the atomic CLOEXEC
                // bit in the underlying socket create, but only for Linux.
                hsock = socket.socket(socket.AF_INET, socket.SOCK_STREAM);
                trSetNoInherit(hsock);
                trEnableTcpKeepAlive(hsock);
                hsock.settimeout(min(15.0, this.timeout));
                hsock.connect(this.resolveEngineHost());
                // if we get here with no exception thrown, then 
                // the connect succeeded; save peer ip addr
                this.lastPeerQuad = hsock.getpeername()[0] + ":" + this.port.ToString();
            }
            catch
            {
                outdata = "http connect(" + this.host + ":" + this.port.ToString() + "): timed out";
                errcode = errno.ETIMEDOUT;
            }
            catch
            {
                outdata = "hostname lookup failed: " + this.host;
                errcode = e.errno;
            }
            catch
            {
                outdata = "gethostbyname lookup failed: " + this.host;
                errcode = e.errno;
            }
            catch (OSError)
            {
                errcode = e.errno;
                outdata = "http connect(" + this.host + ":" + this.port.ToString() + "): ";
                if ((errno.ECONNREFUSED, errno.WSAECONNREFUSED).Contains(errcode))
                {
                    outdata += "connection refused";
                }
                else if ((errno.ECONNRESET, errno.WSAECONNRESET).Contains(errcode))
                {
                    outdata += "connection dropped";
                }
                else if ((errno.ETIMEDOUT, errno.WSAETIMEDOUT).Contains(errcode))
                {
                    outdata += "connect attempt timed-out (routing? firewall?)";
                }
                else if ((errno.EHOSTUNREACH, errno.ENETUNREACH, errno.ENETDOWN).Contains(errcode))
                {
                    outdata += "host or network unreachable";
                }
                else
                {
                    outdata += e.ToString();
                }
            }
            catch (KeyboardInterrupt)
            {
                throw;
            }
            catch
            {
                var _tup_1 = sys.exc_info()[::2];
                var errclass = _tup_1.Item1;
                var excobj = _tup_1.Item2;
                outdata = errclass.@__name__ + " - " + excobj.ToString();
                errcode = 999;
            }
            if (errcode && hsock)
            {
                try
                {
                    hsock.close();
                    hsock = null;
                }
                catch
                {
                    hsock = null;
                }
            }
            return (errcode, outdata, hsock);
        }

        //# --------------------------------- ##
        public virtual object resolveEngineHost()
        {
            if (this.engineResolved)
            {
                // use cached value
                return (this.host, this.port);
            }
            if (!("tractor-engine", "@").Contains(this.host))
            {
                // caller gave a specific non-default name, always cache that
                this.engineResolved = true;
                return (this.host, this.port);
            }
            // otherwise ...
            // For the special case of the default host name,
            // check to see if it is actually resolvable,
            // and if not, try a LAN multicast search
            var attemptDisco = false;
            try
            {
                var h = socket.gethostbyname(this.host);
                if (h)
                {
                    this.engineResolved = true;
                    return (this.host, this.port);
                }
            }
            catch
            {
                attemptDisco = true;
            }
            catch
            {
                attemptDisco = true;
            }
            catch
            {
                throw;
            }
            if (attemptDisco)
            {
                // The nameserver lookup failed, so try the EngineDiscovery
                // fallback service that the engine may be running.
                try
                {
                    var found = TractorLocator.TractorLocator().Search();
                    if (found)
                    {
                        var _tup_1 = found;
                        this.host = _tup_1.Item1;
                        this.port = _tup_1.Item2;
                        this.engineResolved = true;
                        return (this.host, this.port);
                    }
                }
                catch
                {
                    var _tup_2 = sys.exc_info()[::2];
                    var errclass = _tup_2.Item1;
                    var excobj = _tup_2.Item2;
                    this.Debug("TractorLocator error: " + errclass.@__name__ + " - " + excobj.ToString());
                }
            }
            if (!this.resolveFallbackMsgSent)
            {
                this.Debug("could not resolve 'tractor-engine' -- trying localhost");
                this.resolveFallbackMsgSent = true;
            }
            return ("127.0.0.1", 80);
        }

        //# --------------------------------- ##
        public virtual object collectHttpReply(object hsock, object parseCtxName)
        {
            object errnm;
            //
            // collect the reply from an http request already sent on hsock
            //
            if (parseCtxName)
            {
                errnm = parseCtxName.ToString();
            }
            else
            {
                errnm = "";
            }
            var mustTimeWait = false;
            var @out = new byte[] { };
            var err = 0;
            // we rely on the poll/select timeout behavior in the "internal_select"
            // implementation (C code) of the python socket module; that is:
            // the combination of recv + settimeout gets us out of wedged recv
            hsock.settimeout(this.timeout);
            while (1)
            {
                try
                {
                    var r = hsock.recv(4096);
                    if (r)
                    {
                        @out += r;
                    }
                    else
                    {
                        // end of input
                        // convert network http reply bytes to a python3 string
                        @out = @out.decode("utf-8");
                        break;
                    }
                }
                catch
                {
                    @out = "time-out waiting for http reply " + errnm;
                    err = errno.ETIMEDOUT;
                    mustTimeWait = true;
                    break;
                }
                catch (Exception)
                {
                    mustTimeWait = true;
                    @out = "error waiting for http reply " + errnm + " " + e.ToString();
                    try
                    {
                        err = e.errno;
                    }
                    catch
                    {
                        err = 999;
                    }
                    break;
                }
            }
            // Attempt to reduce descriptors held in TIME_WAIT on the
            // engine by dismantling this request socket immediately
            // if we've received an answer.  Usually the close() call
            // returns immediately (no lingering close), but the socket
            // persists in TIME_WAIT in the background for some seconds.
            // Instead, we force it to dismantle early by turning ON
            // linger-on-close() but setting the timeout to zero seconds.
            //
            if (!mustTimeWait)
            {
                hsock.setsockopt(socket.SOL_SOCKET, socket.SO_LINGER, @struct.pack("ii", 1, 0));
            }
            return (err, @out);
        }

        //# --------------------------------- ##
        public virtual object httpUnpackReply(object t, object parseCtxName, object preAnalyzer, object postAnalyzer)
        {
            object errcode;
            object outdata;
            if (t && t.Count)
            {
                var n = t.find("\r\n\r\n");
                var h = t[0::n];
                n += 4;
                outdata = t[n].strip();
                n = h.find(" ") + 1;
                var e = h.find(" ", n);
                errcode = Convert.ToInt32(h[n::e]);
                if (errcode == 200)
                {
                    errcode = 0;
                }
                // expecting a json dict?  parse it
                if (outdata && parseCtxName && (0 == errcode || "{" == outdata[0]))
                {
                    // choose between pure json parse and eval
                    var jsonParser = json.loads;
                    if (!preAnalyzer)
                    {
                        preAnalyzer = this.engineProtocolDetect;
                    }
                    jsonParser = preAnalyzer(h, errcode, jsonParser);
                    try
                    {
                        if (jsonParser)
                        {
                            outdata = jsonParser(outdata);
                        }
                    }
                    catch (Exception)
                    {
                        errcode = -1;
                        this.Debug("json parse:\n" + outdata);
                        outdata = String.Format("parse %s: %s", parseCtxName, this.Xmsg());
                    }
                }
                if (postAnalyzer)
                {
                    postAnalyzer(h, errcode);
                }
            }
            else
            {
                outdata = "no data received";
                errcode = -1;
            }
            return (errcode, outdata);
        }

        //# --------------------------------- ##
        public virtual object GetLastPeerQuad()
        {
            return this.lastPeerQuad;
        }

        //# --------------------------------- ##
        public virtual object Debug(object txt)
        {
            if (this.logger)
            {
                this.logger.debug(txt);
            }
        }

        public virtual object Xmsg()
        {
            if (this.logger && hasattr(this.logger, "Xcpt"))
            {
                return this.logger.Xcpt();
            }
            else
            {
                var _tup_1 = sys.exc_info()[::2];
                var errclass = _tup_1.Item1;
                var excobj = _tup_1.Item2;
                return String.Format("%s - %s", errclass.@__name__, excobj.ToString());
            }
        }

        public virtual object trStrToHex(object str)
        {
            var s = "";
            foreach (var c in str)
            {
                s += String.Format("%02x", ord(c));
            }
            return s;
        }

        public virtual object engineProtocolDetect(object htxt, object errcode, object jsonParser)
        {
            // Examine the engine's http "Server: ..." header to determine
            // whether we may be receiving pre-1.6 blade.config data which
            // is not pure json, in which case we need to use python "eval"
            // rather than json.loads().
            var n = htxt.find("\nServer:");
            if (n)
            {
                n = htxt.find(" ", n) + 1;
                var e = htxt.find("\r\n", n);
                var srvstr = htxt[n::e];
                // "Pixar_tractor/1.5.2 (build info)"
                var v = srvstr.split();
                if (v[0] == "Pixar")
                {
                    // rather than "Pixar_tractor/1.6"
                    v = new List<object> {
                            "1",
                            "0"
                        };
                }
                else
                {
                    v = v[0].split("/")[1].split(".");
                }
                try
                {
                    n = float(v[1]);
                }
                catch
                {
                    n = 0;
                }
                if (v[0] == "1" && n < 6)
                {
                    jsonParser = eval;
                }
            }
            return jsonParser;
        }

        public virtual object PasswordRequired()
        {
            if (null != this.passwdRequired)
            {
                return this.passwdRequired;
            }
            this.passwdRequired = false;
            try
            {
                // get the site-defined python client functions
                var _tup_1 = this.Transaction("config?file=trSiteFunctions.py", null, null);
                var err = _tup_1.Item1;
                var data = _tup_1.Item2;
                if (data && !err)
                {
                    Python_Exec((data, globals()));
                    this.passwdRequired = trSitePasswordHash("01", "01") != null;
                    this.passwordhashfunc = trSitePasswordHash;
                }
                if (!this.passwdRequired)
                {
                    var _tup_2 = this.Transaction("monitor?q=loginscheme", null, "chklogins");
                    err = _tup_2.Item1;
                    data = _tup_2.Item2;
                    if (data && !err)
                    {
                        if (data["validation"].startswith("internal:"))
                        {
                            this.passwordhashfunc = trInternalPasswordHash;
                            this.passwdRequired = true;
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Error due to file missing or bogus functions in the file.
                // Revert back to the default password hash.
                this.passwdRequired = this.passwordhashfunc("01", "01") != null;
            }
            if (this.passwdRequired && !this.passwordhashfunc)
            {
                this.passwordhashfunc = trNoPasswordHash;
            }
            return this.passwdRequired;
        }

        public virtual object Login(object user, object passwd)
        {
            object challenge;
            object data;
            object err;
            //
            // Provides generic login support to the tractor engine/monitor
            // This module first attempts to retrieve the standard python 
            // dashboard functions and executes this file to provide the 
            // TrSitePasswordHash() function
            //
            // If this returns a password that is not None, then the Login module
            // requests a challenge key from the engine, then encodes the password
            // hash and challenge key into the login request
            //
            // The engine will run the "SitePasswordValidator" entry as defined in
            // the crews.config file.
            //
            var loginStr = String.Format("monitor?q=login&user=%s", user);
            var passwdRequired = this.PasswordRequired();
            if (passwdRequired)
            {
                if (!passwd)
                {
                    throw TrHttpError("Password required, but not provided");
                }
                else
                {
                    // get a challenge token from the engine
                    var _tup_1 = this.Transaction("monitor?q=gentoken", null, "gentoken");
                    err = _tup_1.Item1;
                    data = _tup_1.Item2;
                    if (err || !data)
                    {
                        challenge = null;
                    }
                    else
                    {
                        challenge = data["challenge"];
                    }
                    if (err || !challenge)
                    {
                        throw TrHttpError("Failed to generate challenge token." + " code=" + err.ToString() + " - " + data.ToString());
                    }
                    // update the login URL to include the encoded challenge 
                    // and password
                    var challengepass = challenge + "|" + this.passwordhashfunc(passwd, challenge);
                    loginStr += String.Format("&c=%s", urllib2.quote(this.trStrToHex(challengepass)));
                }
            }
            var _tup_2 = this.Transaction(loginStr, null, "register");
            err = _tup_2.Item1;
            data = _tup_2.Item2;
            if (err)
            {
                throw TrHttpError("Tractor login failed. code=" + err.ToString() + " - " + data.ToString());
            }
            var tsid = data["tsid"];
            if (tsid == null)
            {
                throw TrHttpError("Tractor login as '" + user + "' failed. code=" + err.ToString() + " - " + data.ToString());
            }
            return data;
        }
    }

    public static object SetHandleInformation = ctypes.windll.kernel32.SetHandleInformation;

    static Module()
    {
        SetHandleInformation.argtypes = (ctypes.wintypes.HANDLE, ctypes.wintypes.DWORD, ctypes.wintypes.DWORD);
        SetHandleInformation.restype = ctypes.wintypes.BOOL;
    }

    public static object win32_HANDLE_FLAG_INHERIT = 0x00000001;

    public static object trSetNoInherit(object sock)
    {
        var fd = Convert.ToInt32(sock.fileno());
        SetHandleInformation(fd, win32_HANDLE_FLAG_INHERIT, 0);
    }

    public static object trSetInherit(object sock)
    {
        var fd = Convert.ToInt32(sock.fileno());
        SetHandleInformation(fd, win32_HANDLE_FLAG_INHERIT, 1);
    }

    public static object trSetNoInherit(object sock)
    {
        var oldflags = fcntl.fcntl(sock, fcntl.F_GETFD);
        fcntl.fcntl(sock, fcntl.F_SETFD, oldflags | fcntl.FD_CLOEXEC);
    }

    public static object trSetInherit(object sock)
    {
        var oldflags = fcntl.fcntl(sock, fcntl.F_GETFD);
        fcntl.fcntl(sock, fcntl.F_SETFD, oldflags & ~fcntl.FD_CLOEXEC);
    }

    // _______________________________________________________________________
    // TrHttpRPC - a tractor-blade module that makes HTTP requests of
    //             tractor-engine, such as requesting configuration data
    //             and commands from the job queue to execute.
    //
    //            
    //             Note, many of the functions here could be accomplished
    //             using the built-in python urllib2 module.  However, 
    //             that module does not have "json" extraction built-in,
    //             and more importantly:  urllib2 is very slow to setup
    //             new connections.  Using it for obtaining new tasks and
    //             reporting their results can actually reduce the overall
    //             throughput of the tractor system, especailly for very
    //             fast-running tasks.
    //
    // _______________________________________________________________________
    // Copyright (C) 2007-2021 Pixar Animation Studios. All rights reserved.
    //
    // The information in this file is provided for the exclusive use of the
    // software licensees of Pixar.  It is UNPUBLISHED PROPRIETARY SOURCE CODE
    // of Pixar Animation Studios; the contents of this file may not be disclosed
    // to third parties, copied or duplicated in any form, in whole or in part,
    // without the prior written permission of Pixar Animation Studios.
    // Use of copyright notice is precautionary and does not imply publication.
    //
    // PIXAR DISCLAIMS ALL WARRANTIES WITH REGARD TO THIS SOFTWARE, INCLUDING
    // ALL IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS, IN NO EVENT
    // SHALL PIXAR BE LIABLE FOR ANY SPECIAL, INDIRECT OR CONSEQUENTIAL DAMAGES
    // OR ANY DAMAGES WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS,
    // WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION,
    // ARISING OUT OF OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS
    // SOFTWARE.
    // _______________________________________________________________________
    //
    //# ------------------------- ##
    //# ------------------------- ##
    //# ------------------------------------------------------------- ##
    //
    // define a platform-specific routine that makes the given socket
    // uninheritable, we don't want launched subprocesses to retain
    // an open copy of this file descriptor
    //
    public static object trEnableTcpKeepAlive(object sock)
    {
        sock.setsockopt(socket.SOL_SOCKET, socket.SO_KEEPALIVE, 1);
    }

    //# ------------------------------------------------------------- ##
    public static object trNoPasswordHash(object passwd, object challenge)
    {
        //
        // This is the default, no-op, password hash function.
        // The site-provided real one can be defined in the site's
        // tractor configuration directory, in trSiteFunctions.py,
        // or in other override config files.
        //
        return null;
    }

    public static object trInternalPasswordHash(object passwd, object challenge)
    {
        //
        // This encoding function is used for "PAM" style logins.
        // **NOTE** it assumes that your client is connected to the
        // engine over a secure connection (internal LAN or VPN)
        // because a recoverable encoding is used to deliver the
        // password to the unix PAM module on the engine.
        //
        var n = passwd.Count;
        var k = challenge.Count;
        if (k < n)
        {
            n = k;
        }
        var h = "1";
        foreach (var i in Enumerable.Range(0, n - 0))
        {
            k = ord(passwd[i]) ^ ord(challenge[i]);
            if (k > 255)
            {
                k = 255;
            }
            h += String.Format("%02x", k);
        }
        return h;
    }
}
