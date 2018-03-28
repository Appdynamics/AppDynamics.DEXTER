using System;

namespace AppDynamics.Dexter
{
    public class JobTimeRange
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public override String ToString()
        {
            return String.Format(
                "JobTimeRange: {0:o}-{1:o} {2:o}-{3:o}",
                this.From,
                this.To,
                this.From.ToLocalTime(),
                this.To.ToLocalTime());
        }
    }
}
