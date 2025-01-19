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
        public int ElectionTimerCurr { get; set; }
        public int ElectionTimerMax {  get; set; }
        public int TimeoutMultiplier {  get; set; }
        public int InternalDelay { get; set; }

        public int Heartbeat {  get; set; }
        public Dictionary<Guid, INode> Nodes { get; set; }
        public Dictionary<int, Guid> Votes { get; set; }
        public CancellationTokenSource DelayStop { get; set; }
        private bool HasWonElection_Flag { get; set; }
        public bool AppendEntriesResponseFlag { get; set; } // placeholder

        public Node()
        {
            AppendEntriesResponseFlag = false;
            State = "Follower";
            Heartbeat = 50;
            Term = 1;
            IsStarted = false;
            Id = Guid.NewGuid();
            TimeoutMultiplier = 1;
            InternalDelay = 0;
            Random initializer = new Random();
            ElectionTimerMax = initializer.Next(150, 300);
            
            RefreshElectionTimeout();

            Nodes = new Dictionary<Guid, INode>();
            Votes = new Dictionary<int, Guid>();
            DelayStop = new CancellationTokenSource();
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
                            Console.WriteLine($" {Id}\n", e.Message);
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

        public async void RequestVoteRPC(Guid candidate_id, int election_term)
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
            Thread.Sleep(InternalDelay);
            foreach(INode n in Nodes.Values)
            {
                n.AppendEntriesRPC(this.Id);
            }
        }



        public void AppendEntriesRPC(Guid Leader)
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

            if (Nodes.ContainsKey(Leader))
                Nodes[Leader].AppendResponseRPC(Id, validEntryFlag);
        }

        public void AppendEntriesRPC(Guid NewLeader, int newTerm)
        {
            bool validentryflag = false;
            if (Nodes.ContainsKey(NewLeader) && Nodes[NewLeader].Term >= this.Term)
            {
                validentryflag = true;
                State = "Follower";
                Term = Nodes[NewLeader].Term;
                DelayStop.Cancel();
                CurrentLeader = NewLeader;
                RefreshElectionTimeout();
            }

            if (Nodes.ContainsKey(NewLeader))
                Nodes[NewLeader].AppendResponseRPC(Id, validentryflag);
        }

        public void AppendResponseRPC(Guid RPCReceiver, bool response)
        {
            AppendEntriesResponseFlag = response;
        }

        public async void MakeLeader()
        {
            State = "Leader";
            await SendHeartbeat();
            VoteCountForMe++;
        }
    }
}
