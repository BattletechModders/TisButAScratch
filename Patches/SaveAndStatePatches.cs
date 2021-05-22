﻿using System;
using System.Collections.Generic;
using System.Linq;
using Harmony;
using BattleTech;
using TisButAScratch.Framework;
using static TisButAScratch.Framework.GlobalVars;
using BattleTech.Save;
using BattleTech.Save.Test;
using BattleTech.UI;

namespace TisButAScratch.Patches
{
    public class SaveAndStatePatches
    {

        [HarmonyPatch(typeof(SGCharacterCreationCareerBackgroundSelectionPanel), "Done")]

        public static class SGCharacterCreationCareerBackgroundSelectionPanel_Done_Patch
        {
            public static void Postfix(SGCharacterCreationCareerBackgroundSelectionPanel __instance)
            {
                PilotInjuryManager.PreloadIcons();
                sim = UnityGameInstance.BattleTechGame.Simulation;
//                var curPilots = new List<string>();

                if (!sim.Commander.pilotDef.PilotTags.Any(x => x.StartsWith(iGUID)))
                {
                    //sim.Commander.pilotDef.PilotTags.Add($"{iGUID}{sim.Commander.Description.Id}{sim.GenerateSimGameUID()}");
                    sim.Commander.pilotDef.PilotTags.Add($"{iGUID}{sim.Commander.Description.Id}{Guid.NewGuid()}");
                }

                var pKey = sim.Commander.FetchGUID();
//                curPilots.Add(pKey);
                if (!PilotInjuryHolder.HolderInstance.pilotInjuriesMap.ContainsKey(pKey))
                {
                    PilotInjuryHolder.HolderInstance.pilotInjuriesMap.Add(pKey, new List<string>());
                    ModInit.modLog.LogMessage($"Added Commander to pilotInjuriesMap with iGUID {pKey}");
                }


                foreach (Pilot p in sim.PilotRoster)
                {

                    if (!p.pilotDef.PilotTags.Any(x => x.StartsWith(iGUID)))
                    {
                        // p.pilotDef.PilotTags.Add($"{iGUID}{p.Description.Id}{sim.GenerateSimGameUID()}");
                        p.pilotDef.PilotTags.Add($"{iGUID}{p.Description.Id}{Guid.NewGuid()}");
                    }

                    pKey = p.FetchGUID();
//                    curPilots.Add(pKey);
                    if (!PilotInjuryHolder.HolderInstance.pilotInjuriesMap.ContainsKey(pKey))
                    {
                        PilotInjuryHolder.HolderInstance.pilotInjuriesMap.Add(pKey, new List<string>());
                        ModInit.modLog.LogMessage($"{p.Callsign} missing, added to pilotInjuriesMap with iGUID {pKey}");
                    }
                }
            }
        }

        [HarmonyPatch(typeof(SimGameState), "Dehydrate",
            new Type[] {typeof(SimGameSave), typeof(SerializableReferenceContainer)})]
        public static class SGS_Dehydrate_Patch
        {
            public static void Prefix(SimGameState __instance)
            {
                PilotInjuryManager.PreloadIcons();
                sim = __instance;
                var curPilots = new List<string>();

                if (!sim.Commander.pilotDef.PilotTags.Any(x => x.StartsWith(iGUID)))
                {
                    //sim.Commander.pilotDef.PilotTags.Add($"{iGUID}{sim.Commander.Description.Id}{sim.GenerateSimGameUID()}");
                    sim.Commander.pilotDef.PilotTags.Add($"{iGUID}{sim.Commander.Description.Id}{Guid.NewGuid()}");
                }

                var pKey = sim.Commander.FetchGUID();
                curPilots.Add(pKey);
                if (!PilotInjuryHolder.HolderInstance.pilotInjuriesMap.ContainsKey(pKey))
                {
                    PilotInjuryHolder.HolderInstance.pilotInjuriesMap.Add(pKey, new List<string>());
                    ModInit.modLog.LogMessage($"Added Commander to pilotInjuriesMap with iGUID {pKey}");
                }


                foreach (Pilot p in sim.PilotRoster)
                {

                    if (!p.pilotDef.PilotTags.Any(x => x.StartsWith(iGUID)))
                    {
                        // p.pilotDef.PilotTags.Add($"{iGUID}{p.Description.Id}{sim.GenerateSimGameUID()}");
                        p.pilotDef.PilotTags.Add($"{iGUID}{p.Description.Id}{Guid.NewGuid()}");
                    }

                    pKey = p.FetchGUID();
                    curPilots.Add(pKey);
                    if (!PilotInjuryHolder.HolderInstance.pilotInjuriesMap.ContainsKey(pKey))
                    {
                        PilotInjuryHolder.HolderInstance.pilotInjuriesMap.Add(pKey, new List<string>());
                        ModInit.modLog.LogMessage($"{p.Callsign} missing, added to pilotInjuriesMap with iGUID {pKey}");
                    }
                }

                var rm = PilotInjuryHolder.HolderInstance.pilotInjuriesMap.Keys.Where(x => !curPilots.Contains(x));
                foreach (var key in new List<string>(rm))
                {
                    PilotInjuryHolder.HolderInstance.pilotInjuriesMap.Remove(key);
                    ModInit.modLog.LogMessage(
                        $"Pilot with pilotID {key} not in roster, removing from pilotInjuriesMap");
                }

                var rm2 = new List<string>(
                    PilotInjuryHolder.HolderInstance.pilotInjuriesMap.Keys.Where(x => x.EndsWith(aiPilotFlag)));
                foreach (var key in rm2)
                {
                    PilotInjuryHolder.HolderInstance.pilotInjuriesMap.Remove(key);
                    ModInit.modLog.LogMessage($"Pilot with pilotID {key} was AI Pilot, removing from pilotInjuriesMap");
                }

                PilotInjuryHolder.HolderInstance.SerializeInjuryState();
                PilotInjuryHolder.HolderInstance.combatInjuriesMap = new Dictionary<string, List<string>>();
            }
        }

        [HarmonyPatch(typeof(SimGameState), "Rehydrate", new Type[] {typeof(GameInstanceSave)})]
        public static class SGS_Rehydrate_Patch
        {
            public static void Prefix(SimGameState __instance)
            {
                sim?.CompanyTags.Clear(); // this will either help or hurt.  or it wont do a damn thing.
            }

            public static void Postfix(SimGameState __instance)
            {
                sim = __instance;
                PilotInjuryManager.PreloadIcons();
                var curPilots = new List<string>();
                PilotInjuryHolder.HolderInstance.DeserializeInjuryState();
                PilotInjuryHolder.HolderInstance.combatInjuriesMap = new Dictionary<string, List<string>>();
                ModInit.modLog.LogMessage($"Successfully deserialized or determined deserializing unnecessary.");

                if (!sim.Commander.pilotDef.PilotTags.Any(x => x.StartsWith(iGUID)))
                {
                    //sim.Commander.pilotDef.PilotTags.Add($"{iGUID}{sim.Commander.Description.Id}{sim.GenerateSimGameUID()}");
                    sim.Commander.pilotDef.PilotTags.Add($"{iGUID}{sim.Commander.Description.Id}{Guid.NewGuid()}");
                }

                var pKey = sim.Commander.FetchGUID();

                ModInit.modLog.LogMessage($"Fetched Commander iGUID {pKey}");
                curPilots.Add(pKey);
                if (!PilotInjuryHolder.HolderInstance.pilotInjuriesMap.ContainsKey(pKey))
                {
                    PilotInjuryHolder.HolderInstance.pilotInjuriesMap.Add(pKey, new List<string>());
                    ModInit.modLog.LogMessage($"Added Commander to pilotInjuriesMap with iGUID {pKey}");
                }

                foreach (var id in new List<string>(PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey]))
                {
                    if (PilotInjuryManager.ManagerInstance.InjuryEffectsList.All(x => x.injuryID != id) &&
                        PilotInjuryManager.ManagerInstance.InternalDmgInjuries.All(x => x.injuryID != id))
                    {
                        PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey].Remove(id);
                        ModInit.modLog.LogMessage($"Removed deprecated injury from Commander with id {id}");
                    }
                }

                // this bigass clusterfuck is just for if you have existing injuries when first loading up TBAS
                if (PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey].Count <
                    sim.Commander.StatCollection.GetValue<int>("Injuries"))
                {
                    var dmg = sim.Commander.StatCollection.GetValue<int>("Injuries") -
                              PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey].Count;
                    ModInit.modLog.LogMessage($"Commander is missing {dmg} injuries. Rerolling.");
                    PilotInjuryManager.ManagerInstance.rollInjurySG(sim.Commander, dmg, DamageType.Unknown);
                    if (ModInit.modSettings.debilSeverityThreshold > 0
                    ) //now trying to add up "severity" threshold for crippled injury
                    {

                        var injuryList = new List<Injury>();
                        foreach (var id in PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey])
                        {
                            injuryList.AddRange(
                                PilotInjuryManager.ManagerInstance.InjuryEffectsList.Where(x => x.injuryID == id));
                        }

                        var groupedLocs = injuryList.GroupBy(x => x.injuryLoc);

                        foreach (var injuryLoc in groupedLocs)
                        {
                            var t = 0;
                            foreach (var injury in injuryLoc)
                            {
                                t += injury.severity;
                            }

                            if (t >= ModInit.modSettings.debilSeverityThreshold)
                            {
                                sim.Commander.pilotDef.PilotTags.Add(DEBILITATEDTAG);
                                if (ModInit.modSettings.enableLethalTorsoHead && (injuryLoc.Key == InjuryLoc.Head ||
                                    injuryLoc.Key == InjuryLoc.Torso))
                                {
                                    sim.Commander.StatCollection.ModifyStat<bool>("TBAS_Injuries", 0, "LethalInjury",
                                        StatCollection.StatOperation.Set, true);
                                }
                            }
                        }
                    }

                    var rm2 = new List<string>(
                        PilotInjuryHolder.HolderInstance.pilotInjuriesMap.Keys.Where(x => x.EndsWith(aiPilotFlag)));
                    foreach (var key in rm2)
                    {
                        PilotInjuryHolder.HolderInstance.pilotInjuriesMap.Remove(key);
                        ModInit.modLog.LogMessage(
                            $"Pilot with pilotID {key} was AI Pilot, removing from pilotInjuriesMap");
                    }
                }



                foreach (Pilot p in sim.PilotRoster)
                {

                    if (!p.pilotDef.PilotTags.Any(x => x.StartsWith(iGUID)))
                    {
                        //p.pilotDef.PilotTags.Add($"{iGUID}{p.Description.Id}{sim.GenerateSimGameUID()}");
                        p.pilotDef.PilotTags.Add($"{iGUID}{p.Description.Id}{Guid.NewGuid()}");
                        ModInit.modLog.LogMessage($"Added {p.Callsign} iGUID tag");
                    }

                    pKey = p.FetchGUID();
                    ModInit.modLog.LogMessage($"Fetched {p.Callsign} iGUID {pKey}");
                    curPilots.Add(pKey);
                    if (!PilotInjuryHolder.HolderInstance.pilotInjuriesMap.ContainsKey(pKey))
                    {
                        PilotInjuryHolder.HolderInstance.pilotInjuriesMap.Add(pKey, new List<string>());
                        ModInit.modLog.LogMessage($"{p.Callsign} missing, added to pilotInjuriesMap with iGUID {pKey}");
                    }

                    foreach (var id in new List<string>(PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey]))
                    {
                        if (PilotInjuryManager.ManagerInstance.InjuryEffectsList.All(x => x.injuryID != id) &&
                            PilotInjuryManager.ManagerInstance.InternalDmgInjuries.All(x => x.injuryID != id))
                        {
                            PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey].Remove(id);
                            ModInit.modLog.LogMessage($"Removed deprecated injury from {p.Callsign} with id {id}");
                        }
                    }

                    // this bigass clusterfuck is just for if you have existing injuries when first loading up TBAS
                    if (PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey].Count <
                        p.StatCollection.GetValue<int>("Injuries"))
                    {
                        var dmg = p.StatCollection.GetValue<int>("Injuries") -
                                  PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey].Count;
                        ModInit.modLog.LogMessage($"{p.Callsign} is missing {dmg} injuries. Rerolling.");
                        PilotInjuryManager.ManagerInstance.rollInjurySG(p, dmg, DamageType.Unknown);
                        if (ModInit.modSettings.debilSeverityThreshold > 0
                        ) //now trying to add up "severity" threshold for crippled injury
                        {

                            var injuryList = new List<Injury>();
                            foreach (var id in PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey])
                            {
                                injuryList.AddRange(
                                    PilotInjuryManager.ManagerInstance.InjuryEffectsList.Where(
                                        x => x.injuryID == id));
                            }

                            var groupedLocs = injuryList.GroupBy(x => x.injuryLoc);

                            foreach (var injuryLoc in groupedLocs)
                            {
                                var t = 0;
                                foreach (var injury in injuryLoc)
                                {
                                    t += injury.severity;
                                }

                                if (t > ModInit.modSettings.debilSeverityThreshold)
                                {
                                    sim.Commander.pilotDef.PilotTags.Add(DEBILITATEDTAG);
                                    if (ModInit.modSettings.enableLethalTorsoHead &&
                                        (injuryLoc.Key == InjuryLoc.Head ||
                                         injuryLoc.Key == InjuryLoc.Torso))
                                    {
                                        sim.Commander.StatCollection.ModifyStat<bool>("TBAS_Injuries", 0,
                                            "LethalInjury",
                                            StatCollection.StatOperation.Set, true);
                                    }
                                }
                            }
                        }
                    }
                }

                var rm = PilotInjuryHolder.HolderInstance.pilotInjuriesMap.Keys.Where(x => !curPilots.Contains(x));
                foreach (var key in new List<string>(rm))
                {
                    PilotInjuryHolder.HolderInstance.pilotInjuriesMap.Remove(key);
                    ModInit.modLog.LogMessage(
                        $"Pilot with pilotID {key} not in roster, removing from pilotInjuriesMap");
                }


//                foreach (var pilot in new List<Pilot>(sim.PilotRoster))
//                {
//                    if (pilot.GUID == sim.Commander.GUID)
//                    {
//                        sim.PilotRoster.Remove(pilot);
//                        ModInit.modLog.LogMessage($"Removed a commander from pilotRoster");
//                    }
//                }
            }
        }

        [HarmonyPatch(typeof(SimGameState), "ApplySimGameEventResult",
            new Type[] {typeof(SimGameEventResult), typeof(List<object>), typeof(SimGameEventTracker)})]
        [HarmonyPriority(Priority.Last)]
        public static class SGS_ApplySimGameEventResult
        {
            public static void Postfix(SimGameState __instance, SimGameEventResult result, List<object> objects,
                SimGameEventTracker tracker)
            {
                for (var i = 0; i < objects.Count; i++)
                {
                    var obj = objects[i];
                    if (result.Scope == EventScope.MechWarrior || result.Scope == EventScope.SecondaryMechWarrior ||
                        result.Scope == EventScope.TertiaryMechWarrior)
                    {
                        var p = (Pilot) obj;
                        var pKey = p.FetchGUID();
                        if (PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey].Count <
                            p.StatCollection.GetValue<int>("Injuries"))
                        {
                            var dmg = p.StatCollection.GetValue<int>("Injuries") -
                                      PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey].Count;
                            ModInit.modLog.LogMessage($"{p.Callsign} is missing {dmg} injuries. Rerolling.");
                            PilotInjuryManager.ManagerInstance.rollInjurySG(p, dmg, DamageType.Unknown);
                            if (ModInit.modSettings.debilSeverityThreshold > 0
                            ) //now trying to add up "severity" threshold for crippled injury
                            {

                                var injuryList = new List<Injury>();
                                foreach (var id in PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey])
                                {
                                    injuryList.AddRange(
                                        PilotInjuryManager.ManagerInstance.InjuryEffectsList.Where(
                                            x => x.injuryID == id));
                                }

                                var groupedLocs = injuryList.GroupBy(x => x.injuryLoc);

                                foreach (var injuryLoc in groupedLocs)
                                {
                                    var t = 0;
                                    foreach (var injury in injuryLoc)
                                    {
                                        t += injury.severity;
                                    }

                                    if (t > ModInit.modSettings.debilSeverityThreshold)
                                    {
                                        p.pilotDef.PilotTags.Add(DEBILITATEDTAG);
                                        if (ModInit.modSettings.enableLethalTorsoHead &&
                                            (injuryLoc.Key == InjuryLoc.Head ||
                                             injuryLoc.Key == InjuryLoc.Torso))
                                        {
                                            p.StatCollection.ModifyStat<bool>("TBAS_Injuries", 0,
                                                "LethalInjury",
                                                StatCollection.StatOperation.Set, true);
                                        }
                                    }
                                }
                            }

//                            p.ForceRefreshDef(); // added 1221
//                            __instance.RefreshInjuries(); // added 1221
                        }
                    }
                    else if (result.Scope == EventScope.Commander)
                    {
                        var commander = (Pilot) obj;
                        if (string.IsNullOrEmpty(commander.FetchGUID())) return;
                        var pKey = commander.FetchGUID();
                        if (PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey].Count <
                            commander.StatCollection.GetValue<int>("Injuries"))
                        {
                            var dmg = commander.StatCollection.GetValue<int>("Injuries") -
                                      PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey].Count;
                            ModInit.modLog.LogMessage($"Commander is missing {dmg} injuries. Rerolling.");
                            PilotInjuryManager.ManagerInstance.rollInjurySG(commander, dmg,
                                DamageType.Unknown);
                            if (ModInit.modSettings.debilSeverityThreshold > 0
                            ) //now trying to add up "severity" threshold for crippled injury
                            {

                                var injuryList = new List<Injury>();
                                foreach (var id in PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey])
                                {
                                    injuryList.AddRange(
                                        PilotInjuryManager.ManagerInstance.InjuryEffectsList.Where(
                                            x => x.injuryID == id));
                                }

                                var groupedLocs = injuryList.GroupBy(x => x.injuryLoc);

                                foreach (var injuryLoc in groupedLocs)
                                {
                                    var t = 0;
                                    foreach (var injury in injuryLoc)
                                    {
                                        t += injury.severity;
                                    }

                                    if (t >= ModInit.modSettings.debilSeverityThreshold)
                                    {
                                        commander.pilotDef.PilotTags.Add(DEBILITATEDTAG);
                                        if (ModInit.modSettings.enableLethalTorsoHead &&
                                            (injuryLoc.Key == InjuryLoc.Head ||
                                             injuryLoc.Key == InjuryLoc.Torso))
                                        {
                                            commander.StatCollection.ModifyStat<bool>("TBAS_Injuries", 0,
                                                "LethalInjury",
                                                StatCollection.StatOperation.Set, true);
                                        }
                                    }
                                }
                            }
                        }

//                        commander.ForceRefreshDef(); // added 1221
//                        __instance.RefreshInjuries(); // added 1221
                    }
                }
            }
        }

        [HarmonyPatch(typeof(SimGameState), "AddPilotToRoster",
            new Type[] {typeof(PilotDef), typeof(bool), typeof(bool)})]
        public static class SGS_AddPilotToRoster_Patch
        {
            public static void Postfix(SimGameState __instance, PilotDef def, bool updatePilotDiscardPile = false,
                bool initialHiringDontSpawnMessage = false)
            {
                var p = __instance.PilotRoster.FirstOrDefault(x => x.pilotDef.Description.Id == def.Description.Id);
                if (p != null && !p.pilotDef.PilotTags.Any(x => x.StartsWith(iGUID)))
                {
                    // p.pilotDef.PilotTags.Add($"{iGUID}{p.Description.Id}{__instance.GenerateSimGameUID()}");
                    p.pilotDef.PilotTags.Add($"{iGUID}{p.Description.Id}{Guid.NewGuid()}");
                    ModInit.modLog.LogMessage($"Added {p.Callsign} iGUID tag");
                }

                var pKey = p.FetchGUID();
                if (!PilotInjuryHolder.HolderInstance.pilotInjuriesMap.ContainsKey(pKey))
                {
                    PilotInjuryHolder.HolderInstance.pilotInjuriesMap.Add(pKey, new List<string>());
                    if (p != null) ModInit.modLog.LogMessage($"{p.Callsign} missing, added to pilotInjuriesMap");
                }
            }
        }

        [HarmonyPatch(typeof(AbstractActor), "InitEffectStats")]
        public static class AbstractActor_InitEffectStats_Patch
        {
            public static void Postfix(AbstractActor __instance)
            {
                var p = __instance.GetPilot();
                p.StatCollection.AddStatistic<bool>("NeedsFeedbackInjury", false);
                p.StatCollection.AddStatistic<bool>("BledOut", false);
                p.StatCollection.AddStatistic<int>("internalDmgInjuryCount", 0);
                p.StatCollection.AddStatistic<int>(MissionKilledStat, 0);
                p.StatCollection.AddStatistic<int>("BloodBank", p.CalcBloodBank());
                p.StatCollection.AddStatistic<int>("BloodCapacity", p.CalcBloodBank());
                p.StatCollection.AddStatistic<float>("BleedingRate", 0f);
                p.StatCollection.AddStatistic<List<string>>("LastInjuryId", new List<string>());
                __instance.StatCollection.AddStatistic<bool>(ModInit.modSettings.internalDmgStatName, false);
                __instance.StatCollection.AddStatistic<bool>(ModInit.modSettings.isTorsoMountStatName, false);
                __instance.StatCollection.AddStatistic<bool>(ModInit.modSettings.DisableBleedingStat, false);
                __instance.StatCollection.AddStatistic<bool>(ModInit.modSettings.NullifiesInjuryEffectsStat, false);
                ModInit.modLog.LogMessage(
                    $"Initializing {p.Callsign} EffectStat `NeedsFeedbackInjury`, `BledOut`, `internalDmgInjuryCount`, `{MissionKilledStat}`, `LastInjuryId`, `{ModInit.modSettings.internalDmgStatName}`, and `{ModInit.modSettings.isTorsoMountStatName}`");
            }
        }

        //adding AI units (i.e., not commander and not your pilotroster) to pilotInjuriesMap
        [HarmonyPatch(typeof(Team), "AddUnit", new Type[] {typeof(AbstractActor)})]
        public static class Team_AddUnit
        {
            public static void Postfix(Team __instance, AbstractActor unit)
            {
                //still need to make AI GUID end with aiPilotFlag
                var p = unit.GetPilot();

                if (!p.pilotDef.PilotTags.Any(x => x.StartsWith(iGUID)))
                {
                    p.pilotDef.PilotTags.Add(
                        $"{iGUID}{p.Description.Id}{Guid.NewGuid()}{aiPilotFlag}"); //changed to sys NewGuid instead of simguid for skirmish compatibility
                    ModInit.modLog.LogMessage($"Added {p.Callsign} iGUID tag");
                }

                var pKey = p.FetchGUID();
                ModInit.modLog.LogMessage($"Fetched {p.Callsign} iGUID");

                if (!PilotInjuryHolder.HolderInstance.combatInjuriesMap.ContainsKey(pKey))
                {
                    PilotInjuryHolder.HolderInstance.combatInjuriesMap.Add(pKey, new List<string>());
                    ModInit.modLog.LogMessage($"{p.Name} missing, added to combatInjuriesMap");
                }

                if (!PilotInjuryHolder.HolderInstance.pilotInjuriesMap.ContainsKey(pKey))
                {
                    PilotInjuryHolder.HolderInstance.pilotInjuriesMap.Add(pKey, new List<string>());
                    ModInit.modLog.LogMessage($"{p.Name} missing, added to pilotInjuriesMap");
                }

                PilotInjuryManager.ManagerInstance.GatherAndApplyInjuries(unit);
                ModInit.modLog.LogMessage($"Initializing injury effects for {p.Description?.Callsign}");

                if (PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey].Count >
                    p.StatCollection.GetValue<int>("Injuries"))
                {
                    ModInit.modLog.LogMessage(
                        $"{p.Callsign}'s Injury stat < existing injuries. Adding to Injury stat.");
                    p.StatCollection.ModifyStat<int>("TBAS_Injuries", -1,
                        "Injuries",
                        StatCollection.StatOperation.Set, PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey].Count);
                }
            }
        }

        //resetting combatinjuriesMap on restart
        [HarmonyPatch(typeof(LoadTransitioning), "BeginCombatRestart", new Type[] {typeof(Contract)})]
        public static class LoadTransitioning_BeginCombatRestart_Patch
        {
            public static void Prefix(Contract __instance)
            {
                PilotInjuryHolder.HolderInstance.bloodStatForSimGame = new Dictionary<string, float>();
                PilotInjuryHolder.HolderInstance.combatInjuriesMap = new Dictionary<string, List<string>>();
                ModInit.modLog.LogMessage(
                    $"Resetting combatInjuriesMap due to RestartMission button. Somebody must like CTD's.");
            }
        }


        [HarmonyPatch(typeof(Contract), "CompleteContract", new Type[] {typeof(MissionResult), typeof(bool)})]
        public static class Contract_CompleteContract_Patch
        {
            [HarmonyAfter(new string[] {"us.tbone.TrainingMissions"})]
            public static void Prefix(Contract __instance, MissionResult result, bool isGoodFaithEffort)
            {
                var sim = UnityGameInstance.BattleTechGame.Simulation;
                var playerPilots = new List<Pilot>();
                playerPilots.AddRange(sim.PilotRoster);
                playerPilots.Add(sim.Commander);
                var actors = UnityGameInstance.BattleTechGame.Combat.AllActors.Where(x => x.team.IsLocalPlayer);
                foreach (var actor in actors)
                {
                    var p = actor.GetPilot();
                    var pKey = p.FetchGUID();
                    if (string.IsNullOrEmpty(pKey)) continue;
                    if (playerPilots.All(x => x.pilotDef.Description.Id != p.pilotDef.Description.Id)) continue;
//                   if (p.pilotDef.PilotTags.Any(x => x.EndsWith(aiPilotFlag)))
//                   {
//                        p.pilotDef.PilotTags.Remove(DEBILITATEDTAG);
//                        ModInit.modLog.LogMessage($"Removing CrippledTag from AI pilot {p.Callsign} if present");
//                        var rmt = p.pilotDef.PilotTags.Where(x => x.EndsWith(aiPilotFlag));
//                        p.pilotDef.PilotTags.RemoveRange(rmt);
//                        ModInit.modLog.LogMessage($"Removing AI GUID Tag from AI pilot {p.Callsign} if present");
//                        continue;
//                    }
                    //now only adding to pilotInjuryMap at contract resolution instead of on the fly.

                    PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey]
                        .AddRange(PilotInjuryHolder.HolderInstance.combatInjuriesMap[pKey]);
                    ModInit.modLog.LogMessage($"Adding {p.Callsign}'s combatInjuryMap to their pilotInjuryMap");


                    foreach (var inj in PilotInjuryManager.ManagerInstance.InjuryEffectsList.Where(x =>
                        x.injuryID_Post != ""))
                    {
                        if (PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey].Contains(inj.injuryID))
                        {
                            PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey].Remove(inj.injuryID);
                            ModInit.modLog.LogMessage(
                                $"Removed {inj.injuryName} with bleeding effect from {p.Callsign}");
                            PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey].Add(inj.injuryID_Post);
                            ModInit.modLog.LogMessage(
                                $"Added {inj.injuryID_Post} to {p.Callsign} for post-combat injury");
                        }
                    }

                    if (p.StatCollection.GetValue<int>("Injuries") <
                        PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey].Count)
                    {
                        var diff = PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey].Count -
                                   p.StatCollection.GetValue<int>("Injuries");
                        ModInit.modLog.LogMessage(
                            $"Post-Mission Injuries ({p.StatCollection.GetValue<int>("Injuries")}) less than InjuryHolder count ({PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey].Count})");
                        PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey].Reverse();
                        for (int i = 0; i < diff; i++)
                        {
                            ModInit.modLog.LogMessage(
                                $"Removing {PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey][i]} from {p.Callsign}");
                            PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey].RemoveAt(i);
                        }
                    }
                }

                PilotInjuryHolder.HolderInstance.combatInjuriesMap = new Dictionary<string, List<string>>();
            }
        }


        [HarmonyPatch(typeof(SimGameState), "ResolveCompleteContract")]
        [HarmonyPriority(Priority.Low)]
        public static class ResolveCompleteContract_Patch
        {
            public static bool Prepare() => ModInit.modSettings.UseSimBleedingEffects;

            public static void Postfix(SimGameState __instance)
            {
                List<Pilot> list = new List<Pilot>(__instance.PilotRoster) {__instance.Commander};
                foreach (Pilot pilot in list)
                {
                    var pKey = pilot.FetchGUID();
                    if (!PilotInjuryHolder.HolderInstance.bloodStatForSimGame.ContainsKey(pKey)) continue;

                    pilot.ApplyClosestSimGameResult();

                }
                ModInit.modLog.LogMessage(
                    $"Clearing bloodStatForSimGame dict!");
                PilotInjuryHolder.HolderInstance.bloodStatForSimGame = new Dictionary<string, float>();
            }
        }

    }
}