using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaftRESTAPI
{
    public record VoteDeets
    {
        public Guid Id { get; set; }
        public int Term {  get; set; }
        public bool Vote { get; set; }
    }
}
