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
        [Fact]
        public void WhenClientSendsRequestThenLeaderAppendsToLog()
        {
            Node node = new Node();

            node.RequestAdd(1);

            Assert.True(node.CommandLog.Count() > 0);
        }
    }
}
