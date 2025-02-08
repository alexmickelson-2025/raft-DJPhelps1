using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace raft_DJPhelps1
{
    public class Node : INode
    {
        private const int BaseTimerWaitCycle = 400;

        private const int HeartbeatBase = 50;
        private const int MinValueElectionTimeout = 150;
        private const int MaxValueElectionTimeout = 300;
        public int ImportantValue;
        public bool IsStarted { get; set; } // false = NotStarted or Cancel; true = Started
        private bool HasWonElection_Flag { get; set; }
        public bool AppendEntriesResponseFlag { get; set; } // placeholder
        public bool IsCurrentLogCounted { get; set; }
        public bool HasLogEntriesUncommitted { get; set; }
        public string State { get; set; }
        public Guid CurrentLeader { get; set; }
        public Guid Id { get; set; }
        public int Term { get; set; }
        public int VoteCountForMe { get; set; }
        public int ElectionTimerCurr { get; set; }
        public int ElectionTimerMax { get; set; }
        public int TimeoutMultiplier { get; set; }
        public int InternalDelay { get; set; }
        public int NextIndex { get; set; } // Need to know how big the stack is in the leader
        public int LogIndex { get; set; } // Need to know where the new commands are implicitly
        public int LogActionCounter { get; set; }
        public int Heartbeat { get; set; }
        public Dictionary<Guid, INode> Nodes { get; set; }
        public Dictionary<int, Guid> Votes { get; set; }
        public CancellationTokenSource DelayStop { get; set; }
        public Dictionary<int, CommandToken> CommandLog { get; set; }
        public Dictionary<int, ClientStandin> csi { get; set; }

        public Node()
        {
            Random initializer = new Random();

            // Timers
            TimeoutMultiplier = 50;
            Heartbeat = HeartbeatBase * TimeoutMultiplier;
            ElectionTimerMax = initializer.Next(MinValueElectionTimeout, MaxValueElectionTimeout) * TimeoutMultiplier;
            RefreshElectionTimeout();
            
            // Work entries
            Id = Guid.NewGuid();
            State = "Follower";
            Term = 1;
            AppendEntriesResponseFlag = false;
            IsStarted = false;
            HasLogEntriesUncommitted = false;
            ImportantValue = 0;
            InternalDelay = 0;
            NextIndex = 0; // The next index is a new one.
            LogIndex = 0; // The first index is the token index at the start.
            CommandLog = new();
            Nodes = new Dictionary<Guid, INode>();
            Votes = new Dictionary<int, Guid>();
            DelayStop = new CancellationTokenSource();
            csi = new Dictionary<int, ClientStandin>();
        }

        public async void Start()
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
                        while(Heartbeat > 0)
                        {
                            Console.WriteLine($"Value of heartbeat: {Heartbeat}");
                            Heartbeat -= BaseTimerWaitCycle;
                            Console.WriteLine($"Value of heartbeat (2): {Heartbeat}");
                            await Task.Delay(BaseTimerWaitCycle, DelayStop.Token);
                        }
                        await SendHeartbeat();
                        Console.WriteLine($"Heartbeat from: {Id}\n");
                    }
                    catch (OperationCanceledException e)
                    {
                        Console.WriteLine($"Cancel! Term: {Term}, State: {State}, CurrentLeader: {CurrentLeader}", e.Message);
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
                        while(ElectionTimerCurr > 0)
                        {
                            Console.WriteLine($"Election timeout in follower is {ElectionTimerCurr}");
                            await Task.Delay(BaseTimerWaitCycle, DelayStop.Token);
                            ElectionTimerCurr -= BaseTimerWaitCycle;
                            Console.WriteLine($"Election timeout in follower is {ElectionTimerCurr}");
                        }

                        State = "Candidate";
                    }
                    catch (OperationCanceledException e)
                    {
                        Console.WriteLine($"Follower {Id} received heartbeat from leader {CurrentLeader}.\n", e.Message);
                    }
                }
            }

            Console.WriteLine($"Node {Id} stopped!");
        }

        public void Stop()
        {
            IsStarted = false;
            DelayStop.Cancel();
        }

        public void RequestVotesFromClusterRPC()
        {
            Nodes.Select(n =>
            {
                n.Value.RequestVoteRPC(this.Id, this.Term);
                return true;
            }).ToArray();
        }

        public async Task RequestVoteRPC(Guid candidate_id, int election_term)
        {
            //In this method: if vote request received, reset election timeout timer
            if (election_term >= Term)
            {
                if (election_term > Term)
                {
                    State = "Follower";
                    Term = election_term;
                    Console.WriteLine("Higher term signal detected. Reverting to follower.");
                }
                else if (State == "Candidate")
                {
                    Console.WriteLine($"Received vote request from {candidate_id}.");
                    return;
                }


                if (Votes.ContainsKey(election_term) && Votes[election_term] == candidate_id)
                {
                    RefreshElectionTimeout();
                    Term = election_term;
                    Console.WriteLine($"Node {candidate_id} vote request accepted at {DateTime.Now}");
                    DelayStop.Cancel();
                    await Nodes[candidate_id].RespondVoteRPC(Id, election_term, true); // RespondVoteRPC instead of return
                }
                else if (!Votes.ContainsKey(election_term))
                {
                    RefreshElectionTimeout();
                    Votes.Add(election_term, candidate_id);
                    Term = election_term;
                    Console.WriteLine($"Node {candidate_id} vote request accepted at {DateTime.Now}");
                    DelayStop.Cancel();
                    await Nodes[candidate_id].RespondVoteRPC(Id, election_term, true);
                }
                else
                {
                    Console.WriteLine($"Node {candidate_id} vote request rejected at {DateTime.Now}");
                    DelayStop.Cancel();
                    await Nodes[candidate_id].RespondVoteRPC(candidate_id, election_term, false);
                }
            }
            else
            {
                Console.WriteLine($"Node {candidate_id} vote request rejected at {DateTime.Now}");
            }
        }

        public void CommitEntries()
        {
            CommandLog[LogIndex].ISCOMMITTED = true;
            CommandToken ct = CommandLog[LogIndex];

            switch (ct.COMMAND)
            {
                case "add":
                    ImportantValue += ct.VALUE;
                    break;
            }

            if (csi.ContainsKey(LogIndex))
                csi[LogIndex].GetResponse();
        }

        public void CheckIfLogEntriesRemainUncommitted()
        {
            foreach (var command in CommandLog)
            {
                if (command.Value.ISCOMMITTED == false)
                {
                    HasLogEntriesUncommitted = true;
                    return;
                }
                else
                {
                    HasLogEntriesUncommitted = false;
                }
            }
        }

        public async Task RespondVoteRPC(Guid voeter_id, int term, bool voteGranted)
        { // Increemnt VoteForMe property instead of castvote

            if (voteGranted)
                await Task.Run(IncrementVoteCount);

            // Check if enough votes -> cancel and handle with catch block
            // If vote expires, term recycles.
            if (VoteCountForMe > Nodes.Count() / 2)
            {
                DelayStop.Cancel();
                MakeLeader();
            }
        }

        public Task IncrementVoteCount()
        {
            VoteCountForMe++;

            return Task.CompletedTask;
        }

        public async void StartNewElection()
        {
            VoteCountForMe = 0;
            Term++;
            CurrentLeader = Id;

            // Send out requests for votes, and vote for self.
            RequestVotesFromClusterRPC();
            await RespondVoteRPC(Id, Term, true);

            try
            {
                while (ElectionTimerCurr > 0)
                {
                    Console.WriteLine($"ElectionTimeout is {ElectionTimerCurr}");
                    await Task.Delay(BaseTimerWaitCycle, DelayStop.Token);
                    ElectionTimerCurr -= BaseTimerWaitCycle;
                }
            }
            catch (Exception) {
                Console.WriteLine("Election Booted.");
            }
        }

        public void ResetHeartbeat()
        {
            Heartbeat = 50 * TimeoutMultiplier;
        }

        public void RefreshElectionTimeout()
        {
            ElectionTimerCurr = ElectionTimerMax;
        }

        public void IsTimedOut()
        {
            var rand = new Random();
            ElectionTimerMax = rand.Next(MinValueElectionTimeout, MaxValueElectionTimeout) * TimeoutMultiplier;
            RefreshElectionTimeout();
            StartNewElection();
        }

        public async Task SendHeartbeat()
        {
            if (InternalDelay > 5)
            {
                Console.WriteLine($"Internal delay is {InternalDelay}");
                await Task.Delay(InternalDelay);
            }

            CheckIfLogEntriesRemainUncommitted();

            CommandToken ct = GetHeartbeatToken();
            if (HasLogEntriesUncommitted)
                ct = CommandLog[LogIndex];

            if (LogActionCounter > Nodes.Count / 2)
            {
                CommitEntries();
                ct = CommandLog[LogIndex];
                SetupForNextLog();
            }

            foreach (INode n in Nodes.Values)
            {
                Console.WriteLine($"Leader sending heartbeat from {Id} to {n.Id}");
                await n.AppendEntriesRPC(Id, ct);
            }

            ResetHeartbeat();
        }

        public void SetupForNextLog()
        {
            LogActionCounter = 0;
            LogIndex++;
        }

        public async Task AppendEntriesRPC(Guid Leader, CommandToken log_addition)
        {
            Console.WriteLine($"Append request received from {Leader} at {DateTime.Now}");

            bool validLeaderFlag = false;

            if (Nodes.ContainsKey(Leader) && log_addition.TERM >= this.Term)
            {
                validLeaderFlag = true;
                State = "Follower";
                Term = log_addition.TERM;

                RefreshElectionTimeout();
                CurrentLeader = Leader;
            }

            // Empty log entry base case
            if (log_addition.INDEX == 0 && !CommandLog.ContainsKey(0))
            {
                LogIndex = 0;
                NextIndex = 1;
                CommandLog.Add(0, log_addition);
            }


            // Intermittent case: Log is received. 
            // Next step: validate log against index, and against exisitng entry.
            // If token has no command, it is a heartbeat token.
            if (log_addition.COMMAND == "")
            {
                Console.WriteLine($"Heartbeat received at {DateTime.Now}\n");
            }
            else
            {
                // If leader index needs to be decremented (I.E. Log value too high, or token not exists in log
                if (LeaderIndexNeedsToBeDecremented(log_addition))
                {
                    log_addition.ISVALID = false;
                }

                // If leader index is low enough, and the log entries match
                else if (log_addition.INDEX < LogIndex
                    && CommandLog[log_addition.INDEX].TERM == log_addition.TERM)
                {
                    PurgeLogAboveIndex(log_addition.INDEX);
                    LogIndex = log_addition.INDEX;
                }
                else
                {
                    Console.WriteLine($"Token is assumed valid.\nToken: {log_addition}");
                }
            }

            // Log can be committed case
            if (log_addition.ISCOMMITTED && log_addition.ISVALID)
            {
                CommitEntries();
                LogIndex++;
            }
            
            if (Nodes.ContainsKey(Leader))
                await Nodes[Leader].AppendResponseRPC(Id, validLeaderFlag, log_addition);
        }

        private bool LeaderIndexNeedsToBeDecremented(CommandToken log_addition)
        {
            bool tokenIsInLog = CommandLog.ContainsKey(log_addition.INDEX); // entry does or does not exist
            bool indexTooHigh = log_addition.INDEX > LogIndex;
            bool tokensDoNotMatch = false;
            if (tokenIsInLog && CommandLog.ContainsKey(LogIndex))
                tokensDoNotMatch = CommandLog[LogIndex] != log_addition;

            // IF:
            return indexTooHigh // index is too high OR
                || !tokenIsInLog // entry does not exist OR
                || (tokenIsInLog // entry exists AND
                && tokensDoNotMatch); // entries do not match

            // Return true.
        }

        public void PurgeLogAboveIndex(int index)
        {
            foreach (var log in CommandLog)
            {
                if (log.Key > index)
                    CommandLog.Remove(index);
            }
        }

        public async Task AppendResponseRPC(Guid RPCReceiver, bool ValidLeader, CommandToken ValidEntry)
        {
            await Task.Run(() => AppendEntriesResponseFlag = ValidLeader);

            if (!ValidEntry.ISVALID)
                LogIndex--;
            else if (!ValidEntry.ISCOMMITTED && ValidEntry.COMMAND != "")
                LogActionCounter++;
        }

        public void MakeLeader()
        {
            State = "Leader";
            Heartbeat = 0;
            VoteCountForMe = 0;
        }

        public CommandToken GetHeartbeatToken() => new CommandToken()
        {
            COMMAND = "",
            TERM = Term,
            VALUE = 0,
            INDEX = NextIndex,
            ISCOMMITTED = false,
            ISVALID = true
        };

        public async Task<bool> RequestAdd(int input_num)
        {
            await Task.Run(() => Console.WriteLine($"Request to add {input_num} received at {DateTime.Now}"));
            CommandToken newToken = new()
            {
                COMMAND = "add",
                TERM = Term,
                VALUE = input_num,
                INDEX = NextIndex,
                ISCOMMITTED = false,
                ISVALID = true
            };

            if (!IsCurrentLogCounted)
            {
                IsCurrentLogCounted = true;
                LogActionCounter++;
            }

            HasLogEntriesUncommitted = true;
            CommandLog.Add(NextIndex, newToken);
            NextIndex++;

            return true;
        }

        public async Task<bool> RequestAdd(int input_num, ClientStandin cs)
        {
            csi.Add(NextIndex, cs);
            await RequestAdd(input_num);
            return true;
        }
    }
}