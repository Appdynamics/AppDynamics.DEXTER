using System;

namespace AppDynamics.Dexter
{
    public class ControllerCredential
    {

        public string Controller { get; set; }
        public string UserName { get; set; }
        public string UserPassword { get; set; }

        public override string ToString()
        {
            return String.Format("ControllerCredential: {0} {1} ({2})", this.Controller, this.UserName);
        }
    }
}
