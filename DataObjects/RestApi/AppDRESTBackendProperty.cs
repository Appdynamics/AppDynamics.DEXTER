using System;

namespace AppDynamics.OfflineData.DataObjects
{
    /// <summary>
    ///[
    ///  {
    ///    "id": 0,
    ///    "name": "Database",
    ///    "value": "DanielOCRM2016Test2_MSCRM"
    ///  },
    ///  {
    ///    "id": 0,
    ///    "name": "Host",
    ///    "value": "CRM2016FULL"
    ///  }
    ///]
    /// </summary>
    public class AppDRESTBackendProperty
    {
        public int id { get; set; }
        public string name { get; set; }
        public string value { get; set; }

        public override String ToString()
        {
            return String.Format(
                "AppDRESTBackendProperty: {0}({1})",
                this.name,
                this.id);
        }
    }
}
