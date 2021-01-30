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
            if (string.IsNullOrEmpty(pilotID)) return null;

            if (PilotInjuryHolder.HolderInstance.pilotInjuriesMap.ContainsKey(pilotID))
            {
                var rtrn = $"\n";
                if (ModInit.modSettings.debilSeverityThreshold > 0)
                {
                    rtrn += $"<color=#FF0000>Debilitating Severity Threshold Per-Location: {ModInit.modSettings.debilSeverityThreshold}</color=#FF0000>\n\n";
                }
                foreach (var id in PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pilotID])
                {
                    foreach (Injury injury in PilotInjuryManager.ManagerInstance.InjuryEffectsList.Where(x => x.injuryID == id))
                    {
                        var description = $"Severity {injury.severity}: {injury.injuryName}: {injury.description}";
                        rtrn += description + "\n\n";
                    }
                }

                if (ModInit.modSettings.enableInternalDmgInjuries)
                {
                    foreach (var id in PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pilotID])
                    {
                        foreach (Injury feedbackinjury in PilotInjuryManager.ManagerInstance.InternalDmgInjuries.Where(x => x.injuryID == id))
                        {
                            var description = $"Severity {feedbackinjury.severity}: {feedbackinjury.injuryName}: {feedbackinjury.description}";
                            rtrn += description + "\n\n";
                        }
                    }
                }
                

                if (pilot.pilotDef.PilotTags.Contains(DEBILITATEDTAG))
                {
                    rtrn += DEBIL.description;
                }
                return rtrn;
            }
            return null;
        }
    }
}
