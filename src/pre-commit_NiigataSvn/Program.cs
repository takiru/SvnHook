using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SvnHook.PreCommit
{
    class Program
    {
        static void Main(string[] args)
        {
            var precommit = new PreCommit();
            if (!precommit.Load(args))
            {
                Environment.Exit(-1);
            }
            if (!precommit.Validate())
            {
                Environment.Exit(-1);
            }
            Environment.Exit(0);
        }
    }
}