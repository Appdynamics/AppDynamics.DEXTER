using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class BIQSearch : BIQEntityBase
    {
        public string SearchName { get; set; }
        public string InternalName { get; set; }
        public string SearchType { get; set; }
        public string SearchMode { get; set; }
        public string Description { get; set; }
        public string Visualization { get; set; }
        public string ViewMode { get; set; }
        public string DataSource { get; set; }
        public long SearchID { get; set; }
        public string SearchLink { get; set; }

        public int NumWidgets { get; set; }

        public string Query { get; set; }

        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }

        public DateTime CreatedOn { get; set; }
        public DateTime CreatedOnUtc { get; set; }
        public DateTime UpdatedOn { get; set; }
        public DateTime UpdatedOnUtc { get; set; }

        public override String ToString()
        {
            return String.Format(
                "BIQSearch: {0}/{1}({2}) {3}[{4}]({5})",
                this.Controller,
                this.ApplicationName,
                this.ApplicationID,
                this.SearchName,
                this.SearchType,
                this.SearchID);
        }
    }
}
