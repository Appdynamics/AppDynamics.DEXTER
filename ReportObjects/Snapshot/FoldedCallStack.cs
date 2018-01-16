using System;
using System.Linq;

namespace AppDynamics.Dexter.DataObjects
{
    public class FoldedCallStack
    {
        public string CallStack { get; set; }
        public long Count { get; set; }


        public override String ToString()
        {
            return String.Format(
                "FoldedCallStack: Count:{0}, Depth:{1}, Stack:{2}",
                this.Count,
                this.CallStack.Count(x => x == ';'),
                this.CallStack);
        }
    }
}
