using System;
using BattleTech;
using Harmony;
using System.Collections.Generic;
using System.Linq;
using static TisButAScratch.Framework.GlobalVars;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using BattleTech.UI;
using BattleTech.UI.TMProWrapper;

namespace TisButAScratch.Framework
{
    class InjuryDescriptions
    {
        internal static string getPilotInjuryDesc(Pilot pilot)
        {
            var pilotID = pilot.FetchGUID();
            if (PilotInjuryHolder.HolderInstance.pilotInjuriesMap.ContainsKey(pilotID))
            {
                var rtrn = "\n";
                foreach (var id in PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pilotID])
                {
                    foreach (Injury injury in PilotInjuryManager.ManagerInstance.InjuryEffectsList.Where(x => x.injuryID == id))
                    {
                        var description = $"{injury.injuryName}: {injury.description}";
                        rtrn += description + "\n\n";
                    }
                }

                foreach (var id in PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pilotID])
                {
                    foreach (Injury feedbackinjury in PilotInjuryManager.ManagerInstance.InternalDmgInjuries.Where(x => x.injuryID == id))
                    {
                        var description = $"{feedbackinjury.injuryName}: {feedbackinjury.description}";
                        rtrn += description + "\n\n";
                    }
                }

                if (PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pilotID].Contains(CRIPPLED.injuryID))
                {
                    rtrn += CRIPPLED.description;
                }
                return rtrn;
            }
            return null;
        }
    }
}
