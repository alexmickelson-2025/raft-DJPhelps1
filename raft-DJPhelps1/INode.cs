namespace raft_DJPhelps1
{
    public interface INode
    {
        public Guid Id { get; set; }
        public int Term {  get; set; }
        public Dictionary<Guid, INode> Nodes { get; set; }
        Task SendHeartbeat();
        void MakeLeader();
        void RequestVotes();
        void StartNewElection();
        void Start();
        void Stop();
        void ReceiveVoteRPC(Guid id, int term, bool voteGranted);
        void IncrementVoteCount();
        void AppendEntries(Guid leader);
        void AppendEntries(Guid g, int i);
        void RequestVoteRPC(Guid id, int term);
        void AppendResponseRPC(Guid RPCReceiver, bool response);
    }
}
