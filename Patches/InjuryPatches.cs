using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        [HarmonyPatch(typeof(Pilot), "InjurePilot",
            new Type[]
            {
                typeof(string), typeof(int), typeof(int), typeof(DamageType), typeof(Weapon), typeof(AbstractActor)
            })]
        [HarmonyPriority(Priority.First)]
        public static class Pilot_InjurePilot_Patch
        {
            public static void Prefix(Pilot __instance, string sourceID, int stackItemUID, int dmg,
                DamageType damageType, Weapon sourceWeapon, AbstractActor sourceActor)
            {
                PilotInjuryHolder.HolderInstance.injuryStat = __instance.StatCollection.GetValue<int>("Injuries");
                ModInit.modLog.LogMessage(
                    $"{__instance.Callsign} has {PilotInjuryHolder.HolderInstance.injuryStat} injuries before InjurePilot; proceeding.");
            }

            public static void Postfix(Pilot __instance, string sourceID, int stackItemUID, int dmg,
                DamageType damageType, Weapon sourceWeapon, AbstractActor sourceActor)
            {
                if (PilotInjuryHolder.HolderInstance.injuryStat ==
                    __instance.StatCollection.GetValue<int>("Injuries") &&
                    !ModInit.modSettings.enablePainToleranceInjuries)
                {
                    ModInit.modLog.LogMessage(
                        $"{__instance.Callsign} still has {PilotInjuryHolder.HolderInstance.injuryStat} injuries; aborting.");
                    return;
                }

                PilotInjuryHolder.HolderInstance.injuryStat = 0; //wait...why?

                if (ModInit.modSettings.enableInternalDmgInjuries &&
                    __instance.ParentActor.StatCollection.GetValue<bool>(ModInit.modSettings.internalDmgStatName) && __instance.StatCollection.GetValue<bool>("NeedsFeedbackInjury"))
                {
                    ModInit.modLog.LogMessage(
                        $"Rolling neural feedback injury with {dmg} damage for {__instance.Callsign}");
                    PilotInjuryManager.ManagerInstance.rollInjuryFeedback(__instance, dmg, damageType);
                    
                    return;
                }

                else
                {
                    ModInit.modLog.LogMessage($"Rolling standard injury with {dmg} damage for {__instance.Callsign}");
                    PilotInjuryManager.ManagerInstance.rollInjury(__instance, dmg, damageType);
                    
                }

                if ((ModInit.modSettings.cripplingInjuriesThreshold > 0 ||
                     ModInit.modSettings.missionKillSeverityThreshold > 0) &&
                    (damageType != DamageType.Unknown || damageType != DamageType.NOT_SET)
                ) //now trying to add up "severity" threshold for crippled injury or mission kill for pain
                {
                    var pKey = __instance.FetchGUID();
                    var injuryList = new List<Injury>();
                    foreach (var id in PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey])
                    {
                        injuryList.AddRange(
                            PilotInjuryManager.ManagerInstance.InjuryEffectsList.Where(x => x.injuryID == id));
                    }

                    if (ModInit.modSettings.cripplingInjuriesThreshold > 0)
                    {


                        var groupedLocs = injuryList.GroupBy(x => x.injuryLoc);

                        
                        foreach (var injuryLoc in groupedLocs)
                        {
                            var t = new int();

                            t = injuryLoc.Sum(x => x.severity);


                            if (t >= ModInit.modSettings.cripplingInjuriesThreshold)
                            {
                                PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey]
                                    .Add(CRIPPLED.injuryID);
                                __instance.pilotDef.PilotTags.Add(CrippledTag);
                                ModInit.modLog.LogMessage($"{__instance.Callsign} has been Crippled!");

                                if (ModInit.modSettings.enableLethalTorsoHead && (injuryLoc.Key == InjuryLoc.Head ||
                                    injuryLoc.Key == InjuryLoc.Torso))
                                {
                                    __instance.StatCollection.ModifyStat<bool>("TBAS_Injuries", 0, "LethalInjury",
                                        StatCollection.StatOperation.Set, true, -1, true);
                                    ModInit.modLog.LogMessage($"{__instance.Callsign} has crippled Torso or Head; lethal injury!");
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
                if (__instance.pilotDef.PilotTags.Contains(CrippledTag) ||
                    (__instance.StatCollection.GetValue<int>(MissionKilledStat) >=
                     ModInit.modSettings.missionKillSeverityThreshold))
                {
                    __result = true;
                }
            }
        }

        [HarmonyPatch(typeof(Contract), "FinalizeKilledMechWarriors", typeof(SimGameState))]
        [HarmonyAfter(new string[] { "co.uk.cwolf.MissionControl" })]
        public class ContractFinalizeKilledMechwarriorsPatch
        {
            private static MethodInfo pushReport = AccessTools.Method(typeof(Contract), "PushReport");
            private static MethodInfo popReport = AccessTools.Method(typeof(Contract), "PopReport");
            private static MethodInfo reportLog = AccessTools.Method(typeof(Contract), "ReportLog");
            public static bool Prefix(Contract __instance)
            {
                pushReport.Invoke(__instance, new object[]{ "MechWarriorFinalizeKill" });
                foreach (UnitResult unitResult in __instance.PlayerUnitResults)
                {
                    Pilot pilot = unitResult.pilot;
                    PilotDef pilotDef = pilot.pilotDef;
                    if (!unitResult.pilot.IsIncapacitated || unitResult.pilot.pilotDef.IsImmortal)
                    {
                        if (pilotDef != null)
                        {
                            pilotDef.SetRecentInjuryDamageType(DamageType.NOT_SET);
                        }
                    }

                    else if ((unitResult.pilot.StatCollection.GetValue<int>(MissionKilledStat) >=
                             ModInit.modSettings.missionKillSeverityThreshold || unitResult.pilot.pilotDef.PilotTags.Contains(CrippledTag)) && (unitResult.pilot.Injuries < unitResult.pilot.Health && !unitResult.pilot.LethalInjuries))

                    {
                        return false;
                    }

                    else
                    {
                        float num = pilot.LethalInjuries ? sim.Constants.Pilot.LethalDeathChance : sim.Constants.Pilot.IncapacitatedDeathChance;
                        num = Mathf.Max(0f, num - sim.Constants.Pilot.GutsDeathReduction * (float)pilot.Guts);
                        float num2 = sim.NetworkRandom.Float(0f, 1f);
                        string s = string.Format("Pilot {0} needs to roll above {1} to survive. They roll {2} resulting in {3}", new object[]
                        {
                            pilot.Name,
                            num,
                            num2,
                            (num2 < num) ? "DEATH" : "LIFE"
                        });
                        reportLog.Invoke(__instance, new object[] { s });
                        if (num2 < num)
                        {
                            __instance.KilledPilots.Add(pilot);
                        }
                        else if (pilotDef != null)
                        {
                            pilotDef.SetRecentInjuryDamageType(DamageType.NOT_SET);
                        }
                    }
                }
                popReport.Invoke(__instance, new object[] {});
                return false;
            }
        }



        [HarmonyPatch(typeof(SimGameState))]
        [HarmonyPatch("GetInjuryCost", typeof(Pilot))]
        public static class SimGameState_GetInjuryCost_Patch
        {
            public static bool Prepare()
            {
                return ModInit.modSettings.injuryHealTimeMultiplier > 0f;
            }

            public static void Postfix(SimGameState __instance, ref int __result, Pilot p)
            {
                var pKey = p.FetchGUID();
                var injuryList = new List<Injury>();
                foreach (var id in PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey])
                {
                    injuryList.AddRange(
                        PilotInjuryManager.ManagerInstance.InjuryEffectsList.Where(x => x.injuryID == id));
                }

                var sev = Math.Max((injuryList.Sum(x => x.severity) - 1), 0);

                __result = Mathf.RoundToInt(__result * ModInit.modSettings.injuryHealTimeMultiplier) +
                           (sev * ModInit.modSettings.severityCost);
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
                    ModInit.modLog.LogMessage($"{__instance.Callsign} has been healed, but is still CRIPPLED!");
                }
                else
                {
                    PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey]
                        .Clear();
                    ModInit.modLog.LogMessage($"{__instance.Callsign} has been healed!!");
                }
            }
        }

        [HarmonyPatch(typeof(Mech))]
        [HarmonyPatch("ApplyStructureStatDamage",
            new Type[] {typeof(ChassisLocations), typeof(float), typeof(WeaponHitInfo)})]

        public static class Mech_ApplyStructureStatDamage
        {
            public static bool Prepare()
            {
                return ModInit.modSettings.enableInternalDmgInjuries;
            }

            public static void Postfix(Mech __instance, ChassisLocations location, float damage)
            {
                var p = __instance.GetPilot();
                var internalDmgInjuryCount = p.StatCollection.GetValue<int>("internalDmgInjuryCount");

                

                if ((ModInit.modSettings.internalDmgInjuryLocs.Contains(location) ||
                     ModInit.modSettings.internalDmgInjuryLocs.Capacity == 0) &&
                    damage >= ModInit.modSettings.internalDmgLvlReq &&
                    __instance.StatCollection.GetValue<bool>(ModInit.modSettings.internalDmgStatName) &&
                    (internalDmgInjuryCount <
                     ModInit.modSettings.internalDmgInjuryLimit) || ModInit.modSettings.internalDmgInjuryLimit < 1)

                {
                    ModInit.modLog.LogMessage($"{p.Callsign} has {internalDmgInjuryCount} preexisting feedback injuries!");

                    p.StatCollection.ModifyStat<bool>(p.FetchGUID(), 0, "NeedsFeedbackInjury",
                        StatCollection.StatOperation.Set, true, -1, true);

                    ModInit.modLog.LogMessage($"{internalDmgInjuryCount} is < {ModInit.modSettings.internalDmgInjuryLimit}! Injuring {p.Callsign} from structure damage!");

                    p.InjurePilot(p.FetchGUID(), -1, 1, DamageType.ComponentExplosion, null, null);

                    p.StatCollection.ModifyStat<int>(p.FetchGUID(), 0, "internalDmgInjuryCount",
                        StatCollection.StatOperation.Int_Add, 1, -1, true);

                    p.StatCollection.ModifyStat<bool>(p.FetchGUID(), 0, "NeedsFeedbackInjury",
                        StatCollection.StatOperation.Set, false, -1, true);
                }
            }
        }
    }
}