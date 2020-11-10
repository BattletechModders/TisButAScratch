using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Harmony;
using BattleTech;
using TisButAScratch.Framework;
using static TisButAScratch.Framework.GlobalVars;
using System.Threading.Tasks;
using BattleTech.DataObjects;
using BattleTech.Save;
using BattleTech.Save.Test;
using TB.ComponentModel;

namespace TisButAScratch.Patches
{
    class SaveAndStatePatches
    {

        [HarmonyPatch(typeof(SimGameState), "Dehydrate", new Type[] {typeof(SimGameSave), typeof(SerializableReferenceContainer)})]
        public static class SGS_Dehydrate_Patch
        {
            public static void Prefix(SimGameState __instance)
            {
                PilotInjuryManager.PreloadIcons();
                sim = __instance;
                var curPilots = new List<string>();

                if (!sim.Commander.pilotDef.PilotTags.Any(x => x.StartsWith(iGUID)))
                {
                    sim.Commander.pilotDef.PilotTags.Add($"{iGUID}{sim.Commander.Description.Id}{sim.GenerateSimGameUID()}");
                }

                var pKey = sim.Commander.FetchGUID();
                curPilots.Add(pKey);
                if (!PilotInjuryHolder.HolderInstance.pilotInjuriesMap.ContainsKey(pKey))
                {
                    PilotInjuryHolder.HolderInstance.pilotInjuriesMap.Add(pKey, new List<string>());
                    ModInit.modLog.LogMessage($"Added Commander to pilotInjuriesMap");
                }

                
                foreach (Pilot p in sim.PilotRoster)
                {
                    
                    if (!p.pilotDef.PilotTags.Any(x => x.StartsWith(iGUID)))
                    {
                        p.pilotDef.PilotTags.Add($"{iGUID}{p.Description.Id}{sim.GenerateSimGameUID()}");
                    }
                    pKey = p.FetchGUID();
                    curPilots.Add(pKey);
                    if(!PilotInjuryHolder.HolderInstance.pilotInjuriesMap.ContainsKey(pKey))
                    {
                        PilotInjuryHolder.HolderInstance.pilotInjuriesMap.Add(pKey, new List<string>());
                        ModInit.modLog.LogMessage($"{p.Callsign} missing, added to pilotInjuriesMap");
                    }
                }
                var rm = PilotInjuryHolder.HolderInstance.pilotInjuriesMap.Keys.Where(x=>!curPilots.Contains(x));
                foreach (var key in new List<string>(rm))
                {
                    PilotInjuryHolder.HolderInstance.pilotInjuriesMap.Remove(key);
                    ModInit.modLog.LogMessage($"Pilot with pilotID {key} not in roster, removing from pilotInjuriesMap");
                }
                
                PilotInjuryHolder.HolderInstance.SerializeInjuryState();
            }

            public static void Postfix(SimGameState __instance)
            {
                var injuryState = sim.CompanyTags.FirstOrDefault((x) => x.StartsWith(injuryStateTag));
                GlobalVars.sim.CompanyTags.Remove(injuryState);
            }
        }

        [HarmonyPatch(typeof(SimGameState), "Rehydrate", new Type[] {typeof(GameInstanceSave)})]
        public static class SGS_Rehydrate_Patch
        {
            public static void Postfix(SimGameState __instance)
            {
                sim = __instance;
                PilotInjuryManager.PreloadIcons();
                var curPilots = new List<string>();
                PilotInjuryHolder.HolderInstance.DeserializeInjuryState();
                ModInit.modLog.LogMessage($"Successfully deserialized or determined deserializing unnecessary.");

                if (!sim.Commander.pilotDef.PilotTags.Any(x => x.StartsWith(iGUID)))
                {
                    sim.Commander.pilotDef.PilotTags.Add(
                        $"{iGUID}{sim.Commander.Description.Id}{sim.GenerateSimGameUID()}");
                    ModInit.modLog.LogMessage($"Added Commander iGUID tag");
                }

                var pKey = sim.Commander.FetchGUID();

                ModInit.modLog.LogMessage($"Fetched Commander iGUID {pKey}");
                curPilots.Add(pKey);
                if (!PilotInjuryHolder.HolderInstance.pilotInjuriesMap.ContainsKey(pKey))
                {
                    PilotInjuryHolder.HolderInstance.pilotInjuriesMap.Add(pKey, new List<string>());
                    ModInit.modLog.LogMessage($"Added Commander to pilotInjuriesMap");
                }

                foreach (var id in new List<string>(PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey]))
                {
                    if (PilotInjuryManager.ManagerInstance.InjuryEffectsList.All(x => x.injuryID != id) && PilotInjuryManager.ManagerInstance.InternalDmgInjuries.All(x => x.injuryID != id))
                    {
                        PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey].Remove(id);
                        ModInit.modLog.LogMessage($"Removed deprecated injury from Commander with id {id}");
                    }
                }

                /// this bigass clusterfuck is just for if you have existing injuries when first loading up TBAS
                if (PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey].Count <
                    sim.Commander.StatCollection.GetValue<int>("Injuries"))
                {
                    var dmg = sim.Commander.StatCollection.GetValue<int>("Injuries") -
                              PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey].Count;
                    ModInit.modLog.LogMessage($"Commander is missing {dmg} injuries. Rerolling.");
                    PilotInjuryManager.ManagerInstance.rollInjurySG(sim.Commander, dmg, DamageType.Unknown);
                    if (ModInit.modSettings.debilSeverityThreshold > 0) //now trying to add up "severity" threshold for crippled injury
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
                                        StatCollection.StatOperation.Set, true, -1, true);
                                }
                            }
                        }
                    }
                }



                foreach (Pilot p in sim.PilotRoster)
                {

                    if (!p.pilotDef.PilotTags.Any(x => x.StartsWith(iGUID)))
                    {
                        p.pilotDef.PilotTags.Add($"{iGUID}{p.Description.Id}{sim.GenerateSimGameUID()}");
                        ModInit.modLog.LogMessage($"Added {p.Callsign} iGUID tag");
                    }

                    pKey = p.FetchGUID();
                    ModInit.modLog.LogMessage($"Fetched {p.Callsign} iGUID {pKey}");
                    curPilots.Add(pKey);
                    if (!PilotInjuryHolder.HolderInstance.pilotInjuriesMap.ContainsKey(pKey))
                    {
                        PilotInjuryHolder.HolderInstance.pilotInjuriesMap.Add(pKey, new List<string>());
                        ModInit.modLog.LogMessage($"{p.Callsign} missing, added to pilotInjuriesMap");
                    }

                    foreach (var id in new List<string>(PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey]))
                    {
                        if (PilotInjuryManager.ManagerInstance.InjuryEffectsList.All(x => x.injuryID != id) && PilotInjuryManager.ManagerInstance.InternalDmgInjuries.All(x => x.injuryID != id))
                        {
                            PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey].Remove(id);
                            ModInit.modLog.LogMessage($"Removed deprecated injury from {p.Callsign} with id {id}");
                        }
                    }

                    /// this bigass clusterfuck is just for if you have existing injuries when first loading up TBAS
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
                                            StatCollection.StatOperation.Set, true, -1, true);
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
            }
        }

        [HarmonyPatch(typeof(SimGameState), "ApplySimGameEventResult",
            new Type[] {typeof(SimGameEventResult), typeof(List<object>), typeof(SimGameEventTracker)})]
        [HarmonyPriority(Priority.Last)]
        public static class SGS_ApplySimGameEventResult
        {
            public static void Postfix(SimGameState __instance, SimGameEventResult result, List<object> objects, SimGameEventTracker tracker)
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
                                                StatCollection.StatOperation.Set, true, -1, true);
                                        }
                                    }
                                }
                            }
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
                                                StatCollection.StatOperation.Set, true, -1, true);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else return;
                }
            }
        }

        [HarmonyPatch(typeof(SimGameState), "AddPilotToRoster", new Type[] {typeof(PilotDef), typeof(bool), typeof(bool)})]
        public static class SGS_AddPilotToRoster_Patch
        {
            public static void Postfix(SimGameState __instance, PilotDef def, bool updatePilotDiscardPile = false,
                bool initialHiringDontSpawnMessage = false)
            {
                var p = __instance.PilotRoster.FirstOrDefault(x => x.pilotDef.Description.Id == def.Description.Id);
                if (!p.pilotDef.PilotTags.Any(x => x.StartsWith(iGUID)))
                {
                    p.pilotDef.PilotTags.Add($"{iGUID}{p.Description.Id}{__instance.GenerateSimGameUID()}");
                    ModInit.modLog.LogMessage($"Added {p.Callsign} iGUID tag");
                }
                var pKey = p.FetchGUID();
                if (!PilotInjuryHolder.HolderInstance.pilotInjuriesMap.ContainsKey(pKey))
                {
                    PilotInjuryHolder.HolderInstance.pilotInjuriesMap.Add(pKey, new List<string>());
                    ModInit.modLog.LogMessage($"{p.Callsign} missing, added to pilotInjuriesMap");
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
                p.StatCollection.AddStatistic<List<string>>("LastInjuryId", new List<string>());
                __instance.StatCollection.AddStatistic<bool>(ModInit.modSettings.internalDmgStatName, false);
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
                
                ModInit.modLog.LogMessage($"Added {p.Callsign} MissionKilledStat");
                if (!p.pilotDef.PilotTags.Any(x => x.StartsWith(iGUID)))
                {
                    p.pilotDef.PilotTags.Add($"{iGUID}{p.Description.Id}{sim.GenerateSimGameUID()}{aiPilotFlag}");
                    ModInit.modLog.LogMessage($"Added {p.Callsign} iGUID tag");
                }

                var pKey = p.FetchGUID();
                ModInit.modLog.LogMessage($"Fetched {p.Callsign} iGUID");
                //p.StatCollection.AddStatistic("isCrippled", false); //not needed, is now pilot tag (duh)

                //unneeded now? 110920
            //    if (!PilotInjuryHolder.HolderInstance.pilotInjuriesMap.ContainsKey(pKey) && (unit.team == null || !unit.team.IsLocalPlayer || (sim.PilotRoster.All(x => x.FetchGUID() != pKey) && !p.IsPlayerCharacter)))
            //    {
            //        PilotInjuryHolder.HolderInstance.pilotInjuriesMap.Add($"{pKey}", new List<string>());
            //        ModInit.modLog.LogMessage($"Adding AI Pilot {p?.Callsign} to injuryMap");
            //    }

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
                ModInit.modLog.LogMessage($"Initializing injury effects for {p?.Description?.Callsign}");

                if (PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey].Count >
                    p.StatCollection.GetValue<int>("Injuries"))
                {
                    ModInit.modLog.LogMessage($"{p.Callsign}'s Injury stat < existing injuries. Adding to Injury stat.");
                    p.StatCollection.ModifyStat<int>("TBAS_Injuries", -1,
                        "Injuries",
                        StatCollection.StatOperation.Set, PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey].Count, -1, true);
                }
            }
        }

        //resetting combatinjuriesMap on restart
        [HarmonyPatch(typeof(LoadTransitioning), "BeginCombatRestart", new Type[] {typeof(Contract)})]
        static class LoadTransitioning_BeginCombatRestart_Patch
        {
            static void Prefix(Contract __instance)
            {
                PilotInjuryHolder.HolderInstance.combatInjuriesMap = new Dictionary<string, List<string>>();
                ModInit.modLog.LogMessage($"Resetting combatInjuriesMap due to RestartMission button. Somebody must like CTD's.");
            }
        }


        [HarmonyPatch(typeof(Contract), "CompleteContract", new Type[] {typeof(MissionResult), typeof(bool)})]
        static class Contract_CompleteContract_Patch
        {

            static void Prefix(Contract __instance, MissionResult result, bool isGoodFaithEffort)
            {
                var actors = UnityGameInstance.BattleTechGame.Combat.AllActors;
                foreach (var actor in actors)
                {
                    var p = actor.GetPilot();
                    var pKey = p.FetchGUID();

                    if (p.pilotDef.PilotTags.Any(x => x.EndsWith(aiPilotFlag)))
                    {
                        p.pilotDef.PilotTags.Remove(DEBILITATEDTAG);
                        ModInit.modLog.LogMessage($"Removing CrippledTag from AI pilot {p.Callsign} if present");
                        var rmt = p.pilotDef.PilotTags.Where(x => x.EndsWith(aiPilotFlag));
                        p.pilotDef.PilotTags.RemoveRange(rmt);
                        ModInit.modLog.LogMessage($"Removing AI GUID Tag from AI pilot {p.Callsign} if present");
                    }

                    //now only adding to pilotInjuryMap at contract resolution instead of on the fly.

                    PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey].AddRange(PilotInjuryHolder.HolderInstance.combatInjuriesMap[pKey]);
                    ModInit.modLog.LogMessage($"Adding {p.Callsign}'s combatInjuryMap to their pilotInjuryMap");


                        foreach (var inj in PilotInjuryManager.ManagerInstance.InjuryEffectsList.Where(x => x.injuryID_Post != ""))
                    {
                        if (PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey].Contains(inj.injuryID))
                        {
                            PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey].Remove(inj.injuryID);
                            ModInit.modLog.LogMessage($"Removed {inj.injuryName} with bleeding effect from {p.Callsign}");
                            PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey].Add(inj.injuryID_Post);
                            ModInit.modLog.LogMessage($"Added {inj.injuryID_Post} to {p.Callsign} for post-combat injury");
                        }
                    }
                }

                var rm = new List<string>(PilotInjuryHolder.HolderInstance.pilotInjuriesMap.Keys.Where(x => x.EndsWith(aiPilotFlag)));
                foreach (var key in rm)
                {
                    PilotInjuryHolder.HolderInstance.pilotInjuriesMap.Remove(key);
                    ModInit.modLog.LogMessage($"Pilot with pilotID {key} was AI Pilot, removing from pilotInjuriesMap");
                }
            }
        }
    }
}
