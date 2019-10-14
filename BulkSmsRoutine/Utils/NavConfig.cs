using System;
using System.Configuration;
using System.IO;
using System.Net;
using AfricasTalkingCS;
using BulkSmsRoutine.ODataRef;
using BulkSmsRoutine.WebRef;

namespace BulkSmsRoutine.Utils
{
  
        public class NavConfig
        {
            public static NAV ODataObj()
            {
                NAV nav = new NAV(new Uri(ConfigurationManager.AppSettings["ODATA_URI"]))
                {
                    Credentials = new NetworkCredential(ConfigurationManager.AppSettings["W_USER"],
                        ConfigurationManager.AppSettings["W_PWD"], ConfigurationManager.AppSettings["DOMAIN"])
                };
                return nav;
            }

            public void SendSms(int entryno, string phone, string text)
            {
                string username = ConfigurationManager.AppSettings["MyAppUsername"];
                string apiKey = ConfigurationManager.AppSettings["MyAppAPIKey"];
                string recipients = phone;
                string message = text;

                AfricasTalkingGateway gateway = new AfricasTalkingGateway(username, apiKey);
                try
                {

                    dynamic results = gateway.sendMessage(recipients, message);

                    foreach (dynamic result in results)
                    {
                        var status = (string)result["status"];
                        if (status.Equals("Success"))
                        {
                            //mark as sent
                            WriteToFile("SMS Message sent to: " + recipients);
                           // SmsWebRef.MarkSmsAsSent(entryno);
                        }
                        else
                        {
                            WriteToFile("SMS Message was already sent, or an error occured..!");
                        }
                    }
                }
                catch (AfricasTalkingGatewayException e)
                {
                    //  Console.WriteLine("Encountered an error: " + e.Message)
                    WriteToFile("Encountered an error: " + e.Message);

                }
            }
            public void WriteToFile(string text)
            {
                //System.IO.File.Create(AppDomain.CurrentDomain.BaseDirectory + "EmailTestShawn.txt");
                string path = @"C:\ServiceTest\BulkSMSLogs.txt";
                using (StreamWriter writer = new StreamWriter(path, true))
                {
                    writer.WriteLine(text, DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt"));
                    writer.Close();
                }
            }
            public static sms SmsWebRef
            {
                get
                {
                    var ws = new sms();

                    try
                    {
                        var credentials = new NetworkCredential(ConfigurationManager.AppSettings["W_USER"],
                            ConfigurationManager.AppSettings["W_PWD"], ConfigurationManager.AppSettings["DOMAIN"]);
                        ws.Credentials = credentials;
                        ws.PreAuthenticate = true;
                        ws.Timeout = -1;
                    }
                    catch (Exception ex)
                    {
                        ex.Data.Clear();
                    }
                    return ws;
                }
            }
        }
    }

