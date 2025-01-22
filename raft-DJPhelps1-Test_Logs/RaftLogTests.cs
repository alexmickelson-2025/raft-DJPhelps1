using raft_DJPhelps1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace raft_DJPhelps1_Test
{
    public class RaftLogTests
    {
        [Test]
        public void WhenClientSendsRequestThenLeaderAppendsToLog()
        {
            Node node = new Node();

            node.RequestAdd(1);
            var expected = new List<int>() {
                1
            };

            Assert.True(node.CommandLog == expected);
        }
    }
}
