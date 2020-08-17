using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class BIQBusinessJourney : BIQEntityBase
    {
        public const string ENTITY_TYPE = "BizJourney";
        public const string ENTITY_FOLDER = "BIZJOURN";

        public string JourneyName { get; set; }
        public string JourneyDescription { get; set; }
        public string JourneyID { get; set; }

        public string State { get; set; }
        public string KeyField { get; set; }

        public bool IsEnabled { get; set; }

        public string Stages { get; set; }
        public int NumStages { get; set; }

        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }

        public DateTime CreatedOn { get; set; }
        public DateTime CreatedOnUtc { get; set; }
        public DateTime UpdatedOn { get; set; }
        public DateTime UpdatedOnUtc { get; set; }

        public override String ToString()
        {
            return String.Format(
                "BIQBusinessJourney: {0}/{1}({2}) {3}",
                this.Controller,
                this.ApplicationName,
                this.ApplicationID,
                this.JourneyName);
        }
    }
}
