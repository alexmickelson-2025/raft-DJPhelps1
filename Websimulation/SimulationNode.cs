using raft_DJPhelps1;
using System.ComponentModel;

public class SimulationNode : INode
{
    public readonly Node InnerNode;
    public int NodeElectionProgress;
    public int NodeHeartbeatProgress;

    public EventHandler? Refresh;

    public SimulationNode(Node? di_node)
    {
        InnerNode = di_node ?? new Node();
        ResetELTimer();
        ResetHeartbeatTimer();
    }

    public void ResetELTimer()
    {
        NodeElectionProgress = InnerNode.ElectionTimerMax * InnerNode.TimeoutMultiplier;
    }
    public void ResetHeartbeatTimer()
    {
        NodeHeartbeatProgress = InnerNode.Heartbeat * InnerNode.TimeoutMultiplier;
    }

    public Guid Id { get => ((INode)InnerNode).Id; set => ((INode)InnerNode).Id = value; }
    public int Term { get => ((INode)InnerNode).Term; set => ((INode)InnerNode).Term = value; }
    public Dictionary<Guid, INode> Nodes { get => ((INode)InnerNode).Nodes; set => ((INode)InnerNode).Nodes = value; }
    public Guid CurrentLeader {
        get => ((Node)InnerNode).CurrentLeader;
        set => ((Node)InnerNode).CurrentLeader = value;
    }
    public int TimeoutMultiplier { get => ((INode)InnerNode).TimeoutMultiplier; set => ((INode)InnerNode).TimeoutMultiplier = value; }
    public int InternalDelay { get => ((INode)InnerNode).InternalDelay; set => ((INode)InnerNode).InternalDelay = value; }

    

    public void AppendEntriesRPC(Guid leader)
    {
        Refresh?.Invoke(this, EventArgs.Empty);
        ResetELTimer();
        //((INode)InnerNode).AppendEntriesRPC(leader);
    }

    public void AppendEntriesRPC(Guid g, CommandToken i)
    {
        Refresh?.Invoke(this, EventArgs.Empty);
        ResetELTimer();
        ((INode)InnerNode).AppendEntriesRPC(g, i);
    }

    public void IncrementVoteCount()
    {
        ((INode)InnerNode).IncrementVoteCount();
    }

    public void MakeLeader()
    {
        ((INode)InnerNode).MakeLeader();
    }

    public void ReceiveVoteRPC(Guid id, int term, bool voteGranted)
    {
        Refresh?.Invoke(this, EventArgs.Empty);
        ResetELTimer();
        ((INode)InnerNode).ReceiveVoteRPC(id, term, voteGranted);
    }

    public void RequestVoteRPC(Guid id, int term)
    {
        ((INode)InnerNode).RequestVoteRPC(id, term);
    }

    public void RequestVotesFromClusterRPC()
    {
        ((INode)InnerNode).RequestVotesFromClusterRPC();
    }

    public Task SendHeartbeat()
    {
        Refresh?.Invoke(this, EventArgs.Empty);
        ResetHeartbeatTimer();
        return ((INode)InnerNode).SendHeartbeat();
    }

    public void Start()
    {
        ((INode)InnerNode).Start();
    }

    public void StartNewElection()
    {
        ((INode)InnerNode).StartNewElection();
    }

    public void Stop()
    {
        ((INode)InnerNode).Stop();
    }

    public void RequestAdd(int input_num)
    {
        ((INode)InnerNode).RequestAdd(input_num);
    }

    public void AppendResponseRPC(Guid RPCReceiver, bool response1, CommandToken response2)
    {
        ((INode)InnerNode).AppendResponseRPC(RPCReceiver, response1, response2);
    }

    Task INode.ReceiveVoteRPC(Guid id, int term, bool voteGranted)
    {
        return ((INode)InnerNode).ReceiveVoteRPC(id, term, voteGranted);
    }

    Task INode.IncrementVoteCount()
    {
        return ((INode)InnerNode).IncrementVoteCount();
    }

    Task INode.AppendEntriesRPC(Guid leader, CommandToken ct)
    {
        return ((INode)InnerNode).AppendEntriesRPC(leader, ct);
    }

    Task INode.RequestVoteRPC(Guid id, int term)
    {
        return ((INode)InnerNode).RequestVoteRPC(id, term);
    }

    Task INode.AppendResponseRPC(Guid RPCReceiver, bool response1, CommandToken response2)
    {
        return ((INode)InnerNode).AppendResponseRPC(RPCReceiver, response1, response2);
    }

    Task<bool> INode.RequestAdd(int input_num)
    {
        return ((INode)InnerNode).RequestAdd(input_num);
    }
}