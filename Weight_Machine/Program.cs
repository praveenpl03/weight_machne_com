using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using MB.Web;
using System.Timers;
using System.Text.RegularExpressions;

namespace Weight_Machine
{
    class MainClass
    {

        static bool _continue;
        static SerialPort _serialPort;
        public static HttpListener listener;
        public static string url = "http://127.0.0.1:8000/";
        public static string pageViews = "";
        public static int requestCount = 0;
        public static string pageData = "{0}{1}";
        private static Timer aTimer;




        public static void Read()
        {
            while (_continue)
            {
                try
                {
                    string message = _serialPort.ReadLine();
                    Console.WriteLine(message);
                }
                catch (TimeoutException) { }
            }
        }



        public static async Task rimer()
        {
            // Create a timer and set a two second interval.
            aTimer = new System.Timers.Timer();
            aTimer.Interval = 4000;

            // Hook up the Elapsed event for the timer. 
            aTimer.Elapsed += OnTimedEvent;

            // Have the timer fire repeated events (true is the default)
            aTimer.AutoReset = true;

            // Start the timer
            aTimer.Enabled = true;

            Console.WriteLine("Press the Enter key to exit the program at any time... ");
            //Console.ReadLine();
        }


        public static async Task HandleIncomingConnections()
        {




            bool runServer = true;
            while (runServer)
            {

                HttpListenerContext ctx = await listener.GetContextAsync();

                HttpListenerRequest req = ctx.Request;
                HttpListenerResponse resp = ctx.Response;
                resp.AddHeader("Access-Control-Allow-Origin", "*");
                XmlTextReader textReader = new XmlTextReader("config.xml");

                textReader.ReadToFollowing("appSettings");
                textReader.ReadToFollowing("PortName");
                String port = textReader.ReadElementContentAsString();
          
                textReader.ReadToFollowing("BaudRate");
                int baudrate = textReader.ReadElementContentAsInt();

                _serialPort = new SerialPort(port, baudrate, Parity.None, 8, StopBits.One);
                try
                {

                  
                    _serialPort.Open();
                    System.Threading.Thread.Sleep(500);
                    pageViews = _serialPort.ReadLine();

                    pageViews = RemoveSpecialCharacters(pageViews);
                  //  pageViews = pageViews.Replace("]", "");
                    pageViews = pageViews.TrimStart(new Char[] { '0' });
                    if (pageViews == "")
                    {
                        pageViews = "0";
                    }

                }
                catch (Exception ex)
                {

                    Console.WriteLine("Error Data:" + ex);
                    pageViews = "No Port Found/ not Open";

                }
                string disableSubmit = "";
                byte[] data = Encoding.UTF8.GetBytes(String.Format(pageData, pageViews, disableSubmit));
                resp.ContentType = "text/html";
                resp.ContentEncoding = Encoding.UTF8;
                resp.ContentLength64 = data.LongLength;
                await resp.OutputStream.WriteAsync(data, 0, data.Length);
                _serialPort.Close();

                resp.Close();
              
            }
        }
        public static string RemoveSpecialCharacters(string str)
        {
            return Regex.Replace(str, "[^a-zA-Z0-9_.]+", "", RegexOptions.Compiled);
        }
        private static void Main(string[] args)
        {

        

            File.Delete("files/snapshot.jpg");
            var server = new SimpleWebServer("http://127.0.0.1:8001/", "files/");
            server.Start();

            listener = new HttpListener();

            listener.Prefixes.Add(url);
            listener.Start();
            Task x = rimer();

            // Handle requests
            Task listenTask = HandleIncomingConnections();

            Console.WriteLine("Listening for connections on {0}", listenTask.ToString());

            listenTask.GetAwaiter().GetResult();
            x.Start();
        

            listener.Close();



        }
        private static void OnTimedEvent(Object source, System.Timers.ElapsedEventArgs e)
        {
            Console.WriteLine("The Elapsed event was raised at {0}", e.SignalTime);
            convert();
        }
        public static void convert()
        {
            Console.Write("converting");
            Process proc = new Process();

            XmlTextReader textReader = new XmlTextReader("config.xml");

            textReader.ReadToFollowing("appSettings");
            textReader.ReadToFollowing("Rtsp");
            var rtsp = textReader.ReadElementContentAsString();
            proc.StartInfo.FileName = "ffmpeg";
            proc.StartInfo.Arguments = " -y -v info -i \"" + rtsp + "\" -f image2 -vframes 1 \"files/snapshot.jpg\"";
      //      Console.WriteLine(" -y -v info -i \"" + rtsp + "\" -f image2 -vframes 1 \"files/snapshot.jpg\"");
            proc.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            proc.Start();
        }
      

    }
}