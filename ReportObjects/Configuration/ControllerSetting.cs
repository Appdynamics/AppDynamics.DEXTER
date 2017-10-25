using System;

namespace AppDynamics.Dexter.DataObjects
{
    public class ControllerSetting
    {
        public string Controller { get; set; }
        public string ControllerLink { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Scope { get; set; }
        public string Value { get; set; }
        public bool Updateable { get; set; }

        public override String ToString()
        {
            return String.Format(
                "ControllerSetting: {0} {1}={2}",
                this.Controller,
                this.Name,
                this.Value);
        }
    }
}
