using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Harmony;
using BattleTech;
using TisButAScratch.Framework;
using static TisButAScratch.Framework.GlobalVars;
using UnityEngine;

using CustomComponents;
using Localize;
using Logger = TisButAScratch.Framework.Logger;

namespace TisButAScratch.Patches
{
    public class InjuryPatches
    {
        [HarmonyPatch(typeof(Vehicle), "DamageLocation")]
        public static class Vehicle_DamageLocation
        {
            public static bool Prepare() => ModInit.modSettings.injureVehiclePilotOnDestroy != "OFF";
            public static void Prefix(Vehicle __instance, WeaponHitInfo hitInfo, int originalHitLoc, VehicleChassisLocations vLoc, Weapon weapon, float totalArmorDamage, float directStructureDamage, AttackImpactQuality impactQuality)
            {
                var currentStructure = __instance.GetCurrentStructure(vLoc);
                if (currentStructure <= 0f) return;

                var incomingStructureDamage = directStructureDamage + totalArmorDamage - __instance.GetCurrentArmor(vLoc);

                if (currentStructure - incomingStructureDamage <= 0f)
                {
                    ModInit.modLog.LogMessage($"Vehicle location will be destroyed; currentStructure at location {currentStructure}, will take {incomingStructureDamage} damage");
                    var pilot = __instance.GetPilot();
                    if (ModInit.modSettings.injureVehiclePilotOnDestroy == "MAX")
                    {
                        ModInit.modLog.LogMessage($"Vehicle location destroyed, MaxInjure {pilot.Callsign} {pilot.FetchGUID()} due to injureVehiclePilotOnDestroy = MAX");
                        pilot.SetNeedsInjury(InjuryReason.ActorDestroyed);
                        pilot.MaxInjurePilot(__instance.Combat.Constants, hitInfo.attackerId, hitInfo.stackItemUID,
                            DamageType.Combat, weapon, __instance.Combat.FindActorByGUID(hitInfo.attackerId));
                        if (pilot.IsIncapacitated)
                        {
                            __instance.FlagForDeath("Pilot Killed", DeathMethod.PilotKilled, DamageType.Combat, 1, hitInfo.stackItemUID, hitInfo.attackerId, false);
                            __instance.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(new ShowActorInfoSequence(__instance, Strings.T("PILOT INCAPACITATED!"), FloatieMessage.MessageNature.PilotInjury, true)));
                            __instance.HandleDeath(hitInfo.attackerId);
                        }
                        pilot.ClearNeedsInjury();
                    }
                    else if (ModInit.modSettings.injureVehiclePilotOnDestroy == "HIGH")
                    {
                        var dmg = pilot.Health - 1;
                        ModInit.modLog.LogMessage($"Vehicle location destroyed, Injuring {pilot.Callsign} {pilot.FetchGUID()} for {dmg} due to injureVehiclePilotOnDestroy = HIGH");
                        pilot.SetNeedsInjury(InjuryReason.ActorDestroyed);
                        pilot.InjurePilot(hitInfo.attackerId, hitInfo.stackItemUID, dmg,
                            DamageType.Combat, weapon, __instance.Combat.FindActorByGUID(hitInfo.attackerId));
                        if (!pilot.IsIncapacitated)
                        {
                            if (__instance.team.LocalPlayerControlsTeam)
                            {
                                AudioEventManager.PlayAudioEvent("audioeventdef_musictriggers_combat", "friendly_warrior_injured", null, null);
                            }
                            else
                            {
                                AudioEventManager.PlayAudioEvent("audioeventdef_musictriggers_combat", "enemy_warrior_injured", null, null);
                            }
                            IStackSequence sequence;
                            if (pilot.Injuries == 0)
                            {
                                sequence = new ShowActorInfoSequence(__instance, Strings.T("{0}: INJURY IGNORED", new object[]
                                {
                                    pilot.InjuryReasonDescription
                                }), FloatieMessage.MessageNature.PilotInjury, true);
                            }
                            else
                            {
                                sequence = new ShowActorInfoSequence(__instance, Strings.T("{0}: PILOT INJURED", new object[]
                                {
                                    pilot.InjuryReasonDescription
                                }), FloatieMessage.MessageNature.PilotInjury, true);
                                AudioEventManager.SetPilotVOSwitch<AudioSwitch_dialog_dark_light>(AudioSwitch_dialog_dark_light.dark, __instance);
                                AudioEventManager.PlayPilotVO(VOEvents.Pilot_TakeDamage, __instance, null, null, true);
                            }
                            __instance.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(sequence));
                        }
                        pilot.ClearNeedsInjury();
                        if (pilot.IsIncapacitated)
                        {
                            __instance.FlagForDeath("Pilot Killed", DeathMethod.PilotKilled, DamageType.Combat, 1, hitInfo.stackItemUID, hitInfo.attackerId, false);
                            __instance.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(new ShowActorInfoSequence(__instance, Strings.T("PILOT INCAPACITATED!"), FloatieMessage.MessageNature.PilotInjury, true)));
                            __instance.HandleDeath(hitInfo.attackerId);
                        }
                    }
                    else if (ModInit.modSettings.injureVehiclePilotOnDestroy == "SINGLE")
                    {
                        ModInit.modLog.LogMessage($"Vehicle location destroyed, Injuring {pilot.Callsign} {pilot.FetchGUID()} for 1 due to injureVehiclePilotOnDestroy = SINGLE");
                        pilot.SetNeedsInjury(InjuryReason.ActorDestroyed);
                        pilot.InjurePilot(hitInfo.attackerId, hitInfo.stackItemUID, 1,
                            DamageType.Combat, weapon, __instance.Combat.FindActorByGUID(hitInfo.attackerId));
                        if (!pilot.IsIncapacitated)
                        {
                            if (__instance.team.LocalPlayerControlsTeam)
                            {
                                AudioEventManager.PlayAudioEvent("audioeventdef_musictriggers_combat", "friendly_warrior_injured", null, null);
                            }
                            else
                            {
                                AudioEventManager.PlayAudioEvent("audioeventdef_musictriggers_combat", "enemy_warrior_injured", null, null);
                            }
                            IStackSequence sequence;
                            if (pilot.Injuries == 0)
                            {
                                sequence = new ShowActorInfoSequence(__instance, Strings.T("{0}: INJURY IGNORED", new object[]
                                {
                                    pilot.InjuryReasonDescription
                                }), FloatieMessage.MessageNature.PilotInjury, true);
                            }
                            else
                            {
                                sequence = new ShowActorInfoSequence(__instance, Strings.T("{0}: PILOT INJURED", new object[]
                                {
                                    pilot.InjuryReasonDescription
                                }), FloatieMessage.MessageNature.PilotInjury, true);
                                AudioEventManager.SetPilotVOSwitch<AudioSwitch_dialog_dark_light>(AudioSwitch_dialog_dark_light.dark, __instance);
                                AudioEventManager.PlayPilotVO(VOEvents.Pilot_TakeDamage, __instance, null, null, true);
                            }
                            __instance.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(sequence));
                        }
                        pilot.ClearNeedsInjury();
                        if (pilot.IsIncapacitated)
                        {
                            __instance.FlagForDeath("Pilot Killed", DeathMethod.PilotKilled, DamageType.Combat, 1, hitInfo.stackItemUID, hitInfo.attackerId, false);
                            __instance.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(new ShowActorInfoSequence(__instance, Strings.T("PILOT INCAPACITATED!"), FloatieMessage.MessageNature.PilotInjury, true)));
                            __instance.HandleDeath(hitInfo.attackerId);
                        }
                    }
                }
            }
        }

        // this should hopefully cause vehile injuries and injuries when certain components are damaged/destroyeded. also implements "forced ejections" for pilots.
        [HarmonyPatch(typeof(MechComponent), "DamageComponent",
            new Type[]
            {
                typeof(WeaponHitInfo), typeof(ComponentDamageLevel), typeof(bool)
            })]
        public static class MechComponent_DamageComponent_Patch
        {
            public static bool Prepare()
            {
                return (ModInit.modSettings.lifeSupportCustomID.Count != 0 &&
                        ModInit.modSettings.crewOrCockpitCustomID.Count != 0);
            }

            public static void Postfix(MechComponent __instance, WeaponHitInfo hitInfo,
                ComponentDamageLevel damageLevel, bool applyEffects)
            {
                var pilot = __instance.parent.GetPilot();
                if (__instance.parent.StatCollection.GetValue<bool>(ModInit.modSettings.isTorsoMountStatName) &&
                    (__instance.parent is Mech && (__instance.LocationDef.Location & ChassisLocations.Head) != 0))
                {
                    ModInit.modLog.LogMessage($"Head hit, but cockpit components not located in head!");
                    return;
                }

                if ((__instance.parent is Mech && (__instance.LocationDef.Location & ChassisLocations.Head) == 0 ||
                     __instance.parent is Vehicle) && ModInit.modSettings.crewOrCockpitCustomID.Any
                        (x => __instance.componentDef.GetComponents<Category>().Any(c => c.CategoryID == x)) &&
                    (damageLevel == ComponentDamageLevel.Penalized))
                {
                    ModInit.modLog.LogMessage(
                        $"Cockpit component ({__instance.Description.UIName}) damaged, pilot needs injury!");
//                    pilot.SetNeedsInjury(InjuryReason.ComponentExplosion);

                    pilot.InjurePilot(hitInfo.attackerId, hitInfo.stackItemUID, 1, DamageType.Combat, null, pilot.ParentActor.Combat.FindActorByGUID(hitInfo.attackerId));
                    if (!pilot.IsIncapacitated)
                    {
                        if (pilot.ParentActor.team.LocalPlayerControlsTeam)
                        {
                            AudioEventManager.PlayAudioEvent("audioeventdef_musictriggers_combat", "friendly_warrior_injured", null, null);
                        }
                        else
                        {
                            AudioEventManager.PlayAudioEvent("audioeventdef_musictriggers_combat", "enemy_warrior_injured", null, null);
                        }
                        IStackSequence sequence;
                        if (pilot.Injuries == 0)
                        {
                            sequence = new ShowActorInfoSequence(pilot.ParentActor, Strings.T("{0}: INJURY IGNORED", new object[]
                            {
                                    pilot.InjuryReasonDescription
                            }), FloatieMessage.MessageNature.PilotInjury, true);
                        }
                        else
                        {
                            sequence = new ShowActorInfoSequence(pilot.ParentActor, Strings.T("{0}: PILOT INJURED", new object[]
                            {
                                    pilot.InjuryReasonDescription
                            }), FloatieMessage.MessageNature.PilotInjury, true);
                            AudioEventManager.SetPilotVOSwitch<AudioSwitch_dialog_dark_light>(AudioSwitch_dialog_dark_light.dark, pilot.ParentActor);
                            AudioEventManager.PlayPilotVO(VOEvents.Pilot_TakeDamage, pilot.ParentActor, null, null, true);
                        }
                        pilot.ParentActor.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(sequence));
                    }
                    pilot.ClearNeedsInjury();
                    if (pilot.IsIncapacitated)
                    {
                        pilot.ParentActor.FlagForDeath("Pilot Killed", DeathMethod.PilotKilled, DamageType.Combat, 1, hitInfo.stackItemUID, hitInfo.attackerId, false);
                        pilot.ParentActor.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(new ShowActorInfoSequence(pilot.ParentActor, Strings.T("PILOT INCAPACITATED!"), FloatieMessage.MessageNature.PilotInjury, true)));
                        pilot.ParentActor.HandleDeath(hitInfo.attackerId);
                    }
                    return;
                }

                if ((__instance.parent is Mech && (__instance.LocationDef.Location & ChassisLocations.Head) == 0 ||
                     __instance.parent is Vehicle) && ModInit.modSettings.crewOrCockpitCustomID.Any
                        (x => __instance.componentDef.GetComponents<Category>().Any(c => c.CategoryID == x)) &&
                    (damageLevel == ComponentDamageLevel.Destroyed))
                {
                    ModInit.modLog.LogMessage(
                        $"Cockpit component ({__instance.Description.UIName}) destroyed, pilot needs injury!");
                    pilot.MaxInjurePilot(__instance.parent.Combat.Constants, hitInfo.attackerId, hitInfo.stackItemUID, DamageType.ComponentExplosion, null, __instance.parent.Combat.FindActorByGUID(hitInfo.attackerId));
                    __instance.parent.FlagForDeath("Injuries", DeathMethod.CenterTorsoDestruction, DamageType.HeadShot, 1, 1, hitInfo.attackerId, true);
                    __instance.parent.HandleDeath(hitInfo.attackerId);
                    return;
                }


                if (ModInit.modSettings.lifeSupportSupportsLifeTM &&
                    __instance.parent.StatCollection.GetValue<bool>(ModInit.modSettings.isTorsoMountStatName) &&
                    ModInit.modSettings.lifeSupportCustomID.Any
                        (x => __instance.componentDef.GetComponents<Category>().Any(c => c.CategoryID == x)))
                {
                    if (pilot.pilotDef.PilotTags.Contains(ModInit.modSettings.pilotPainShunt))
                    {
                        ModInit.modLog.LogMessage($"Pilot {pilot.Callsign} has {ModInit.modSettings.pilotPainShunt}, ignoring injury from life support damage.");
                        return;
                    }
                    if (damageLevel == ComponentDamageLevel.Penalized)
                    {
                        ModInit.modLog.LogMessage($"Life support ({__instance.Description.UIName}) damaged with Torso-Mount Cockpit! {pilot.Callsign} is being cooked!");
                        //pilot.SetNeedsInjury(InjuryReason.ComponentExplosion);
                        pilot.InjurePilot(hitInfo.attackerId, hitInfo.stackItemUID, 1, DamageType.Combat, null, pilot.ParentActor.Combat.FindActorByGUID(hitInfo.attackerId));
                        if (!pilot.IsIncapacitated)
                        {
                            if (pilot.ParentActor.team.LocalPlayerControlsTeam)
                            {
                                AudioEventManager.PlayAudioEvent("audioeventdef_musictriggers_combat", "friendly_warrior_injured", null, null);
                            }
                            else
                            {
                                AudioEventManager.PlayAudioEvent("audioeventdef_musictriggers_combat", "enemy_warrior_injured", null, null);
                            }
                            IStackSequence sequence;
                            if (pilot.Injuries == 0)
                            {
                                sequence = new ShowActorInfoSequence(pilot.ParentActor, Strings.T("{0}: INJURY IGNORED", new object[]
                                {
                                    pilot.InjuryReasonDescription
                                }), FloatieMessage.MessageNature.PilotInjury, true);
                            }
                            else
                            {
                                sequence = new ShowActorInfoSequence(pilot.ParentActor, Strings.T("{0}: PILOT INJURED", new object[]
                                {
                                    pilot.InjuryReasonDescription
                                }), FloatieMessage.MessageNature.PilotInjury, true);
                                AudioEventManager.SetPilotVOSwitch<AudioSwitch_dialog_dark_light>(AudioSwitch_dialog_dark_light.dark, pilot.ParentActor);
                                AudioEventManager.PlayPilotVO(VOEvents.Pilot_TakeDamage, pilot.ParentActor, null, null, true);
                            }
                            pilot.ParentActor.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(sequence));
                        }
                        pilot.ClearNeedsInjury();
                        if (pilot.IsIncapacitated)
                        {
                            pilot.ParentActor.FlagForDeath("Pilot Killed", DeathMethod.PilotKilled, DamageType.Combat, 1, hitInfo.stackItemUID, hitInfo.attackerId, false);
                            pilot.ParentActor.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(new ShowActorInfoSequence(pilot.ParentActor, Strings.T("PILOT INCAPACITATED!"), FloatieMessage.MessageNature.PilotInjury, true)));
                            pilot.ParentActor.HandleDeath(hitInfo.attackerId);
                        }
                        return;
                    }
                    if (damageLevel == ComponentDamageLevel.Destroyed)
                    {
                        ModInit.modLog.LogMessage($"Life support ({__instance.Description.UIName}) destroyed with Torso-Mount Cockpit! {pilot.Callsign} is well-done!");
                        pilot.LethalInjurePilot(__instance.parent.Combat.Constants, hitInfo.attackerId, hitInfo.stackItemUID, true, DamageType.OverheatSelf, null, null);
                        __instance.parent.FlagForDeath("Injuries", DeathMethod.PilotKilled, DamageType.HeadShot, 1, 1, hitInfo.attackerId, true);
                        __instance.parent.HandleDeath(hitInfo.attackerId);
                        return;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Mech), "ApplyHeadStructureEffects",
            new Type[]
            {
                typeof(ChassisLocations), typeof(LocationDamageLevel), typeof(LocationDamageLevel), typeof(WeaponHitInfo)
            })]

        public static class Mech_ApplyHeadStructureEffects
        {
            [HarmonyPriority(Priority.First)]
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
            public static bool Prepare() => false; //disabled for now
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
        public static class Pilot_InjurePilot_Patch
        {
            [HarmonyPriority(Priority.First)]

            public static bool Prefix(Pilot __instance, string sourceID, int stackItemUID, int dmg, DamageType damageType, Weapon sourceWeapon, AbstractActor sourceActor)
            {
                try
                {
                    if (__instance == null) return true;
                    ModInit.modLog.LogMessage(
                            $"{__instance.Callsign} has {__instance.StatCollection.GetValue<int>("Injuries")} injuries before InjurePilot; proceeding.");
                        PilotInjuryHolder.HolderInstance.injuryStat =
                            __instance.StatCollection.GetValue<int>("Injuries");
                        ModInit.modLog.LogMessage(
                            $"{__instance.Callsign} injuryStat set to {PilotInjuryHolder.HolderInstance.injuryStat}.");

                        if (__instance.pilotDef.PilotTags.Contains(ModInit.modSettings.pilotPainShunt) &&
                            (damageType == DamageType.Overheat || damageType == DamageType.OverheatSelf ||
                             damageType == DamageType.AmmoExplosion || damageType == DamageType.ComponentExplosion))
                    {
                        ModInit.modLog.LogMessage(
                            $"Pilot {__instance.Callsign} has {ModInit.modSettings.pilotPainShunt}, ignoring injury from {damageType}.");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex);
                    return true;
                }
                return true;
            }

            public static void Postfix(Pilot __instance, string sourceID, int stackItemUID, int dmg,
                DamageType damageType, Weapon sourceWeapon, AbstractActor sourceActor)
            {
                var pKey = __instance.FetchGUID();

                if (PilotInjuryHolder.HolderInstance.injuryStat >= //changed to <= instead of == 1021
                    __instance.StatCollection.GetValue<int>("Injuries"))
                {
                    ModInit.modLog.LogMessage(
                        $"{__instance.Callsign}_{pKey} still has {PilotInjuryHolder.HolderInstance.injuryStat} injuries; aborting.");
                    return;
                }

                if (ModInit.modSettings.enableInternalDmgInjuries &&
                    __instance.ParentActor.StatCollection.GetValue<bool>(ModInit.modSettings.internalDmgStatName) &&
                    __instance.StatCollection.GetValue<bool>("NeedsFeedbackInjury"))
                {
                    ModInit.modLog.LogMessage(
                        $"Rolling neural feedback injury with {dmg} damage for {__instance.Callsign}_{pKey}");
                    PilotInjuryManager.ManagerInstance.rollInjuryFeedback(__instance, dmg, damageType);

                    return;
                }

                else
                {
                    ModInit.modLog.LogMessage($"Rolling standard injury with {dmg} damage for {__instance.Callsign} {pKey}");
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
                                ModInit.modLog.LogMessage($"{__instance.Callsign}_{pKey} has been debilitated!");

                                if (ModInit.modSettings.enableLethalTorsoHead && (injuryLoc.Key == InjuryLoc.Head ||
                                    injuryLoc.Key == InjuryLoc.Torso))
                                {
                                    __instance.StatCollection.ModifyStat<bool>("TBAS_Injuries", 0, "LethalInjury",
                                        StatCollection.StatOperation.Set, true);
                                    ModInit.modLog.LogMessage(
                                        $"{__instance.Callsign}_{pKey} has debilitated Torso or Head; lethal injury!");
                                }
                            }
                        }
                    }
                }
            }
        }


        [HarmonyPatch(typeof(Pilot))]
        [HarmonyPatch("CanPilot", MethodType.Getter)]
        
        public static class Pilot_CanPilot_Patch
        {
            [HarmonyPriority(Priority.First)]
            public static void Postfix(Pilot __instance, ref bool __result)
            {
                if (__instance.pilotDef.PilotTags.Contains(DEBILITATEDTAG) || __instance.Injuries == __instance.Health)
                {
                    __result = false;
                }
                else
                {
                    __result = true;
                }
            }
        }

        [HarmonyPatch(typeof(Pilot))]
        [HarmonyPatch("IsIncapacitated", MethodType.Getter)]
       
        public static class Pilot_IsIncapacitated_Patch
        {
            [HarmonyPriority(Priority.Last)]
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
       
        public class ContractFinalizeKilledMechwarriorsPatch
        {
            private static MethodInfo pushReport = AccessTools.Method(typeof(Contract), "PushReport");
            private static MethodInfo popReport = AccessTools.Method(typeof(Contract), "PopReport");
            private static MethodInfo reportLog = AccessTools.Method(typeof(Contract), "ReportLog");
            [HarmonyAfter(new string[] { "co.uk.cwolf.MissionControl" })]
            public static bool Prefix(Contract __instance)
            {
                pushReport.Invoke(__instance, new object[] {"MechWarriorFinalizeKill"});
                foreach (UnitResult unitResult in __instance.PlayerUnitResults)
                {
                    Pilot pilot = unitResult.pilot;
                    PilotDef pilotDef = pilot.pilotDef;
                    if (!unitResult.pilot.IsIncapacitated || unitResult.pilot.pilotDef.IsImmortal || unitResult.pilot.HasEjected)
                    {
                        if (pilotDef != null)
                        {
                            pilotDef.SetRecentInjuryDamageType(DamageType.NOT_SET);
                        }

                        if (pilot.IsPlayerCharacter)
                        {
                            pilot.StatCollection.ModifyStat<bool>(pilot.FetchGUID(), 0, "LethalInjury", StatCollection.StatOperation.Set, false, -1, true);
                        }
                        
                    }

                    else if ((unitResult.pilot.StatCollection.GetValue<int>(MissionKilledStat) >=
                              ModInit.modSettings.missionKillSeverityThreshold ||
                              unitResult.pilot.pilotDef.PilotTags.Contains(DEBILITATEDTAG)) &&
                             (unitResult.pilot.Injuries < unitResult.pilot.Health && !unitResult.pilot.LethalInjuries) || (unitResult.pilot.StatCollection.GetValue<bool>("BledOut") && !ModInit.modSettings.BleedingOutLethal))

                    {
                        ModInit.modLog.LogMessage(
                            $"{pilot.Callsign} was mission-killed, debilitated, or bled out with BleedingOutLethal = false!");
                    }

                    else
                    {
                        
                        float num = pilot.LethalInjuries
                            ? sim.Constants.Pilot.LethalDeathChance
                            : sim.Constants.Pilot.IncapacitatedDeathChance;
                        num = Mathf.Max(0f, num - sim.Constants.Pilot.GutsDeathReduction * (float) pilot.Guts);
                        float num2 = sim.NetworkRandom.Float();
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
                if (p.Injuries == 0) //this should hopefully fix non-injury timeouts being weird due to multipliying injury cost. #hbswhy; changed from PilotDef to Pilot - 12/27
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
                PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey] = new List<string>();
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

                if (p.pilotDef.PilotTags.Contains(ModInit.modSettings.pilotPainShunt))
                {
                    ModInit.modLog.LogMessage(
                        $"{p.Callsign}_{pKey} has {ModInit.modSettings.pilotPainShunt}, ignoring feedback!");
                    return;
                }

                if ((ModInit.modSettings.internalDmgInjuryLocs.Contains(location) ||
                     ModInit.modSettings.internalDmgInjuryLocs.Capacity == 0) &&
                    damage >= ModInit.modSettings.internalDmgLvlReq &&
                    __instance.StatCollection.GetValue<bool>(ModInit.modSettings.internalDmgStatName) &&
                    (internalDmgInjuryCount <
                     ModInit.modSettings.internalDmgInjuryLimit) || ModInit.modSettings.internalDmgInjuryLimit < 1)

                {
                    ModInit.modLog.LogMessage(
                        $"{p.Callsign}_{pKey} has {internalDmgInjuryCount} preexisting feedback injuries!");

                    p.StatCollection.ModifyStat<bool>(p.FetchGUID(), 0, "NeedsFeedbackInjury",
                        StatCollection.StatOperation.Set, true);

                    ModInit.modLog.LogMessage(
                        $"{internalDmgInjuryCount} is < {ModInit.modSettings.internalDmgInjuryLimit}! Injuring {p.Callsign}_{pKey} from structure damage!");

                    p.InjurePilot(p.FetchGUID(), -1, 1, DamageType.ComponentExplosion, null, null);

                    p.StatCollection.ModifyStat<int>(p.FetchGUID(), 0, "internalDmgInjuryCount",
                        StatCollection.StatOperation.Int_Add, 1);

                    p.StatCollection.ModifyStat<bool>(p.FetchGUID(), 0, "NeedsFeedbackInjury",
                        StatCollection.StatOperation.Set, false);
                }
            }
        }
    }
}