using System;

namespace AppDynamics.Dexter.DataObjects
{
    public class EntityBackend : EntityBase
    {
        public const string ENTITY_TYPE = "Backend";
        public const string ENTITY_FOLDER = "BACK";

        public long BackendID { get; set; }
        public string BackendLink { get; set; }
        public string BackendName { get; set; }

        public string BackendType { get; set; }

        public int? NumProps { get; set; }
        public string Prop1Name { get; set; }
        public string Prop1Value { get; set; }
        public string Prop2Name { get; set; }
        public string Prop2Value { get; set; }
        public string Prop3Name { get; set; }
        public string Prop3Value { get; set; }
        public string Prop4Name { get; set; }
        public string Prop4Value { get; set; }
        public string Prop5Name { get; set; }
        public string Prop5Value { get; set; }
        public string Prop6Name { get; set; }
        public string Prop6Value { get; set; }
        public string Prop7Name { get; set; }
        public string Prop7Value { get; set; }

        public override long EntityID
        {
            get
            {
                return this.BackendID;
            }
        }
        public override string EntityName
        {
            get
            {
                return this.BackendName;
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

        public EntityBackend Clone()
        {
            return (EntityBackend)this.MemberwiseClone();
        }

        public override String ToString()
        {
            return String.Format(
                "EntityBackend:  {0}/{1}({2})/{3}({4})",
                this.Controller,
                this.ApplicationName,
                this.ApplicationID, 
                this.BackendName,
                this.BackendID);
        }
    }
}
