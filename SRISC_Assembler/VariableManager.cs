using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRISC_Assembler
{
    public static class VariableManager
    {
        public class MemObj
        {
            public bool Active { get; set; }
            public int Location { get; set; }
            public int Length { get; set; }
            public int LastModified { get; set; }

            public MemObj(int type, int loc)
            {
                Active = true;
                Location = loc;
                Length = type;
                LastModified = 0;
            }
        }

        // Records variables in use
        // [0] - invalid, [1-111] - RAM, [112-115] - Registers
        static Dictionary<string, MemObj> Variables = new Dictionary<string, MemObj>();


    }
}
