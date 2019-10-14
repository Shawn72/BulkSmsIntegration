using System.ServiceProcess;

namespace BulkSmsRoutine
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            #if DEBUG
                BulkSmsServ mSev = new BulkSmsServ();
                mSev.OnDebug();
                System.Threading.Thread.Sleep((System.Threading.Timeout.Infinite));

            #else
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[]
                {
                    new BulkSmsServ()
                };
                ServiceBase.Run(ServicesToRun);
            #endif
        }
    }
}
