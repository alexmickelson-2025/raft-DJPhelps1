namespace raft_DJPhelps1
{
    public interface INode
    {
        Task MakeLeader();
        Task RequestVote(Guid id, object term);
        Task StartNewElection();
    }
}
