using raft_DJPhelps1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace raft_DJPhelps1
{
    public record NodeData(
        Guid Node_Id,
        bool Status,
        int ElectionTimer,
        int Term,
        Guid CurrentTermLeader,
        int NextIndex,
        int LogIndex,
        Dictionary<int, CommandToken> Log,
        string? State,
        int TimeoutMultiplier
    );
}
