using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TisButAScratch.Framework
{
    public class ModState
    {
        public static Dictionary<string, List<string>> LastInjuryIDs = new Dictionary<string, List<string>>();
        public static void ResetStateAfterContract()
        {
            LastInjuryIDs = new Dictionary<string, List<string>>();
        }
    }
}
