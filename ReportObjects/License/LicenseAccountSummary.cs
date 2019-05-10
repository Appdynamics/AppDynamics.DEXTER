using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class LicenseAccountSummary
    {
        public string Controller { get; set; }

        public long AccountID { get; set; }
        public string AccountName { get; set; }
        public string AccountNameGlobal { get; set; }
        public string AccountNameEUM { get; set; }
        public string AccessKey1 { get; set; }
        public string AccessKey2 { get; set; }
        public string LicenseKeyEUM { get; set; }
        public string ServiceKeyES { get; set; }

        public DateTime ExpirationDate { get; set; }

        public string LicenseLink { get; set; }

        public override String ToString()
        {
            return String.Format(
                "LicenseAccountSummary: {0}/{1}",
                this.Controller,
                this.AccountName);
        }
    }
}
