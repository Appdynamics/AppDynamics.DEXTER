using System;
using System.Collections.Generic;
using System.Text;

namespace AppDynamics.Dexter.ReportObjects
{
    public class CallChainContainer
    {
        public CallChainContainer()
        {
            this.CallTimings = new List<CallTiming>();
        }

        public string From { get; set; }
        public string ToEntityName { get; set; }
        public string ToEntityType { get; set; }
        public string ExitType { get; set; }
        public List<CallTiming> CallTimings { get; set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("{0}", this.From);
            sb.Append("->");
            sb.AppendFormat("[{0}]", this.ExitType);
            sb.Append("/[");
            if (this.CallTimings.Count > 1)
            {
                sb.AppendFormat("{0}:(", this.CallTimings.Count);
            }
            for (int i = 0; i < this.CallTimings.Count; i++)
            {
                sb.AppendFormat("{0} ms", this.CallTimings[i].Duration);
                if (this.CallTimings[i].Async == true)
                {
                    sb.Append(" async");
                }
                if (i < this.CallTimings.Count - 1)
                {
                    sb.Append(", ");                    
                }
            }
            if (this.CallTimings.Count > 1)
            {
                sb.Append(")");
            }
            sb.Append("]->");
            switch (this.ToEntityType)
            {
                case "Backend":
                    sb.AppendFormat("<{0}>", this.ToEntityName);
                    break;
                case "Tier":
                    sb.AppendFormat("({0})", this.ToEntityName);
                    break;
                case "Application":
                    sb.AppendFormat("{{{0}}}", this.ToEntityName);
                    break;
                default:
                    sb.AppendFormat("?0?", this.ToEntityName);
                    break;
            }
            return sb.ToString();
        }
    }

    public class CallTiming
    {
        public long Duration { get; set; }
        public bool Async { get; set; }
    }
}
