using raft_DJPhelps1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaftRESTAPI
{
    public record NodeData
    {
        public Guid Id;
        public bool Status;
        public int ElectionTimer;
        public int Term;
        public Guid CurrentTermLeader;
        public int CommittedEntryIndex;
        public int LogIndex;
        public List<CommandToken> Log;
        public string? State;
        public int TimeoutMultiplier;
    }
}
