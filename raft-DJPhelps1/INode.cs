namespace raft_DJPhelps1
{
    public interface INode
    {
        Task SendHeartbeat();
        Task MakeLeader();
        void RequestVotes();
        void StartNewElection();
        void Start();
        void Stop();
        public Guid Id { get; set; }
        public int Term {  get; set; }

        void AppendEntries(Guid leader);
        void AppendEntries(Guid g, int i);
        Task<bool> RequestVoteRPC(Guid id, int term);
    }
}
