using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using BattleTech;
using BattleTech.UI;
using BattleTech.UI.TMProWrapper;
using BattleTech.UI.Tooltips;
using TisButAScratch.Framework;
using Harmony;
using JetBrains.Annotations;
using UnityEngine;

namespace TisButAScratch.Patches
{
    class DescriptionPatches
    {
        [HarmonyPatch(typeof(SGBarracksRosterSlot), "Refresh")]
        [HarmonyAfter(new string[] {"ca.jwolf.MechAffinity"})] //without whom this would not have happened

        public static class SGBarracksRosterSlot_Refresh_Patch
        {
            public static void Postfix(SGBarracksRosterSlot __instance, GameObject ___incapacitatedObj)
            {
                if (__instance.Pilot == null)
                {
                    return;
                }

                if (___incapacitatedObj.GetComponentInChildren<HBSTooltip>(false) == null) return;
                var tooltip = ___incapacitatedObj.GetComponentInChildren<HBSTooltip>(false);


                Pilot pilot = __instance.Pilot;
                string Desc = tooltip.GetText();
                if (String.IsNullOrEmpty(Desc))
                {
                    Desc = "";
                }

                Desc += "<b>Injuries:</b>";
                Desc += InjuryDescriptions.getPilotInjuryDesc(pilot);
                
                var descDef = new BaseDescriptionDef("Injuries", pilot.Callsign, Desc, null);
                tooltip.SetDefaultStateData(TooltipUtilities.GetStateDataFromObject(descDef));
            }
        }

        [HarmonyPatch(typeof(SGBarracksDossierPanel), "SetPilot", new Type[]{typeof(Pilot), typeof(SGBarracksMWDetailPanel), typeof(bool), typeof(bool)})]
        [HarmonyAfter(new string[] {"ca.jwolf.MechAffinity"})] //without whom this would not have happened
        public static class SGBarracksDossierPanel_SetPilot_Patch
        {
            public static void Postfix(SGBarracksDossierPanel __instance, Pilot p, GameObject ___injureBackground)
            {
                if (___injureBackground.GetComponentInChildren<HBSTooltip>(false) == null) return;
                var tooltip = ___injureBackground.GetComponentInChildren<HBSTooltip>(false);

                string Desc = tooltip.GetText();
                if (String.IsNullOrEmpty(Desc))
                {
                    Desc = "";
                }

                Desc += "<b>Injuries:</b>";
                Desc += InjuryDescriptions.getPilotInjuryDesc(p);

                var descDef = new BaseDescriptionDef("Injuries", p.Callsign, Desc, null);
                tooltip.SetDefaultStateData(TooltipUtilities.GetStateDataFromObject(descDef));
            }
        }
        [HarmonyPatch(typeof(TaskManagementElement), "UpdateTaskInfo")]
        [HarmonyAfter(new string[] { "ca.jwolf.MechAffinity" })] //without whom this would not have happened

        public static class TaskManagementElement_UpdateTaskInfo_Patch
        {
            public static void Postfix(TaskManagementElement __instance, WorkOrderEntry ___entry, LocalizableText ___subTitleText)
            {
                if (___entry.Type == WorkOrderType.MedLabHeal)
                {
                    ___subTitleText.SetText("INJURED");

                }
            }
        }
    }
}
