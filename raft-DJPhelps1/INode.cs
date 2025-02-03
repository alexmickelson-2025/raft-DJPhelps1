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
        Task ReceiveVoteRPC(Guid id, int term, bool voteGranted);
        Task IncrementVoteCount();
        Task AppendEntriesRPC(Guid leader, CommandToken ct);
        //void AppendEntriesRPC(Guid g, int i);
        Task RequestVoteRPC(Guid id, int term);
        Task AppendResponseRPC(Guid RPCReceiver, bool response1, CommandToken response2);
        Task<bool> RequestAdd(int input_num);
    }
}
