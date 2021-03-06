﻿using System;

namespace AppDynamics.Dexter.DataObjects
{
    /// <summary>
    ///  {
    ///    "background": false,
    ///    "entryPointType": "ASP_DOTNET",
    ///    "id": 162,
    ///    "internalName": "Web-AppWebService",
    ///    "name": "Web-AppWebService",
    ///    "tierId": 31,
    ///    "tierName": "Web"
    ///  }
    /// </summary>
    public class AppDRESTBusinessTransaction
    {
        public bool background { get; set; }
        public string entryPointType { get; set; }
        public long id { get; set; }
        public string internalName { get; set; }
        public string name { get; set; }
        public long tierId { get; set; }
        public string tierName { get; set; }

        public override String ToString()
        {
            return String.Format(
                "AppDRESTBusinessTransaction: {0}({1})",
                this.name,
                this.id);
        }
    }
}
