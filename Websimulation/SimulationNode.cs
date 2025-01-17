using raft_DJPhelps1;

public class SimulationNode
{
    public readonly Node InnerNode;

    public SimulationNode(Node? di_node)
    {
        InnerNode = di_node ?? new Node();
    }

    public Task AppendEntries(Guid leader)
    {
        //return ((INode)InnerNode).AppendEntries(leader);
        throw new NotImplementedException();
    }

    public Task AppendEntries(Guid nodeId, int CurrentTerm)
    {
        //return ((INode)InnerNode).AppendEntries(nodeId, CurrentTerm);
        throw new NotImplementedException();
    }

    public Task MakeLeader()
    {
        //return ((INode)InnerNode).MakeLeader();
        throw new NotImplementedException();
    }

    public Task<bool> RequestVoteRPC(Guid id, int term)
    {
        //return ((INode)InnerNode).RequestVoteRPC(id, term);
        throw new NotImplementedException();
    }

    public Task RequestVotes()
    {
        //return ((INode)InnerNode).RequestVotes();
        throw new NotImplementedException();
    }

    public Task SendHeartbeat()
    {
        //return ((INode)InnerNode).SendHeartbeat();
        throw new NotImplementedException();
    }

    public Task Start()
    {
        //return ((INode)InnerNode).Start();
        throw new NotImplementedException();
    }

    public Task StartNewElection()
    {
        //return ((INode)InnerNode).StartNewElection();
        throw new NotImplementedException();
    }
    // public SimulationNode
}