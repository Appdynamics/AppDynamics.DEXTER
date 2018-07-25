using System;
using System.Collections.Generic;

namespace AppDynamics.Dexter.ReportObjects
{
    public class EntitySIMBase
    {
        public string Controller { get; set; }
        public string ControllerLink { get; set; }

        public long ApplicationID { get; set; }
        public string ApplicationLink { get; set; }
        public string ApplicationName { get; set; }

        public virtual long EntityID { get; }
        public virtual string EntityName { get; }
        public virtual string FolderName { get; }
        public virtual string EntityType { get; }
    }
}
