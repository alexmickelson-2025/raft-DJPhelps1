using NSubstitute;
using NSubstitute.Core.Arguments;
using raft_DJPhelps1;
using raft_DJPhelps1_Test;
using System.Xml.Linq;

namespace raft_TDD_Tests
{
    public class RaftElectionTests
    {
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
        public void NodeReceivesAppendEntriesPersistsLeadershipStatus_Test()
        {
            Node node = NodeFactory.StartNewNode("Leader");
            var node2 = Substitute.For<Node>();
            node.Nodes.Add(node2.Id, node2);

            node.Start();
            Thread.Sleep(100);

            node.Stop();
            Assert.True(node2.CurrentLeader == node.Id);
        }

        [Fact] // Testing #3
        public void NodeInitializesInFollowerState_Test()
        {
            Node node = new Node();

            Assert.True(node.State == "Follower");
        }

        [Fact] // Testing #4
        public void NodeStartsElectionWithinElectionTimeout_Test()
        {
            Node node = new Node();

            node.Start();
            Thread.Sleep(350);
            node.Stop();

            Assert.True(node.State == "Candidate");
        }

        [Fact] // Testing #5
        public void ElectionTimeResetsBetweenInterval_Test()
        {
            Node node = new Node();

            node.Start();
            Thread.Sleep(node.ElectionTimerMax + 40);
            node.Stop();

            Assert.True(node.ElectionTimerMax >= 150 && node.ElectionTimerMax <= 300);
        }

        [Fact] // Testing #6
        public void ElectionStartIncrementsTerm_Test()
        {
            Node node = new Node();
            var expectedTerm = node.Term;

            node.Start();
            Thread.Sleep(node.ElectionTimerMax + 50);
            node.Stop();

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
        public void CandidatesSendVoteRequestWhenElectionStarts()
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

            node1.Start();

            Console.WriteLine(node1.State, node1.Id.ToString(), node1.Term);
            Thread.Sleep(300);
            node1.Stop();

            node2.Received().RequestVoteRPC(Arg.Any<Guid>(), Arg.Any<int>());
            node3.Received().RequestVoteRPC(Arg.Is<Guid>(Id => Id == node1.Id), Arg.Is<int>(term => term == 4));

        }

        [Fact] // Testing #8.2
        public void CandidateBecomesALeaderWhenItReceivesMajorityVotes_Cluster5_Test()
        {
            Node node1 = NodeFactory.StartNewNode("Candidate");
            node1.Term = 2;
            var node2 = Substitute.For<INode>();
            var node3 = Substitute.For<INode>();
            var node4 = Substitute.For<INode>();
            var node5 = Substitute.For<INode>();
            node2.Id.Returns(Guid.NewGuid());
            node3.Id.Returns(Guid.NewGuid());
            node4.Id.Returns(Guid.NewGuid());
            node5.Id.Returns(Guid.NewGuid());


            node1.Nodes.Add(node2.Id, node2);
            node1.Nodes.Add(node3.Id, node3);
            node1.Nodes.Add(node4.Id, node4);
            node1.Nodes.Add(node5.Id, node5);

            node1.Start();

            Thread.Sleep(350);

            node1.ReceiveVoteRPC(node2.Id, 2, true);
            node1.ReceiveVoteRPC(node3.Id, 2, true);
            node1.ReceiveVoteRPC(node4.Id, 2, true);
            node1.ReceiveVoteRPC(node5.Id, 2, true);

            node1.Stop();

            Assert.True(node1.State == "Leader");
        }

        [Fact] // Testing #10
        public void CandidateReceivesMajorityVotesWhileWaitingForUnresponsiveNode_Test()
        {
            Node node1 = NodeFactory.StartNewNode("Candidate");
            node1.Term = 2;
            var node2 = Substitute.For<INode>();
            var node3 = Substitute.For<INode>();
            var node4 = Substitute.For<INode>();
            var node5 = Substitute.For<INode>();
            node2.Id.Returns(Guid.NewGuid());
            node3.Id.Returns(Guid.NewGuid());
            node4.Id.Returns(Guid.NewGuid());
            node5.Id.Returns(Guid.NewGuid());


            node1.Nodes.Add(node2.Id, node2);
            node1.Nodes.Add(node3.Id, node3);
            node1.Nodes.Add(node4.Id, node4);
            node1.Nodes.Add(node5.Id, node5);

            node1.Start();

            Thread.Sleep(350);

            node1.ReceiveVoteRPC(node2.Id, 2, true);
            node1.ReceiveVoteRPC(node3.Id, 2, true);

            node1.Stop();

            Assert.True(node1.State == "Leader");
        }


        // Testing 11
        [Fact]
        public void GivenNewCandidateServer_ServerVotesForSelf_Test()
        {
            Node node1 = new Node();

            node1.StartNewElection();

            Assert.True(node1.VoteCountForMe == 1);
        }

        // Testing 12
        [Fact]
        public void GivenCandidateReceivesLaterAppendEntries_CandidateBecomesFollower_Test()
        {
            Node node1 = new Node();
            Node node2 = new Node();
            node1.Nodes.Add(node2.Id, node2);
            node2.Nodes.Add(node1.Id, node1);
            node2.Term = 5;
            node2.State = "Leader";
            node1.State = "Candidate";

            node1.Start();
            node2.Start();
            Thread.Sleep(1000);

            Assert.True(node1.Term == 5);
            Assert.True(node1.State == "Follower");
        }

        // Testing 13
        [Fact]
        public void GivenCandidateReceivesSameTermAppendEntries_CandidateBecomesFollower_Test()
        {
            Node node1 = new Node();
            Node node2 = new Node();
            node1.Nodes.Add(node2.Id, node2);
            node2.Nodes.Add(node1.Id, node1);
            node2.Term = 5;
            node1.Term = 5;
            node2.State = "Leader";
            node1.State = "Candidate";

            node1.Start();
            node2.Start();
            Thread.Sleep(1000);

            Assert.True(node1.Term == 5);
            Assert.True(node1.State == "Follower");
        }

        // Testing 14
        [Fact]
        public void IfNodeReceivesSecondRequestForVoteForSameTerm_NodeRespondsNO_Test()
        {
            Node node1 = new Node();
            var node2 = Substitute.For<Node>();
            node2.VoteCountForMe = 0;
            node2.Id = Guid.NewGuid();
            node2.Term = 2;
            node1.Votes.Add(2, node2.Id);
            node1.Nodes.Add(node2.Id, node2);

            node2.When(x => x.RequestVotesFromClusterRPC()).Do(x => {
                node1.RequestVoteRPC(node2.Id, node2.Term);
            });
            node2.When(x => x.IncrementVoteCount()).Do(x => { node2.VoteCountForMe++; });

            node2.RequestVotesFromClusterRPC();

            Assert.True(node2.VoteCountForMe == 0);
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
        public void GivenCandidateWhenElectionTimerExpiresInsideAnElection_NewElectionStarted_Test()
        {
            Node node1 = new Node();
            var node2 = Substitute.For<Node>();
            var node3 = Substitute.For<Node>();
            var ExpectedTermState1 = node1.Term;

            node2.VoteCountForMe = 0;
            node2.Id = Guid.NewGuid();
            node3.Id = Guid.NewGuid();
            node2.Term = 2;
            node1.Votes.Add(2, node2.Id);
            node1.Nodes.Add(node2.Id, node2);
            node1.Nodes.Add(node3.Id, node3);

            node2.When(x => x.RequestVoteRPC(Arg.Any<Guid>(), Arg.Any<int>())).Do(x => { });
            node3.When(x => x.RequestVoteRPC(Arg.Any<Guid>(), Arg.Any<int>())).Do(x => { });


            Assert.True(ExpectedTermState1 < node1.Term);
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
            commandToken.term = 2;
            commandToken.value = 2;
            commandToken.command = "add";
            commandToken.index = 0;
            commandToken.is_committed = false;


            node.AppendEntriesRPC(node.Id, commandToken);

            node2.Received().AppendResponseRPC(Arg.Any<Guid>(), Arg.Any<bool>(), Arg.Any<CommandToken>());
        }

        // Testing 18
        [Fact]
        public void GivenCandidateReceivesAppendEntryFromPreviousTerm_CandidateRejects_Test()
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
                Do(x => { 
                node.AppendEntriesRPC(node2.Id, new CommandToken()
                {
                    term = 1,
                    value = 0,
                    command = "",
                    index = 0,
                    is_committed = false
                });
                });
            node2.AppendEntriesRPC(node2.Id, new CommandToken());

            Assert.True(!node2.AppendEntriesResponseFlag);
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
        public void WhenClientSendsRequestThenLeaderAppendsToLog()
        {
            Node node = new Node();

            node.RequestAdd(1);
            var expected = new Dictionary<int, CommandToken>();

            Assert.True(node.CommandLog.Count > 0);
        }

    }
}