using raft_DJPhelps1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace raft_DJPhelps1
{
    public class NetworkClusterNode : INode
    {

        public string Url { get; }
        public bool StartFlag = false;
        private HttpClient client = new();

        public NetworkClusterNode(Guid id, string url)
        {
            Id = id;
            Url = url;
        }

        public async Task ToggleOperation()
        {
            try
            {
                await client.PostAsJsonAsync(Url + "/request/start", !StartFlag);
                StartFlag = !StartFlag;
            }
            catch (HttpRequestException)
            {
                Console.WriteLine($"node {Url} is down");
            }
        }

        public async Task RequestAppendEntries(AppendDeets request)
        {
            try
            {
                await client.PostAsJsonAsync(Url + "/request/appendEntries", request);
            }
            catch (HttpRequestException)
            {
                Console.WriteLine($"node {Url} is down");
            }
        }

        public async Task RequestVote(VoteDeets request)
        {
            try
            {
                await client.PostAsJsonAsync(Url + "/request/vote", request);
            }
            catch (HttpRequestException)
            {
                Console.WriteLine($"node {Url} is down");
            }
        }

        public async Task RespondAppendEntries(AppendDeets response)
        {
            try
            {
                await client.PostAsJsonAsync(Url + "/response/appendEntries", response);
            }
            catch (HttpRequestException)
            {
                Console.WriteLine($"node {Url} is down");
            }
        }

        public async Task ResponseVote(VoteDeets response)
        {
            try
            {
                await client.PostAsJsonAsync(Url + "/response/vote", response);
            }
            catch (HttpRequestException)
            {
                Console.WriteLine($"node {Url} is down");
            }
        }

        public async Task<bool> RequestAdd(int request_value)
        {
            try
            {
                await client.PostAsJsonAsync(Url + "/request/add", request_value);
                return true;
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"node {Url} is down");
                return false;
            }
        }

        public async Task RequestTimeclockChange(int delay, int timescale)
        {
            try
            {
                ClockPacingToken ct = new ClockPacingToken()
                {
                    DelayValue = delay,
                    TimeScaleMultiplier = timescale
                };
                await client.PostAsJsonAsync(Url + "/request/clockupdate", ct);
            }
            catch (HttpRequestException)
            {
                Console.WriteLine($"Node {Url} is down");
            }
        }

        public async Task<NodeData?> RequestNodeHealth()
        {
            try
            {
                return await client.GetFromJsonAsync<NodeData>(Url + "/nodehealth");
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Node {Url} is down");
            }
            return null;
        }

        public Guid Id { get; set; }
        public int Term { get; set; }
        public Dictionary<Guid, INode> Nodes { get; set; }
        public int TimeoutMultiplier { get; set; }
        public int InternalDelay { get; set; }

        public Task AppendEntriesRPC(Guid leader, CommandToken ct)
        {
            throw new NotImplementedException();
        }

        public Task AppendResponseRPC(Guid RPCReceiver, bool response1, CommandToken response2)
        {
            throw new NotImplementedException();
        }

        public Task IncrementVoteCount()
        {
            throw new NotImplementedException();
        }

        public void MakeLeader()
        {
            throw new NotImplementedException();
        }

        public Task ReceiveVoteRPC(Guid id, int term, bool voteGranted)
        {
            throw new NotImplementedException();
        }

        public Task RequestVoteRPC(Guid id, int term)
        {
            throw new NotImplementedException();
        }

        public void RequestVotesFromClusterRPC()
        {
            throw new NotImplementedException();
        }

        public Task SendHeartbeat()
        {
            throw new NotImplementedException();
        }

        public void Start()
        {
            throw new NotImplementedException();
        }

        public void StartNewElection()
        {
            throw new NotImplementedException();
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }
    }
}
