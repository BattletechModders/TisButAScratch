using System;
using BattleTech;
using Harmony;
using System.Linq;
using BattleTech.UI;
using TisButAScratch.Framework;
using UnityEngine;
using Text = Localize.Text;

namespace TisButAScratch.Patches
{
    public class BleedingOut
    {

        [HarmonyPatch(typeof(AbstractActor), "OnActivationEnd",
            new Type[] {typeof(string), typeof(int)})]
        public static class AbstractActor_OnActivationEnd
        {
            public static void Prefix(AbstractActor __instance)
            {
                var effects = __instance.Combat.EffectManager.GetAllEffectsTargeting(__instance);

                if (!effects.Any(x =>
                    x.EffectData.Description.Id.EndsWith(ModInit.modSettings.BleedingOutSuffix))) return;
                var p = __instance.GetPilot();
                var pKey = p.FetchGUID();
                var bleedRate = p.GetBleedingRate();
                ModInit.modLog.LogMessage(
                    $"{p.Callsign}_{pKey} bleeding out at rate of {bleedRate}/activation!");
                var bloodBank = p.GetBloodBank();
                ModInit.modLog.LogMessage(
                    $"{p.Callsign}_{pKey}: Current bloodBank at {bloodBank}!");
                var newbloodBank = Mathf.RoundToInt(bloodBank - bleedRate);
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

                    __instance.FlagForDeath("Bled Out", DeathMethod.PilotKilled, DamageType.Unknown, 1, 1, p.FetchGUID(), true);

                    if (ModInit.modSettings.BleedingOutLethal) p.StatCollection.ModifyStat<bool>("TBAS_Injuries", 0, "LethalInjury",
                        StatCollection.StatOperation.Set, true);
                    __instance.HandleDeath(p.FetchGUID()); // added handledeath for  bleeding out
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
                var effects = __instance.Combat.EffectManager.GetAllEffectsTargeting(actor);

                if (!effects.Any(x =>
                    x.EffectData.Description.Id.EndsWith(ModInit.modSettings.BleedingOutSuffix))) return;

//                var byActivations = effects.OrderBy(x=>x.Duration.numActivationsRemaining).Where(
//                    x => x.EffectData.Description.Id.EndsWith(ModInit.modSettings.BleedingOutSuffix) && x.Duration.numActivationsRemaining > 0).ToList();
//                var byMovements = effects.OrderBy(x=>x.Duration.numMovementsRemaining).Where(
//                    x => x.EffectData.Description.Id.EndsWith(ModInit.modSettings.BleedingOutSuffix) && x.Duration.numMovementsRemaining > 0).ToList();
//                var byPhases = effects.OrderBy(x=>x.Duration.numPhasesRemaining).Where(
//                    x => x.EffectData.Description.Id.EndsWith(ModInit.modSettings.BleedingOutSuffix) && x.Duration.numPhasesRemaining > 0).ToList();
//                var byRounds = effects.OrderBy(x=>x.Duration.numRoundsRemaining).Where(
//                    x => x.EffectData.Description.Id.EndsWith(ModInit.modSettings.BleedingOutSuffix) && x.Duration.numRoundsRemaining > 0).ToList();

//                var bleeding = new Effect();
//                if (byActivations.Any())
//                {
//                    bleeding = byActivations.First();
//                }
//                else if (byMovements.Any())
//                {
//                    bleeding = byMovements.First();
//                }
//                else if (byPhases.Any())
//                {
//                    bleeding = byPhases.First();
//                }
//                else if (byRounds.Any())
//                {
//                    bleeding = byRounds.First();
//                }
//                else
//                {
//                    bleeding = effects.FirstOrDefault(x=>
//                        x.EffectData.Description.Id.EndsWith(ModInit.modSettings.BleedingOutSuffix));
//                    ModInit.modLog.LogMessage($"ERROR: We used the first effect we found, which probably isn't right.");
//                }

//                if (bleeding != null)
//                {
                    var durationInfo = Mathf.FloorToInt(actor.GetPilot().GetBloodBank() / actor.GetPilot().GetBleedingRate() - 1); 
//                    var durationInfo = new int[]
//                    {
//                        bleeding.Duration.numActivationsRemaining,
//                        bleeding.Duration.numMovementsRemaining,
//                        bleeding.Duration.numPhasesRemaining,
//                        bleeding.Duration.numRoundsRemaining
//                    }.Max() - 1;
                var eject = "";
                if (durationInfo <= 0)
                {
                    eject = "EJECT NOW OR DIE!";
                }

                var txt = new Text("<color=#FF0000>Pilot is bleeding out! {0} activations remaining! {2}</color=#FF0000>",
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
        [HarmonyBefore(new string[] { "us.frostraptor.LowVisibility" })]
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
                var actor = theInstance.DisplayedCombatant as AbstractActor;
                if (!effect.Description.Id.EndsWith(ModInit.modSettings.BleedingOutSuffix)) return;
                // var effectsList = em.GetAllEffectsCreatedBy(__instance.DisplayedCombatant.GUID);
                var effectsList = em.GetAllEffectsTargeting(actor);

                if (effectsList.Count > 0)
                {
                    var tgtEffect = effectsList.FirstOrDefault(x => x.EffectData == effect);
                    if (tgtEffect != null)
                    {
                        if (actor != null)
                        {
                            var durationInfo = Mathf.FloorToInt(actor.GetPilot().GetBloodBank() / actor.GetPilot().GetBleedingRate() - 1); 
//                        var durationInfo = new int[]
//                        {
//                            tgtEffect.Duration.numActivationsRemaining,
//                            tgtEffect.Duration.numMovementsRemaining,
//                            tgtEffect.Duration.numPhasesRemaining,
//                            tgtEffect.Duration.numRoundsRemaining
//                        }.Max() - 1;
                            var eject = "";
                            if (durationInfo <= 0)
                            {
                                eject = "EJECT NOW OR DIE!";
                            }
                            var txt = new Text("\n<color=#FF0000>Pilot is bleeding out! {0} activations remaining! {2}</color=#FF0000>", new object[]
                            {
                                durationInfo,
                                eject
                            });

                            __result.AppendLine(txt);
                        }
                    }
                }
            }
        }
    }
}