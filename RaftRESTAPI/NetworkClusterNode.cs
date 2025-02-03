using raft_DJPhelps1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace RaftRESTAPI
{
    public class NetworkClusterNode : INode
    {

        public string Url { get; }
        private HttpClient client = new();

        public NetworkClusterNode(Guid id, string url)
        {
            Id = id;
            Url = url;
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

        public async Task RequestAdd(int request_value)
        {
            await client.PostAsJsonAsync(Url + "/request/add", request_value);
        }

        public Guid Id { get; set; }
        public int Term { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Dictionary<Guid, INode> Nodes { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public int TimeoutMultiplier { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public int InternalDelay { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

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
