using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static TisButAScratch.Framework.GlobalVars;
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

 //               if (___incapacitatedObj.GetComponentInChildren<HBSTooltip>(false) == null) return;
                if (___incapacitatedObj.GetComponentInChildren<HBSTooltip>(false) == null && !__instance.Pilot.pilotDef.PilotTags.Contains(CrippledTag)) return;

                var tooltip = ___incapacitatedObj.GetComponentInChildren<HBSTooltip>(false);

                if (tooltip == null && __instance.Pilot.pilotDef.PilotTags.Contains(CrippledTag))
                {
                    ___incapacitatedObj.gameObject.SetActive(true);
                       tooltip = ___incapacitatedObj.GetComponentInChildren<HBSTooltip>(true);
                    tooltip.gameObject.SetActive(true);
                }

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

        [HarmonyPatch(typeof(SGBarracksDossierPanel), "SetPilot",
            new Type[] {typeof(Pilot), typeof(SGBarracksMWDetailPanel), typeof(bool), typeof(bool)})]
        [HarmonyAfter(new string[] {"ca.jwolf.MechAffinity"})] //without whom this would not have happened
        public static class SGBarracksDossierPanel_SetPilot_Patch
        {
            public static void Postfix(SGBarracksDossierPanel __instance, Pilot p, GameObject ___injureBackground,
                GameObject ___timeoutBackground)
            {
//                if (___injureBackground.GetComponentInChildren<HBSTooltip>(false) == null) return; //this was original, worked with injuries but not crippled

                if (___injureBackground.GetComponentInChildren<HBSTooltip>(false) == null &&
                    !p.pilotDef.PilotTags.Contains(CrippledTag)) return;

                if (p.pilotDef.PilotTags.Contains(CrippledTag) && !___injureBackground.activeSelf)
                {
                    ___timeoutBackground.SetActive(true);

                    var tooltip = ___timeoutBackground.GetComponentInChildren<HBSTooltip>(true);

                    tooltip.gameObject.SetActive(true);

                    string Desc = tooltip.GetText();
                    if (String.IsNullOrEmpty(Desc))
                    {
                        Desc = "";
                    }

                    Desc += "<b>Injuries:</b>";
                    Desc += InjuryDescriptions.getPilotInjuryDesc(p);

                    var descDef = new BaseDescriptionDef("Injuries", p.Callsign, Desc, null);
                    tooltip.SetDefaultStateData(TooltipUtilities.GetStateDataFromObject(descDef));
                    return;
                }

                else
                {
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
