using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Newtonsoft.Json;

namespace KaufmannConsolidacionKpi
{
    public partial class Service1 : ServiceBase
    {
        private Timer timer = new Timer { Interval = 60 * 1000 * 60, AutoReset = true };
        private static int startHour = 22;
        private static int stopHour = 23;
        private static string logFilePath = "C:/LogKPI/Log.txt";
        private static string handlerUrl = "http://servidorsc.no-ip.org:9000/srskaufmann/req/handlers/consolidaSLA.ashx?1=Consolidar";
        private static int lastMonthConsolided;
        public Service1()
        {
            InitializeComponent();
        }

        public void OnDebug()
        {
            OnStart(null);
        }
        protected override void OnStart(string[] args)
        {
            Run();
            timer.Elapsed += timer_Elapsed;
            timer.Start();
        }
        private static void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Run();
        }

        private static void Run ()
        {
            var now = DateTime.Now;
            if (lastMonthConsolided == now.Month) return;
            var lastDay = DateTime.DaysInMonth(now.Year, now.Month);
            if (now.Day == lastDay)
            {
                if (now.Hour > startHour && now.Hour < stopHour)
                {
                    CallHandler(now);
                }
            }
        }
        protected override void OnStop()
        {
        }

        private static void CallHandler(DateTime now)
        {
            try
            {
                var query = "&anio=" + now.Year + "&mes=" + now.Month;
                HttpWebRequest http = (HttpWebRequest)WebRequest.Create(handlerUrl + query);
                http.Timeout = 5 * 60 * 1000;
                WebResponse response = http.GetResponse();

                System.IO.Stream stream = response.GetResponseStream();
                StreamReader sr = new StreamReader(stream);
                string content = sr.ReadToEnd();
                var result = JsonConvert.DeserializeObject<Response>(content);
                if(result.done)
                {
                    lastMonthConsolided = now.Month;
                    using (var file = new StreamWriter(logFilePath, true))
                    {
                        file.WriteLine("Consolidación KPI realizada para el mes {0} y el año {1}\t{2}", now.Month, now.Year, now.ToLongDateString());
                    }
                }
                else
                {
                    using (var file = new StreamWriter(logFilePath, true))
                    {
                        file.WriteLine("No se pudo realizar la consolidación para el mes {0} y el año {1}: {2}\t{3}", now.Month, now.Year, result.message, now.ToLongDateString());
                    }

                }
            }
            catch (Exception ex)
            {
                using (var file = new StreamWriter(logFilePath, true))
                {
                    file.WriteLine("Ocurrió un error durante la consolidación para el mes {0} y el año {1}: {2}\t{3}", now.Month, now.Year, ex.ToString(), now.ToLongDateString());
                }
            }
        }

        public struct Response
        {
            public bool done { get; set; }
            public string message { get; set; }
        }
    }
}
