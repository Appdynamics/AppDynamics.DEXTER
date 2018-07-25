using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class EntitySIMMachineProcess : EntitySIMBase
    {
        public const string ENTITY_TYPE = "SIM Machine Process";
        public const string ENTITY_FOLDER = "MCHN";

        public long TierID { get; set; }
        public string TierName { get; set; }

        public long NodeID { get; set; }
        public string NodeName { get; set; }

        public long MachineID { get; set; }
        public string MachineName { get; set; }

        public string Class { get; set; }
        public string ClassID { get; set; }
        public string Name { get; set; }
        public string CommandLine { get; set; }
        public string RealUser { get; set; }
        public string RealGroup { get; set; }
        public string EffectiveUser { get; set; }
        public string EffectiveGroup { get; set; }
        public string State { get; set; }
        public int NiceLevel { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public int PID { get; set; }
        public int ParentPID { get; set; }
        public int PGID { get; set; }

        public override long EntityID
        {
            get
            {
                return this.MachineID;
            }
        }
        public override string EntityName
        {
            get
            {
                return this.MachineName;
            }
        }
        public override string EntityType
        {
            get
            {
                return ENTITY_TYPE;
            }
        }
        public override string FolderName
        {
            get
            {
                return ENTITY_FOLDER;
            }
        }

        public EntityNode Clone()
        {
            return (EntityNode)this.MemberwiseClone();
        }

        public override String ToString()
        {
            return String.Format(
                "EntitySIMMachineCPU: {0}/{1}({2})/{3}({4})/{5}({6})",
                this.Controller,
                this.ApplicationName,
                this.ApplicationID,
                this.TierName,
                this.TierID,
                this.NodeName,
                this.NodeID);
        }
    }
}
