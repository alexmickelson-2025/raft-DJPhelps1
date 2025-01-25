using Castle.Components.DictionaryAdapter.Xml;
using NSubstitute;
using NSubstitute.Core.Arguments;
using raft_DJPhelps1;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace raft_DJPhelps1_Test
{
    // Dallin's Tests for Raft Logs
    public class RaftLogTests
    {
        // 1. When a leader receives a client command the leader sends the log entry in the next appendentries RPC to all nodes
        // 2. When a leader receives a command from the client, it is appended to its log
        // 3. When a node is new, its log is empty
        // 4. When a leader wins an election, it initializes the nextIndex for each follower to the index just after the last one it its log
        // 5. Leaders maintain an "nextIndex" for each follower that is the index of the next log entry the leader will send to that follower
        // 6. Highest committed index from the leader is included in AppendEntries RPC's
        // 7. When a follower learns that a log entry is committed, it applies the entry to its local state machine
        // 8. When the leader has received a majority confirmation of a log, it commits it
        // 9. The leader commits logs by incrementing its committed log index
        // 10.Given a follower receives an appendentries with log(s) it will add those entries to its personal log


        // Testing #1
        [Fact]
        public async void WhenALeaderReceivesCommand_LogSentViaAppendEntries_Test_Single()
        {
            Node node = new Node();
            var node2 = Substitute.For<Node>();
            var new_moq_id = Guid.NewGuid();
            node2.Id = new_moq_id;
            node.Nodes.Add(node2.Id, node);

            node.MakeLeader();
            node.RequestAdd(10);
            await node.SendHeartbeat();
            CommandToken token = Substitute.For<CommandToken>();


            node2.Received().AppendEntriesRPC(Arg.Any<Guid>(), token);
        }


        // Testing #1
        [Fact]
        public async void WhenALeaderReceivesCommand_LogSentViaAppendEntries_Test_Plural()
        {
            Node node = new Node();
            var node2 = Substitute.For<Node>();
            var node3 = Substitute.For<Node>();
            var new_moq_id = Guid.NewGuid();
            node2.Id = new_moq_id;
            new_moq_id = Guid.NewGuid();
            node3.Id = new_moq_id;
            node.Nodes.Add(node2.Id, node2);
            node.Nodes.Add(node3.Id, node3);

            node.RequestAdd(10);
            await node.SendHeartbeat();
            CommandToken token = Substitute.For<CommandToken>();

            node2.Received().AppendEntriesRPC(Arg.Any<Guid>(), token);
            node3.Received().AppendEntriesRPC(Arg.Any<Guid>(), token);
        }


        // Testing #2
        [Fact]
        public void WhenClientSendsRequest_ThenLeaderAppendsToLog_Test()
        {
            Node node = new Node();

            node.RequestAdd(1);

            Assert.True(node.CommandLog.Count() > 0);
        }

        // Testing #3
        [Fact]
        public void WhenANodeIsNew_LogIsEmpty_Test()
        {
            Node node = new Node();

            Assert.True(node.CommandLog.Count == 0);
        }

        
        // Testing #4
        [Fact]
        public void WhenALeaderWinsAnElection_InitializesNextIndexForEveryFollower_Test()
        {
            Node node = new Node();
            var node2 = Substitute.For<Node>();
            var new_moq_id = Guid.NewGuid();
            node2.Id = new_moq_id;
            new_moq_id = Guid.NewGuid();
            node.Nodes.Add(node2.Id, node2);
            node2.NextIndex = 13412;

            node.MakeLeader();

            Assert.True(node2.NextIndex == 1);
        }


        // Testing #5
        [Fact]
        public async Task LeadersMaintainNextIndexAsync_Test()
        {
            var node = new Node();
            node.RequestAdd(2);
            node.RequestAdd(3);
            node.RequestAdd(4);
            await node.SendHeartbeat();
            await node.SendHeartbeat();
            await node.SendHeartbeat();
            await node.SendHeartbeat();

            Assert.True(node.NextIndex == 3);
        }


        // Testing #6
        [Fact]
        public async Task HighestCommittedIndexFromLeaderIncludedInAppendEntries_Test()
        {
            var node = new Node();
            node.RequestAdd(2);
            node.RequestAdd(3);
            node.RequestAdd(4); // Set up a decent number of entries
            var node_mock = Substitute.For<Node>();
            
            node_mock.Id = Guid.NewGuid();
            node.Nodes.Add(node_mock.Id, node_mock);
            node.CommandLog[0].is_committed = true; // Commit the first entry.
            



            // If the appendEntriesRPC call commits the next 
            Assert.True(node_mock.CommandLog[0].is_committed);
        }
    }
}
