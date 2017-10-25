using System;
using System.Collections.Generic;

namespace AppDynamics.Dexter.DataObjects
{
    public class EntityTierConfiguration
    {
        public string Controller { get; set; }
        public string ControllerLink { get; set; }

        public long ApplicationID { get; set; }
        public string ApplicationLink { get; set; }
        public string ApplicationName { get; set; }

        public long TierID { get; set; }
        public string TierName { get; set; }
        public string TierType { get; set; }
        public string TierDescription { get; set; }

        public bool IsDynamicScalingEnabled { get; set; }

        public string MemoryConfig { get; set; }
        public string CacheConfig { get; set; }
        public string CustomCacheConfig { get; set; }

        public int NumBTs { get; set; }
        public int NumBTTypes { get; set; }
        public override String ToString()
        {
            return String.Format(
                "EntityTierConfiguration: {0}/{1}/{2}",
                this.Controller,
                this.ApplicationName,
                this.TierName);
        }
    }
}
