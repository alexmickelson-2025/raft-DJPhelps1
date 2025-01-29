using NSubstitute;
using NSubstitute.Core.Arguments;
using raft_DJPhelps1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace raft_DJPhelps1_Test
{
    public class RaftElectionPausingTests
    {
        [Fact]
        public void PausedLeaderWaits400MS_ThenOtherNodesGetNoHeartbeats()
        {
            Node node = new Node();
            var node_mock = Substitute.For<Node>();
            node_mock.Id = Guid.NewGuid();
            node.Nodes.Add(node_mock.Id, node_mock);

            node.State = "Leader";
            node.Start();
            Thread.Sleep(50);
            node.Stop();
            Thread.Sleep(400);

            node_mock.Received(1).AppendEntriesRPC(node.Id, Arg.Any<CommandToken>());
        }
    }
}
