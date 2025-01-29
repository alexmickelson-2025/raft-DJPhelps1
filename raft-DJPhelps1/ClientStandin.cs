using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace raft_DJPhelps1
{
    public class ClientStandin([Required] INode node)
    {
        [Required]
        private INode _node = node;
        public INode? Node { 
            get ;
            set;
        }
        public bool GotResponseFlag = false;

        public void GetResponse()
        {
            GotResponseFlag = true;
        }
    }
}
