using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Threading;
using BulkSmsRoutine.Utils;

namespace BulkSmsRoutine
{
    public partial class BulkSmsServ : ServiceBase
    {
        private Timer _mySchedular;
        public BulkSmsServ()
        {
            InitializeComponent();
        }

        public void OnDebug()
        {
            OnStart(null);
        }
        protected override void OnStart(string[] args)
        {
            WriteToFile("SMS Service has started {0}");
            SchedulethatService();
        }

        protected override void OnStop()
        {
            WriteToFile("SMS Service has stopped {0}");
            WriteToFile("#################################### ");
            _mySchedular.Dispose();
        }

        public void ExcecuteSendSms()
        {
            try
            {
                //execute SMS Sending below here....
                var webRefConn = NavConfig.SmsWebRef;
                var oDataConn = NavConfig.ODataObj();
                var bulkmsgId = oDataConn.BulkSMSs.ToList().Where(d => d.Message_Sent == false)
                    .Select(y => y.EntryNo).FirstOrDefault();
                int entryNo = Convert.ToInt32(bulkmsgId);
                var bulkMesg = webRefConn.FnGetSavedBulkMsg(entryNo);

                switch (bulkMesg)
                {
                    //No unsent message found!
                    case "noBulkText":
                            WriteToFile("################ No Bulk SMS to Send, Re-Run service#################### ");
                            //Schedule the service
                            SchedulethatService();
                        break;
                    //unsent message found
                    default:
                            var contacts = oDataConn.BulkMsgContacts.ToList().Where(r => r.Check_for_Test == true);
                            foreach (var contact in contacts)
                            {
                                new NavConfig().SendSms(entryNo, contact.Mobile_Phone_No, bulkMesg);
                                WriteToFile("SMS Send successfully to: " + contact.Mobile_Phone_No);
                            }
                            var marked = webRefConn.FnMarkBulkSmsAsSent(entryNo);
                            WriteToFile("Bulk Message: " + bulkMesg);
                            WriteToFile("Message EntryNo: " + entryNo);
                            WriteToFile("Marked if sent?: " + marked);
                            WriteToFile("#################################### ");
                            //Schedule the service
                            SchedulethatService();
                        break;
                }
            }
            catch (Exception ex)
            {
                WriteToFile("SMS Service Exception Error : {0} " + ex.Message + ex.StackTrace);
                WriteToFile("#################################### ");
            }
        }
        public void SchedulethatService()
        {
            try
            {
                _mySchedular = new Timer(SchedularCallback);
                string mode = ConfigurationManager.AppSettings["Mode"].ToUpper();
                WriteToFile("SMS Service Mode: " + mode + " {0}");

                //Set the Default Time.
                DateTime scheduledTime = DateTime.MinValue;

                if (mode == "DAILY")
                {
                    //Get the Scheduled Time from AppSettings.
                    scheduledTime = DateTime.Parse(ConfigurationManager.AppSettings["ScheduledTime"]);
                    if (DateTime.Now > scheduledTime)
                    {
                        //If Scheduled Time is passed set Schedule for the next day.
                        scheduledTime = scheduledTime.AddDays(1);
                    }
                }

                if (mode.ToUpper() == "INTERVAL")
                {
                    //Get the Interval in Minutes from AppSettings.
                    int intervalMinutes = Convert.ToInt32(ConfigurationManager.AppSettings["IntervalMinutes"]);

                    //Set the Scheduled Time by adding the Interval to Current Time.
                    scheduledTime = DateTime.Now.AddMinutes(intervalMinutes);
                    if (DateTime.Now > scheduledTime)
                    {
                        //If Scheduled Time is passed set Schedule for the next Interval.
                        scheduledTime = scheduledTime.AddMinutes(intervalMinutes);
                    }
                }

                TimeSpan timeSpan = scheduledTime.Subtract(DateTime.Now);
           
                string schedule = string.Format("{0} day(s) {1} hour(s) {2} minute(s) {3} seconds(s)", timeSpan.Days, timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);

                WriteToFile("SMS Service scheduled to run after: " + schedule + " {0}");

                //Get the difference in Minutes between the Scheduled and Current Time.
                int dueTime = Convert.ToInt32(timeSpan.TotalMilliseconds);

                //Change the Timer's Due Time.
                _mySchedular.Change(dueTime, Timeout.Infinite);
            }
            catch (Exception ex)
            {
                WriteToFile("SMS Service Error on: {0} " + ex.Message + ex.StackTrace);
                //Restart  the Windows Service.
                using (ServiceController serviceController = new ServiceController("BulkSmsServ"))
                {
                    serviceController.Stop();
                    serviceController.Start();
                }
            }
        }
        private void SchedularCallback(object e)
        {
            ExcecuteSendSms();
        }

        private void WriteToFile(string text)
        {
            //System.IO.File.Create(AppDomain.CurrentDomain.BaseDirectory + "EmailTestShawn.txt");
            string path = @"C:\ServiceTest\BulkSMSLogs.txt";
            using (StreamWriter writer = new StreamWriter(path, true))
            {
                writer.WriteLine(text, DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt"));
                writer.Close();
            }
        }

    }
}
