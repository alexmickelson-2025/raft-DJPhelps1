using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace raft_DJPhelps1
{
    public class CommandToken : IEquatable<CommandToken>
    {
        public string COMMAND { get; set; } = "";
        public int VALUE { get; set; } = 0;
        public int TERM { get; set; } = 1;
        public int INDEX { get; set; } = 0;
        public bool ISCOMMITTED { get; set; } = false;
        public bool ISVALID { get; set; } = true;

        bool IEquatable<CommandToken>.Equals(CommandToken? other)
        {
            if(other is null)
                throw new ArgumentNullException("Null value of comparison between two Command Tokens");
            return this.COMMAND == other.COMMAND &&
                this.VALUE == other.VALUE &&
                this.TERM == other.TERM &&
                this.INDEX == other.INDEX &&
                this.ISVALID == other.ISVALID &&
                this.ISCOMMITTED == other.ISCOMMITTED;
        }
    }
}
