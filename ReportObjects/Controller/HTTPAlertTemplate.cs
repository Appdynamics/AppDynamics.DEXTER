using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class HTTPAlertTemplate
    {
        public string Controller { get; set; }

        public string Name { get; set; }

        public string Method { get; set; }
        public string Scheme { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
        public string Path { get; set; }
        public string Query { get; set; }

        public string AuthType { get; set; }
        public string AuthUsername { get; set; }
        public string AuthPassword { get; set; }

        public string Headers { get; set; }
        public string ContentType { get; set; }
        public string FormData { get; set; }
        public string Payload { get; set; }

        public long ConnectTimeout { get; set; }
        public long SocketTimeout { get; set; }

        public string ResponseAny { get; set; }
        public string ResponseNone { get; set; }

        public long TemplateID { get; set; }

        public override String ToString()
        {
            return String.Format(
                "HTTPAlertTemplate: {0} {1} {2}://{3}:{4}/{5}?{6}",
                this.Controller,
                this.Name,
                this.Scheme, 
                this.Host,
                this.Port,
                this.Path,
                this.Query);
        }
    }
}
