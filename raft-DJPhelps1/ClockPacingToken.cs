using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace raft_DJPhelps1
{
    public record ClockPacingToken
    {
        public int DelayValue;
        public int TimeScaleMultiplier;
    }
}
