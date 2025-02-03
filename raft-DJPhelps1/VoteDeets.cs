using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace raft_DJPhelps1
{
    public record VoteDeets
    {
        public Guid Id { get; set; }
        public int Term {  get; set; }
        public bool Vote { get; set; }
    }
}
