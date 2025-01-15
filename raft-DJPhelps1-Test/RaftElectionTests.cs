using NSubstitute;
using raft_DJPhelps1;
using System.Xml.Linq;

namespace raft_TDD_Tests
{
    public class RaftElectionTests
    {
        [Fact] // Testing #1
        public async Task NodeLeaderSendsHeartbeatWithinInterval_Test()
        {
            Node node = NodeFactory.StartNewNode("Leader");
            var node3 = Substitute.For<Node>();
            node.Nodes.Add(node3.Id, node3);
            node3.Nodes.Add(node.Id, node);
            
            await node.Start();
            await Task.Delay(300);

            await node.Stop();
            await node3.Received().AppendEntries(node.Id);
            Assert.True(node3.State == "Follower");
        }

        [Fact] // Testing #2
        public async Task NodeReceivesAppendEntriesPersistsLeadershipStatus_Test()
        {
            Node node = NodeFactory.StartNewNode("Leader");
            var node2 = Substitute.For<Node>();
            node.Nodes.Add(node2.Id, node2);
            node2.Nodes.Add(node.Id, node);

            await node.Start();
            await Task.Delay(100);

            await node.Stop();
            Assert.True(node2.CurrentLeader == node.Id);
        }

        [Fact] // Testing #3
        public void NodeInitializesInFollowerState_Test()
        {
            Node node = new Node();

            Assert.True(node.State == "Follower");
        }

        [Fact] // Testing #4
        public async Task NodeStartsElectionWithinElectionTimeout_Test()
        {
            Node node = new Node();

            await node.Start();
            await Task.Delay(node.ElectionTimerMax + 20);
            await node.Stop();

            Assert.True(node.State == "Candidate");
        }

        [Fact] // Testing #5
        public async Task ElectionTimeResetsBetweenInterval_Test()
        {
            Node node = new Node();
            
            await node.Start();
            await Task.Delay(node.ElectionTimerMax + 1);
            await node.Stop();

            Assert.True(node.ElectionTimerMax >= 150 && node.ElectionTimerMax <= 300);
        }

        [Fact] // Testing #6
        public async Task ElectionStartIncrementsTerm_Test()
        {
            Node node = new Node();
            var expectedTerm = node.Term;

            await node.Start();
            await Task.Delay(node.ElectionTimerMax+30);
            await node.Stop();
            
            var actualTerm = node.Term;

            Assert.True(expectedTerm < actualTerm);
        }

        [Fact] // Testing #7
        public async Task FollowerResetsElectionTimerAfterAppendEntriesRPC_Test()
        {
            Node node = NodeFactory.StartNewNode("Leader");
            Node node3 = NodeFactory.StartNewNode("Follower");
            node.Nodes.Add(node3.Id, node3);
            node3.Nodes.Add(node.Id, node);
            var expectedTimeoutCurr = node3.ElectionTimerMax;

            await node.Start();
            await node3.Start();
            await Task.Delay(400);
            await node3.Stop();
            await node.Stop();

            Assert.True(node.State == "Leader");
            Assert.True(node3.State == "Follower");
            Assert.True(node3.ElectionTimeoutCurr == node3.ElectionTimerMax);
        }

        [Fact] // Testing #8.1
        public async Task CandidateBecomesALeaderWhenItReceivesMajorityVotes_Cluster1_Test()
        {
            Node node1 = new Node();

            await node1.Start();
            await Task.Delay(1000);
            await node1.Stop();

            Assert.True(node1.State == "Leader");
        }

        [Fact] // Testing #8.2
        public async Task CandidateBecomesALeaderWhenItReceivesMajorityVotes_Cluster5_Test()
        {
            Node node1 = new Node();
            Node node2 = new Node();
            Node node3 = new Node();
            Node node4 = new Node();
            Node node5 = new Node();
            
            node1.Nodes.Add(node2.Id, node2);
            node1.Nodes.Add(node3.Id, node3);
            node1.Nodes.Add(node4.Id, node4);
            node1.Nodes.Add(node5.Id, node5);


            await node1.Start();
            await node2.Start();
            await node3.Start();
            await node4.Start();
            await node5.Start();
            await Task.Delay(1000);
            await node5.Stop();
            await node4.Stop();
            await node3.Stop();
            await node2.Stop();
            await node1.Stop();

            Assert.True(node1.State == "Leader");
        }

        [Fact] // Testing #10
        public async Task NodeConclusionElectionWithUnresponsiveNode_Test()
        {
            Node node1 = new Node();
            Node node2 = new Node();
            Node node3 = new Node();
            Node node4 = new Node();
            Node node5 = new Node();

            node1.Nodes.Add(node2.Id, node2);
            node1.Nodes.Add(node3.Id, node3);
            node1.Nodes.Add(node4.Id, node4);
            node1.Nodes.Add(node5.Id, node5);


            await node1.Start();
            await node2.Start();
            await node3.Start();
            await node5.Start();
            await Task.Delay(1000);
            await node5.Stop();
            await node3.Stop();
            await node2.Stop();
            await node1.Stop();

            Assert.True(node1.State == "Leader");
        }

        [Fact] // Testing #10
        public async Task CandidateReceivesMajorityVotesWhileWaitingForUnresponsiveNode_Test()
        {
            Node node1 = new Node();
            Node node2 = new Node();
            Node node3 = new Node();
            Node node4 = new Node();
            Node node5 = new Node();
            node1.Term = 4;

            node1.Nodes.Add(node2.Id, node2);
            node1.Nodes.Add(node3.Id, node3);
            node1.Nodes.Add(node4.Id, node4);
            node1.Nodes.Add(node5.Id, node5);


            await node1.Start();
            await node2.Start();
            await node3.Start();
            await node5.Start();
            await Task.Delay(1000);
            await node5.Stop();
            await node3.Stop();
            await node2.Stop();
            await node1.Stop();

            Assert.True(node1.State == "Leader");
            Assert.True(node4.Term == 5);
        }

        // DEPRECATED TEST: NEEDS REWRITE
        //
        //[Fact] 
        //public async Task Test1()
        //{
        //    Node node1 = NodeFactory.StartNewNode("Candidate");
        //    Node node2 = NodeFactory.StartNewNode("Candidate");
        //    Node node3 = NodeFactory.StartNewNode("Follower");
        //    Node node4 = NodeFactory.StartNewNode("Follower");
        //    Node node5 = NodeFactory.StartNewNode("Follower");
            

        //    // Act
        //    // (assume time out happens before this act step)
        //    await node1.StartNewElection(); // Need to start a new term for both new candidates
        //    await node2.StartNewElection();
        //    foreach (Guid id in node1.Nodes.Keys)
        //    {
        //        await node1.RequestVotes();
        //    }
        //    foreach (Guid id in node2.Nodes.Keys)
        //    {
        //        await node2.RequestVotes();
        //    }

        //    if (node1.VoteCount > (node1.Nodes.Count()) / 2 && node2.VoteCount <= (node2.Nodes.Count()) / 2)
        //        node1.MakeLeader();
        //    else
        //        node2.MakeLeader();

        //    Assert.True(node1.State == "Leader");
        //    Assert.True(node2.State == "Follower");
        //}
    }
}