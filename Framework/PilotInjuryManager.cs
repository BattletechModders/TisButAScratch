using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using UnityEngine;
using SVGImporter;
using BattleTech;
using FluffyUnderware.DevTools.Extensions;
using HBS.Collections;
using HBS.Util;
using Localize;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static TisButAScratch.Framework.GlobalVars;


namespace TisButAScratch.Framework
{
    public static class PilotExtensions
    {
        internal static string FetchGUID(this Pilot pilot)
        {
            var guid = pilot.pilotDef.PilotTags.FirstOrDefault(x => x.StartsWith(iGUID));
            if (string.IsNullOrEmpty(guid))
            {
                ModInit.modLog.LogMessage($"WTF IS {pilot.Callsign}'s GUID NULL?!");
            }
            return guid;
        }

        internal static int CalcBloodBank(this Pilot pilot)
        {
            var factor = pilot.Health;
            if (ModInit.modSettings.UseGutsForBloodBank)
            {
                factor = pilot.Guts;
            }
            return Math.Max(ModInit.modSettings.minBloodBank, Mathf.RoundToInt((factor * ModInit.modSettings.factorBloodBankMult) +
                                                                               ModInit.modSettings.baseBloodBankAdd));
        }
        internal static int GetBloodCapacity(this Pilot pilot)
        {
            return pilot.StatCollection.GetValue<int>("BloodCapacity");
        }

        internal static int GetBloodBank(this Pilot pilot)
        {
            return pilot.StatCollection.GetValue<int>("BloodBank");
        }

        internal static void SetBloodBank(this Pilot pilot, int bank)
        {
            pilot.StatCollection.Set<int>("BloodBank", bank);
        }

        internal static float GetBleedingRate(this Pilot pilot)
        {
            return pilot.StatCollection.GetValue<float>("BleedingRate");
        }

        internal static void SetBleedingRate(this Pilot pilot, float rate)
        {
            pilot.StatCollection.Set<float>("BleedingRate", rate);
        }

        internal static void ApplyClosestSimGameResult(this Pilot pilot)
        {
            if (ModInit.modSettings.SimBleedingEffects.Count == 0 || !ModInit.modSettings.UseSimBleedingEffects) return;
            var pKey = pilot.FetchGUID();
            if (!PilotInjuryHolder.HolderInstance.bloodStatForSimGame.ContainsKey(pKey))
            {
                ModInit.modLog.LogMessage($"Something very wrong here!");
                PilotInjuryHolder.HolderInstance.bloodStatForSimGame.Add(pKey, 1);
            }
            var bloodLevelForSim = PilotInjuryHolder.HolderInstance.bloodStatForSimGame[pKey];

            var bleedingEffectTotal = PilotInjuryManager.ManagerInstance.SimBleedingEffectList.Count + 1;

            
            var simBleedLvl = 0;
            foreach (var simEffect in PilotInjuryManager.ManagerInstance.SimBleedingEffectList)
            {
                var simBleedingEffectDecimal = 1 - simEffect.bleedingEffectLvl / (float) bleedingEffectTotal;
                ModInit.modLog.LogMessage($"{simEffect.simBleedingEffectID} needs blood level <= {simBleedingEffectDecimal} to apply.");
                if (!(bloodLevelForSim <= simBleedingEffectDecimal)) continue;
                simBleedLvl = simEffect.bleedingEffectLvl;
                ModInit.modLog.LogMessage($"Calculated SimBleedEffectLvl {simBleedLvl}.");
                break;
            }
            var tempList = new List<SimBleedingEffect>(PilotInjuryManager.ManagerInstance.SimBleedingEffectList.Where(x=>x.bleedingEffectLvl == simBleedLvl));

            var idx = UnityEngine.Random.Range(0, tempList.Count);

            var chosenBleedingResult = tempList[idx];

            ModInit.modLog.LogMessage($"{chosenBleedingResult.simBleedingEffectID} chosen for pilot {pilot.Description.Callsign}_{pKey}");

            var objects = new List<object> {pilot};
            foreach (var result in chosenBleedingResult.simResult)
            {
                SimGameState.ApplySimGameEventResult(result, objects);
            }
        }


        internal static void ApplyClosestBleedingEffect(this Pilot pilot)
        {
            if (ModInit.modSettings.BleedingEffects.Count == 0 || !ModInit.modSettings.UseBleedingEffects) return;
            var bloodLevelDecimal = (float)pilot.GetBloodBank() / (float)pilot.GetBloodCapacity();
            var pKey = pilot.FetchGUID();
            if (!PilotInjuryHolder.HolderInstance.bloodStatForSimGame.ContainsKey(pKey) && ModInit.modSettings.UseSimBleedingEffects)
            {
                PilotInjuryHolder.HolderInstance.bloodStatForSimGame.Add(pKey, bloodLevelDecimal);
            }
            else if (ModInit.modSettings.UseSimBleedingEffects)
            {
                PilotInjuryHolder.HolderInstance.bloodStatForSimGame[pKey] = bloodLevelDecimal;
            }
            ModInit.modLog.LogMessage($"Calculated {pKey}'s bloodLevel fraction: {bloodLevelDecimal}!");
            var bleedingEffectTotal = PilotInjuryManager.ManagerInstance.BleedingEffectsList.Count + 1;

            var bleedLvl = 0;
            foreach (var bleedEffect in PilotInjuryManager.ManagerInstance.BleedingEffectsList)
            {
                var bleedingEffectDecimal = 1 - bleedEffect.bleedingEffectLvl / (float) bleedingEffectTotal;
                ModInit.modLog.LogMessage($"{bleedEffect.bleedingName} needs blood level <= {bleedingEffectDecimal} to apply.");
                if (!(bloodLevelDecimal <=
                      bleedingEffectDecimal)) continue;
                bleedLvl = bleedEffect.bleedingEffectLvl;
                ModInit.modLog.LogMessage($"Calculated bleedEffectLvl {bleedLvl}.");
                break;
            }
            var tempList = new List<BleedingEffect>(PilotInjuryManager.ManagerInstance.BleedingEffectsList.Where(x=>x.bleedingEffectLvl == bleedLvl));
            if (tempList.Count == 0) return;

            var idx = UnityEngine.Random.Range(0, tempList.Count);

            var chosenBleedingEffect = tempList[idx];

            ModInit.modLog.LogMessage($"{chosenBleedingEffect.bleedingName} chosen for pilot {pilot.Description.Callsign}_{pKey}");

            var effectsList =
                UnityGameInstance.BattleTechGame.Combat.EffectManager.GetAllEffectsTargeting(pilot.ParentActor);

            foreach (EffectData effectData in chosenBleedingEffect.effects)
            {
                if (effectsList.Any(x => x.EffectData.Description.Id == effectData.Description.Id))
                {
                    ModInit.modLog.LogMessage($"{pilot.Description.Callsign}_{pKey} already has bleeding effect {effectData.Description.Name}, skipping.");
                    continue;
                }
                ModInit.modLog.LogMessage($"processing {effectData.Description.Name} for {pilot.Description.Callsign}_{pKey}");

                if (effectData.targetingData.effectTriggerType == EffectTriggerType.Passive &&
                    effectData.targetingData.effectTargetType == EffectTargetType.Creator)
                {
                    string id = ($"BleedingEffect_{pilot.Description.Callsign}_{effectData.Description.Id}");

                    ModInit.modLog.LogMessage($"Applying {id}");
                    pilot.ParentActor.Combat.EffectManager.CreateEffect(effectData, id, -1, pilot.ParentActor, pilot.ParentActor, default(WeaponHitInfo), 1);
                }
            }
        }
    }

    public class PilotInjuryManager
    {
        private static PilotInjuryManager _instance;
        public List<Injury> InjuryEffectsList;
        public List<Injury> InternalDmgInjuries;
        public List<BleedingEffect> BleedingEffectsList;
        public List<SimBleedingEffect> SimBleedingEffectList;

        public static PilotInjuryManager ManagerInstance
        {
            get
            {
                if (_instance == null) _instance = new PilotInjuryManager();
                return _instance;
            }
        }

        internal SimGameEventResult processSimBleedingSettings(JObject jObject)
        {
            var simResult = new SimGameEventResult();

            simResult.Scope = jObject["Scope"].ToObject<EventScope>();
            simResult.Requirements = jObject["Requirements"].ToObject<RequirementDef>();
            simResult.AddedTags = new TagSet();
            simResult.AddedTags.FromJSON(jObject["AddedTags"].ToString());
            simResult.RemovedTags = new TagSet();
            simResult.RemovedTags.FromJSON(jObject["RemovedTags"].ToString());
            
            simResult.Stats = jObject["Stats"].ToObject<SimGameStat[]>();
            simResult.Actions = jObject["Actions"].ToObject<SimGameResultAction[]>();
            simResult.ForceEvents = jObject["ForceEvents"].ToObject<SimGameForcedEvent[]>();
            simResult.TemporaryResult = jObject["TemporaryResult"].ToObject<bool>();
            simResult.ResultDuration = jObject["ResultDuration"].ToObject<int>();

            return simResult;
        }

        internal void Initialize()
        {
            SimBleedingEffectList = new List<SimBleedingEffect>();
            foreach (var simBleedingEffect in ModInit.modSettings.SimBleedingEffects) 
            {
                ModInit.modLog.LogMessage($"Adding effects for {simBleedingEffect.simBleedingEffectID}!");
                foreach (var jObject in simBleedingEffect.simResultJO)
                {
                    var simResult = processSimBleedingSettings(jObject);
                    ModInit.modLog.LogMessage($"TEMPORARY: {simResult.Scope}\n{simResult.Requirements}\n{simResult.AddedTags}\n{simResult.RemovedTags}\n{simResult.Stats} AND {JsonConvert.SerializeObject(simResult)}!");

                    simBleedingEffect.simResult.Add(simResult);
                }
                SimBleedingEffectList.Add(simBleedingEffect);
            }

            SimBleedingEffectList = SimBleedingEffectList.OrderByDescending(x => x.bleedingEffectLvl).ToList();

            BleedingEffectsList = new List<BleedingEffect>();
            foreach (var bleedingEffect in ModInit.modSettings.BleedingEffects) 
            {
                ModInit.modLog.LogMessage($"Adding effects for {bleedingEffect.bleedingName}!");
                foreach (var jObject in bleedingEffect.effectDataJO)
                {
                    var effectData = new EffectData();
                    effectData.FromJSON(jObject.ToString());
                    bleedingEffect.effects.Add(effectData);
                }
                BleedingEffectsList.Add(bleedingEffect);
            }

            BleedingEffectsList = BleedingEffectsList.OrderByDescending(x => x.bleedingEffectLvl).ToList();

            InjuryEffectsList = new List<Injury>();
            foreach (var injuryEffect in ModInit.modSettings.InjuryEffectsList) 
            {
                ModInit.modLog.LogMessage($"Adding effects for {injuryEffect.injuryName}!");
                foreach (var jObject in injuryEffect.effectDataJO)
                {
                    var effectData = new EffectData();
                    effectData.FromJSON(jObject.ToString());
                    injuryEffect.effects.Add(effectData);
                }
                InjuryEffectsList.Add(injuryEffect);
            }



            if (ModInit.modSettings.enableInternalDmgInjuries)
            {
                InternalDmgInjuries = new List<Injury>();

                foreach (var internalDmgEffect in ModInit.modSettings.InternalDmgInjuries)
                {
                    ModInit.modLog.LogMessage($"Adding effects for {internalDmgEffect.injuryName}!");
                    foreach (var jObject in internalDmgEffect.effectDataJO)
                    {
                        var effectData = new EffectData();
                        effectData.FromJSON(jObject.ToString());
                        internalDmgEffect.effects.Add(effectData);
                    }
                    InternalDmgInjuries.Add(internalDmgEffect);
                }
            }
        }
        internal static void PreloadIcons()
        {
            var dm = UnityGameInstance.BattleTechGame.DataManager;
            var loadRequest = dm.CreateLoadRequest();

            foreach (var bleedingEffect in PilotInjuryManager.ManagerInstance.BleedingEffectsList)
            {
                foreach (var effectData in bleedingEffect.effects)
                {
                    loadRequest.AddLoadRequest<SVGAsset>(BattleTechResourceType.SVGAsset, effectData.Description.Icon, null);
                }
            }

            foreach (var injury in PilotInjuryManager.ManagerInstance.InjuryEffectsList)
            {
                foreach (var effectData in injury.effects)
                {
                    loadRequest.AddLoadRequest<SVGAsset>(BattleTechResourceType.SVGAsset, effectData.Description.Icon, null);
                }
            }

            if (ModInit.modSettings.enableInternalDmgInjuries)
            {
                foreach (var injury in PilotInjuryManager.ManagerInstance.InternalDmgInjuries)
                {
                    foreach (var effectData in injury.effects)
                    {
                        loadRequest.AddLoadRequest<SVGAsset>(BattleTechResourceType.SVGAsset, effectData.Description.Icon, null);
                    }
                }
            }
            loadRequest.ProcessRequests();
        }

        internal void GatherAndApplyInjuries(AbstractActor actor)
        {
            var p = actor.GetPilot();
            var pKey = p.FetchGUID();

            foreach (var tag in p.pilotDef.PilotTags.Where(x=>x.StartsWith("TBAS_SimBleed")))
            {
                var matches = TBAS_SimBleedStatMod.Matches(tag);
                if (matches.Count <= 0) continue;
                var statType = matches[0].Groups["type"].Value;
                var statMod = matches[0].Groups["value"].Value;
                int.TryParse(statMod, out var resultINT);
                if (resultINT != 0)
                {
                    p?.StatCollection.ModifyStat<int>("TBAS_Injuries", 0, statType,
                        StatCollection.StatOperation.Int_Add, resultINT);
                    ModInit.modLog.LogMessage($"Pilot {pKey} gets {resultINT} {statType} due to bleeding previously.");
                }
                else
                {
                    float.TryParse(statMod, out var resultFLT);
                    if (resultFLT != 0)
                    {
                        p?.StatCollection.ModifyStat<float>("TBAS_Injuries", 0, statType,
                            StatCollection.StatOperation.Float_Add, resultFLT);
                        ModInit.modLog.LogMessage($"Pilot {pKey} gets {resultFLT} {statType} due to bleeding previously.");
                    }
                }
            }


            foreach (var id in PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey])
            {
                foreach (Injury injury in ManagerInstance.InternalDmgInjuries.Where(x => x.injuryID == id))
                {
                    this.applyInjuryEffects(actor, injury);
                    ModInit.modLog.LogMessage($"Gathered {injury.injuryName} for {p.Description.Callsign}_{pKey}");
                }
                foreach (Injury injury in ManagerInstance.InjuryEffectsList.Where(x => x.injuryID == id))
                {
                    this.applyInjuryEffects(actor, injury);
                    ModInit.modLog.LogMessage($"Gathered {injury.injuryName} for {p.Description.Callsign}_{pKey}");
                }
            }
        }

        private void applyInjuryEffects(AbstractActor actor, Injury injury)
        {
            var p = actor.GetPilot();
            if (actor.StatCollection.GetValue<bool>(ModInit.modSettings.NullifiesInjuryEffectsStat))
            {
                ModInit.modLog.LogMessage($"Found advanced life-support: nullifying injury effects for {p.Callsign}");
                return;
            }
            var pKey = p.FetchGUID();
            ModInit.modLog.LogMessage($"processing {injury.effects.Count} injury effects for {p.Description.Callsign}_{pKey}");
            foreach (EffectData effectData in injury.effects)
            {
                ModInit.modLog.LogMessage($"processing {effectData.Description.Name} for {p.Description.Callsign}_{pKey}");

                if (effectData.targetingData.effectTriggerType == EffectTriggerType.Passive &&
                    effectData.targetingData.effectTargetType == EffectTargetType.Creator)
                {
                    string id = ($"InjuryEffect_{p.Description.Callsign}_{effectData.Description.Id}");

                    ModInit.modLog.LogMessage($"Applying {id}");
                    actor.Combat.EffectManager.CreateEffect(effectData, id, -1, actor, actor, default(WeaponHitInfo), 1);
                }
            }
            //added to display floaties? need totest
            if (actor.Combat.TurnDirector.GameHasBegun)
            {
                actor.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(
                    new ShowActorInfoSequence(actor, new Text("{0}! Severity {1} Injury!", new object[] { injury.injuryName, injury.severity }), FloatieMessage.MessageNature.PilotInjury, true)));

//                if (!string.IsNullOrEmpty(injury.injuryID_Post))
//                {
//                    var txt = new Text("<color=#FF0000>Pilot is bleeding out!</color=#FF0000>");

//                    actor.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(
//                        new ShowActorInfoSequence(actor, txt, FloatieMessage.MessageNature.PilotInjury, true)));
//                }

                var effects = actor.Combat.EffectManager.GetAllEffectsTargeting(actor);

                if (!effects.Any(x =>
                    x.EffectData.Description.Id.EndsWith(ModInit.modSettings.BleedingOutSuffix))) return;

                var durationInfo = Mathf.FloorToInt(actor.GetPilot().GetBloodBank() / actor.GetPilot().GetBleedingRate() - 1); 
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

            }
        }


        internal void rollInjury(Pilot pilot, int dmg, DamageType damageType) //to be postfix patched into InjurePilot
        {
            InjuryLoc loc;
            var pKey = pilot.FetchGUID();

            if (!PilotInjuryHolder.HolderInstance.combatInjuriesMap.ContainsKey(pKey))
            {
                PilotInjuryHolder.HolderInstance.combatInjuriesMap.Add(pKey, new List<string>());
                ModInit.modLog.LogMessage($"{pilot.Name} missing, added to combatInjuriesMap");
            }

            if (!PilotInjuryHolder.HolderInstance.pilotInjuriesMap.ContainsKey(pKey))
            {
                PilotInjuryHolder.HolderInstance.pilotInjuriesMap.Add(pKey, new List<string>());
                ModInit.modLog.LogMessage($"{pilot.Name} missing, added to pilotInjuriesMap");
            }

            for (int i = 0; i < dmg; i++)
            {
                //adding locations weights for preexisting injuries

                //does pilot have existing injuries
                var injuryLocs = new List<int>(Enumerable.Range(2,6));
                if (PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey].Count > 0 || PilotInjuryHolder.HolderInstance.combatInjuriesMap[pKey].Count > 0)
                {
                    var curInjuryList = new List<Injury>();
                    ModInit.modLog.LogMessage($"{pilot?.Callsign} has preexisting conditions, processing location weight");
                    foreach (var id in PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey])
                    {
                        curInjuryList.AddRange(
                            PilotInjuryManager.ManagerInstance.InjuryEffectsList.Where(x => x.injuryID == id));
                    }

                    foreach (var id in PilotInjuryHolder.HolderInstance.combatInjuriesMap[pKey])
                    {
                        curInjuryList.AddRange(
                            PilotInjuryManager.ManagerInstance.InjuryEffectsList.Where(x => x.injuryID == id));
                    }

                    if (pilot != null && pilot.ParentActor.StatCollection.GetValue<bool>(ModInit.modSettings.DisableBleedingStat))
                    {
                        curInjuryList.RemoveAll(x => !string.IsNullOrEmpty(x.injuryID_Post));
                        ModInit.modLog.LogMessage($"Found advanced life-support: no bleeding out injuries allowed for {pilot.Callsign}!");
                    }

                    if (ModInit.modSettings.reInjureWeightAppliesCurrentContract)
                    {
                        foreach (var inj in curInjuryList)
                        {
                            for (int t = 0; t < ModInit.modSettings.reInjureLocWeight; t++)
                            {
                                injuryLocs.Add((int)inj.injuryLoc);
                            }
                            ModInit.modLog.LogMessage($"{inj.injuryLoc.ToString()} has weight of {ModInit.modSettings.reInjureLocWeight}");
                        }
                    }

                    else
                    {
                        foreach (var inj in curInjuryList.Where(x => pilot != null && !pilot.StatCollection.GetValue<List<string>>("LastInjuryId").Contains(x.injuryID)))
                        {
                            for (int t = 0; t < ModInit.modSettings.reInjureLocWeight; t++)
                            {
                                injuryLocs.Add((int)inj.injuryLoc);
                            }
                            ModInit.modLog.LogMessage($"{inj.injuryLoc.ToString()} has weight of {ModInit.modSettings.reInjureLocWeight}");
                        }
                    }
                    ModInit.modLog.LogMessage($"Final list of injury location indices: {string.Join(",", injuryLocs)}");
                }


                var injuryList = new List<Injury>(ManagerInstance.InjuryEffectsList);

                var idx = UnityEngine.Random.Range(0, injuryLocs.Count);
                loc = (InjuryLoc)injuryLocs[idx];

//                loc = (InjuryLoc)UnityEngine.Random.Range(2, 8); // old

                if (damageType == DamageType.Overheat || damageType == DamageType.OverheatSelf)
                {
                    injuryList.RemoveAll(x => x.couldBeThermal == false || x.severity >= 100);
                }

                else if (damageType == DamageType.Knockdown || damageType == DamageType.KnockdownSelf)
                {
                    injuryList.RemoveAll(x => x.couldBeThermal || x.severity >= 100);
                }
                else
                {
                    injuryList.RemoveAll(x => x.severity >= 100);
                }
                ModInit.modLog.LogMessage($"Injury Loc {loc} chosen for {pilot?.Callsign}");

                injuryList.RemoveAll(x => x.severity >= 100 || x.injuryLoc != loc);
//                injuryList.RemoveAll(x => PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey].Contains(x.injuryID));
//                ModInit.modLog.LogMessage($"Removed all injuries that {pilot?.Callsign} already has.");

                var chosen = injuryList[UnityEngine.Random.Range(0, injuryList.Count)]; 
                ModInit.modLog.LogMessage($"Injury {chosen.injuryName} chosen for {pilot?.Callsign}");

                PilotInjuryHolder.HolderInstance.combatInjuriesMap[pKey].Add(chosen.injuryID);
                ModInit.modLog.LogMessage(
                    $"Adding {chosen.injuryName} to {pilot?.Callsign}'s combat injury map. PilotID: {pKey}");

                var newList = pilot?.StatCollection.GetValue<List<string>>("LastInjuryId");
                newList?.Add(chosen.injuryID);

                pilot?.StatCollection.ModifyStat<List<string>>("TBAS_Injuries", 0, "LastInjuryId",
                    StatCollection.StatOperation.Set, newList);

                ModInit.modLog.LogMessage(
                    $"Setting {chosen.injuryName} to {pilot?.Callsign}'s LastInjuryId stat. PilotID: {pKey}");

                pilot?.StatCollection.ModifyStat<int>("TBAS_Injuries", 0, MissionKilledStat,
                    StatCollection.StatOperation.Int_Add, chosen.severity);

                ModInit.modLog.LogMessage(
                    $"Adding {chosen.injuryName}'s severity value: {chosen.severity} to {pilot?.Callsign}'s MissionKilledStat");

                if (!String.IsNullOrEmpty(chosen.injuryID_Post))
                {
                    var em = UnityGameInstance.BattleTechGame.Combat.EffectManager;
                    var effects = em.GetAllEffectsTargeting(pilot?.ParentActor); //targeting parent actor maybe?
                    if (pilot.GetBleedingRate() == 0f)
                    {
                        pilot.SetBleedingRate(chosen.severity);
                    }
                    else
                    {
                        foreach (var unused in effects.Where(x =>
                            x.EffectData.Description.Id.EndsWith(ModInit.modSettings.BleedingOutSuffix)))
                        {
                            if (ModInit.modSettings.additiveBleedingFactor < 0)
                            {
                                var currentRate = pilot.GetBleedingRate();
                                pilot.SetBleedingRate(currentRate + ModInit.modSettings.additiveBleedingFactor);
                            }

                            else if (ModInit.modSettings.additiveBleedingFactor > 1)
                            {
                                var currentRate = pilot.GetBleedingRate();
                                pilot.SetBleedingRate(currentRate * ModInit.modSettings.additiveBleedingFactor);
                            }
                        }
                    }

                    if (pilot.GetBleedingRate() > 0f)
                    {
                        pilot.ApplyClosestBleedingEffect();
                    }
                }
                applyInjuryEffects(pilot?.ParentActor, chosen);
            }
        }

        internal void rollInjuryFeedback(Pilot pilot, int dmg, DamageType damageType) //to be postfix patched into InjurePilot
        {
            var pKey = pilot.FetchGUID();

            if (!PilotInjuryHolder.HolderInstance.combatInjuriesMap.ContainsKey(pKey))
            {
                PilotInjuryHolder.HolderInstance.combatInjuriesMap.Add(pKey, new List<string>());
                ModInit.modLog.LogMessage($"{pilot.Name} missing, added to combatInjuriesMap");
            }

            if (!PilotInjuryHolder.HolderInstance.pilotInjuriesMap.ContainsKey(pKey))
            {
                PilotInjuryHolder.HolderInstance.pilotInjuriesMap.Add(pKey, new List<string>());
                ModInit.modLog.LogMessage($"{pilot.Name} missing, added to pilotInjuriesMap");
            }

            var loc = InjuryLoc.Head;

            for (int i = 0; i < dmg; i++)
            {
                var injuryList = new List<Injury>(ManagerInstance.InternalDmgInjuries);


                injuryList.RemoveAll(x => x.severity >= 100 || x.injuryLoc != loc);
                var chosen = injuryList[UnityEngine.Random.Range(0, injuryList.Count)];
                ModInit.modLog.LogMessage($"Feedback Injury {chosen.injuryName} chosen for {pilot?.Callsign}");
                

                PilotInjuryHolder.HolderInstance.combatInjuriesMap[pKey].Add(chosen.injuryID);
                ModInit.modLog.LogMessage(
                    $"Adding {chosen.injuryName} to {pilot?.Callsign}'s combat injury map. PilotID: {pKey}");

                pilot?.StatCollection.ModifyStat<int>("TBAS_Injuries", 0, MissionKilledStat,
                    StatCollection.StatOperation.Int_Add, chosen.severity);
                ModInit.modLog.LogMessage(
                    $"Adding {chosen.injuryName}'s severity value: {chosen.severity} to {pilot?.Callsign}'s MissionKilledStat");

                applyInjuryEffects(pilot?.ParentActor, chosen);

            }
        }

        internal void rollInjurySG(Pilot pilot, int dmg, DamageType damageType) //to be postfix patched into InjurePilot
        {
            var pKey = pilot.FetchGUID();


            if (!PilotInjuryHolder.HolderInstance.pilotInjuriesMap.ContainsKey(pKey))
            {
                PilotInjuryHolder.HolderInstance.pilotInjuriesMap.Add(pKey, new List<string>());
                ModInit.modLog.LogMessage($"{pilot.Name} missing, added to pilotInjuriesMap");
            }

            

            InjuryLoc loc;

            for (int i = 0; i < dmg; i++)
            {
                var injuryList = new List<Injury>(PilotInjuryManager.ManagerInstance.InjuryEffectsList);
                loc = (InjuryLoc)UnityEngine.Random.Range(2, 8);
                    ModInit.modLog.LogMessage($"Injury Loc {loc} chosen for {pilot?.Callsign}");


                injuryList.RemoveAll(x => x.severity >= 100 || x.injuryLoc != loc || !string.IsNullOrEmpty(x.injuryID_Post));
 //               injuryList.RemoveAll(x => PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey].Contains(x.injuryID));
                var chosen = injuryList[UnityEngine.Random.Range(0, injuryList.Count)];
                ModInit.modLog.LogMessage($"Injury {chosen.injuryName} chosen for {pilot?.Callsign}");

                PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey].Add(chosen.injuryID);
                ModInit.modLog.LogMessage(
                    $"Adding {chosen.injuryName} to {pilot?.Callsign}'s injury map. PilotID: {pKey}");
            }
        }
    }
}
