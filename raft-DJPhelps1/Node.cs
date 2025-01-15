using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace raft_DJPhelps1
{
    public class Node : INode
    {
        public bool IsStarted { get; set; }
        public string State { get; set; }     
        public Guid CurrentLeader { get; set; }
        public Guid Id { get; internal set; }
        public int ElectionTimeoutCurr { get; set; }
        public int ElectionTimerMax {  get; set; }
        public int Heartbeat {  get; set; }
        public int VoteCount { get; set; }
        public int Term { get; set; }
        public Dictionary<Guid, Node> Nodes { get; set; } = new Dictionary<Guid, Node>();
        public Dictionary<(int,Guid), Guid> Votes { get; set; } = new Dictionary<(int,Guid), Guid>();
        public CancellationTokenSource DelayStop { get; set; }

        public Node()
        {
            Random initializer = new Random();
            ElectionTimeoutCurr = initializer.Next(150, 300);
            RefreshElectionTimeout();
            State = "Follower";
            Heartbeat = 50;
            DelayStop = new CancellationTokenSource();
            Term = 1;
        }


        public async Task MakeLeader()
        {
            State = "Leader";
            foreach (var node in Nodes.Values)
            {
                await node.AppendEntries(Id, Term);
            }
        }

        public async Task RequestVotes()
        {
            Votes.Add((Term, Id), Id);

            foreach (Node n in Nodes.Values)
            {
                if(await n.RequestVoteRPC(Id, Term))
                {
                    Votes.Add((Term,n.Id), Id);
                }
            }
        }

        public async Task<bool> RequestVoteRPC(Guid id, int term)
        {
            await Task.Delay(5);
            
            if (Votes.ContainsKey((term,Id)) && Votes[(term,Id)] != this.Id && Votes[(term, Id)] == id)
            {
                DelayStop.Cancel();
                return true;
            }
            else if(!Votes.ContainsKey((term, Id))) {
                Votes.Add((term, Id), id);
                DelayStop.Cancel();
                return true;
            }
            else
            {
                DelayStop.Cancel();
                return false;
            }
        }

        public async Task StartNewElection()
        {
            State = "Candidate";
            Term++;
            await RequestVotes();
            await Task.Delay(ElectionTimeoutCurr);
            if (Votes.Where(
                x => x.Key == (Term,Id) && x.Value == this.Id
                ).Count() > Nodes.Count / 2)
            {
                await MakeLeader();
            }
        }

        public async Task Start()
        {
            IsStarted = true;
            int i = 100;
            while (IsStarted && i > 0)
            {
                if(State == "Leader")
                {
                    try
                    {
                        await Task.Delay(Heartbeat, DelayStop.Token);
                    }
                    catch (Exception e)
                    {

                    }
                    await SendHeartbeat();    
                }
                if(State == "Candidate")
                {
                    try
                    {
                        await Task.Delay(ElectionTimeoutCurr, DelayStop.Token);
                        await ElectionIsTimedOut();
                    }
                    catch (Exception e)
                    {

                    }
                }
                if(State == "Follower")
                {
                    try
                    {
                        await Task.Delay(ElectionTimeoutCurr, DelayStop.Token);
                        await LeaderIsTimedOut();
                    }
                    catch (Exception e)
                    {

                    }
                }

                i--;
            }
        }

        public void RefreshElectionTimeout()
        {
            ElectionTimeoutCurr = ElectionTimerMax;
        }

        public async Task ElectionIsTimedOut()
        {
            var rand = new Random();
            ElectionTimerMax = rand.Next(150, 300);
            RefreshElectionTimeout();
            await StartNewElection();
        }

        public async Task LeaderIsTimedOut()
        {
            var rand = new Random();
            ElectionTimerMax = rand.Next(150, 300);
            RefreshElectionTimeout();
            await StartNewElection();
        }

        public async Task SendHeartbeat()
        {
            foreach(Node n in Nodes.Values)
            {
                await n.AppendEntries(this.Id);
            }
        }

        public async Task Stop()
        {
            IsStarted = false;
            DelayStop.Cancel();
        }

        public async Task AppendEntries(Guid Leader)
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

        public async Task AppendEntries(Guid NewLeader, int newTerm)
        {
            DelayStop.Cancel();
            RefreshElectionTimeout();
        }
    }
}
