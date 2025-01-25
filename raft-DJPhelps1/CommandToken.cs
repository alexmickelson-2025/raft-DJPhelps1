using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace raft_DJPhelps1
{
    public class CommandToken
    {
        public string command = "";
        public int value= 0;
        public int term = 1;
        public int index = 0;
        public bool is_committed = false;
    }
}
