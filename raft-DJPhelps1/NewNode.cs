using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace raft_DJPhelps1
{
    public class NewNode : INode
    {
        public Guid Id { get; set; }
        public int Term { get; set; }
        public Dictionary<Guid, INode> Nodes { get; set; }
        public int TimeoutMultiplier { get; set; }
        public int InternalDelay { get; set; }

        public NewNode()
        {
            Id = Guid.NewGuid();
            Term = 0;
            Nodes = new Dictionary<Guid, INode>();
        }

        public void AppendEntriesRPC(Guid leader)
        {
            throw new NotImplementedException();
        }

        public void AppendEntriesRPC(Guid g, int i)
        {
            throw new NotImplementedException();
        }

        public void AppendResponseRPC(Guid RPCReceiver, bool response)
        {
            throw new NotImplementedException();
        }

        public void IncrementVoteCount()
        {
            throw new NotImplementedException();
        }

        public void MakeLeader()
        {
            throw new NotImplementedException();
        }

        public void ReceiveVoteRPC(Guid id, int term, bool voteGranted)
        {
            throw new NotImplementedException();
        }

        public void RequestVoteRPC(Guid id, int term)
        {
            throw new NotImplementedException();
        }

        public void RequestVotesFromClusterRPC()
        {
            throw new NotImplementedException();
        }

        public Task SendHeartbeat()
        {
            throw new NotImplementedException();
        }

        public void Start()
        {
            throw new NotImplementedException();
        }

        public void StartNewElection()
        {
            throw new NotImplementedException();
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }
    }
}
