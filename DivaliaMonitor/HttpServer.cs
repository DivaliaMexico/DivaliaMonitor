using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Text.RegularExpressions;
using System.IO;
using System.Windows.Forms;
using System.Web.Script.Serialization;
using System.Management;
using System.Diagnostics;
using System.Net.NetworkInformation;
using DivaliaMonitor;
using DivaliaMonitor.Hardware;

namespace HTTPServer
{
    class Client
    {
        private void RequestAuth(TcpClient Client)
        {
            var Code = 401;
            string CodeStr = Code.ToString() + " " + ((HttpStatusCode)Code).ToString();
            string Html = "<html><body><h1>" + CodeStr + "</h1></body></html>";
            string Str = "HTTP/1.1 " + CodeStr + "\n" +
                         "Content-type: text/html\n" + 
                         "Content-Length:" + Html.Length.ToString() + "\n" + 
                         "WWW-Authenticate: Basic realm=\"Protected\"\n\n" +
                         Html;
            byte[] Buffer = Encoding.ASCII.GetBytes(Str);
            Client.GetStream().Write(Buffer, 0, Buffer.Length);
            Client.Close();
        }

        private void SendError(TcpClient Client, int Code)
        {
            string CodeStr = Code.ToString() + " " + ((HttpStatusCode)Code).ToString();
            string Html = "<html><body><h1>" + CodeStr + "</h1></body></html>";
            string Str = "HTTP/1.1 " + CodeStr + "\nContent-type: text/html\nContent-Length:" + Html.Length.ToString() + "\n\n" + Html;
            byte[] Buffer = Encoding.ASCII.GetBytes(Str);
            Client.GetStream().Write(Buffer, 0, Buffer.Length);
            Client.Close();
        }

        public Client(TcpClient Client)
        {
            string Request = "";
            byte[] Buffer = new byte[1024];
            int Count;
            while ((Count = Client.GetStream().Read(Buffer, 0, Buffer.Length)) > 0)
            {
                Request += Encoding.ASCII.GetString(Buffer, 0, Count);
                if (Request.IndexOf("\r\n\r\n") >= 0 || Request.Length > 4096)
                {
                    break;
                }
            }

            Match ReqMatch = Regex.Match(Request, @"^\w+\s+([^\s\?]+)[^\s]*\s+HTTP/.*|");
            if (ReqMatch == Match.Empty) { SendError(Client, 400); return; }
            
            var AuthConfig = Program.settings.GetValue("auth", "");
            if (AuthConfig != "")
            {
                Match AuthMatch = Regex.Match(Request, @"Authorization:\s+Basic\s+([^\s\n\r]+)");

                if (AuthMatch != Match.Empty)
                {
                    var Password = Encoding.ASCII.GetString(System.Convert.FromBase64String(AuthMatch.Groups[1].Value));
                    if (Password != AuthConfig)
                    {
                        RequestAuth(Client);
                        return;
                    }
                }
                else
                {
                    RequestAuth(Client);
                    return;
                }
            }

            byte[] buffer;

            string RequestUri = ReqMatch.Groups[1].Value;
            RequestUri = Uri.UnescapeDataString(RequestUri);

            string image = @"data:image/png;base64,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";

            if (RequestUri == "/smart")
            {
                var json = new JavaScriptSerializer().Serialize(HDD.GetSMART());
                buffer = System.Text.Encoding.UTF8.GetBytes(json);
            }
            else if (RequestUri == "/hardware")
            {
                var json = new JavaScriptSerializer().Serialize(DivaliaMonitor.Hardware.Monitor.getData());
                buffer = System.Text.Encoding.UTF8.GetBytes(json);
            }
            else if (RequestUri == "/ping")
            {
                buffer = System.Text.Encoding.UTF8.GetBytes("OK");
            }
            else if (RequestUri == "/ipaddress")
            {
                buffer = System.Text.Encoding.UTF8.GetBytes(DivaliaMonitor.Hardware.Networking.GetIPAdress().ToString());
            }
            else if (RequestUri == "/dnsaddress")
            {
                buffer = System.Text.Encoding.UTF8.GetBytes(DivaliaMonitor.Hardware.Networking.GetDnsAdress().ToString());
            }
            else if (RequestUri == "/uptime")
            {
                PerformanceCounter upTime = new PerformanceCounter("System", "System Up Time");
                upTime.NextValue();
                TimeSpan ts = TimeSpan.FromSeconds(upTime.NextValue());
                buffer = System.Text.Encoding.ASCII.GetBytes("Dias: " + ts.Days.ToString() + " Horas: " + ts.Hours.ToString() + " Minutos: " + ts.Minutes.ToString() + " Segundos: " + ts.Seconds.ToString());
            }
            else if (RequestUri == "/credits")
            {
                string output = @"
                <head>
                <title>Divalia Windows Server Monitor</title>
                <style>
                    a {
                      transition: color .4s;
                      color: #265C83;
                    }

                    a:link,
                    a:visited { color: #265C83; }
                    a:hover   { color: #7FDBFF; }
                    a:active  {
                      transition: color .3s;
                      color: #007BE6;
                    }

                    .link { text-decoration: none; }
                </style>
                </head>
                   <img src=""data:image/png;base64,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"";
                    <hr />
                    <h3>Divalia Windows Server Monitor</h3>
                   <p>Divalia WSM esta basado en <a href=""https://github.com/druidvav/nekoMonitoring"">nekoMonitoring</a> y licenciado bajo <a href=""https://www.gnu.org/licenses/gpl-3.0.html"">GNU General Public License</a>.</p>
                   <p>Se puede encontrar el fuente de este proyecto en <a href=""https://github.com/divaliamexico"">Divalia Mexico</a>, te invitamos a contribuir y reportar errores.
                ";
                buffer = System.Text.Encoding.UTF8.GetBytes(output);
            }
            else
            {
                string output = @"
                <head>
                <title>Divalia Windows Server Monitor</title>
                <style>
                    a {
                      transition: color .4s;
                      color: #265C83;
                    }

                    a:link,
                    a:visited { color: #265C83; }
                    a:hover   { color: #7FDBFF; }
                    a:active  {
                      transition: color .3s;
                      color: #007BE6;
                    }

                    .link { text-decoration: none; }
                </style>
                </head>
                   <img src=""data:image/png;base64,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"";
                    <hr />
                    <h3>Divalia Windows Server Monitor</h3>
                    <ul>
                        <li><a href=""/hardware"">Hardware</a></li>
                        <li><a href=""/smart"">S.M.A.R.T</a></li>
                        <li><a href=""/ipaddress"">IP Address</a></li>
                        <li><a href=""/dnsaddress"">DNS Address</a></li>
                        <li><a href=""/ping"">Ping</a></li>
                        <li><a href=""/uptime"">Uptime</a></li>
                        <li><a href=""/credits"">Sobre Divalia WSM</a></li>
                    </ul>
                ";
                buffer = System.Text.Encoding.UTF8.GetBytes(output);
            }

            try
            {
                string Headers = "HTTP/1.1 200 OK\nContent-Type: text/html\nContent-Length: " + buffer.Length + "\n\n";
                byte[] HeadersBuffer = Encoding.ASCII.GetBytes(Headers);
                Client.GetStream().Write(HeadersBuffer, 0, HeadersBuffer.Length);
                Client.GetStream().Write(buffer, 0, buffer.Length);
                Client.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }

    class Server
    {
        protected TcpListener Listener;
        public static ManualResetEvent allDone = new ManualResetEvent(false);

        public Server(int Port)
        {
            Listener = new TcpListener(IPAddress.Any, Port);
            Listener.Start();
            while (true)
            {
                allDone.Reset();
                Listener.BeginAcceptTcpClient(new AsyncCallback(ClientThread), Listener);
                allDone.WaitOne();
            }
        }

        static void ClientThread(IAsyncResult ar)
        {
            TcpListener listener = (TcpListener) ar.AsyncState;
            TcpClient client = listener.EndAcceptTcpClient(ar);
            new Client((TcpClient)client);
            allDone.Set();
        }

        ~Server()
        {
            if (Listener != null)
            {
                Listener.Stop();
            }
        }
    }
}
