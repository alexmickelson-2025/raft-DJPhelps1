using raft_DJPhelps1;
using System;
using System.Collections.Generic;
using System.Data;
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
        public Guid Id { get; set; }
        public int Term { get; set; }
        public Dictionary<Guid, INode> Nodes { get; set; }
        public int TimeoutMultiplier { get; set; }
        public int InternalDelay { get; set; }
        public NodeData nodestatus { get; set; }

        public NetworkClusterNode(Guid id, string url)
        {
            Id = id;
            Url = url;
            Nodes = new();
            nodestatus = new NodeData(
             Node_Id : Guid.Empty,
             NextIndex : 0,
             LogIndex : 0,
             CurrentTermLeader : Guid.Empty,
             ElectionTimeout : -1,
             ElectionTimer : -1,
             Heartbeat : -1,
             Log : new(),
             State : "down",
             Status : false,
             Term : -1,
             TimeoutMultiplier : 0,
             StateMachineVal: 0 );
        }

        public async Task ToggleOperation(bool flag)
        {
            try
            {
                await client.PostAsJsonAsync(Url + "/request/start", flag);
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
            catch (HttpRequestException)
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
                nodestatus = await client.GetFromJsonAsync<NodeData>(Url + "/nodedata") ?? throw new ArgumentNullException("could not retrieve node data");
                return nodestatus;
            }
            catch (HttpRequestException)
            {
                Console.WriteLine($"Node {Url} is down");
            }
            return null;
        }


        public async Task AppendEntriesRPC(Guid leader, CommandToken ct)
        {
            try
            {
                AppendDeets headeritems = new AppendDeets() { CT = ct, ID = leader };
                await client.PostAsJsonAsync(Url + "/request/appendEntries", headeritems);
            }
            catch (HttpRequestException)
            {
                Console.WriteLine($"node {Url} is down");
            }
        }

        public async Task AppendResponseRPC(Guid leader, bool response1, CommandToken ct)
        {
            try
            {
                AppendDeets headeritems = new AppendDeets() { CT = ct, ID = leader, VLF = response1 };
                await client.PostAsJsonAsync(Url + "/response/appendEntries", headeritems);
            }
            catch (HttpRequestException)
            {
                Console.WriteLine($"node {Url} is down");
            }
        }

        public async Task RespondVoteRPC(Guid id, int term, bool voteGranted)
        {
            try
            {
                VoteDeets request = new VoteDeets() { ID = id, TERM = term, VOTE = voteGranted };
                await client.PostAsJsonAsync(Url + "/response/vote", request);
            }
            catch (HttpRequestException)
            {
                Console.WriteLine($"node {Url} is down");
            }
        }

        public async Task RequestVoteRPC(Guid id, int term)
        {
            try
            {
                VoteDeets request = new VoteDeets() { ID = id, TERM = term };
                await client.PostAsJsonAsync(Url + "/request/vote", request);
            }
            catch (HttpRequestException)
            {
                Console.WriteLine($"node {Url} is down");
            }
        }
    }
}
