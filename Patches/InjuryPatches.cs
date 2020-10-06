using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Harmony;
using BattleTech;
using TisButAScratch.Framework;
using static TisButAScratch.Framework.GlobalVars;
using System.Threading.Tasks;
using UnityEngine;

namespace TisButAScratch.Patches
{
    class InjuryPatches
    {
        //main patch to apply injury effects on injured pilot
        [HarmonyPatch(typeof(Pilot), "InjurePilot", new Type[] {typeof(string), typeof(int),typeof(int), typeof(DamageType), typeof(Weapon),typeof(AbstractActor)})]
        public static class Pilot_InjurePilot_Patch
        {
            public static void Postfix(Pilot __instance, string sourceID, int stackItemUID, int dmg, DamageType damageType, Weapon sourceWeapon, AbstractActor sourceActor)
            {
                ModInit.modLog.LogMessage($"Rolling injury with {dmg} damage for {__instance.Name}");
                PilotInjuryManager.ManagerInstance.rollInjury(__instance, dmg, damageType);

                if (damageType == DamageType.Unknown || damageType == DamageType.NOT_SET)
                {
                    return;
                }

                if (ModInit.modSettings.cripplingInjuriesThreshold > 0) //now trying to add up "severity" threshold for crippled injury
                {
                    var pKey = __instance.FetchGUID();
                    var injuryList = new List<Injury>();
                    foreach (var id in PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey])
                    {
                        injuryList.AddRange(
                            PilotInjuryManager.ManagerInstance.InjuryEffectsList.Where(x => x.injuryID == id));
                    }

                    var groupedLocs = injuryList.GroupBy(x => x.injuryLoc);

                    foreach (var injuryLoc in groupedLocs)
                    {
                        var t = 0;
                        foreach (var injury in injuryLoc)
                        {
                            t += injury.severity;
                        }

                        if (t > ModInit.modSettings.cripplingInjuriesThreshold)
                        {
                            PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey]
                                .Add(CRIPPLED.injuryID);
                            __instance.pilotDef.PilotTags.Add(CrippledTag);
                            if (ModInit.modSettings.enableLethalTorsoHead && (injuryLoc.Key == InjuryLoc.Head ||
                                                                              injuryLoc.Key == InjuryLoc.Torso))
                            {
                                __instance.StatCollection.ModifyStat<bool>("TBAS_Injuries", 0, "LethalInjury",
                                    StatCollection.StatOperation.Set, true, -1, true);
                            }
                        }
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(Pilot))]
    [HarmonyPatch("CanPilot", MethodType.Getter)]
    [HarmonyAfter(new string[] {"dZ.Zappo.Pilot_Fatigue"})]
    public static class Pilot_CanPilot_Patch
    {
        public static void Postfix(Pilot __instance, ref bool __result)
        {
            __result = true;
            if (__instance.pilotDef.PilotTags.Contains(CrippledTag))
            {
                __result = false;
            }
        }
    }

    [HarmonyPatch(typeof(Pilot))]
    [HarmonyPatch("IsIncapacitated", MethodType.Getter)]

    public static class Pilot_IsIncapacitated_Patch
    {
        public static void Postfix(Pilot __instance, ref bool __result)
        {
            if (__instance.pilotDef.PilotTags.Contains(CrippledTag))
            {
                __result = true;
            }
        }
    }

    [HarmonyPatch(typeof(SimGameState))]
    [HarmonyPatch("GetInjuryCost", typeof(Pilot))]
    public static class SimGameState_GetInjuryCost_Patch
    {
        public static bool Prepare()
        { return ModInit.modSettings.injuryHealTimeMultiplier > 0f;}

        public static int Postfix(SimGameState __instance, Pilot p, ref int __result)
        {
            var pKey = p.FetchGUID();
            var injuryList = new List<Injury>();
            foreach (var id in PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey])
            {
                injuryList.AddRange(
                    PilotInjuryManager.ManagerInstance.InjuryEffectsList.Where(x => x.injuryID == id));
            }

            var sev = Math.Max((injuryList.Sum(x => x.severity)-1), 0);

            __result += sev * ModInit.modSettings.severityCost;

            return Mathf.RoundToInt(__result * ModInit.modSettings.injuryHealTimeMultiplier);
        }
    }

    [HarmonyPatch(typeof(Pilot))]
    [HarmonyPatch("ClearInjuries", new Type[] {typeof(string), typeof(int), typeof(string)})]
    public static class Pilot_ClearInjuries_Patch
    {
        public static void Postfix(Pilot __instance)
        {
            var pKey = __instance.FetchGUID();
            if (__instance.pilotDef.PilotTags.Contains(CrippledTag))
            {
                PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey]
                    .RemoveAll(x => x != CRIPPLED.injuryID);
                ModInit.modLog.LogMessage($"{__instance.Name} has been healed, but is still CRIPPLED!");
            }
            else
            {
                PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey]
                    .Clear();
                ModInit.modLog.LogMessage($"{__instance.Name} has been healed!!");
            }
        }
    }
}