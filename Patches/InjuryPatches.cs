using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Security.Cryptography.X509Certificates;
using BattleTech;
using TisButAScratch.Framework;
using static TisButAScratch.Framework.GlobalVars;
using UnityEngine;
using CustomComponents;
using Localize;

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
                    ModInit.modLog?.Info?.Write($"Vehicle location will be destroyed; currentStructure at location {currentStructure}, will take {incomingStructureDamage} damage");
                    var pilot = __instance.GetPilot();
                    if (ModInit.modSettings.injureVehiclePilotOnDestroy == "MAX")
                    {
                        ModInit.modLog?.Info?.Write($"Vehicle location destroyed, MaxInjure {pilot.Callsign} {pilot.FetchGUID()} due to injureVehiclePilotOnDestroy = MAX");
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
                        var dmg = pilot.TotalHealth - 1;
                        ModInit.modLog?.Info?.Write($"Vehicle location destroyed, Injuring {pilot.Callsign} {pilot.FetchGUID()} for {dmg} due to injureVehiclePilotOnDestroy = HIGH");
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
                        ModInit.modLog?.Info?.Write($"Vehicle location destroyed, Injuring {pilot.Callsign} {pilot.FetchGUID()} for 1 due to injureVehiclePilotOnDestroy = SINGLE");
                        pilot.SetNeedsInjury(InjuryReason.ActorDestroyed);
                        __instance.CheckPilotStatusFromAttack(hitInfo.attackerId, hitInfo.attackSequenceId, hitInfo.stackItemUID);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Mech), "DamageLocation")]
        public static class Mech_DamageLocation
        {
            public static bool Prepare() => ModInit.modSettings.injureVehiclePilotOnDestroy != "OFF";
            public static void Prefix(Mech __instance, int originalHitLoc, WeaponHitInfo hitInfo, ArmorLocation aLoc, Weapon weapon, float totalArmorDamage, float directStructureDamage, int hitIndex, AttackImpactQuality impactQuality, DamageType damageType)
            {
                if (!__instance.UnitIsCustomUnitVehicle()) return;
                var cLoc = MechStructureRules.GetChassisLocationFromArmorLocation(aLoc);
                if (cLoc == ChassisLocations.None || cLoc == ChassisLocations.Arms || cLoc == ChassisLocations.MainBody) return;
                var structureStat = __instance.GetStringForStructureLocation(cLoc);
                if (structureStat == null) return;
                var currentStructure = __instance.GetCurrentStructure(cLoc);
                if (currentStructure <= 0f) return;
                var incomingStructureDamage = directStructureDamage + totalArmorDamage - __instance.GetCurrentArmor(aLoc);
                if (currentStructure - incomingStructureDamage <= 0f)
                {
                    ModInit.modLog?.Info?.Write($"[Mech_DamageLocation] Vehicle location will be destroyed; currentStructure at location {currentStructure}, will take {incomingStructureDamage} damage");
                    var pilot = __instance.GetPilot();
                    if (ModInit.modSettings.injureVehiclePilotOnDestroy == "MAX")
                    {
                        ModInit.modLog?.Info?.Write($"[Mech_DamageLocation] Vehicle location destroyed, MaxInjure {pilot.Callsign} {pilot.FetchGUID()} due to injureVehiclePilotOnDestroy = MAX");
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
                        var dmg = pilot.TotalHealth - 1;
                        ModInit.modLog?.Info?.Write($"[Mech_DamageLocation] Vehicle location destroyed, Injuring {pilot.Callsign} {pilot.FetchGUID()} for {dmg} due to injureVehiclePilotOnDestroy = HIGH");
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
                        ModInit.modLog?.Info?.Write($"[Mech_DamageLocation] Vehicle location destroyed, Injuring {pilot.Callsign} {pilot.FetchGUID()} for 1 due to injureVehiclePilotOnDestroy = SINGLE");
                        pilot.SetNeedsInjury(InjuryReason.ActorDestroyed);
                        __instance.CheckPilotStatusFromAttack(hitInfo.attackerId, hitInfo.attackSequenceId, hitInfo.stackItemUID);
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
                var pKey = pilot.FetchGUID();
                if (__instance.parent.GetStaticUnitTags().Contains(ModInit.modSettings.disableTBASTag) || (ModInit.modSettings.disableTBASTroopers && __instance.parent.UnitIsTrooperSquad()))
                {
                    ModInit.modLog?.Info?.Write(
                        $"[DamageComponent] {pilot.Callsign}_{pKey} has {ModInit.modSettings.disableTBASTag} or disableTBASTroopers {ModInit.modSettings.disableTBASTroopers}, not processing TBAS injuries.");
                    return;
                }

                if (__instance.parent.StatCollection.GetValue<bool>(ModInit.modSettings.isTorsoMountStatName) &&
                    (__instance.parent is Mech && (__instance.LocationDef.Location & ChassisLocations.Head) != 0))
                {
                    ModInit.modLog?.Info?.Write($"Head hit, but cockpit components not located in head!");
                    return;
                }

                if ((__instance.parent is Mech && (__instance.LocationDef.Location & ChassisLocations.Head) == 0 ||
                     __instance.parent is Vehicle) && ModInit.modSettings.crewOrCockpitCustomID.Any
                        (x => __instance.componentDef.GetComponents<Category>().Any(c => c.CategoryID == x)) &&
                    (damageLevel == ComponentDamageLevel.Penalized))
                {
                    ModInit.modLog?.Info?.Write(
                        $"Cockpit component ({__instance.Description.UIName}) damaged, pilot needs injury!");
                    pilot.SetNeedsInjury(InjuryReason.ComponentExplosion);
                    __instance.parent.CheckPilotStatusFromAttack(hitInfo.attackerId, hitInfo.attackSequenceId, hitInfo.stackItemUID);
                    //pilot.InjurePilot(hitInfo.attackerId, hitInfo.stackItemUID, 1, DamageType.Combat, null, pilot.ParentActor.Combat.FindActorByGUID(hitInfo.attackerId));
 //                   if (!pilot.IsIncapacitated)
 //                   {
 //                       if (pilot.ParentActor.team.LocalPlayerControlsTeam)
 //                       {
 //                           AudioEventManager.PlayAudioEvent("audioeventdef_musictriggers_combat", "friendly_warrior_injured", null, null);
 //                       }
 //                       else
 //                       {
  //                          AudioEventManager.PlayAudioEvent("audioeventdef_musictriggers_combat", "enemy_warrior_injured", null, null);
  //                      }
  //                      IStackSequence sequence;
  //                      if (pilot.Injuries == 0)
  //                      {
  //                          sequence = new ShowActorInfoSequence(pilot.ParentActor, Strings.T("{0}: INJURY IGNORED", new object[]
  //                          {
  //                                  pilot.InjuryReasonDescription
  //                          }), FloatieMessage.MessageNature.PilotInjury, true);
  //                      }
  //                      else
  //                      {
  //                          sequence = new ShowActorInfoSequence(pilot.ParentActor, Strings.T("{0}: PILOT INJURED", new object[]
  //                          {
  //                                  pilot.InjuryReasonDescription
  //                          }), FloatieMessage.MessageNature.PilotInjury, true);
  //                          AudioEventManager.SetPilotVOSwitch<AudioSwitch_dialog_dark_light>(AudioSwitch_dialog_dark_light.dark, pilot.ParentActor);
  //                          AudioEventManager.PlayPilotVO(VOEvents.Pilot_TakeDamage, pilot.ParentActor, null, null, true);
  //                      }
  //                      pilot.ParentActor.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(sequence));
  //                  }
  //                  pilot.ClearNeedsInjury();
  //                  if (pilot.IsIncapacitated)
  //                  {
  //                      pilot.ParentActor.FlagForDeath("Pilot Killed", DeathMethod.PilotKilled, DamageType.Combat, 1, hitInfo.stackItemUID, hitInfo.attackerId, false);
  //                      pilot.ParentActor.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(new ShowActorInfoSequence(pilot.ParentActor, Strings.T("PILOT INCAPACITATED!"), FloatieMessage.MessageNature.PilotInjury, true)));
  //                      pilot.ParentActor.HandleDeath(hitInfo.attackerId);
  //                  }
                    return;
                }

                if ((__instance.parent is Mech && (__instance.LocationDef.Location & ChassisLocations.Head) == 0 ||
                     __instance.parent is Vehicle) && ModInit.modSettings.crewOrCockpitCustomID.Any
                        (x => __instance.componentDef.GetComponents<Category>().Any(c => c.CategoryID == x)) &&
                    (damageLevel == ComponentDamageLevel.Destroyed))
                {
                    ModInit.modLog?.Info?.Write(
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
                        ModInit.modLog?.Info?.Write($"Pilot {pilot.Callsign} has {ModInit.modSettings.pilotPainShunt}, ignoring injury from life support damage.");
                        return;
                    }
                    if (damageLevel == ComponentDamageLevel.Penalized)
                    {
                        ModInit.modLog?.Info?.Write($"Life support ({__instance.Description.UIName}) damaged with Torso-Mount Cockpit! {pilot.Callsign} is being cooked!");
                        pilot.SetNeedsInjury(InjuryReason.ComponentExplosion);
                        __instance.parent.CheckPilotStatusFromAttack(hitInfo.attackerId, hitInfo.attackSequenceId, hitInfo.stackItemUID);
//                        pilot.InjurePilot(hitInfo.attackerId, hitInfo.stackItemUID, 1, DamageType.Combat, null, pilot.ParentActor.Combat.FindActorByGUID(hitInfo.attackerId));
//                        if (!pilot.IsIncapacitated)
//                        {
//                            if (pilot.ParentActor.team.LocalPlayerControlsTeam)
//                            {
//                                AudioEventManager.PlayAudioEvent("audioeventdef_musictriggers_combat", "friendly_warrior_injured", null, null);
//                            }
//                            else
//                            {
//                                AudioEventManager.PlayAudioEvent("audioeventdef_musictriggers_combat", "enemy_warrior_injured", null, null);
//                            }
//                            IStackSequence sequence;
//                            if (pilot.Injuries == 0)
//                            {
//                                sequence = new ShowActorInfoSequence(pilot.ParentActor, Strings.T("{0}: INJURY IGNORED", new object[]
//                                {
//                                    pilot.InjuryReasonDescription
 //                               }), FloatieMessage.MessageNature.PilotInjury, true);
 //                           }
 //                           else
 //                           {
 //                               sequence = new ShowActorInfoSequence(pilot.ParentActor, Strings.T("{0}: PILOT INJURED", new object[]
 //                               {
 //                                   pilot.InjuryReasonDescription
 //                               }), FloatieMessage.MessageNature.PilotInjury, true);
 //                               AudioEventManager.SetPilotVOSwitch<AudioSwitch_dialog_dark_light>(AudioSwitch_dialog_dark_light.dark, pilot.ParentActor);
 //                               AudioEventManager.PlayPilotVO(VOEvents.Pilot_TakeDamage, pilot.ParentActor, null, null, true);
 //                           }
 //                           pilot.ParentActor.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(sequence));
 //                       }
 //                       pilot.ClearNeedsInjury();
 //                       if (pilot.IsIncapacitated)
 //                       {
 //                           pilot.ParentActor.FlagForDeath("Pilot Killed", DeathMethod.PilotKilled, DamageType.Combat, 1, hitInfo.stackItemUID, hitInfo.attackerId, false);
 //                           pilot.ParentActor.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(new ShowActorInfoSequence(pilot.ParentActor, Strings.T("PILOT INCAPACITATED!"), FloatieMessage.MessageNature.PilotInjury, true)));
//                            pilot.ParentActor.HandleDeath(hitInfo.attackerId);
//                        }
                        return;
                    }
                    if (damageLevel == ComponentDamageLevel.Destroyed)
                    {
                        ModInit.modLog?.Info?.Write($"Life support ({__instance.Description.UIName}) destroyed with Torso-Mount Cockpit! {pilot.Callsign} is well-done!");
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
            public static void Prefix(ref bool __runOriginal, Mech __instance, ChassisLocations location, LocationDamageLevel oldDamageLevel,
                LocationDamageLevel newDamageLevel, WeaponHitInfo hitInfo)
            {
                if (!__runOriginal) return;
                if (__instance.StatCollection.GetValue<bool>(ModInit.modSettings.isTorsoMountStatName))
                {
                    var currentReason = __instance.GetPilot().InjuryReason;
                    if (currentReason != InjuryReason.HeadHit && currentReason != InjuryReason.NotSet)
                    {
                        ModInit.modLog?.Info?.Write($" [Mech_ApplyHeadStructureEffects Prefix] Head hit, cockpit not located in head BUT existing reason == {currentReason}, not processing set needs Injury here!");
                        __runOriginal = false;
                        return;
                    }

                    if (newDamageLevel == LocationDamageLevel.Destroyed)
                    {
                        ModInit.modLog?.Info?.Write($"[Mech_ApplyHeadStructureEffects Prefix] Head destroyed, but cockpit not located in head! Not injuring pilot here!");
                        __runOriginal = false;
                        return;
                    }
                }
                __runOriginal = true;
                return;
            }

            public static void Postfix(Mech __instance, ChassisLocations location, LocationDamageLevel oldDamageLevel,
                LocationDamageLevel newDamageLevel, WeaponHitInfo hitInfo)
            {
                if (__instance.StatCollection.GetValue<bool>(ModInit.modSettings.isTorsoMountStatName))
                {
                    var currentReason = __instance.GetPilot().InjuryReason;
                    if (currentReason == InjuryReason.HeadHit)
                    {
                        ModInit.modLog?.Info?.Write($"[Mech_ApplyHeadStructureEffects Postfix] Head hit, but cockpit not located in head! Existing injury reason was {currentReason}, resetting to NotSet!");
                        __instance.GetPilot().SetNeedsInjury(InjuryReason.NotSet);
                    }
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
            [HarmonyWrapSafe]
            public static void Prefix(ref bool __runOriginal, Pilot __instance, string sourceID, int stackItemUID, int dmg, DamageType damageType, Weapon sourceWeapon, AbstractActor sourceActor)
            {
                if (!__runOriginal)return;
                if (__instance == null)
                {
                    __runOriginal = true;
                    return;
                }
                ModInit.modLog?.Info?.Write(
                        $"{__instance.Callsign} has {__instance.StatCollection.GetValue<int>("Injuries")} injuries before InjurePilot; proceeding.");
                PilotInjuryHolder.HolderInstance.injuryStat = __instance.StatCollection.GetValue<int>("Injuries");
                ModInit.modLog?.Info?.Write($"{__instance.Callsign} injuryStat set to {PilotInjuryHolder.HolderInstance.injuryStat}.");

                if (__instance.pilotDef.PilotTags.Contains(ModInit.modSettings.pilotPainShunt) &&
                    (damageType == DamageType.Overheat || damageType == DamageType.OverheatSelf ||
                     damageType == DamageType.AmmoExplosion || damageType == DamageType.ComponentExplosion || (int)__instance.injuryReason == 101 || (int)__instance.injuryReason == 666 || (int)__instance.injuryReason == 667
                     || "OVERHEATED".Equals(__instance.InjuryReasonDescription, StringComparison.InvariantCultureIgnoreCase))) //add head-only injury herre?
                    {
                        ModInit.modLog?.Info?.Write(
                            $"Pilot {__instance.Callsign} has {ModInit.modSettings.pilotPainShunt}, ignoring injury from {damageType}.");
                        __runOriginal = false;
                        return;
                    }
                __runOriginal = true;
                return;
            }

            public static void Postfix(Pilot __instance, string sourceID, int stackItemUID, int dmg,
                DamageType damageType, Weapon sourceWeapon, AbstractActor sourceActor)
            {
                var pKey = __instance.FetchGUID();

                if (__instance.ParentActor.GetStaticUnitTags().Contains(ModInit.modSettings.disableTBASTag) || (ModInit.modSettings.disableTBASTroopers && __instance.ParentActor.UnitIsTrooperSquad()))
                {
                    ModInit.modLog?.Info?.Write(
                        $"[Pilot_InjurePilot_Patch_Post] {__instance.Callsign}_{pKey} has {ModInit.modSettings.disableTBASTag} or disableTBASTroopers {ModInit.modSettings.disableTBASTroopers}, not processing TBAS injuries.");
                    return;
                }

                if (PilotInjuryHolder.HolderInstance.injuryStat >= //changed to <= instead of == 1021
                    __instance.StatCollection.GetValue<int>("Injuries"))
                {
                    ModInit.modLog?.Info?.Write(
                        $"{__instance.Callsign}_{pKey} still has {PilotInjuryHolder.HolderInstance.injuryStat} injuries; aborting.");
                    return;
                }

                if (ModInit.modSettings.enableInternalDmgInjuries &&
                    __instance.ParentActor.StatCollection.GetValue<bool>(ModInit.modSettings.internalDmgStatName) &&
                    __instance.StatCollection.GetValue<bool>("NeedsFeedbackInjury"))
                {
                    ModInit.modLog?.Info?.Write(
                        $"Rolling neural feedback injury with {dmg} damage for {__instance.Callsign}_{pKey}");
                    PilotInjuryManager.ManagerInstance.rollInjuryFeedback(__instance, dmg, damageType);

                    return;
                }

                else
                {
                    ModInit.modLog?.Info?.Write($"Rolling standard injury with {dmg} damage for {__instance.Callsign} {pKey}");
                    PilotInjuryManager.ManagerInstance.rollInjury(__instance, dmg, damageType, __instance.injuryReason);
                }

                if ((ModInit.modSettings.debilSeverityThreshold > 0) &&
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
                                //__instance.pilotDef.PilotTags.Add(DebilitatedPrefix);
                                __instance.pilotDef.PilotTags.Add($"{DebilitatedPrefix}_{injuryLoc.Key}");
                                ModInit.modLog?.Info?.Write($"{__instance.Callsign}_{pKey} has been debilitated!");
                                if (ModInit.modSettings.enableLethalTorsoHead && (injuryLoc.Key == InjuryLoc.Head ||
                                        injuryLoc.Key == InjuryLoc.Torso))
                                {
                                    __instance.StatCollection.ModifyStat<bool>("TBAS_Injuries", 0, "LethalInjury",
                                        StatCollection.StatOperation.Set, true);
                                    ModInit.modLog?.Info?.Write(
                                        $"{__instance.Callsign}_{pKey} has debilitated Torso or Head; lethal injury!");
                                }
//                                __instance.ParentActor.FlagForDeath(DEBILITATEDTAG, DeathMethod.PilotKilled, DamageType.Combat, 1, stackItemUID, sourceActor.GUID, true);
//                                __instance.ParentActor.HandleDeath(sourceActor.GUID);
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
                if (__instance.Injuries > 0)
                {
                    __result = true;
                }
                if (__instance.pilotDef.PilotTags.Contains(PermanentlyIncapacitated) || __instance.pilotDef.PilotTags.Any(x=>x.StartsWith(DebilitatedPrefix)) || __instance.Injuries >= __instance.Health || __instance.pilotDef.TimeoutRemaining > 0)
                {
                    __result = false;
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
                if (ModInit.modSettings.debilIncapacitates && __instance.pilotDef.PilotTags.Any(x => x.StartsWith(DebilitatedPrefix)) || 
                    __instance.StatCollection.GetValue<bool>("BledOut") ||
                    (ModInit.modSettings.enableConsciousness &&__instance.StatCollection.GetValue<int>(MissionKilledStat) >= __instance.StatCollection.GetValue<int>("MissionKilledThreshold") && __instance.StatCollection.GetValue<int>("MissionKilledThreshold") > 0))
                {
                    __result = true;
                }
            }
        }

        [HarmonyPatch(typeof(Contract), "FinalizeKilledMechWarriors", typeof(SimGameState))]
       
        public class ContractFinalizeKilledMechwarriorsPatch
        {
            //public static MethodInfo pushReport = AccessTools.Method(typeof(Contract), "PushReport");
            //public static MethodInfo popReport = AccessTools.Method(typeof(Contract), "PopReport");
            //public static MethodInfo reportLog = AccessTools.Method(typeof(Contract), "ReportLog");
            [HarmonyAfter(new string[] { "co.uk.cwolf.MissionControl" })]
            public static void Prefix(ref bool __runOriginal, Contract __instance)
            {
                if (!__runOriginal) return;
                __instance.PushReport("MechWarriorFinalizeKill");
                //pushReport.Invoke(__instance, new object[] {"MechWarriorFinalizeKill"});
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

                    else if (((ModInit.modSettings.enableConsciousness && unitResult.pilot.StatCollection.GetValue<int>(MissionKilledStat) >=
                              pilot.StatCollection.GetValue<int>("MissionKilledThreshold")) ||
                              unitResult.pilot.pilotDef.PilotTags.Any(x => x.StartsWith(DebilitatedPrefix))) &&
                             (unitResult.pilot.Injuries < unitResult.pilot.Health && !unitResult.pilot.LethalInjuries) || (unitResult.pilot.StatCollection.GetValue<bool>("BledOut") && !ModInit.modSettings.BleedingOutLethal))

                    {
                        ModInit.modLog?.Info?.Write(
                            $"{pilot.Callsign} was mission-killed, debilitated, or bled out with BleedingOutLethal = false!");
                    }

                    else
                    {
                        var lethalDeathChance = sim.CompanyStats.GetValue<float>("LethalDeathChance") + sim.Constants.Pilot.LethalDeathChance;
                        var incapacitatedDeathChance = sim.CompanyStats.GetValue<float>("IncapacitatedDeathChance") + sim.Constants.Pilot.IncapacitatedDeathChance;
                        float num = pilot.LethalInjuries
                            ? lethalDeathChance
                            : incapacitatedDeathChance;
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
                        __instance.ReportLog(s);
                        //reportLog.Invoke(__instance, new object[] {s});
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
                __instance.PopReport();
                //popReport.Invoke(__instance, new object[] { });
                __runOriginal = false;
                return;
            }
        }

        [HarmonyPatch(typeof(SimGameState), "RefreshInjuries")]
        public static class SimGameState_RefreshInjuries
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> codes = instructions.ToList();
                
                // only update injury cost of new cost is greater than old cost
                int index = codes.FindIndex(c => c == codes.First(x => x.opcode == OpCodes.Beq));

                codes[index].opcode = OpCodes.Bge;

                return codes.AsEnumerable();
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

                if (ModInit.modSettings.timeHealsAllWounds)
                {
                    foreach (var tag in p.pilotDef.PilotTags)
                    {
                        if (tag.StartsWith(DebilitatedPrefix))
                        {
                            crippled += (float)(ModInit.modSettings.debilitatedCost / (ModInit.modSettings.medtechDebilMultiplier * __instance.MedTechSkill));
                        }
                    }
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
                    __instance.pilotDef.PilotTags.RemoveRange(GlobalVars.DebilLocationList);
                }
                ModInit.modLog?.Info?.Write($"{__instance.Callsign} has been healed!!");
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

            public static void Postfix(Mech __instance, ChassisLocations location, float damage, WeaponHitInfo hitInfo)
            {
                var p = __instance.GetPilot();
                var pKey = p.FetchGUID();
                var internalDmgInjuryCount = p.StatCollection.GetValue<int>("internalDmgInjuryCount");

                if (p.pilotDef.PilotTags.Contains(ModInit.modSettings.pilotPainShunt))
                {
                    ModInit.modLog?.Info?.Write(
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
                    ModInit.modLog?.Info?.Write(
                        $"{p.Callsign}_{pKey} has {internalDmgInjuryCount} preexisting feedback injuries!");

                    p.StatCollection.ModifyStat<bool>(p.FetchGUID(), 0, "NeedsFeedbackInjury",
                        StatCollection.StatOperation.Set, true);

                    ModInit.modLog?.Info?.Write(
                        $"{internalDmgInjuryCount} is < {ModInit.modSettings.internalDmgInjuryLimit}! Injuring {p.Callsign}_{pKey} from structure damage!");

                    p.SetNeedsInjury(DescriptionPatches.Pilot_InjuryReasonDescription_Patch.InjuryReasonFeedback); // change this to new "feedback EI" reason?
                    __instance.CheckPilotStatusFromAttack(hitInfo.attackerId, hitInfo.attackSequenceId, hitInfo.stackItemUID);
                    //p.InjurePilot(p.FetchGUID(), -1, 1, DamageType.ComponentExplosion, null, null);

                    p.StatCollection.ModifyStat<int>(p.FetchGUID(), 0, "internalDmgInjuryCount",
                        StatCollection.StatOperation.Int_Add, 1);

                    p.StatCollection.ModifyStat<bool>(p.FetchGUID(), 0, "NeedsFeedbackInjury",
                        StatCollection.StatOperation.Set, false);
                }
            }
        }
    }
}