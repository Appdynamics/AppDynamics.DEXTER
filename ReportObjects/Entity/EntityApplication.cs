using System;

namespace AppDynamics.Dexter.DataObjects
{
    public class EntityApplication : EntityBase
    {
        public const string ENTITY_TYPE = "Application";
        public const string ENTITY_FOLDER = "APP";

        public string Description { get; set; }

        public int NumBackends { get; set; }
        public int NumBTs { get; set; }
        public int NumErrors { get; set; }
        public int NumIPs { get; set; }
        public int NumNodes { get; set; }
        public int NumSEPs { get; set; }
        public int NumTiers { get; set; }

        public int NumSnapshots { get; set; }
        public int NumSnapshotsNormal { get; set; }
        public int NumSnapshotsSlow { get; set; }
        public int NumSnapshotsVerySlow { get; set; }
        public int NumSnapshotsStall { get; set; }
        public int NumSnapshotsError { get; set; }

        public int NumEvents { get; set; }
        public int NumEventsInfo { get; set; }
        public int NumEventsWarning { get; set; }
        public int NumEventsError { get; set; }
        public int NumHRViolations { get; set; }
        public int NumHRViolationsWarning { get; set; }
        public int NumHRViolationsCritical { get; set; }

        public override long EntityID
        {
            get
            {
                return this.ApplicationID;
            }
        }
        public override string EntityName
        {
            get
            {
                return this.ApplicationName;
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
        
        public EntityApplication Clone()
        {
            return (EntityApplication)this.MemberwiseClone();
        }

        public override String ToString()
        {
            return String.Format(
                "EntityApplication: {0}/{1}({2})",
                this.Controller,
                this.ApplicationName,
                this.ApplicationID);
        }
    }
}
