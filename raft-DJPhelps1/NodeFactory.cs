using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace raft_DJPhelps1
{
    public static class NodeFactory
    {
        public static Node StartNewNode(string state, Guid? setGuid = null)
        {
            Node node = new Node();
            node.State = state;
            if (setGuid == null)
                node.Id = Guid.NewGuid();
            else
                node.Id = ((Guid)setGuid);
            return node;
        }
    }
}
