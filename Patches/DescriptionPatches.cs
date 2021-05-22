﻿using System;
using static TisButAScratch.Framework.GlobalVars;
using BattleTech;
using BattleTech.UI;
using BattleTech.UI.TMProWrapper;
using BattleTech.UI.Tooltips;
using TisButAScratch.Framework;
using Harmony;
using UnityEngine;

namespace TisButAScratch.Patches
{
    public class DescriptionPatches
    {
        [HarmonyPatch(typeof(SGBarracksRosterSlot), "Refresh")]
        
        public static class SGBarracksRosterSlot_Refresh_Patch
        {
            [HarmonyAfter(new string[] { "ca.jwolf.MechAffinity" })] //without whom this would not have happened
            public static void Postfix(SGBarracksRosterSlot __instance, GameObject ___incapacitatedObj)
            {
                if (__instance.Pilot == null)
                {
                    return;
                }

 //               if (___incapacitatedObj.GetComponentInChildren<HBSTooltip>(false) == null) return;
                if (___incapacitatedObj.GetComponentInChildren<HBSTooltip>(false) == null && !__instance.Pilot.pilotDef.PilotTags.Contains(DEBILITATEDTAG)) return;

                var tooltip = ___incapacitatedObj.GetComponentInChildren<HBSTooltip>(false);

                if (tooltip == null && __instance.Pilot.pilotDef.PilotTags.Contains(DEBILITATEDTAG))
                {
                    ___incapacitatedObj.gameObject.SetActive(true);
                       tooltip = ___incapacitatedObj.GetComponentInChildren<HBSTooltip>(true);
                    tooltip.gameObject.SetActive(true);
                }

                Pilot pilot = __instance.Pilot;
                string Desc = tooltip?.GetText();
                if (String.IsNullOrEmpty(Desc))
                {
                    Desc = "";
                }

                Desc += "<b>Injuries:</b>\n";
                Desc += InjuryDescriptions.getPilotInjuryDesc(pilot);
                
                var descDef = new BaseDescriptionDef("Injuries", pilot.Callsign, Desc, null);
                tooltip?.SetDefaultStateData(TooltipUtilities.GetStateDataFromObject(descDef));
            }
        }

        [HarmonyPatch(typeof(SGBarracksDossierPanel), "SetPilot",
            new Type[] {typeof(Pilot), typeof(SGBarracksMWDetailPanel), typeof(bool), typeof(bool)})]
        
        public static class SGBarracksDossierPanel_SetPilot_Patch
        {
            [HarmonyAfter(new string[] { "ca.jwolf.MechAffinity" })] //without whom this would not have happened
            public static void Postfix(SGBarracksDossierPanel __instance, Pilot p, GameObject ___injureBackground,
                GameObject ___timeoutBackground)
            {
//                if (___injureBackground.GetComponentInChildren<HBSTooltip>(false) == null) return; //this was original, worked with injuries but not crippled

                if (___injureBackground.GetComponentInChildren<HBSTooltip>(false) == null &&
                    !p.pilotDef.PilotTags.Contains(DEBILITATEDTAG)) return;

                if (p.pilotDef.PilotTags.Contains(DEBILITATEDTAG) && !___injureBackground.activeSelf)
                {
                    ___timeoutBackground.SetActive(true);

                    var tooltip = ___timeoutBackground.GetComponentInChildren<HBSTooltip>(true);

                    tooltip.gameObject.SetActive(true);

                    string Desc = tooltip.GetText();
                    if (String.IsNullOrEmpty(Desc))
                    {
                        Desc = "";
                    }

                    Desc += "<b>Injuries:</b>\n";
                    Desc += InjuryDescriptions.getPilotInjuryDesc(p);

                    var descDef = new BaseDescriptionDef("Injuries", p.Callsign, Desc, null);
                    tooltip.SetDefaultStateData(TooltipUtilities.GetStateDataFromObject(descDef));
                }

                else
                {
                    var tooltip = ___injureBackground.GetComponentInChildren<HBSTooltip>(false);

                    string Desc = tooltip.GetText();
                    if (String.IsNullOrEmpty(Desc))
                    {
                        Desc = "";
                    }

                    Desc += "<b>Injuries:</b>\n";
                    Desc += InjuryDescriptions.getPilotInjuryDesc(p);

                    var descDef = new BaseDescriptionDef("Injuries", p.Callsign, Desc, null);
                    tooltip.SetDefaultStateData(TooltipUtilities.GetStateDataFromObject(descDef));
                }
            }
        }

        [HarmonyPatch(typeof(TaskManagementElement), "UpdateTaskInfo")]
        
        public static class TaskManagementElement_UpdateTaskInfo_Patch
        {
            [HarmonyPriority(Priority.Last)]
            public static void Postfix(TaskManagementElement __instance, WorkOrderEntry ___entry, LocalizableText ___subTitleText, UIColorRefTracker ___subTitleColor)
            {
                if (___entry.Type != WorkOrderType.MedLabHeal) return;
                if (!(___entry is WorkOrderEntry_MedBayHeal medbayHealEntry) ||
                    medbayHealEntry.Pilot.Injuries == 0) return;
                ___subTitleText.SetText("INJURED");
                ___subTitleColor.SetUIColor(UIColor.RedHalf);
            }
        }
    }
}
