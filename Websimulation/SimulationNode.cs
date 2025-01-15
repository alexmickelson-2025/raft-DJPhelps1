using raft_DJPhelps1;

public class SimulationNode : INode
{
    public readonly Node InnerNode;

    public SimulationNode(Node? di_node)
    {
        InnerNode = di_node ?? new Node();
    }

    public Task AppendEntries(Guid leader)
    {
        return ((INode)InnerNode).AppendEntries(leader);
    }

    public Task AppendEntries(Guid nodeId, int CurrentTerm)
    {
        return ((INode)InnerNode).AppendEntries(nodeId, CurrentTerm);
    }

    public Task MakeLeader()
    {
        return ((INode)InnerNode).MakeLeader();
    }

    public Task<bool> RequestVoteRPC(Guid id, int term)
    {
        return ((INode)InnerNode).RequestVoteRPC(id, term);
    }

    public Task RequestVotes()
    {
        return ((INode)InnerNode).RequestVotes();
    }

    public Task SendHeartbeat()
    {
        return ((INode)InnerNode).SendHeartbeat();
    }

    public Task Start()
    {
        return ((INode)InnerNode).Start();
    }

    public Task StartNewElection()
    {
        return ((INode)InnerNode).StartNewElection();
    }
    // public SimulationNode
}