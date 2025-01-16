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
        public Guid CurrentLeader { get; set; }
        public Guid Id { get; set; }
        public int Term { get; set; }
        public int ElectionTimeoutCurr { get; set; }
        public int ElectionTimerMax {  get; set; }
        public int Heartbeat {  get; set; }
        public int VoteCount { get; set; }
        public Dictionary<Guid, INode> Nodes { get; set; } = new Dictionary<Guid, INode>();
        public Dictionary<(int,Guid), Guid> Votes { get; set; } = new Dictionary<(int,Guid), Guid>();
        public CancellationTokenSource DelayStop { get; set; }

        public Node()
        {
            DelayStop = new CancellationTokenSource();
            Random initializer = new Random();
            ElectionTimeoutCurr = initializer.Next(150, 300);
            State = "Follower";
            Heartbeat = 50;
            Term = 1;
            IsStarted = false;
            RefreshElectionTimeout();
            Id = Guid.NewGuid();
        }


        public async Task MakeLeader()
        {
            State = "Leader";
            foreach (var node in Nodes.Values)
            {
                node.AppendEntries(Id, Term);
            }
        }

        public void RequestVotes()
        {
            Votes.Add((Term, Id), Id);
            Task.Run(async () =>
            { 
                foreach (INode n in Nodes.Values)
                {
                    if(await n.RequestVoteRPC(Id, Term))
                    {
                        Votes.Add((Term,n.Id), Id);
                    }
            }
            });
        }

        public async Task<bool> RequestVoteRPC(Guid id, int term)
        {
            await Task.Run(() => { });
            if(term >= Term)
            {
                if (Votes.ContainsKey((term,Id)) && Votes[(term,Id)] == id)
                {
                    Term = term;
                    DelayStop.Cancel();
                    return true;
                }
                else if(!Votes.ContainsKey((term, Id))) {
                    Votes.Add((term, Id), id);
                    Term = term;
                    DelayStop.Cancel();
                    return true;
                }
                else
                {
                    DelayStop.Cancel();
                    return false;
                }
            }
            else
            {
                // log deny vote to id X
                return false;
            }
        }

        public void StartNewElection()
        {
            State = "Candidate";

            Term++;

            Task.Run(RequestVotes);
            Thread.Sleep(ElectionTimeoutCurr);

            int votecountforme = 0;

            foreach(var vote in Votes)
            {
                if (vote.Key.Item1 == Term && vote.Value == Id)
                    votecountforme++;
            }

            if (votecountforme > Nodes.Count() / 2)
            {
                Task.Run(MakeLeader);
            }
            else
            {
                State = "Follower";
            }
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
                            await Task.Delay(Heartbeat, DelayStop.Token);
                            await SendHeartbeat();
                            Console.WriteLine($"Heartbeat from: {Id}\n");
                        }
                        catch (OperationCanceledException e)
                        {
                            Console.WriteLine("Heartbeat cancelled!",e.Message);
                        }
                    }
                    if (State == "Candidate")
                    {
                        try
                        {
                            await Task.Delay(ElectionTimeoutCurr, DelayStop.Token);
                            IsTimedOut();
                        }
                        catch (OperationCanceledException e)
                        {
                            Console.WriteLine($"Election cancelled!",e.Message);
                        }
                    }
                    if (State == "Follower")
                    {
                        try
                        {
                            await Task.Delay(ElectionTimeoutCurr, DelayStop.Token);
                            IsTimedOut();

                        }
                        catch (OperationCanceledException e)
                        { 
                            Console.WriteLine($"Election complete. Result: leader is {Id}\n", e.Message);
                        }
                    }
                }
            });
        }

        public void RefreshElectionTimeout()
        {
            ElectionTimeoutCurr = ElectionTimerMax;
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
            foreach(INode n in Nodes.Values)
            {
                n.AppendEntries(this.Id);
            }
        }

        public void Stop()
        {
            IsStarted = false;
            DelayStop.Cancel();
        }

        public void AppendEntries(Guid Leader)
        {
            if (Nodes.ContainsKey(Leader) && Nodes[Leader].Term >= this.Term)
            {
                State = "Follower";
                Term = Nodes[Leader].Term;
            }    

            DelayStop.Cancel();
            CurrentLeader = Leader;
            RefreshElectionTimeout();
        }

        public void AppendEntries(Guid NewLeader, int newTerm)
        {
            DelayStop.Cancel();
            RefreshElectionTimeout();
        }
    }
}
