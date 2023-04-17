using System;
using static TisButAScratch.Framework.GlobalVars;
using BattleTech;
using BattleTech.UI;
using BattleTech.UI.Tooltips;
using TisButAScratch.Framework;
using UnityEngine;

namespace TisButAScratch.Patches
{
    public class DescriptionPatches
    {
        //pretty much copied from MechEngineer
        [HarmonyPatch(typeof(Pilot), "InjuryReasonDescription", MethodType.Getter)]
        public static class Pilot_InjuryReasonDescription_Patch // also add head-only injury reason?
        {
            public static InjuryReason InjuryReasonOverheat = (InjuryReason)666;
            public static InjuryReason InjuryReasonFeedback = (InjuryReason)667;
            public static void Postfix(Pilot __instance, ref string __result)
            {
                if (__instance.InjuryReason == InjuryReasonOverheat)
                {
                    __result = "OVERHEATED";
                }
                if (__instance.InjuryReason == InjuryReasonFeedback)
                {
                    __result = "NEURAL FEEDBACK";
                }
            }
        }

        [HarmonyPatch(typeof(SGBarracksRosterSlot), "Refresh")]
        
        public static class SGBarracksRosterSlot_Refresh_Patch
        {
            [HarmonyAfter(new string[] { "ca.jwolf.MechAffinity" })] //without whom this would not have happened
            public static void Postfix(SGBarracksRosterSlot __instance)
            {
                if (__instance.Pilot == null)
                {
                    return;
                }

                var displayAnyway = false;
                Pilot pilot = __instance.Pilot;
                var pKey = pilot.FetchGUID();

                if (PilotInjuryHolder.HolderInstance.pilotInjuriesMap.ContainsKey(pKey))
                {
                    if (PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey].Count > 0)
                    {
                        displayAnyway = true;
                    }
                }
 
                if (__instance.incapacitatedObj.GetComponentInChildren<HBSTooltip>(false) == null && !__instance.Pilot.pilotDef.PilotTags.Contains(DEBILITATEDTAG) && !displayAnyway) return;

                var tooltip = __instance.incapacitatedObj.GetComponentInChildren<HBSTooltip>(false);

                if (tooltip == null && (__instance.Pilot.pilotDef.PilotTags.Contains(DEBILITATEDTAG) || displayAnyway))
                {
                    __instance.incapacitatedObj.gameObject.SetActive(true);
                    tooltip = __instance.incapacitatedObj.GetComponentInChildren<HBSTooltip>(true);
                    tooltip.gameObject.SetActive(true);
                }


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
            public static void Postfix(SGBarracksDossierPanel __instance, Pilot p)
            {
                var displayAnyway = false;

                var pKey = p.FetchGUID();
                if (PilotInjuryHolder.HolderInstance.pilotInjuriesMap.ContainsKey(pKey))
                {
                    if (PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey].Count > 0)
                    {
                        displayAnyway = true;
                    }
                }

                if (__instance.injureBackground.GetComponentInChildren<HBSTooltip>(false) == null &&
                    !p.pilotDef.PilotTags.Contains(DEBILITATEDTAG) && !displayAnyway) return;
                if (displayAnyway && !__instance.injureBackground.activeSelf)
                {
                    __instance.injureBackground.SetActive(true);
                }

                if (p.pilotDef.PilotTags.Contains(DEBILITATEDTAG) && !__instance.injureBackground.activeSelf)
                {
                    __instance.timeoutBackground.SetActive(true);

                    var tooltip = __instance.timeoutBackground.GetComponentInChildren<HBSTooltip>(true);

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
                    var tooltip = __instance.injureBackground.GetComponentInChildren<HBSTooltip>(false);

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
            public static void Postfix(TaskManagementElement __instance)
            {
                if (__instance.entry.Type != WorkOrderType.MedLabHeal) return;
                if (!(__instance.entry is WorkOrderEntry_MedBayHeal medbayHealEntry) ||
                    medbayHealEntry.Pilot.Injuries == 0) return;
                __instance.subTitleText.SetText("INJURED");
                __instance.subTitleColor.SetUIColor(UIColor.RedHalf);
            }
        }

        [HarmonyPatch(typeof(TaskTimelineWidget), "OnTaskDetailsClicked")]
        public static class TaskTimelineWidget_OnTaskDetailsClicked_Patch
        {
            static void Postfix(TaskTimelineWidget __instance, TaskManagementElement element)
            {
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) return; //needed so Timeline mods AdvanceToTask works.
                if (element == null) return;
                if (element.Entry.Type != WorkOrderType.MedLabHeal) return;
                if (!(element.Entry is WorkOrderEntry_MedBayHeal medbayHealEntry) ||
                    medbayHealEntry.Pilot.Injuries == 0) return;
                var sim = UnityGameInstance.BattleTechGame.Simulation;
                sim.SetTimeMoving(false);

                var pilot = medbayHealEntry.Pilot;
                var Desc = $"<b>PILOT: {pilot.FirstName} \"{pilot.Callsign}\" {pilot.LastName}</b>\n";
                Desc += "<b>STATUS: INJURED</b>\n";
                Desc += $"<b>SUMMARY:</b>\n";
                Desc += $"{InjuryDescriptions.getPilotInjuryDescCompact(pilot)}";

                PauseNotification.Show("Medical Summary", Desc, pilot.GetPortraitSprite(), "", true, null);
            }
        }
    }
}
