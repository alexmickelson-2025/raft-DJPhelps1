using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace raft_DJPhelps1
{
    public class CommandToken : IEquatable<CommandToken>
    {
        public string command   = "";
        public int value        = 0;
        public int term         = 1;
        public int index        = 0;
        public bool is_committed= false;
        public bool is_valid    = true;

        bool IEquatable<CommandToken>.Equals(CommandToken? other)
        {
            return this.command == other.command &&
                this.value == other.value &&
                this.term == other.term &&
                this.index == other.index &&
                this.is_valid == other.is_valid &&
                this.is_committed == other.is_committed;
        }
    }
}
