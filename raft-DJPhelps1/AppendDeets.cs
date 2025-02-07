using raft_DJPhelps1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace raft_DJPhelps1 { 
    public record AppendDeets
    {
        public Guid ID { get; set; }
        public required CommandToken CT { get; set; }
        //Valid leader flag
        public bool VLF { get; set; }
    }
}
