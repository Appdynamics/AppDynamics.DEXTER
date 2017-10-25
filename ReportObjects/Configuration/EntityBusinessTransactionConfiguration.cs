using System;
using System.Collections.Generic;

namespace AppDynamics.Dexter.DataObjects
{
    public class EntityBusinessTransactionConfiguration
    {
        public string Controller { get; set; }
        public string ControllerLink { get; set; }

        public long ApplicationID { get; set; }
        public string ApplicationLink { get; set; }
        public string ApplicationName { get; set; }

        public long TierID { get; set; }
        public string TierName { get; set; }

        public long BTID { get; set; }
        public string BTName { get; set; }
        public string BTType { get; set; }

        public bool IsExcluded { get; set; }
        public bool IsBackground { get; set; }
        public bool IsEUMEnabled { get; set; }
        public string IsEUMPossible { get; set; }
        public bool IsAnalyticsEnabled { get; set; }

        public string BTSLAConfig { get; set; }
        public string BTSnapshotCollectionConfig { get; set; }
        public string BTRequestThresholdConfig { get; set; }
        public string BTBackgroundSnapshotCollectionConfig { get; set; }
        public string BTBackgroundRequestThresholdConfig { get; set; }

        public int NumAssignedMIDCs { get; set; }
        public string AssignedMIDCs { get; set; }

        public override String ToString()
        {
            return String.Format(
                "EntityBusinessTransactionConfiguration: {0}/{1}/{2}/{3}",
                this.Controller,
                this.ApplicationName,
                this.TierName,
                this.BTName);
        }
    }
}
