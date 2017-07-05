namespace AppDynamics.OfflineData.JobParameters
{
    public class JobInputSnapshotType
    {
        public bool Normal { get; set; }
        public bool Slow { get; set; }
        public bool VerySlow { get; set; }
        public bool Stall { get; set; }
        public bool Error { get; set; }
    }
}
