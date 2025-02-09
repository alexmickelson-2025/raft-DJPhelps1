using NSubstitute;
using NSubstitute.Core.Arguments;
using raft_DJPhelps1;
using raft_DJPhelps1_Test;
using System.Xml.Linq;

namespace raft_TDD_Tests
{
    public class RaftElectionTests
    {
        /*
            1. When a leader is active it sends a heart beat within 50ms.
            2. When a node receives an AppendEntries from another node, then first node remembers that other node is the current leader.
            3. When a new node is initialized, it should be in follower state.
            4. When a follower doesn't get a message for 300ms then it starts an election.
            5. When the election time is reset, it is a random value between 150 and 300ms.
                between
                random: call n times and make sure that there are some that are different (other properties of the distribution if you like)
            6. When a new election begins, the term is incremented by 1.
                Create a new node, store id in variable.
                wait 300 ms
                reread term (?)
                assert after is greater (by at least 1)
            7. When a follower does get an AppendEntries message, it resets the election timer. (i.e. it doesn't start an election even after more than 300ms)
            8. Given an election begins, when the candidate gets a majority of votes, it becomes a leader. (think of the easy case; can use two tests for single and multi-node clusters)
            9. Given a candidate receives a majority of votes while waiting for unresponsive node, it still becomes a leader.
            10. A follower that has not voted and is in an earlier term responds to a RequestForVoteRPC with yes. (the reply will be a separate RPC)
            11. Given a candidate server that just became a candidate, it votes for itself.
            12. Given a candidate, when it receives an AppendEntries message from a node with a later term, then candidate loses and becomes a follower.
            13. Given a candidate, when it receives an AppendEntries message from a node with an equal term, then candidate loses and becomes a follower.
            14. If a node receives a second request for vote for the same term, it should respond no. (again, separate RPC for response)
            15. If a node receives a second request for vote for a future term, it should vote for that node.
            16. Given a candidate, when an election timer expires inside of an election, a new election is started.
            17. When a follower node receives an AppendEntries request, it sends a response.
            18. Given a candidate receives an AppendEntries from a previous term, then rejects.
            19. When a candidate wins an election, it immediately sends a heart beat.
         */

        [Fact] // Testing #1
        public void NodeLeaderSendsHeartbeatWithinInterval_Test()
        {
            Node node = NodeFactory.StartNewNode("Leader");
            var node3 = Substitute.For<Node>();
            node.Nodes.Add(node3.Id, node3);

            node.Start();
            Thread.Sleep(100);

            node.Stop();
            //node3.Received().AppendEntriesRPC(node.Id);
        }

        [Fact] // Testing #2
        public async void NodeReceivesAppendEntriesPersistsLeadershipStatus_Test()
        {
            Node node = new Node();
            Node node2 = new Node();
            node2.State = "Leader";
            node.Nodes.Add(node2.Id, node2);
            node2.Nodes.Add(node.Id, node);

            await node2.SendHeartbeat();

            Assert.True(node.CurrentLeader == node2.Id);
        }

        [Fact] // Testing #3
        public void NodeInitializesInFollowerState_Test()
        {
            Node node = new Node();

            Assert.True(node.State == "Follower");
        }

        [Fact] // Testing #4
        public async void NodeStartsElectionWithinElectionTimeout_Test()
        {
            Node node = new Node();
            var mock_node = Substitute.For<INode>();
            mock_node.Id = Guid.NewGuid();
            node.Nodes.Add(mock_node.Id, mock_node);
            node.BaseTimerWaitCycle = 100;
            node.ElectionTimerMax = 1100;
            node.ElectionTimerCurr = 840;

            node.Start();
            await Task.Delay(1000);
            node.Stop();

            await mock_node.Received().RequestVoteRPC(Arg.Any<Guid>(), Arg.Any<int>());
        }

        [Fact] // Testing #5
        public void ElectionTimeResetsBetweenInterval_Test()
        {
            Node node = new Node();

            node.ElectionTimerCurr = 200;
            node.RefreshElectionTimeout();

            Assert.True(node.ElectionTimerCurr == node.ElectionTimerMax);
            Assert.True(node.ElectionTimerCurr <= 300 * 100);
        }

        [Fact] // Testing #6
        public void ElectionStartIncrementsTerm_Test()
        {
            Node node = new Node();
            var expectedTerm = node.Term;

            node.StartNewElection();

            var actualTerm = node.Term;

            Assert.True(expectedTerm < actualTerm);
        }

        [Fact] // Testing #7
        public void FollowerResetsElectionTimerAfterAppendEntriesRPC_Test()
        {
            Node node = NodeFactory.StartNewNode("Leader");
            Node node3 = NodeFactory.StartNewNode("Follower");
            node.Nodes.Add(node3.Id, node3);
            node3.Nodes.Add(node.Id, node);

            node.Start();
            node3.Start();
            Thread.Sleep(400);
            node3.Stop();
            node.Stop();

            Assert.True(node3.State == "Follower");
        }

        [Fact] // Testing #8.1
        public void CandidateBecomesALeaderWhenItReceivesMajorityVotes_Cluster1_Test()
        {
            Node node1 = new Node();
            node1.State = "Candidate";

            node1.Start();
            Thread.Sleep(1000);
            node1.Stop();

            Assert.True(node1.State == "Leader");
        }

        [Fact] // Testing #8.2
        public async void CandidatesSendVoteRequestWhenElectionStarts()
        {
            Node node1 = NodeFactory.StartNewNode("Follower");
            node1.Term = 3;
            var node2 = Substitute.For<INode>();
            var node3 = Substitute.For<INode>();
            var i = Guid.NewGuid();
            var j = Guid.NewGuid();
            Console.WriteLine(i.ToString() + " " + j.ToString());
            node2.Id.Returns(i);
            node3.Id.Returns(j);
            node1.Nodes.Add(node2.Id, node2);
            node1.Nodes.Add(node3.Id, node3);

            node1.StartNewElection();
            await Task.Delay(300);

            node2.Received().RequestVoteRPC(node1.Id, Arg.Any<int>());
            node3.Received().RequestVoteRPC(node1.Id, Arg.Any<int>());
        }

        [Fact] // Testing #8.2
        public async void CandidateBecomesALeaderWhenItReceivesMajorityVotes_Cluster3_Test()
        {
            Node node1 = NodeFactory.StartNewNode("Candidate");
            node1.Term = 2;
            var node2 = Substitute.For<INode>();
            var node3 = Substitute.For<INode>();
            var i = Guid.NewGuid();
            var j = Guid.NewGuid();
            node2.Id.Returns(i);
            node3.Id.Returns(j);
            node1.Nodes.Add(node2.Id, node2);
            node1.Nodes.Add(node3.Id, node3);

            await node1.RespondVoteRPC(node2.Id, 2, true);
            await node1.RespondVoteRPC(node3.Id, 2, true);

            Assert.True(node1.State == "Leader");
        }

        [Fact] // Testing #10
        public async void CandidateReceivesMajorityVotesWhileWaitingForUnresponsiveNode_Test()
        {
            Node node1 = NodeFactory.StartNewNode("Candidate");
            node1.Term = 2;
            var node2 = Substitute.For<INode>();
            var node3 = Substitute.For<INode>();
            node2.Id = Guid.NewGuid();
            node3.Id = Guid.NewGuid();
            node1.Nodes.Add(node2.Id, node2);
            node1.Nodes.Add(node3.Id, node3);

            node1.VoteCountForMe++;
            await node1.RespondVoteRPC(node2.Id, 2, true);

            Assert.True(node1.State == "Leader");
        }


        // Testing 11
        [Fact]
        public async void GivenNewCandidateServer_ServerVotesForSelf_Test()
        {
            Node node1 = new Node();
            node1.Heartbeat = 5;
            node1.TimeoutMultiplier = 1;
            node1.ElectionTimerMax = 30;
            node1.BaseTimerWaitCycle = 2;
            node1.RefreshElectionTimeout();

            node1.StartNewElection();
            await Task.Delay(50);

            Assert.True(node1.Votes[node1.Term] == node1.Id);
        }

        // Testing 12
        [Fact]
        public async void GivenCandidateReceivesLaterAppendEntries_CandidateBecomesFollower_Test()
        {
            Node node1 = new Node();
            var node2 = Substitute.For<INode>();
            node2.Id = Guid.NewGuid();
            node1.Nodes.Add(node2.Id, node2);
            node2.Term = 5;
            node1.State = "Candidate";

            await node1.AppendEntriesRPC(node2.Id, new CommandToken() { TERM = 5 });

            Assert.True(node1.Term == 5);
            Assert.True(node1.State == "Follower");
        }

        // Testing 13
        [Fact]
        public async void GivenCandidateReceivesSameTermAppendEntries_CandidateBecomesFollower_Test()
        {
            Node node1 = new Node();
            var node2 = Substitute.For<INode>();
            node2.Id = Guid.NewGuid();
            node1.Nodes.Add(node2.Id, node2);
            node2.Term = 5;
            node1.Term = 5;
            node1.State = "Candidate";

            await node1.AppendEntriesRPC(node2.Id, new CommandToken() { TERM = 5 });

            Assert.True(node1.Term == 5);
            Assert.True(node1.State == "Follower");
        }

        // Testing 14
        [Fact]
        public async void IfNodeReceivesSecondRequestForVoteForSameTerm_NodeRespondsNO_Test()
        {
            Node node1 = new Node();
            var node2 = Substitute.For<INode>();
            node2.Id = Guid.NewGuid();
            node2.Term = 2;
            node1.Votes.Add(2, node2.Id);
            node1.Nodes.Add(node2.Id, node2);

            await node1.RequestVoteRPC(node2.Id, node2.Term);
            await node1.RequestVoteRPC(node2.Id, node2.Term);

            await node2.Received(4).RespondVoteRPC(node2.Id, node2.Term, true);
        }

        // Testing 15
        [Fact]
        public void IfNodeReceivesSecondRequestForVoteForFutureTerm_NodeRespondsYes_Test()
        {
            Node node1 = new Node();
            var node2 = Substitute.For<Node>();
            node2.VoteCountForMe = 0;
            node2.Id = Guid.NewGuid();
            node2.Term = 4;
            node1.Votes.Add(3, node2.Id);
            node1.Nodes.Add(node2.Id, node2);

            node2.When(x => x.RequestVotesFromClusterRPC()).Do(x => {
                node1.RequestVoteRPC(node2.Id, node2.Term);
            });
            node2.When(x => x.IncrementVoteCount()).Do(x => { node2.VoteCountForMe++; });

            node2.RequestVotesFromClusterRPC();

            Assert.True(node2.VoteCountForMe == 1);
        }

        // Testing 16
        [Fact]
        public async void GivenCandidateWhenElectionTimerExpiresInsideAnElection_NewElectionStarted_Test()
        {
            Node node1 = new Node();
            var node2 = Substitute.For<INode>();
            var node3 = Substitute.For<INode>();
            var i = Guid.NewGuid();
            var j = Guid.NewGuid();
            node2.Id.Returns(i);
            node3.Id.Returns(j);
            node1.Nodes.Add(node2.Id, node2);
            node1.Nodes.Add(node3.Id, node3);
            node1.State = "Candidate";
            var ExpectedState = node1.State;
            node1.TimeoutMultiplier = 1;
            node1.BaseTimerWaitCycle = 100;
            node1.ElectionTimerCurr = 850;

            node1.Start();
            await Task.Delay(1800);
            node1.Stop();

            await node2.Received().RequestVoteRPC(node1.Id, 3);
            await node3.Received().RequestVoteRPC(node1.Id, 3);
        }

        // Testing 17
        [Fact]
        public void WhenFollowerNodeReceivesAppendEntriesRequest_NodeSendsResponse_Test()
        {
            Node node = new Node();
            var node2 = Substitute.For<Node>();
            node2.Id = Guid.NewGuid();
            node.Nodes.Add(node2.Id, node2);

            CommandToken commandToken = Substitute.For<CommandToken>();
            commandToken.TERM = 2;
            commandToken.VALUE = 2;
            commandToken.COMMAND = "add";
            commandToken.INDEX = 0;
            commandToken.ISCOMMITTED = false;


            node.AppendEntriesRPC(node.Id, commandToken);

            node2.Received().AppendResponseRPC(Arg.Any<Guid>(), Arg.Any<bool>(), Arg.Any<CommandToken>());
        }

        // Testing 18
        [Fact]
        public async void GivenCandidateReceivesAppendEntryFromPreviousTerm_CandidateRejects_Test()
        {
            Node node = new Node();
            node.State = "Candidate";
            node.Term = 3;
            var node2 = Substitute.For<Node>();
            node2.Id = Guid.NewGuid();
            node2.Term = 0;
            node2.AppendEntriesResponseFlag = true;
            node.Nodes.Add(node2.Id, node2);

            node2.When(x => x.AppendEntriesRPC(Arg.Any<Guid>(), Arg.Any<CommandToken>())).
                Do(async x => { 
                await node.AppendEntriesRPC(node2.Id, new CommandToken()
                {
                    TERM = 1,
                    VALUE = 0,
                    COMMAND = "",
                    INDEX = 0,
                    ISCOMMITTED = false
                });
                });
            await node2.AppendEntriesRPC(node2.Id, new CommandToken());

            Assert.True(node2.AppendEntriesResponseFlag);
        }

        // Testing 19
        [Fact]
        public void WhenCandidateWinsElection_ThenImmediateHeartbeat()
        {
            Node node = new Node();
            node.State = "Candidate";
            var node2 = Substitute.For<Node>(); 
            node2.Id = Guid.NewGuid();
            node.Nodes.Add(node2.Id, node2);

            node.MakeLeader();

            //node2.Received().AppendEntriesRPC(Arg.Any<Guid>());
        }


        // Putting the test for logging here until I can figure out why it isn't linking
        [Fact]
        public async void WhenClientSendsRequestThenLeaderAppendsToLog()
        {
            Node node = new Node();

            await node.RequestAdd(1);
            var expected = new Dictionary<int, CommandToken>();

            Assert.True(node.CommandLog.Count > 0);
        }

    }
}