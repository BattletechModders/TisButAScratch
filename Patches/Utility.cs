using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleTech;
using Harmony;
using HBS.Collections;

namespace TisButAScratch.Patches
{
    public static class Utility
    {
        public static Dictionary<string, TagSet> CachedTagSets = new Dictionary<string, TagSet>();

        public static bool UnitIsCustomUnitVehicle(this ICombatant combatant)
        {
            if (!combatant.StatCollection.ContainsStatistic("CUFakeVehicle")) return false;
            return combatant.StatCollection.GetValue<bool>("CUFakeVehicle");
        }

        public static bool UnitIsTrooperSquad(this ICombatant combatant)
        {
            if (!combatant.StatCollection.ContainsStatistic("CUTrooperSquad")) return false;
            return combatant.StatCollection.GetValue<bool>("CUTrooperSquad");
        }
    }

    public class UtilityPatches
    {
        [HarmonyPatch(typeof(MechDef))]
        [HarmonyPatch("MechTags", MethodType.Getter)]
        public static class MechDef_MechTags_Getter_DEBUG
        {
            public static bool Prepare() => ModInit.modSettings.debugPatchEnabled;

            public static void Postfix(MechDef __instance, ref TagSet __result)
            {
                if (UnityGameInstance.HasInstance)
                {
                    var combat = UnityGameInstance.BattleTechGame?.Combat;
                    if (combat == null) return;
                }
                
                if (string.IsNullOrEmpty(__instance?.Description?.Id))
                {
                    //ModInit.modLog?.Info?.Write($"[UtilityPatches_MechTag] .");
                    return;
                }
                if (!Utility.CachedTagSets.ContainsKey(__instance?.Description?.Id))
                {
                    Utility.CachedTagSets.Add(__instance?.Description?.Id, __result);
                    var values = $"\n";
                    foreach (var tag in __result)
                    {
                        values += $"\n{tag}";
                    }
                    ModInit.modLog?.Info?.Write($"[UtilityPatches_MechTag] TagSet for {__instance.Description.Id} did not exist in cache, inititalizing with values: {values}.");
                    return;
                }
                if (!Utility.CachedTagSets[__instance.Description.Id].Equals(__result))
                {
                    var values = $"\n";
                    foreach (var tag in __result)
                    {
                        values += $"\n{tag}";
                    }
                    ModInit.modLog?.Info?.Write($"[UtilityPatches_MechTag] Equality check failure, Tagset changed since init. Dumping stack trace and tagset: {Environment.StackTrace} TagSet Values: {values}");
                }
            }
        }
    }
}
