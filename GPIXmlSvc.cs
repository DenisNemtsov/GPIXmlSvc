using System.ServiceProcess;

namespace GPIXmlSvc
{
    public partial class GPIXmlSvc : ServiceBase
    {
        public GPIXmlSvc()
        {
            ServiceName = Program.ServiceName;
            InitializeComponent();
            CanStop = true;
            CanPauseAndContinue = true;
            AutoLog = false;
        }

        protected override void OnStart(string[] args)
        {
            Program.Start();
        }

        protected override void OnStop()
        {
            Program.Stop();
        }
    }
}