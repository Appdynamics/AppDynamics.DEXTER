using System;
using System.Collections.Generic;

namespace AppDynamics.Dexter.DataObjects
{
    /// <summary>
    ///  {
    ///    "applicationComponentNodeId": 0,
    ///    "exitPointType": "ADODOTNET",
    ///    "id": 132,
    ///    "name": "Customer Database.CRM2016FULL.DanielOCRM2016Test2_MSCRM",
    ///    "properties":     [
    ///      {
    ///        "id": 0,
    ///        "name": "Database",
    ///        "value": "DanielOCRM2016Test2_MSCRM"
    ///      },
    ///            {
    ///        "id": 0,
    ///        "name": "Host",
    ///        "value": "CRM2016FULL"
    ///      }
    ///    ],
    ///    "tierId": 0
    ///  }
    /// </summary>
    public class AppDRESTBackend
    {
        public int applicationComponentNodeId { get; set; }
        public string exitPointType { get; set; }
        public long id { get; set; }
        public string name { get; set; }
        public List<AppDRESTBackendProperty> properties { get; set; }
        public long tierId { get; set; }

        public override String ToString()
        {
            return String.Format(
                "AppDRESTBackend: {0}({1})",
                this.name,
                this.id);
        }
    }
}
