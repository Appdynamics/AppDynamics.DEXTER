using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class MethodCallLineClassTypeMapping
    {
        public string ClassPrefix { get; set; }
        public string FrameworkType { get; set; }
        public string FlameGraphColorStart { get; set; }
        public string FlameGraphColorEnd { get; set; }

        public override String ToString()
        {
            return String.Format(
                "MethodCallLineClassTypeMapping: {0}->{1}",
                this.ClassPrefix,
                this.FrameworkType);
        }
    }
}
