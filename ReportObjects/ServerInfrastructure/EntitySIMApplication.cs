using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class EntitySIMApplication : EntitySIMBase
    {
        public const string ENTITY_TYPE = "SIM Application";
        public const string ENTITY_FOLDER = "APP";

        public int NumTiers { get; set; }
        public int NumNodes { get; set; }
        public int NumMachines { get; set; }

        public int NumSAs { get; set; }

        public int NumSAEvents { get; set; }

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
                "EntitySIMApplication: {0}/{1}({2})",
                this.Controller,
                this.ApplicationName,
                this.ApplicationID);
        }
    }
}
