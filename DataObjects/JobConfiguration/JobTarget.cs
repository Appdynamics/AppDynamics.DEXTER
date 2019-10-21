using System;
using System.Net;

namespace AppDynamics.Dexter
{
    public class JobTarget
    {
        private string _controller = string.Empty;
        private Uri _controllerURI = null;

        public string Controller
        {
            get
            {
                return this._controller;
            }
            set
            {
                if (value.Length > 0)
                {
                    try
                    {
                        Uri controllerUri = new Uri(value);
                        this._controllerURI = controllerUri;
                        this._controller = controllerUri.GetComponents(UriComponents.SchemeAndServer, UriFormat.SafeUnescaped);
                    }
                    catch { }
                }
            }
        }

        public string UserName { get; set; }
        public string UserPassword { get; set; }
        public string Application { get; set; }
        public long ApplicationID { get; set; }
        public long DBCollectorID { get; set; }
        public long ParentApplicationID { get; set; }
        public bool NameRegex { get; set; }
        public string Type { get; set; }
        public string ControllerVersion { get; set; }

        public JobTarget Clone()
        {
            return (JobTarget)this.MemberwiseClone();
        }

        public override string ToString()
        {
            return String.Format("JobTarget: {0}/{1}({2})[{3}]", this.Controller, this.Application, this.ApplicationID, this.Type);
        }
    }
}
