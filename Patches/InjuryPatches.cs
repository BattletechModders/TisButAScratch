using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Text;
using Harmony;
using BattleTech;
using TisButAScratch.Framework;
using static TisButAScratch.Framework.GlobalVars;
using System.Threading.Tasks;
using UnityEngine;

using CustomComponents;
using CustAmmoCategories;

namespace TisButAScratch.Patches
{
    class InjuryPatches
    {
        // this should hopefully cause vehile injuries and injuries when certain components are damaged/destroyeded. also implements "forced ejections" for pilots.
        [HarmonyPatch(typeof(MechComponent), "DamageComponent",
            new Type[]
            {
                typeof(WeaponHitInfo), typeof(ComponentDamageLevel), typeof(bool)
            })]
        public static class MechComponent_DamageComponent_Patch
        {
            public static void Postfix(MechComponent __instance, WeaponHitInfo hitInfo,
                ComponentDamageLevel damageLevel, bool applyEffects)
            {

                if (__instance.parent.StatCollection.GetValue<bool>(ModInit.modSettings.isTorsoMountStatName) &&
                    (__instance.parent is Mech && (__instance.LocationDef.Location & ChassisLocations.Head) != 0))
                {
                    ModInit.modLog.LogMessage($"Head hit, but cockpit components not located in head!");
                    return;
                }

                if (ModInit.modSettings.lifeSupportSupportsLifeTM &&
                    __instance.parent.StatCollection.GetValue<bool>(ModInit.modSettings.isTorsoMountStatName) &&
                    ModInit.modSettings.lifeSupportCustomID.Any
                        (x => __instance.componentDef.GetComponents<Category>().Any(c => c.CategoryID == x)))
                {
                    if (damageLevel == ComponentDamageLevel.Penalized)
                    {
                        ModInit.modLog.LogMessage($"Life support damaged with Torso-Mount Cockpit! {__instance.parent.GetPilot().Callsign} is being cooked!");
                        __instance.parent.GetPilot().SetNeedsInjury(InjuryReason.ComponentExplosion);
                        return;
                    }
                    if (damageLevel == ComponentDamageLevel.Destroyed)
                    {
                        ModInit.modLog.LogMessage($"Life support destroyed with Torso-Mount Cockpit! {__instance.parent.GetPilot().Callsign} is well-done!");
                        __instance.parent.GetPilot().LethalInjurePilot(__instance.parent.Combat.Constants, hitInfo.attackerId, hitInfo.stackItemUID, true, DamageType.OverheatSelf, null, null);
                        return;
                    }
                }

                if (((__instance.parent is Mech && (__instance.LocationDef.Location & ChassisLocations.Head) == 0) || __instance.parent is Vehicle) && ModInit.modSettings.crewOrCockpitCustomID.Any
                    (x => __instance.componentDef.GetComponents<Category>().Any(c => c.CategoryID == x)) && (damageLevel == ComponentDamageLevel.Penalized || damageLevel == ComponentDamageLevel.Destroyed))
                {
                    ModInit.modLog.LogMessage($"Cockpit components damaged/destroyed, pilot needs injury!");
                    __instance.parent.GetPilot().SetNeedsInjury(InjuryReason.ComponentExplosion);
                    return;
                }

                if (__instance.parent is Mech && !__instance.parent.StatCollection.GetValue<bool>(ModInit.modSettings.isTorsoMountStatName) && ModInit.modSettings.hostileEnvironmentsEject &&
                    ModInit.modSettings.lifeSupportCustomID.Any
                        (x => __instance.componentDef.GetComponents<Category>().Any(c => c.CategoryID == x)) &&
                    (damageLevel == ComponentDamageLevel.Destroyed) && ModInit.modSettings.hostileEnvironments.Any(x => x == __instance.parent.Combat.MapMetaData.biomeDesignMask.Id))
                {
                    ModInit.modLog.LogMessage($"Life support destroyed in hostile environment! {__instance.parent.GetPilot().Callsign} ejecting!");
                    __instance.parent.EjectPilot("LIFESUPPORTDESTROYED", 0, DeathMethod.PilotEjection, false);
                }
            }
        }

        [HarmonyPatch(typeof(Mech), "ApplyHeadStructureEffects",
            new Type[]
            {
                typeof(ChassisLocations), typeof(LocationDamageLevel), typeof(LocationDamageLevel), typeof(WeaponHitInfo)
            })]
        [HarmonyPriority(Priority.First)]
        public static class Mech_ApplyHeadStructureEffects
        {
            public static bool Prefix(Mech __instance, ChassisLocations location, LocationDamageLevel oldDamageLevel,
                LocationDamageLevel newDamageLevel, WeaponHitInfo hitInfo)
            {
                if (__instance.StatCollection.GetValue<bool>(ModInit.modSettings.isTorsoMountStatName))
                {
                    ModInit.modLog.LogMessage($"Head hit, but cockpit not located in head! No injury should be sustained!");
                    __instance.GetPilot().SetNeedsInjury(InjuryReason.NotSet);
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        [HarmonyPatch(typeof(Vehicle), "CheckPilotStatus",
            new Type[]
            {
                typeof(float), typeof(int), typeof(string)
            })]
        public static class Vehicle_CheckPilotStatus_Patch
        {
            public static void Prefix(Vehicle __instance, float gutsRoll, int stackItemID, string sourceID)
            {
                var p = __instance.GetPilot();
                if (p.NeedsInjury)
                {
                    ModInit.modLog.LogMessage($"Injuring {__instance.Description.UIName} pilot {__instance.GetPilot().Callsign} due to crew compartment damage!");
                    __instance.GetPilot().InjurePilot(sourceID, stackItemID, 1, DamageType.ComponentExplosion, null, null);
                }
            }
        }

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
                var pKey = __instance.FetchGUID();

                if (PilotInjuryHolder.HolderInstance.injuryStat >= //changed to <= instead of == 1021
                    __instance.StatCollection.GetValue<int>("Injuries"))
                {
                    ModInit.modLog.LogMessage(
                        $"{__instance.Callsign}{pKey} still has {PilotInjuryHolder.HolderInstance.injuryStat} injuries; aborting.");
                    return;
                }

                if (ModInit.modSettings.enableInternalDmgInjuries &&
                    __instance.ParentActor.StatCollection.GetValue<bool>(ModInit.modSettings.internalDmgStatName) &&
                    __instance.StatCollection.GetValue<bool>("NeedsFeedbackInjury"))
                {
                    ModInit.modLog.LogMessage(
                        $"Rolling neural feedback injury with {dmg} damage for {__instance.Callsign}{pKey}");
                    PilotInjuryManager.ManagerInstance.rollInjuryFeedback(__instance, dmg, damageType);

                    return;
                }

                else
                {
                    ModInit.modLog.LogMessage($"Rolling standard injury with {dmg} damage for {__instance.Callsign}{pKey}");
                    PilotInjuryManager.ManagerInstance.rollInjury(__instance, dmg, damageType);

                }

                if ((ModInit.modSettings.debilSeverityThreshold > 0 ||
                     ModInit.modSettings.missionKillSeverityThreshold > 0) &&
                    (damageType != DamageType.Unknown && damageType != DamageType.NOT_SET)
                ) //now trying to add up "severity" threshold for crippled injury or mission kill for pain
                {
                    
                    var injuryList = new List<Injury>();
                    foreach (var id in PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey])
                    {
                        injuryList.AddRange(
                            PilotInjuryManager.ManagerInstance.InjuryEffectsList.Where(x => x.injuryID == id));
                    }
                    foreach (var id in PilotInjuryHolder.HolderInstance.combatInjuriesMap[pKey])
                    {
                        injuryList.AddRange(
                            PilotInjuryManager.ManagerInstance.InjuryEffectsList.Where(x => x.injuryID == id));
                    }

                    if (ModInit.modSettings.debilSeverityThreshold > 0)
                    {


                        var groupedLocs = injuryList.GroupBy(x => x.injuryLoc);


                        foreach (var injuryLoc in groupedLocs)
                        {
                            var t = injuryLoc.Sum(x => x.severity);


                            if (t >= ModInit.modSettings.debilSeverityThreshold)
                            {
                                __instance.pilotDef.PilotTags.Add(DEBILITATEDTAG);
                                ModInit.modLog.LogMessage($"{__instance.Callsign}{pKey} has been debilitated!");

                                if (ModInit.modSettings.enableLethalTorsoHead && (injuryLoc.Key == InjuryLoc.Head ||
                                    injuryLoc.Key == InjuryLoc.Torso))
                                {
                                    __instance.StatCollection.ModifyStat<bool>("TBAS_Injuries", 0, "LethalInjury",
                                        StatCollection.StatOperation.Set, true, -1, true);
                                    ModInit.modLog.LogMessage(
                                        $"{__instance.Callsign}{pKey} has debilitated Torso or Head; lethal injury!");
                                }
                            }
                        }
                    }
                }
            }
        }


        [HarmonyPatch(typeof(Pilot))]
        [HarmonyPatch("CanPilot", MethodType.Getter)]
        [HarmonyPriority(Priority.Last)]
        public static class Pilot_CanPilot_Patch
        {
            public static void Postfix(Pilot __instance, ref bool __result)
            {
                __result = true;
                if (__instance.pilotDef.PilotTags.Contains(DEBILITATEDTAG) || (__instance.pilotDef.TimeoutRemaining > 0))
                {
                    __result = false;
                }
            }
        }

        [HarmonyPatch(typeof(Pilot))]
        [HarmonyPatch("IsIncapacitated", MethodType.Getter)]
        [HarmonyPriority(Priority.Last)]
        public static class Pilot_IsIncapacitated_Patch
        {
            public static void Postfix(Pilot __instance, ref bool __result)
            {
                if (ModInit.modSettings.debilIncapacitates && __instance.pilotDef.PilotTags.Contains(DEBILITATEDTAG) || 
                    __instance.StatCollection.GetValue<bool>("BledOut") ||
                    (__instance.StatCollection.GetValue<int>(MissionKilledStat) >= ModInit.modSettings.missionKillSeverityThreshold && ModInit.modSettings.missionKillSeverityThreshold > 0))
                {
                    __result = true;
                }
            }
        }

        [HarmonyPatch(typeof(Contract), "FinalizeKilledMechWarriors", typeof(SimGameState))]
        [HarmonyAfter(new string[] {"co.uk.cwolf.MissionControl"})]
        public class ContractFinalizeKilledMechwarriorsPatch
        {
            private static MethodInfo pushReport = AccessTools.Method(typeof(Contract), "PushReport");
            private static MethodInfo popReport = AccessTools.Method(typeof(Contract), "PopReport");
            private static MethodInfo reportLog = AccessTools.Method(typeof(Contract), "ReportLog");

            public static bool Prefix(Contract __instance)
            {
                pushReport.Invoke(__instance, new object[] {"MechWarriorFinalizeKill"});
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
                              ModInit.modSettings.missionKillSeverityThreshold ||
                              unitResult.pilot.pilotDef.PilotTags.Contains(DEBILITATEDTAG)) &&
                             (unitResult.pilot.Injuries < unitResult.pilot.Health && !unitResult.pilot.LethalInjuries) || (unitResult.pilot.StatCollection.GetValue<bool>("BledOut") && !ModInit.modSettings.BleedingOutLethal))

                    {
                        return false;
                    }

                    else
                    {
                        float num = pilot.LethalInjuries
                            ? sim.Constants.Pilot.LethalDeathChance
                            : sim.Constants.Pilot.IncapacitatedDeathChance;
                        num = Mathf.Max(0f, num - sim.Constants.Pilot.GutsDeathReduction * (float) pilot.Guts);
                        float num2 = sim.NetworkRandom.Float(0f, 1f);
                        string s = string.Format(
                            "Pilot {0} needs to roll above {1} to survive. They roll {2} resulting in {3}", new object[]
                            {
                                pilot.Name,
                                num,
                                num2,
                                (num2 < num) ? "DEATH" : "LIFE"
                            });
                        reportLog.Invoke(__instance, new object[] {s});
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

                popReport.Invoke(__instance, new object[] { });
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
                if (p.pilotDef.Injuries == 0) //this should hopefully fix non-injury timeouts being weird due to multipliying injury cost. #hbswhy
                {
                    __result =  (p.pilotDef.TimeoutRemaining * __instance.GetDailyHealValue());
                    return;
                }

                var pKey = p.FetchGUID();
                var injuryList = new List<Injury>();
                foreach (var id in PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey])
                {
                    injuryList.AddRange(
                        PilotInjuryManager.ManagerInstance.InjuryEffectsList.Where(x => x.injuryID == id));
                }

                var sev = Math.Max((injuryList.Sum(x => x.severity) - 1), 0);
                var crippled = 0f;
                if (p.pilotDef.PilotTags.Contains(DEBILITATEDTAG) && ModInit.modSettings.timeHealsAllWounds)
                {
                    crippled = (float) (ModInit.modSettings.debilitatedCost / (ModInit.modSettings.medtechDebilMultiplier * __instance.MedTechSkill));
                }

                __result = Mathf.RoundToInt((__result * ModInit.modSettings.injuryHealTimeMultiplier) + (sev * ModInit.modSettings.severityCost) + crippled);
            }
        }

        [HarmonyPatch(typeof(Pilot))]
        [HarmonyPatch("ClearInjuries", new Type[] {typeof(string), typeof(int), typeof(string)})]
        public static class Pilot_ClearInjuries_Patch
        {
            public static void Postfix(Pilot __instance)
            {
                var pKey = __instance.FetchGUID();
                PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey].Clear();
                if (ModInit.modSettings.timeHealsAllWounds)
                {
                    __instance.pilotDef.PilotTags.Remove(DEBILITATEDTAG);
                }
                ModInit.modLog.LogMessage($"{__instance.Callsign} has been healed!!");
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
                var pKey = p.FetchGUID();
                var internalDmgInjuryCount = p.StatCollection.GetValue<int>("internalDmgInjuryCount");

                if ((ModInit.modSettings.internalDmgInjuryLocs.Contains(location) ||
                     ModInit.modSettings.internalDmgInjuryLocs.Capacity == 0) &&
                    damage >= ModInit.modSettings.internalDmgLvlReq &&
                    __instance.StatCollection.GetValue<bool>(ModInit.modSettings.internalDmgStatName) &&
                    (internalDmgInjuryCount <
                     ModInit.modSettings.internalDmgInjuryLimit) || ModInit.modSettings.internalDmgInjuryLimit < 1)

                {
                    ModInit.modLog.LogMessage(
                        $"{p.Callsign}{pKey} has {internalDmgInjuryCount} preexisting feedback injuries!");

                    p.StatCollection.ModifyStat<bool>(p.FetchGUID(), 0, "NeedsFeedbackInjury",
                        StatCollection.StatOperation.Set, true, -1, true);

                    ModInit.modLog.LogMessage(
                        $"{internalDmgInjuryCount} is < {ModInit.modSettings.internalDmgInjuryLimit}! Injuring {p.Callsign}{pKey} from structure damage!");

                    p.InjurePilot(p.FetchGUID(), -1, 1, DamageType.ComponentExplosion, null, null);

                    p.StatCollection.ModifyStat<int>(p.FetchGUID(), 0, "internalDmgInjuryCount",
                        StatCollection.StatOperation.Int_Add, 1, -1, true);

                    p.StatCollection.ModifyStat<bool>(p.FetchGUID(), 0, "NeedsFeedbackInjury",
                        StatCollection.StatOperation.Set, false, -1, true);
                }
            }
        }

        [HarmonyPatch(typeof(Effect))]
        [HarmonyPatch("OnEffectExpiration")]
        public static class Effect_OnEffectExpiration_Patch
        {
            public static void Postfix(Effect __instance)
            {

                if (__instance.id.EndsWith(ModInit.modSettings.BleedingOutSuffix) && __instance.Target is AbstractActor target)
                {
                    var p = target.GetPilot();
                    var pKey = p.FetchGUID();
                    p.StatCollection.ModifyStat<bool>("TBAS_Injuries", 0, "BledOut",
                        StatCollection.StatOperation.Set, true, -1, true);
                    ModInit.modLog.LogMessage(
                        $"{p.Callsign}{pKey} has bled out!");

                    target.FlagForDeath("Bled Out", DeathMethod.PilotKilled, DamageType.Unknown, 1, 1, p.FetchGUID(), true);

                    if (ModInit.modSettings.BleedingOutLethal) p.StatCollection.ModifyStat<bool>("TBAS_Injuries", 0, "LethalInjury",
                        StatCollection.StatOperation.Set, true, -1, true);
                }
            }
        }
    }
}