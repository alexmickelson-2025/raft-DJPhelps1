namespace raft_DJPhelps1
{
    public interface INode
    {
        public Guid Id { get; set; }
        public int Term {  get; set; }
        public Dictionary<Guid, INode> Nodes { get; set; }
        public int TimeoutMultiplier { get; set; }
        public int InternalDelay { get; set; }
        Task SendHeartbeat();
        void MakeLeader();
        void RequestVotesFromClusterRPC();
        void StartNewElection();
        void Start();
        void Stop();
        void ReceiveVoteRPC(Guid id, int term, bool voteGranted);
        void IncrementVoteCount();
        void AppendEntriesRPC(Guid leader, CommandToken ct);
        void AppendEntriesRPC(Guid g, int i);
        void RequestVoteRPC(Guid id, int term);
        void AppendResponseRPC(Guid RPCReceiver, bool response1, CommandToken response2);
        void RequestAdd(int input_num);
    }
}
