using System;
using BattleTech;
using Harmony;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleTech.UI;
using Localize;
using UnityEngine;
using UnityEngine.UI;
using static TisButAScratch.Framework.GlobalVars;
using Text = Localize.Text;

namespace TisButAScratch.Patches
{
    class EffectExpiry
    {
        private static CombatHUDStatusPanel theInstance;
//        [HarmonyPatch(typeof(CombatHUDStatusPanel), "ShowEffectStatuses", new Type[] { typeof(CombatHUDStatusPanel), typeof(AbstractActor), typeof(AbilityDef.SpecialRules), typeof(Vector3)})]
        [HarmonyPatch(typeof(CombatHUDStatusPanel), "ShowEffectStatuses")]
        [HarmonyBefore(new string[] { "us.frostraptor.LowVisibility" })]
        static class CombatHUDStatusPanel_ShowEffectStatuses
        {
            static void Prefix(CombatHUDStatusPanel __instance, AbstractActor actor,
                AbilityDef.SpecialRules specialRulesFilter, Vector3 worldPos)
            {
                if (__instance != null)
                theInstance = __instance;
            }
        }

        [HarmonyPatch(typeof(CombatHUDStatusPanel), "ProcessDetailString",
            new Type[] {typeof(EffectData), typeof(int)})]
        static class CombatHUDStatusPanel_ProcessDetailString
        {
            static void Postfix(CombatHUDStatusPanel __instance, ref Text __result, EffectData effect,
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

                        var durationInfo = new int[]
                        {
                            tgtEffect.Duration.numActivationsRemaining,
                            tgtEffect.Duration.numMovementsRemaining,
                            tgtEffect.Duration.numPhasesRemaining,
                            tgtEffect.Duration.numRoundsRemaining
                        }.Max();

                        var txt = new Text("\n<color=#FF0000>Unit is bleeding out! {0} {1} remaining!</color=#FF0000>", new object[]
                        {
                            durationInfo,
                            ModInit.modSettings.BleedingOutTimerString
                        });
                        __result.AppendLine(txt);
                    }
                }
            }
        }
    }
}