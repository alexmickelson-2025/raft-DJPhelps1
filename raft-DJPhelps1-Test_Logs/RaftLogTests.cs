using Castle.Components.DictionaryAdapter.Xml;
using NSubstitute;
using NSubstitute.Core.Arguments;
using raft_DJPhelps1;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace raft_DJPhelps1_Test
{
    // Dallin's Tests for Raft Logs
    public class RaftLogTests
    {
        /// <summary>
        /// 
        /// 1. When a leader receives a client command the leader sends the log entry in the next appendentries RPC to all nodes
        /// 2. When a leader receives a command from the client, it is appended to its log
        /// 3. When a node is new, its log is empty
        /// 4. When a leader wins an election, it initializes the nextIndex for each follower to the index just after the last one it its log
        /// 5. Leaders maintain an "nextIndex" for each follower that is the index of the next log entry the leader will send to that follower
        /// 6. Highest committed index from the leader is included in AppendEntries RPC's
        /// 7. When a follower learns that a log entry is committed, it applies the entry to its local state machine
        /// 8. When the leader has received a majority confirmation of a log, it commits it
        /// 9. The leader commits logs by incrementing its committed log index
        /// 10.Given a follower receives an appendentries with log(s) it will add those entries to its personal log
        /// 
        /// 11.A followers response to an appendentries includes the followers term number and log entry index
        /// 12.When a leader receives a majority responses from the clients after a log replication heartbeat, the leader sends a confirmation response to the client
        /// 13.Given a leader node, when a log is committed, it applies it to its internal state machine
        /// 14.When a follower receives a valid heartbeat, it increases its commitIndex to match the commit index of the heartbeat
        ///   A. Reject the heartbeat if the previous log index / term number does not match your log
        /// 
        /// 15.When sending an AppendEntries RPC, the leader includes the index and term of the entry in its log that immediately precedes the new entries
        ///   A. If the follower does not find an entry in its log with the same index and term, then it refuses the new entries
        ///    i. term must be same or newer
        ///    ii. if index is greater, it will be decreased by leader
        ///    iii. if index is less, we delete what we have
        ///   B. If a follower rejects the AppendEntries RPC, the leader decrements nextIndex and retries the AppendEntries RPC
        /// 
        /// 16.When a leader sends a heartbeat with a log, but does not receive responses from a majority of nodes, the entry is uncommitted
        /// 17.If a leader does not response from a follower, the leader continues to send the log entries in subsequent heartbeats
        /// 18.If a leader cannot commit an entry, it does not send a response to the client
        /// 19.If a node receives an appendentries with a logs that are too far in the future from your local state, you should reject the appendentries
        /// 20.If a node receives an appendentries with a term and index that do not match, you will reject the appendentry until you find a matching log
        ///
        /// </summary>

        // Testing Logs #1
        [Fact]
        public async void WhenALeaderReceivesCommand_LogSentViaAppendEntries_Test_Single()
        {
            Node node = new Node();
            var node2 = Substitute.For<INode>();
            Guid new_moq_id = Guid.NewGuid();
            node2.Id = new_moq_id;
            node.Nodes.Add(node2.Id, node2);

            node.RequestAdd(10);
            await node.SendHeartbeat();

            node2.Received().AppendEntriesRPC(Arg.Any<Guid>(), Arg.Any<CommandToken>());
        }


        // Testing Logs #1
        [Fact]
        public async void WhenALeaderReceivesCommand_LogSentViaAppendEntries_Test_Plural()
        {
            Node node = new Node();
            var node2 = Substitute.For<INode>();
            var node3 = Substitute.For<INode>();
            var new_moq_id = Guid.NewGuid();
            node2.Id = new_moq_id;
            new_moq_id = Guid.NewGuid();
            node3.Id = new_moq_id;
            node.Nodes.Add(node2.Id, node2);
            node.Nodes.Add(node3.Id, node3);

            node.RequestAdd(10);
            await node.SendHeartbeat();

            node2.Received().AppendEntriesRPC(Arg.Any<Guid>(), Arg.Any<CommandToken>());
            node3.Received().AppendEntriesRPC(Arg.Any<Guid>(), Arg.Any<CommandToken>());
        }


        // Testing Logs #2
        [Fact]
        public void WhenClientSendsRequest_ThenLeaderAppendsToLog_Test()
        {
            Node node = new Node();

            node.RequestAdd(1);

            Assert.True(node.CommandLog.Count() > 0);
        }

        // Testing Logs #3
        [Fact]
        public void WhenANodeIsNew_LogIsEmpty_Test()
        {
            Node node = new Node();

            Assert.True(node.CommandLog.Count == 0);
        }


        // Testing Logs #4
        [Fact]
        public void WhenALeaderWinsAnElection_InitializesNextIndexForEveryFollower_Test()
        {
            Node node = new Node();
            var node2 = Substitute.For<INode>();
            var new_moq_id = Guid.NewGuid();
            node2.Id = new_moq_id;
            new_moq_id = Guid.NewGuid();
            node.Nodes.Add(node2.Id, node2);

            node.MakeLeader();

            node2.Received().AppendEntriesRPC(Arg.Any<Guid>(), Arg.Is<CommandToken>(x => x.index == 0));
        }


        // Testing Logs #5
        [Fact]
        public async Task LeadersMaintainNextIndexAsync_Test()
        {
            var node = new Node();
            await node.RequestAdd(2);
            await node.RequestAdd(3);
            await node.RequestAdd(4);
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
            var node_mock = Substitute.For<INode>();
            CommandToken tokens = new CommandToken()
            {
                index = 0,
                is_committed = true,
                command = "",
                value = 0,
                term = 1
            };
            node_mock.Id = Guid.NewGuid();

            node.Nodes.Add(node_mock.Id, node_mock);
            //node_mock.When(x => x.AppendEntriesRPC(node.Id, tokens)).Do(x =>
            //    node.AppendResponseRPC(node_mock.Id, true, tokens));

            node.MakeLeader();
            node.RequestAdd(3);
            await node.SendHeartbeat();

            node_mock.Received().AppendEntriesRPC(Arg.Any<Guid>(), Arg.Any<CommandToken>());
        }


        // Testing #7
        [Fact]
        public async void FollowerCommitsEntryAsSoonAsItLearnsItsEntryIsCommitted_Test()
        {
            var test_node = new Node();
            var token = new CommandToken()
            {
                index = 0,
                is_committed = false,
                is_valid = true,
                value = 0,
                command = "add",
                term = 1
            };
            test_node.CommandLog.Add(0, token);

            token.is_committed = true;
            await test_node.AppendEntriesRPC(Guid.NewGuid(), token);

            Assert.Equal(test_node.CommandLog[0], token);
        }


        // Testing #8 
        [Fact]
        public async void WhenLeaderReceivesMajorityConfirmationOnLog_CommitsLog_Test_Single()
        {
            var test_node = new Node();
            var token = new CommandToken()
            {
                value = 1,
                is_committed = false,
                command = "add",
                term = 1,
                index = 0,
                is_valid = true
            };
            await test_node.RequestAdd(1);

            test_node.HasLogEntriesUncommitted = true;
            await test_node.SendHeartbeat();

            Assert.True(test_node.CommandLog[0].is_committed);
        }


        [Fact]
        public async void WhenLeaderReceivesMajorityConfirmationOnLog_CommitsLog_Test_Multiple()
        {
            var test_node = new Node();
            var node2 = Substitute.For<INode>();
            var node3 = Substitute.For<INode>();
            node2.Id = Guid.NewGuid();
            node3.Id = Guid.NewGuid();
            test_node.Nodes.Add(node2.Id, node2);
            test_node.Nodes.Add(node3.Id, node3);
            var token = new CommandToken()
            {
                value = 1,
                is_committed = false,
                command = "add",
                term = 1,
                index = 0,
                is_valid = true
            };
            test_node.CommandLog.Add(token.index, token);

            test_node.LogActionCounter = 2;
            await test_node.SendHeartbeat();

            Assert.True(test_node.CommandLog[0].is_committed);
        }

        // Testing #9 
        [Fact]
        public async void LeaderCommitsLogsByIncrementingItsLogIndex_Test()
        {
            var test_node = new Node();
            await test_node.RequestAdd(3);

            await test_node.SendHeartbeat();

            Assert.True(test_node.LogIndex == 1);
        }


        // Testing #10
        [Fact]
        public async void GivenAFollowerReceivesLogs_FollowerAddsToPersonalLog_Test()
        {
            var test_node = new Node();
            var token = new CommandToken()
            {
                value = 1,
                command = "sub",
                term = 1,
                index = 0,
                is_committed = false,
                is_valid = true
            };

            await test_node.AppendEntriesRPC(Guid.NewGuid(), token);

            Assert.True(test_node.CommandLog.ContainsKey(token.index));
            Assert.True(test_node.CommandLog[0] == token);
        }


        // Testing #11
        [Fact]
        public async void FollowerRespondsWithItsOwnTermAndLogIndex_Test()
        {
            var test_node = new Node();
            var mock_node = Substitute.For<INode>();
            mock_node.Id = Guid.NewGuid();
            test_node.Nodes.Add(mock_node.Id, mock_node);
            var token = new CommandToken()
            {
                command = "test",
                term = 1,
                index = 0,
                is_committed = false,
                is_valid = true,
                value = 2
            };

            await test_node.AppendEntriesRPC(mock_node.Id, token);

            await mock_node.Received().AppendResponseRPC(Arg.Any<Guid>(), Arg.Any<bool>(),
                Arg.Is<CommandToken>(x => x.term == test_node.Term && x.index == test_node.LogIndex));
        }


        // Testing #12
        [Fact]
        public async void LeaderSendsConfirmationHeartbeat()
        {
            var test_node = new Node();
            var mock_node = Substitute.For<INode>();
            mock_node.Id = Guid.NewGuid();
            CommandToken token = new CommandToken()
            {
                value = 1,
                command = "add",
                term = 1,
                index = 0,
                is_committed = false,
                is_valid = true,
            };
            await test_node.RequestAdd(1);
            mock_node.When(x => x.AppendEntriesRPC(Arg.Any<Guid>(), Arg.Any<CommandToken>())).Do(async x =>
                await test_node.AppendResponseRPC(mock_node.Id, true, token)
            );
            test_node.Nodes.Add(mock_node.Id, mock_node);

            await test_node.SendHeartbeat();

            await mock_node.Received().AppendEntriesRPC(Arg.Any<Guid>(), Arg.Is<CommandToken>(x => x.is_committed == true));
        }


        // Testing #13
        [Fact]
        public async void WhenLogCommitted_CommandAppliedToStateMachine_Test()
        {
            var test_node = new Node();
            await test_node.RequestAdd(3);
            var expected = 3;

            await test_node.SendHeartbeat();

            Assert.Equal(expected, test_node.ImportantValue);
        }


        // Testing #14
        [Fact]
        public void WhenFollowerReceivesHeartbeat_UpdatesCommitLog_Test_Accept()
        {
            var test_node = new Node();
            CommandToken token = new CommandToken() // heartbeat token needs to have an index higher than starting.
            {
                index = 3
            };
            var expected_index = 3;
            test_node.LogIndex = 3;

            test_node.AppendEntriesRPC(Guid.NewGuid(), token);

            Assert.Equal(expected_index, test_node.LogIndex);
        }

        // Testing #14 (A)
        [Fact]
        public void WhenFollowerRecivesHeartbeat_UpdatesCommitLog_Test_Reject()
        {
            var test_node = new Node();
            CommandToken token = new CommandToken()
            {
                index = 1
            };
            var expected_index = 3;
            test_node.LogIndex = 3;

            test_node.AppendEntriesRPC(Guid.NewGuid(), token);

            Assert.Equal(expected_index, test_node.LogIndex);
        }


        // Testing #15
        //15.When sending an AppendEntries RPC, the leader includes the index and term of the entry in its log that immediately precedes the new entries
        [Fact]
        public async void LeaderIncludesPreviousLogAndIndex_Test()
        {
            var test_node = new Node();
            test_node.RequestAdd(3);
            test_node.RequestAdd(4);
            var mock_node = Substitute.For<INode>();
            var mock_node2 = Substitute.For<INode>();
            mock_node2.Id = Guid.NewGuid();
            mock_node.Id = Guid.NewGuid();
            test_node.Nodes.Add(mock_node.Id, mock_node);
            test_node.Nodes.Add(mock_node2.Id, mock_node2);

            await test_node.SendHeartbeat();

            mock_node.Received().AppendEntriesRPC(Arg.Any<Guid>(), Arg.Is<CommandToken>(x => x.index == test_node.LogIndex));
            mock_node.Received().AppendEntriesRPC(Arg.Any<Guid>(), Arg.Is<CommandToken>(x => x.index < test_node.NextIndex));
        }

        //   A. If the follower does not find an entry in its log with the same index and term, then it refuses the new entries
        //    i. term must be same or newer
        //    ii. if index is greater, it will be decreased by leader
        //    iii. if index is less, we delete what we have
        [Fact] // Test for case i.
        public void FollowerDoesNotFindEntryInLogwithAppropriateIndexAndTerm_Test_TermLesser()
        {
            var test_node = new Node();
            CommandToken token = new CommandToken();
            test_node.Term = 4;

            test_node.AppendEntriesRPC(Guid.NewGuid(), token);

            Assert.True(test_node.Term != 1);
        }

        [Fact] // Test for case ii.
        public void FollowerDoesNotFindEntryInLogWithAppropriateIndexAndTerm_Test_IndexGreater()
        {
            var test_node = new Node();
            CommandToken token = new CommandToken()
            {
                index = 5,
                command = "add",
                value = 1
            };
            var mock_node = Substitute.For<INode>();
            mock_node.Id = Guid.NewGuid();
            test_node.Nodes.Add(mock_node.Id, mock_node);

            test_node.AppendEntriesRPC(mock_node.Id, token);

            mock_node.Received().AppendResponseRPC(test_node.Id, true, Arg.Is<CommandToken>(x =>
                x.is_valid == false
            ));
        }

        [Fact] // Test for case iii.
        public async void FollowerDoesNotFindEntryInLogWithAppropriateIndexAndTerm_Test_IndexLesser()
        {
            var test_node = new Node();
            CommandToken token = new CommandToken()
            {
                index = 5,
                command = "add",
                value = 1
            };
            test_node.LogIndex = 5;

            await test_node.AppendResponseRPC(Guid.NewGuid(), true, new CommandToken()
            {
                index = 5,
                command = "add",
                value = 1,
                is_valid = false
            });

            Assert.True(test_node.LogIndex == 4);
        }

        //   B. If a follower rejects the AppendEntries RPC, the leader decrements nextIndex and retries the AppendEntries RPC
        [Fact]
        public async void LeaderRetriesWithPreviousEntry_Test()
        {
            var test_node = new Node();
            var mock_index = Substitute.For<INode>();
            mock_index.Id = Guid.NewGuid();
            test_node.Nodes.Add(mock_index.Id, mock_index);
            await test_node.RequestAdd(1);
            await test_node.RequestAdd(3);

            test_node.LogIndex++; // First entry committed.
            test_node.CommandLog[0].is_committed = true;
            test_node.ImportantValue += test_node.CommandLog[0].value;
            CommandToken rejecttoken = test_node.CommandLog[1];
            rejecttoken.is_valid = false;

            await test_node.AppendResponseRPC(mock_index.Id, true, rejecttoken);
            int actual_index_post_rejection = test_node.LogIndex;
            await test_node.SendHeartbeat();

            Assert.Equal(0, actual_index_post_rejection);
            await mock_index.Received().AppendEntriesRPC(Arg.Any<Guid>(), Arg.Is<CommandToken>(x =>
            x == test_node.CommandLog[0]));
        }

        // Testing #16
        [Fact]
        public async void WhenALeaderSendsAHeartbeatButDoesNotReceiveResponsesFromAMajorityOfNodes_LogUncommitted_Test()
        {
            var test_node = new Node();
            var mock1 = Substitute.For<INode>();
            var mock2 = Substitute.For<INode>();
            mock1.Id = Guid.NewGuid();
            mock2.Id = Guid.NewGuid();
            test_node.RequestAdd(3);
            mock1.When(x => x.AppendEntriesRPC(Arg.Any<Guid>(), Arg.Any<CommandToken>()))
                .Do(x => { });
            mock1.When(x => x.AppendEntriesRPC(Arg.Any<Guid>(), Arg.Any<CommandToken>()))
                .Do(x => { });
            test_node.Nodes.Add(mock1.Id, mock1);
            test_node.Nodes.Add(mock2.Id, mock2);

            await test_node.SendHeartbeat();
            await test_node.SendHeartbeat();
            await test_node.SendHeartbeat();
            await test_node.SendHeartbeat();
            await test_node.SendHeartbeat();

            Assert.False(test_node.CommandLog[0].is_committed);
        }


        // Testing #17
        [Fact]
        public async void LeaderContinuesSendingResponsesEvenIfUnresponsive_Test()
        {
            var test_node = new Node();
            var mock1 = Substitute.For<INode>();
            var mock2 = Substitute.For<INode>();
            mock1.Id = Guid.NewGuid();
            mock2.Id = Guid.NewGuid();
            test_node.RequestAdd(3);
            mock1.When(x => x.AppendEntriesRPC(Arg.Any<Guid>(), Arg.Any<CommandToken>()))
                .Do(x => { });
            mock1.When(x => x.AppendEntriesRPC(Arg.Any<Guid>(), Arg.Any<CommandToken>()))
                .Do(x => { });
            test_node.Nodes.Add(mock1.Id, mock1);
            test_node.Nodes.Add(mock2.Id, mock2);

            await test_node.SendHeartbeat();
            await test_node.SendHeartbeat();
            await test_node.SendHeartbeat();
            await test_node.SendHeartbeat();
            await test_node.SendHeartbeat();

            mock1.Received(5).AppendEntriesRPC(Arg.Any<Guid>(), test_node.CommandLog[0]);
        }


        // Testing #18
        [Fact]
        public async void NodeWillNotTellClientIfLogUncommitted_Test()
        {
            var test_node = new Node();
            ClientStandin test_client = new(test_node);
            var mock1 = Substitute.For<INode>();
            var mock2 = Substitute.For<INode>();
            mock1.Id = Guid.NewGuid();
            mock2.Id = Guid.NewGuid();
            test_node.RequestAdd(3, test_client);
            mock1.When(x => x.AppendEntriesRPC(Arg.Any<Guid>(), Arg.Any<CommandToken>()))
                .Do(x => { });
            mock1.When(x => x.AppendEntriesRPC(Arg.Any<Guid>(), Arg.Any<CommandToken>()))
                .Do(x => { });
            test_node.Nodes.Add(mock1.Id, mock1);
            test_node.Nodes.Add(mock2.Id, mock2);

            await test_node.SendHeartbeat();
            await test_node.SendHeartbeat();
            await test_node.SendHeartbeat();
            await test_node.SendHeartbeat();
            await test_node.SendHeartbeat();

            Assert.False(test_client.GotResponseFlag);
        }


        // Testing #19 
        [Fact]
        public void NodeRejectsAppendEntriesIfTooFarAhead_Test()
        {
            var test_node = new Node();
            var mock1 = Substitute.For<INode>();
            mock1.Id = Guid.NewGuid();
            test_node.Nodes.Add(mock1.Id, mock1);
            CommandToken token = new CommandToken()
            {
                command = "add",
                value = 2,
                index = 4,
                is_committed = true,
                is_valid = true,
                term = 1
            };
            test_node.AppendEntriesRPC(mock1.Id, token);

            mock1.Received().AppendResponseRPC(Arg.Any<Guid>(), true, Arg.Is<CommandToken>(x=>
                x.is_valid == false ));
            Assert.True(test_node.CommandLog.Count == 0);
        }


        // Testing #20
        [Fact]
        public async void NodeRejectsAppendEntriesIfRejectsIndexesUntilAppropriateOneArrives_Test()
        {
            var test_node = new Node();
            var mock1 = Substitute.For<INode>();
            mock1.Id = Guid.NewGuid();
            test_node.Nodes.Add(mock1.Id, mock1);
            List<CommandToken> tokens = new List<CommandToken>()
            {
                new CommandToken()
                {
                    command = "add",
                    value = 2,
                    index = 3,
                    is_committed = true,
                    is_valid = true,
                    term = 1
                },
                new CommandToken()
                {
                    command = "add",
                    value = 2,
                    index = 2,
                    is_committed = true,
                    is_valid = true,
                    term = 1
                },
                new CommandToken()
                {
                    command = "add",
                    value = 2,
                    index = 1,
                    is_committed = true,
                    is_valid = true,
                    term = 1
                },
                new CommandToken()
                {
                    command = "add",
                    value = 2,
                    index = 0,
                    is_committed = true,
                    is_valid = true,
                    term = 1
                }
            };
            
            foreach(var t in tokens)
            {
                await test_node.AppendEntriesRPC(mock1.Id, t);
            }

            await mock1.Received().AppendResponseRPC(Arg.Any<Guid>(), true, Arg.Is<CommandToken>(x =>
                x.is_valid == true && x.index == 0));
            Assert.True(test_node.CommandLog.Count == 1);
        }
    }
}
