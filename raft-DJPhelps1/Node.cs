using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace raft_DJPhelps1
{
    public class Node : INode
    {
        public bool IsStarted { get; set; } // false = NotStarted or Cancel; true = Started
        public string State { get; set; }     
        public int VoteCountForMe { get; set; }
        public Guid CurrentLeader { get; set; }
        public Guid Id { get; set; }
        public int Term { get; set; }
        public int ImportantValue;
        public int ElectionTimerCurr { get; set; }
        public int ElectionTimerMax {  get; set; }
        public int TimeoutMultiplier {  get; set; }
        public int InternalDelay { get; set; }
        public int NextIndex { get; set; } // Need to know how big the stack is in the leader
        public int LogIndex { get; set; } // Need to know where the new commands are implicitly
        public int LogActionCounter { get; set; }
        public int Heartbeat {  get; set; }
        public Dictionary<Guid, INode> Nodes { get; set; }
        public Dictionary<int, Guid> Votes { get; set; }
        public CancellationTokenSource DelayStop { get; set; }
        private bool HasWonElection_Flag { get; set; }
        public bool AppendEntriesResponseFlag { get; set; } // placeholder
        public Dictionary<int, CommandToken> CommandLog { get; set; }

        public Node()
        {
            AppendEntriesResponseFlag = false;
            State = "Follower";
            TimeoutMultiplier = 1;
            Heartbeat = 50 * TimeoutMultiplier;
            Term = 1;
            IsStarted = false;
            Id = Guid.NewGuid();
            ImportantValue = 0;
            InternalDelay = 0;
            NextIndex = 0; // The next index is a new one.
            LogIndex = 0; // The first index is the token index at the start.
            Random initializer = new Random();
            ElectionTimerMax = initializer.Next(150, 300);
            CommandLog = new();
            RefreshElectionTimeout();
            Nodes = new Dictionary<Guid, INode>();
            Votes = new Dictionary<int, Guid>();
            DelayStop = new CancellationTokenSource();
        }

        public void ChangeHeartbeat()
        {

        }

        public void Start()
        {
            Task.Run(async () =>
            {
                if (IsStarted)
                    return;

                IsStarted = true;
                while (IsStarted)
                {
                    if (State == "Leader")
                    {
                        try
                        {
                            await Task.Delay(Heartbeat * TimeoutMultiplier, DelayStop.Token);
                            await SendHeartbeat();
                            Console.WriteLine($"Heartbeat from: {Id}\n");
                        }
                        catch (OperationCanceledException e)
                        {
                            Console.WriteLine("Heartbeat cancelled!", e.Message);
                        }
                    }
                    else if (State == "Candidate")
                    {
                        // Request all votes -> wait for election to end
                        // if timed out, start new election

                        try
                        { // move to StartNewElection Only
                            IsTimedOut();
                        }
                        catch (OperationCanceledException e)
                        {
                            Console.WriteLine($"Election cancelled!", e.Message);
                        }
                    }
                    else if (State == "Follower")
                    {
                        try
                        {
                            await Task.Delay(ElectionTimerCurr * TimeoutMultiplier, DelayStop.Token);

                            State = "Candidate";
                        }
                        catch (OperationCanceledException e)
                        {
                            Console.WriteLine($"Follower {Id} received heartbeat from leader.\n", e.Message);
                        }
                    }
                }
            });
        }

        public void Stop()
        {
            IsStarted = false;
            DelayStop.Cancel();
        }

        public void RequestVotesFromClusterRPC()
        {
            Nodes.Select(n => {
                n.Value.RequestVoteRPC(this.Id, this.Term);
                return true;
                }).ToArray();
        }

        public void RequestVoteRPC(Guid candidate_id, int election_term)
        {
            //In this method: if vote request received, reset election timeout timer
            if(election_term >= Term)
            {
                if (election_term > Term)
                {
                    State = "follower";
                    Term = election_term;
                    Console.WriteLine("Higher term signal detected. Reverting to follower.");
                }
                else if(State == "Candidate")
                {
                    Console.WriteLine($"Received vote request from {candidate_id}.");
                    return;
                }
                

                if (Votes.ContainsKey(election_term) && Votes[election_term] == candidate_id)
                {
                    RefreshElectionTimeout();
                    Term = election_term;
                    DelayStop.Cancel();
                    Nodes[candidate_id].ReceiveVoteRPC(Id, election_term, true); // RespondVoteRPC instead of return
                    Console.WriteLine($"Node {candidate_id} vote request accepted at{DateTime.Now}");
                }
                else if(!Votes.ContainsKey(election_term)) {
                    RefreshElectionTimeout();
                    Votes.Add(election_term, candidate_id);
                    Term = election_term;
                    DelayStop.Cancel();
                    Nodes[candidate_id].ReceiveVoteRPC(Id, election_term, true);
                    Console.WriteLine($"Node {candidate_id} vote request accepted at{DateTime.Now}");
                }
                else
                {
                    DelayStop.Cancel();
                    Nodes[candidate_id].ReceiveVoteRPC(candidate_id, election_term, false);
                    Console.WriteLine($"Node {candidate_id} vote request rejected at{DateTime.Now}");
                }
                
            }
            else
            {
                Console.WriteLine($"Node {candidate_id} vote request rejected at{DateTime.Now}");
            }
        }

        public void CommitEntries()
        {
            CommitEntryRPC();
            foreach (Node node in Nodes.Values)
            {
                node.CommitEntryRPC();
            }
            LogActionCounter = 0;
        }

        public void CommitEntryRPC()
        {
            CommandLog[LogIndex].is_committed = true;
            CommandToken ct = CommandLog[LogIndex];

            switch (ct.command)
            {
                case "add": ImportantValue += ct.value;
                    break;
            }

            LogIndex++;
        }

        public void ReceiveVoteRPC(Guid voeter_id, int term, bool voteGranted)
        { // Increemnt VoteForMe property instead of castvote
            
            if (voteGranted)
                IncrementVoteCount();

            // Check if enough votes -> cancel and handle with catch block
            // If vote expires, term recycles.
            if (VoteCountForMe > Nodes.Count() / 2)
            {
                DelayStop.Cancel();
                MakeLeader();
            }
        }

        public void IncrementVoteCount()
        {
            VoteCountForMe++;
        }

        public void StartNewElection()
        {
            VoteCountForMe = 0;
            Term++;
            CurrentLeader = Id;

            RequestVotesFromClusterRPC();
            ReceiveVoteRPC(Id, Term, true);

            while (ElectionTimerCurr > 0)
            {
                Thread.Sleep(10);
                ElectionTimerCurr -= 10;
            }

        }


        public void RefreshElectionTimeout()
        {
            ElectionTimerCurr = ElectionTimerMax;
        }

        public void IsTimedOut()
        {
            var rand = new Random();
            ElectionTimerMax = rand.Next(150, 300);
            RefreshElectionTimeout();
            StartNewElection();
        }

        public async Task SendHeartbeat()
        {
            if(InternalDelay > 5)
                await Task.Delay(InternalDelay);

            foreach(INode n in Nodes.Values)
            {
                n.AppendEntriesRPC(this.Id, CommandLog[LogIndex]);
            }
        }



        public void AppendEntriesRPC(Guid Leader, CommandToken log_addition)
        {
            bool validEntryFlag = false;

            if (Nodes.ContainsKey(Leader) && log_addition.term >= this.Term)
            {
                validEntryFlag = true;
                State = "Follower";
                Term = log_addition.term;

                DelayStop.Cancel();
                CurrentLeader = Leader;
                RefreshElectionTimeout();
            }

            if(log_addition.index == 0)
            {
                LogIndex = 0;
                NextIndex = 1;
                CommandLog.Add(0, log_addition);
            }
            else
            {

                LogIndex = log_addition.index;

                //if (CommandLog.ContainsKey(log_addition.index) && CommandLog[log_addition.index])
                CommandLog[LogIndex] = log_addition;
            }

            if (Nodes.ContainsKey(Leader))
                Nodes[Leader].AppendResponseRPC(Id, validEntryFlag, log_addition);
        }

        public void AppendEntriesRPC(Guid Leader, int newTerm)
        {
            bool validEntryFlag = false;
            if (Nodes.ContainsKey(Leader) && Nodes[Leader].Term >= this.Term)
            {
                validEntryFlag = true;
                State = "Follower";
                Term = Nodes[Leader].Term;

                DelayStop.Cancel();
                CurrentLeader = Leader;
                RefreshElectionTimeout();
            }

            var newct = new CommandToken();

            if (Nodes.ContainsKey(Leader))
                Nodes[Leader].AppendResponseRPC(Id, validEntryFlag, newct);
        }

        public void AppendResponseRPC(Guid RPCReceiver, bool ValidLeader, CommandToken ValidEntry)
        {
            AppendEntriesResponseFlag = ValidLeader;
            if (ValidEntry.command == "")
                return;
            if (!ValidEntry.is_committed)
                LogIndex--;
            else if (ValidEntry.is_committed)
                LogActionCounter++;
        }

        public async void MakeLeader()
        {
            State = "Leader";

            CommandToken newToken = new()
            {
                command = "",
                term = Term,
                value = 0,
                index = NextIndex,
                is_committed = false
            };

            CommandLog.Add(NextIndex, newToken);
            NextIndex++;
            await SendHeartbeat();
            VoteCountForMe = 0;
        }


        public void RequestAdd(int input_num)
        {
            CommandToken newToken = new()
            {
                command = "add",
                term = Term,
                value = input_num,
                index = NextIndex,
                is_committed = false
            };

            CommandLog.Add(NextIndex, newToken);
            NextIndex++;
        }
    }
}
