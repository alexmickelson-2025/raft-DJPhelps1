using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace raft_DJPhelps1
{
    public record VoteDeets
    {
        public Guid ID { get; set; }
        public int TERM {  get; set; }
        public bool VOTE { get; set; }
    }
}
