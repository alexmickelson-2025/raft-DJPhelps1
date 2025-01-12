using raft_DJPhelps1;

namespace raft_TDD_Tests
{
    public class UnitTest1
    {
        [Fact]
        public async Task Test1()
        {
            Node node1 = NodeFactory.StartNewNode("Candidate");
            Node node2 = NodeFactory.StartNewNode("Candidate");
            Node node3 = NodeFactory.StartNewNode("Follower");
            Node node4 = NodeFactory.StartNewNode("Follower");
            Node node5 = NodeFactory.StartNewNode("Follower");
            

            // Act
            // (assume time out happens before this act step)
            await node1.StartNewElection(); // Need to start a new term for both new candidates
            await node2.StartNewElection();
            foreach (Guid id in node1.ClusterList)
            {
                await node1.RequestVote(id, node1.Term);
            }
            foreach (Guid id in node2.ClusterList)
            {
                await node2.RequestVote(id, node2.Term);
            }

            if (node1.VoteCount > (node1.ClusterList.Count() + 1) / 2 && node2.VoteCount > (node2.ClusterList.Count() + 1) / 2)
                node1.MakeLeader();
            else
                node2.MakeLeader();

            Assert.True(node1.State == "Leader");
            Assert.True(node2.State == "Follower");
        }
    }
}