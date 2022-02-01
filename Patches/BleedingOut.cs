using System;
using System.ComponentModel;
using BattleTech;
using Harmony;
using System.Linq;
using BattleTech.UI;
using Localize;
using TisButAScratch.Framework;
using UnityEngine;
using Text = Localize.Text;
using static TisButAScratch.Framework.GlobalVars;

namespace TisButAScratch.Patches
{
    public class BleedingOut
    {
        //pretty much copied from MechEngineer
        [HarmonyPatch(typeof(Pilot), "InjuryReasonDescription", MethodType.Getter)]
        public static class Pilot_InjuryReasonDescription_Patch
        {
            public static InjuryReason InjuryReasonOverheat = (InjuryReason)666;
            public static void Postfix(Pilot __instance, ref string __result)
            {
                if (__instance.InjuryReason == InjuryReasonOverheat)
                {
                    __result = "OVERHEATED";
                }
            }
        }

        [HarmonyPatch(typeof(AbstractActor), "OnActivationEnd",
            new Type[] {typeof(string), typeof(int)})]
        public static class AbstractActor_OnActivationEnd
        {
            public static void Prefix(AbstractActor __instance, string sourceID, int stackItemID)
            {
                if (__instance == null) return;
                var p = __instance.GetPilot();
                var pKey = p.FetchGUID();
                if (__instance.GetStaticUnitTags().Contains(ModInit.modSettings.disableTBASTag) || (ModInit.modSettings.disableTBASTroopers && __instance.UnitIsTrooperSquad()))
                {
                    ModInit.modLog.LogMessage(
                        $"[OnActivationEnd] {__instance.GetPilot().Callsign}_{pKey} has {ModInit.modSettings.disableTBASTag} or disableTBASTroopers {ModInit.modSettings.disableTBASTroopers}, not processing TBAS injuries.");
                    return;
                }
                if (__instance is Mech mech)
                {
                    if (mech.IsOverheated)
                    {
                        if (__instance.StatCollection.GetValue<bool>(ModInit.modSettings.OverheatInjuryStat))
                        {
                            p.SetNeedsInjury(Pilot_InjuryReasonDescription_Patch.InjuryReasonOverheat);
                            p.InjurePilot(sourceID, stackItemID, 1,
                                DamageType.Overheat, default(Weapon), __instance);
                            p.ClearNeedsInjury();
                        }
                    }
                }

                ModInit.modLog.LogMessage(
                    $"Actor {p.Callsign} {pKey} ending turn.");
                var effects = __instance.Combat.EffectManager.GetAllEffectsTargeting(__instance);
                if (effects.Count == 0) return;
                var continuator = false;
                foreach (var effect in effects)
                {
                    if (effect.EffectData?.Description?.Id == null)
                    {
                        ModInit.modLog.LogMessage(
                            $"Effect {effect?.EffectData} had null description");
                        continue;
                    }

                    if (effect.EffectData.Description.Id.EndsWith(ModInit.modSettings.BleedingOutSuffix))
                    {
                        continuator = true;
                        break;
                    }
                }
                if (!continuator) return;

                var baseRate = p.GetBleedingRate();
                var multi = p.GetBleedingRateMulti();
                var bleedRate = baseRate * multi;
                ModInit.modLog.LogMessage(
                    $"OnActivationEnd: {p.Callsign}_{pKey} bleeding out at rate of {bleedRate}/activation from base {baseRate} * multi {multi}!");
                var bloodBank = p.GetBloodBank();
                var bloodCap = p.GetBloodCapacity();
                if (bloodBank > bloodCap)
                {
                    ModInit.modLog.LogMessage(
                        $"OnActivationEnd: {p.Callsign}_{pKey} bloodbank {bloodBank} > blood capacity {bloodCap}. Setting bloodbank to blood capacity before bleeding continues.");
                    p.SetBloodBank(bloodCap);
                    bloodBank = p.GetBloodBank();
                }

                ModInit.modLog.LogMessage(
                    $"{p.Callsign}_{pKey}: Current bloodBank at {bloodBank}!");
                var newbloodBank = bloodBank - bleedRate;
                p.SetBloodBank(newbloodBank);
                ModInit.modLog.LogMessage(
                    $"{p.Callsign}_{pKey}: BloodBank set to {p.GetBloodBank()}");

                if (newbloodBank <= 0)
                {
                    if (__instance.WasEjected) return;
                    
                    p.StatCollection.ModifyStat<bool>("TBAS_Injuries", 0, "BledOut",
                        StatCollection.StatOperation.Set, true);
                    ModInit.modLog.LogMessage(
                        $"{p.Callsign}_{pKey} has bled out!");

                    __instance.FlagForDeath("Bled Out", DeathMethod.PilotKilled, DamageType.Unknown, 1, 1, p.FetchGUID(), false);
                    if (ModInit.modSettings.BleedingOutLethal) p.StatCollection.ModifyStat<bool>("TBAS_Injuries", 0, "LethalInjury",
                        StatCollection.StatOperation.Set, true);
                    __instance.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(new ShowActorInfoSequence(__instance, Strings.T("PILOT INCAPACITATED!"), FloatieMessage.MessageNature.PilotInjury, true)));
                    __instance.HandleDeath(p.FetchGUID()); // added handledeath for  bleeding out
                    return;
                }
                if (p.IsIncapacitated)
                {
                    __instance.FlagForDeath("Pilot Killed", DeathMethod.PilotKilled, DamageType.Unknown, 1, 1, p.FetchGUID(), false);
                    __instance.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(new ShowActorInfoSequence(__instance, Strings.T("PILOT INCAPACITATED!"), FloatieMessage.MessageNature.PilotInjury, true)));
                    __instance.HandleDeath(p.FetchGUID());
                    return;
                }

                if (ModInit.modSettings.UseBleedingEffects && bleedRate > 0)
                {
                    p.ApplyClosestBleedingEffect();
                    //probably should handle/apply bleeding out effects here
                }
            }
        }

        [HarmonyPatch(typeof(CombatHUD), "OnActorSelected",
            new Type[] {typeof(AbstractActor)})]
        public static class CombatHUD_OnActorSelected_Patch
        {
            public static void Postfix(CombatHUD __instance, AbstractActor actor)
            {
                var p = actor.GetPilot();
                var pKey = p.FetchGUID();
                if (p.StatCollection.GetValue<int>(MissionKilledStat) > 0 && ModInit.modSettings.enableConsciousness)
                {
                    var mknum = p.StatCollection.GetValue<int>(MissionKilledStat);
                    var missionKill = new Text("<color=#C65102>Pilot's current Consciousness Threshold: {0} of {1}</color=#C65102>",
                        new object[]
                        {
                            mknum,
                            p.StatCollection.GetValue<int>("MissionKilledThreshold")
                        });
                    actor.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(
                        new ShowActorInfoSequence(actor, missionKill, FloatieMessage.MessageNature.PilotInjury, false)));
                }


                var effects = __instance.Combat.EffectManager.GetAllEffectsTargeting(actor);
                var continuator = false;
                foreach (var effect in effects)
                {
                    if (effect.EffectData?.Description?.Id == null)
                    {
                        ModInit.modLog.LogMessage(
                            $"Effect {effect?.EffectData} had null description");
                        continue;
                    }

                    if (effect.EffectData.Description.Id.EndsWith(ModInit.modSettings.BleedingOutSuffix))
                    {
                        continuator = true;
                        break;
                    }
                }
                if (!continuator) return;
                
                var baseRate = p.GetBleedingRate();
                var multi = p.GetBleedingRateMulti();
                var bleedRate = baseRate * multi;
                ModInit.modLog.LogMessage(
                    $"OnActorSelected: {p.Callsign}_{pKey} bleeding out at rate of {bleedRate}/activation from base {baseRate} * multi {multi}!");
                var durationInfo = Mathf.CeilToInt(p.GetBloodBank() / (bleedRate) -1); 

                ModInit.modLog.LogMessage(
                    $"At OnActorSelected: Found bleeding effect(s) for {actor.GetPilot().Callsign}, processing time to bleedout for display: {durationInfo} activations remain");

                var eject = "";
                if (durationInfo <= 0)
                {
                    eject = "EJECT NOW OR DIE!";
                }

                var txt = new Text("<color=#FF0000>Pilot is bleeding out! {0} activations remaining! {1}</color=#FF0000>",
                    new object[]
                    {
                        durationInfo,
                        eject
                    });

                actor.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(
                    new ShowActorInfoSequence(actor, txt, FloatieMessage.MessageNature.PilotInjury, false)));

//                }
            }
        }

        private static CombatHUDStatusPanel theInstance;

        [HarmonyPatch(typeof(CombatHUDStatusPanel), "ShowEffectStatuses")]
        public static class CombatHUDStatusPanel_ShowEffectStatuses
        {
            public static void Prefix(CombatHUDStatusPanel __instance, AbstractActor actor,
                AbilityDef.SpecialRules specialRulesFilter, Vector3 worldPos)
            {
                if (__instance != null)
                    theInstance = __instance;
            }
        }

        [HarmonyPatch(typeof(CombatHUDStatusPanel), "ProcessDetailString",
            new Type[] {typeof(EffectData), typeof(int)})]
        public static class CombatHUDStatusPanel_ProcessDetailString
        {
            public static void Postfix(CombatHUDStatusPanel __instance, ref Text __result, EffectData effect,
                int numDuplicateEffects)
            {
                var em = UnityGameInstance.BattleTechGame.Combat.EffectManager; 
                
                //CombatHUD chud = (CombatHUD) Traverse.Create(__instance).("HUD").GetValue();
                // var em = chud.Combat.EffectManager;
                if (!(theInstance.DisplayedCombatant is AbstractActor actor)) return;
                if (effect?.Description?.Id == null)
                {
                    ModInit.modLog.LogTrace(
                        $"Effect {effect} had null description");
                    return;
                }
                var p = actor.GetPilot();

                if (p.StatCollection.GetValue<int>(MissionKilledStat) > 0 && ModInit.modSettings.enableConsciousness && PilotInjuryManager.ManagerInstance.InjuryEffectIDs.Contains(effect.Description.Id))
                {
                    var mknum = p.StatCollection.GetValue<int>(MissionKilledStat);
                    var missionKill = new Text(
                        "\n<color=#C65102>Pilot's current Consciousness Threshold: {0} of {1}</color=#C65102>",
                        new object[]
                        {
                            mknum,
                            p.StatCollection.GetValue<int>("MissionKilledThreshold")
                        });
                    __result.AppendLine(missionKill);
                }

                if (!effect.Description.Id.EndsWith(ModInit.modSettings.BleedingOutSuffix)) return;
                // var effectsList = em.GetAllEffectsCreatedBy(__instance.DisplayedCombatant.GUID);
                var effectsList = em.GetAllEffectsTargeting(actor);

                if (effectsList.Count <= 0) return;

                var pKey = p.FetchGUID();
                var baseRate = p.GetBleedingRate();
                var multi = p.GetBleedingRateMulti();
                var bleedRate = baseRate * multi;
                ModInit.modLog.LogTrace(
                    $"ProcessDetailString: {p.Callsign}_{pKey} bleeding out at rate of {bleedRate}/activation from base {baseRate} * multi {multi}!");
                var durationInfo = Mathf.CeilToInt(p.GetBloodBank() / (bleedRate) - 1);

                ModInit.modLog.LogTrace(
                    $"At ProcessDetailString: Found bleeding effect(s) for {actor.GetPilot().Callsign}, processing time to bleedout for display: {durationInfo} activations remain");
                var tgtEffect = effectsList.FirstOrDefault(x => x.EffectData == effect);
                if (tgtEffect == null) return;
                var eject = "";
                if (durationInfo <= 0)
                {
                    eject = "EJECT NOW OR DIE!";
                }
                var txt = new Text("\n<color=#FF0000>Pilot is bleeding out! {0} activations remaining! {1}</color=#FF0000>", new object[]
                {
                    durationInfo,
                    eject
                });
                __result.AppendLine(txt);
            }
        }
    }
}