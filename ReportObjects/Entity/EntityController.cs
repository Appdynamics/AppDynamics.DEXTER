using System;

namespace AppDynamics.Dexter.DataObjects
{
    public class EntityController : EntityBase
    {
        public string UserName { get; set; }
        public string Version { get; set; }

        public int NumApps { get; set; }

        public override String ToString()
        {
            return String.Format(
                "EntityController: {0}",
                this.Controller);
        }

    }
}
