using System;

namespace AppDynamics.Dexter.DataObjects
{
    public class FlameGraphBox
    {
        public string FullName { get; set; }
        public int Depth { get; set; }
        public int Start { get; set; }
        public int Samples { get; set; }
        public long Duration { get; set; }
        public int End
        {
            get
            {
                return this.Start + this.Samples;
            }
        }

        public override String ToString()
        {
            return String.Format(
                "FlameGraphBox: {0}, Depth={1}, Start={2}, End={3}, Samples={4}, Duration={5}",
                this.FullName,
                this.Depth,
                this.Start,
                this.End,
                this.Samples,
                this.Duration);
        }
    }
}
