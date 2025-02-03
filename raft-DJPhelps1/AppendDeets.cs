using raft_DJPhelps1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaftRESTAPI
{
    public record AppendDeets
    {
        public Guid id;
        public required CommandToken ct;
        //Valid leader flag
        public bool vlf;
    }
}
