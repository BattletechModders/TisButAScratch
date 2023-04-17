using BattleTech;
using System.Linq;
using static TisButAScratch.Framework.GlobalVars;

namespace TisButAScratch.Framework
{
    public static class InjuryDescriptions
    {
        internal static string getPilotInjuryDescCompact(Pilot pilot)
        {
            var pilotID = pilot.FetchGUID();
            if (string.IsNullOrEmpty(pilotID)) return null;

            var rtrn = "";
            if (PilotInjuryHolder.HolderInstance.pilotInjuriesMap.ContainsKey(pilotID))
            {
                if (ModInit.modSettings.debilSeverityThreshold > 0)
                {
                    rtrn +=
                        $"<color=#FF0000>Debilitating Severity Threshold Per-Location: {ModInit.modSettings.debilSeverityThreshold}</color=#FF0000>\n";
                }

                foreach (var id in PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pilotID])
                {
                    foreach (Injury injury in PilotInjuryManager.ManagerInstance.InjuryEffectsList.Where(x =>
                        x.injuryID == id))
                    {
                        var description = $"Severity {injury.severity}: {injury.injuryName}: {injury.description}";
                        rtrn += description + "\n";
                    }
                }

                if (ModInit.modSettings.enableInternalDmgInjuries)
                {
                    foreach (var id in PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pilotID])
                    {
                        foreach (Injury feedbackinjury in PilotInjuryManager.ManagerInstance.InternalDmgInjuries.Where(
                            x => x.injuryID == id))
                        {
                            var description =
                                $"Severity {feedbackinjury.severity}: {feedbackinjury.injuryName}: {feedbackinjury.description}";
                            rtrn += description + "\n";
                        }
                    }
                }


                if (pilot.pilotDef.PilotTags.Contains(DEBILITATEDTAG))
                {
                    rtrn += DEBIL.description + "\n";
                }
            }

            if (ModInit.modSettings.UseSimBleedingEffects)
            {
                foreach (var tempResult in sim.TemporaryResultTracker.Where(x =>
                    x.TargetPilot == pilot && x.Stats != null))
                {
                    var statDesc = "";
                    foreach (var tag in tempResult.AddedTags)
                    {
                        var matches = TBAS_SimBleedStatMod.Matches(tag);
                        if (matches.Count <= 0) continue;
                        var statType = matches[0].Groups["type"].Value;
                        var statMod = matches[0].Groups["value"].Value;
                        //                        var statOp = matches[0].Groups["operation"].Value;
                        statDesc += $"{statMod} {statType}, ";
                    }

                    statDesc += $" for {tempResult.ResultDuration - tempResult.DaysElapsed} days when in combat.";
                    rtrn += statDesc + "\n";
                }
            }

            return rtrn;
        }
        internal static string getPilotInjuryDesc(Pilot pilot)
        {
            var pilotID = pilot.FetchGUID();
            if (string.IsNullOrEmpty(pilotID)) return null;

            var rtrn = "";
            if (PilotInjuryHolder.HolderInstance.pilotInjuriesMap.ContainsKey(pilotID))
            {
                if (ModInit.modSettings.debilSeverityThreshold > 0)
                {
                    rtrn +=
                        $"<color=#FF0000>Debilitating Severity Threshold Per-Location: {ModInit.modSettings.debilSeverityThreshold}</color=#FF0000>\n\n";
                }

                foreach (var id in PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pilotID])
                {
                    foreach (Injury injury in PilotInjuryManager.ManagerInstance.InjuryEffectsList.Where(x =>
                        x.injuryID == id))
                    {
                        var description = $"Severity {injury.severity}: {injury.injuryName}: {injury.description}";
                        rtrn += description + "\n\n";
                    }
                }

                if (ModInit.modSettings.enableInternalDmgInjuries)
                {
                    foreach (var id in PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pilotID])
                    {
                        foreach (Injury feedbackinjury in PilotInjuryManager.ManagerInstance.InternalDmgInjuries.Where(
                            x => x.injuryID == id))
                        {
                            var description =
                                $"Severity {feedbackinjury.severity}: {feedbackinjury.injuryName}: {feedbackinjury.description}";
                            rtrn += description + "\n\n";
                        }
                    }
                }


                if (pilot.pilotDef.PilotTags.Contains(DEBILITATEDTAG))
                {
                    rtrn += DEBIL.description + "\n\n";
                }
            }

            if (ModInit.modSettings.UseSimBleedingEffects)
            {
                foreach (var tempResult in sim.TemporaryResultTracker.Where(x =>
                    x.TargetPilot == pilot && x.Stats != null))
                {
                    var statDesc = "";
                    foreach (var tag in tempResult.AddedTags)
                    {
                        var matches = TBAS_SimBleedStatMod.Matches(tag);
                        if (matches.Count <= 0) continue;
                        var statType = matches[0].Groups["type"].Value;
                        var statMod = matches[0].Groups["value"].Value;
//                        var statOp = matches[0].Groups["operation"].Value;
                        statDesc += $"{statMod} {statType}, ";
                    }

                    statDesc += $" for {tempResult.ResultDuration - tempResult.DaysElapsed} days when in combat.";
                    rtrn += statDesc + "\n\n";
                }
            }

            return rtrn;
        }

    }
}
