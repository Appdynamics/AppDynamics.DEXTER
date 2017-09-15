using System;

namespace AppDynamics.Dexter.DataObjects
{
    /// <summary>
    ///{
    ///  "description": "",
    ///  "id": 11226,
    ///  "name": "CRMLIVE-NAM-SB201"
    ///},
    ///  {
    ///  "description": "",
    ///  "id": 11227,
    ///  "name": "CRMLIVE-NAM-SB202"
    ///},
    /// </summary>
    public class AppDRESTApplication
    {
        public string description { get; set; }
        public long id { get; set; }
        public string name { get; set; }

        public override String ToString()
        {
            return String.Format(
                "AppDRESTApplication: {0}({1})",
                this.name,
                this.id);
        }
    }
}
