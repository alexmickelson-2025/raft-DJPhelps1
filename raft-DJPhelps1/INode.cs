namespace raft_DJPhelps1
{
    public interface INode
    {
        Task SendHeartbeat();
        Task MakeLeader();
        Task RequestVotes();
        Task StartNewElection();
        Task Start();
        Task AppendEntries(Guid leader);
        Task AppendEntries(Guid g, int i);
        Task<bool> RequestVoteRPC(Guid id, int term);
    }
}
