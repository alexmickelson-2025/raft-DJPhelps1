using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace raft_DJPhelps1
{
    public class Node : INode
    {
        public string State { get; set; }        
        public Guid Id { get; internal set; }
        public int ElectionTimeout { get; set; }
        public IEnumerable<Guid> ClusterList { get; set; }
        public int VoteCount { get; set; }
        public object Term { get; set; }

        public void MakeLeader()
        {
            throw new NotImplementedException();
        }

        public async Task RequestVote(Guid id, object term)
        {
            throw new NotImplementedException();
        }

        public async Task StartNewElection()
        {
            throw new NotImplementedException();
        }
    }
}
