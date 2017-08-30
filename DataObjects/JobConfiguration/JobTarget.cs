using System;

namespace AppDynamics.Dexter
{
    public class JobTarget
    {
        public string Controller { get; set; }
        public string UserName { get; set; }
        public string UserPassword { get; set; }
        public string Application { get; set; }
        public int ApplicationID { get; set; }
        public bool NameRegex { get; set; }
        public JobTargetStatus Status { get; set; }

        public override string ToString()
        {
            return String.Format("JobTarget: {0}/{1}({2})", this.Controller, this.Application, this.ApplicationID);
        }
    }
}
