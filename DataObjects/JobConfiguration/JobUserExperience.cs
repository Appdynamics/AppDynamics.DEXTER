namespace AppDynamics.Dexter
{
    public class JobUserExperience
    {
        public bool Normal { get; set; }
        public bool Slow { get; set; }
        public bool VerySlow { get; set; }
        public bool Stall { get; set; }
        public bool Error { get; set; }
    }
}
