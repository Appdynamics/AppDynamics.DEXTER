using System;

namespace AppDynamics.OfflineData.DataObjects
{
    /// <summary>
    ///{
    ///  "name": "CrmAction.WhoAmI",
    ///  "type": "folder"
    ///},
    ///  {
    ///  "name": "CrmAction.RetrieveMultiple",
    ///  "type": "folder"
    ///}
    /// </summary>
    public class AppDRESTMetricFolder
    {
        public string name { get; set; }
        public string type { get; set; }

        public override String ToString()
        {
            return String.Format(
                "AppDRESTMetricFolder: {0}({1})",
                this.name,
                this.type);
        }
    }
}
