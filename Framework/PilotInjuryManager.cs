using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SVGImporter;
using BattleTech;
using BattleTech.UI;
using HBS;
using HBS.Collections;
using Localize;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TisButAScratch.Patches;
using static TisButAScratch.Framework.GlobalVars;


namespace TisButAScratch.Framework
{
    public static class PilotExtensions
    {
        public static bool TagReqsAreMet(this Pilot pilot, IMechLabDraggableItem item, LanceLoadoutSlot slot)
        {
            if (item.ItemType == MechLabDraggableItemType.Mech)
            {
                if (!(item is LanceLoadoutMechItem lanceLoadoutMechItem)) return true;
                foreach (var component in lanceLoadoutMechItem.MechDef.Inventory)
                {
                    foreach (var ctag in component.Def.ComponentTags)
                    {
                        if (ModInit.modSettings.pilotingReqs.TryGetValue(ctag, out var req))
                        {
                            foreach (var pTag in req.PilotTags)
                            {
                                if (pTag.StartsWith("!"))
                                {
                                    if (pilot.pilotDef.PilotTags.Contains(pTag.Substring(1)))
                                    {
                                        GenericPopupBuilder.Create($"Cannot Add {item.MechDef.Name}", Strings.T($"{item.MechDef.Name} contains {component.Def.Description.Name}. {req.PilotTagDisplay}")).AddFader(new UIColorRef?(LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.PopupBackfill), 0f, true).Render();
                                        return false;
                                    }
                                }
                                else if (!pilot.pilotDef.PilotTags.Contains(pTag))
                                {
                                    GenericPopupBuilder.Create($"Cannot Add {item.MechDef.Name}", Strings.T($"{item.MechDef.Name} contains {component.Def.Description.Name}. {req.PilotTagDisplay}")).AddFader(new UIColorRef?(LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.PopupBackfill), 0f, true).Render();
                                    return false;
                                }
                            }
                        }
                    }
                }
                return true;
            }
            if (item.ItemType == MechLabDraggableItemType.Pilot)
            {
                if (slot.SelectedMech == null) return true;
                foreach (var component in slot.selectedMech.MechDef.Inventory)
                {
                    foreach (var ctag in component.Def.ComponentTags)
                    {
                        if (ModInit.modSettings.pilotingReqs.TryGetValue(ctag, out var req))
                        {
                            foreach (var pTag in req.PilotTags)
                            {
                                if (pTag.StartsWith("!"))
                                {
                                    if (pilot.pilotDef.PilotTags.Contains(pTag.Substring(1)))
                                    {
                                        GenericPopupBuilder
                                            .Create($"Cannot Add {pilot.Callsign}",
                                                Strings.T(
                                                    $"{slot.selectedMech.MechDef.Name} contains {component.Def.Description.Name}. {req.PilotTagDisplay}"))
                                            .AddFader(
                                                new UIColorRef?(LazySingletonBehavior<UIManager>.Instance
                                                    .UILookAndColorConstants.PopupBackfill), 0f, true).Render();
                                        return false;
                                    }
                                }
                                else if (!pilot.pilotDef.PilotTags.Contains(pTag))
                                {
                                    GenericPopupBuilder
                                        .Create($"Cannot Add {pilot.Callsign}",
                                            Strings.T(
                                                $"{slot.selectedMech.MechDef.Name} contains {component.Def.Description.Name}. {req.PilotTagDisplay}"))
                                        .AddFader(
                                            new UIColorRef?(LazySingletonBehavior<UIManager>.Instance
                                                .UILookAndColorConstants.PopupBackfill), 0f, true).Render();
                                    return false;
                                }
                            }
                        }
                    }
                }
                return true;
            }
            return true;
        }

        public static string FetchGUID(this Pilot pilot)
        {
            var guid = pilot.pilotDef.PilotTags.FirstOrDefault(x => x.StartsWith(iGUID));
            if (string.IsNullOrEmpty(guid))
            {
                pilot.pilotDef.PilotTags.Add($"{iGUID}{pilot.pilotDef.Description.Id}{Guid.NewGuid()}");
                guid = pilot.pilotDef.PilotTags.FirstOrDefault(x => x.StartsWith(iGUID));
                ModInit.modLog?.Info?.Write($"WTF IS {pilot.Callsign}'s GUID NULL?!, making a new GUID I guess.");
                if (!PilotInjuryHolder.HolderInstance.pilotInjuriesMap.ContainsKey(guid))
                {
                    PilotInjuryHolder.HolderInstance.pilotInjuriesMap.Add(guid, new List<string>());
                    ModInit.modLog?.Info?.Write($"{pilot.Callsign} was also not in pilotInjuriesMap. Adding them.");
                }
                return guid;
            }
            return guid;
        }

        public static float CalcBloodBank(this Pilot pilot)
        {
            var factor = pilot.Health;
            if (ModInit.modSettings.UseGutsForBloodBank)
            {
                factor = pilot.Guts;
            }
            return Math.Max(ModInit.modSettings.minBloodBank, (factor * ModInit.modSettings.factorBloodBankMult) +
                                                                               ModInit.modSettings.baseBloodBankAdd);
        }
        public static float GetBloodCapacity(this Pilot pilot)
        {
            return pilot.StatCollection.GetValue<float>("BloodCapacity");
        }

        public static float GetBloodBank(this Pilot pilot)
        {
            return pilot.StatCollection.GetValue<float>("BloodBank");
        }

        public static void SetBloodBank(this Pilot pilot, float bank)
        {
            pilot.StatCollection.Set<float>("BloodBank", bank);
        }

        public static float GetBleedingRate(this Pilot pilot)
        {
            return pilot.StatCollection.GetValue<float>("BleedingRate");
        }

        public static void SetBleedingRate(this Pilot pilot, float rate)
        {
            pilot.StatCollection.Set<float>("BleedingRate", rate);
        }

        public static float GetBleedingRateMulti(this Pilot pilot)
        {
            return pilot.StatCollection.GetValue<float>("BleedingRateMulti");
        }

        public static void ApplyClosestSimGameResult(this Pilot pilot)
        {
            if (ModInit.modSettings.SimBleedingEffects.Count == 0 || !ModInit.modSettings.UseSimBleedingEffects) return;
            var pKey = pilot.FetchGUID();
            if (!PilotInjuryHolder.HolderInstance.bloodStatForSimGame.ContainsKey(pKey))
            {
                ModInit.modLog?.Info?.Write($"Something very wrong here!");
                PilotInjuryHolder.HolderInstance.bloodStatForSimGame.Add(pKey, 1);
            }
            var bloodLevelForSim = PilotInjuryHolder.HolderInstance.bloodStatForSimGame[pKey];

            var bleedingEffectTotal = PilotInjuryManager.ManagerInstance.SimBleedingEffectList.Count + 1;

            
            var simBleedLvl = 0;
            foreach (var simEffect in PilotInjuryManager.ManagerInstance.SimBleedingEffectList)
            {
                var simBleedingEffectDecimal = 1 - simEffect.bleedingEffectLvl / (float) bleedingEffectTotal;
                ModInit.modLog?.Info?.Write($"{simEffect.simBleedingEffectID} needs blood level <= {simBleedingEffectDecimal} to apply.");
                if (!(bloodLevelForSim <= simBleedingEffectDecimal)) continue;
                simBleedLvl = simEffect.bleedingEffectLvl;
                ModInit.modLog?.Info?.Write($"Calculated SimBleedEffectLvl {simBleedLvl}.");
                break;
            }
            var tempList = new List<SimBleedingEffect>(PilotInjuryManager.ManagerInstance.SimBleedingEffectList.Where(x=>x.bleedingEffectLvl == simBleedLvl));

            var idx = UnityEngine.Random.Range(0, tempList.Count);

            var chosenBleedingResult = tempList[idx];

            ModInit.modLog?.Info?.Write($"{chosenBleedingResult.simBleedingEffectID} chosen for pilot {pilot.Description.Callsign}_{pKey}");

            var objects = new List<object> {pilot};
            foreach (var result in chosenBleedingResult.simResult)
            {
                SimGameState.ApplySimGameEventResult(result, objects);
            }
        }

        public static void ApplyClosestBleedingEffect(this Pilot pilot)
        {
            if (ModInit.modSettings.BleedingEffects.Count == 0 || !ModInit.modSettings.UseBleedingEffects) return;
            var bloodLevelDecimal = pilot.GetBloodBank() / pilot.GetBloodCapacity();
            var pKey = pilot.FetchGUID();
            if (!PilotInjuryHolder.HolderInstance.bloodStatForSimGame.ContainsKey(pKey) && ModInit.modSettings.UseSimBleedingEffects)
            {
                PilotInjuryHolder.HolderInstance.bloodStatForSimGame.Add(pKey, bloodLevelDecimal);
            }
            else if (ModInit.modSettings.UseSimBleedingEffects)
            {
                PilotInjuryHolder.HolderInstance.bloodStatForSimGame[pKey] = bloodLevelDecimal;
            }
            ModInit.modLog?.Info?.Write($"Calculated {pKey}'s bloodLevel fraction: {bloodLevelDecimal}!");
            var bleedingEffectTotal = PilotInjuryManager.ManagerInstance.BleedingEffectsList.Count + 1;

            var bleedLvl = 0;
            foreach (var bleedEffect in PilotInjuryManager.ManagerInstance.BleedingEffectsList)
            {
                var bleedingEffectDecimal = 1 - bleedEffect.bleedingEffectLvl / (float) bleedingEffectTotal;
                ModInit.modLog?.Info?.Write($"{bleedEffect.bleedingName} needs blood level <= {bleedingEffectDecimal} to apply.");
                if (!(bloodLevelDecimal <=
                      bleedingEffectDecimal)) continue;
                bleedLvl = bleedEffect.bleedingEffectLvl;
                ModInit.modLog?.Info?.Write($"Calculated bleedEffectLvl {bleedLvl}.");
                break;
            }
            var tempList = new List<BleedingEffect>(PilotInjuryManager.ManagerInstance.BleedingEffectsList.Where(x=>x.bleedingEffectLvl == bleedLvl));
            if (tempList.Count == 0) return;

            var idx = UnityEngine.Random.Range(0, tempList.Count);

            var chosenBleedingEffect = tempList[idx];

            ModInit.modLog?.Info?.Write($"{chosenBleedingEffect.bleedingName} chosen for pilot {pilot.Description.Callsign}_{pKey}");

            var effectsList =
                UnityGameInstance.BattleTechGame.Combat.EffectManager.GetAllEffectsTargeting(pilot.ParentActor);

            foreach (EffectData effectData in chosenBleedingEffect.effects)
            {
                if (effectsList.Any(x => x.EffectData?.Description?.Id == effectData?.Description?.Id))
                {
                    ModInit.modLog?.Info?.Write($"{pilot.Description.Callsign}_{pKey} already has bleeding effect {effectData.Description.Name}, skipping.");
                    continue;
                }
                ModInit.modLog?.Info?.Write($"processing {effectData.Description.Name} for {pilot.Description.Callsign}_{pKey}");

                if (effectData.targetingData.effectTriggerType == EffectTriggerType.Passive &&
                    effectData.targetingData.effectTargetType == EffectTargetType.Creator)
                {
                    var id = ($"BleedingEffect_{pilot.Description.Callsign}_{effectData.Description.Id}");

                    ModInit.modLog?.Info?.Write($"Applying {id}");
                    pilot.ParentActor.Combat.EffectManager.CreateEffect(effectData, id, -1, pilot.ParentActor, pilot.ParentActor, default(WeaponHitInfo), 1);
                }
            }
        }
    }

    public class PilotInjuryManager
    {
        public static PilotInjuryManager _instance;
        public List<OvercrowdedEffect> OvercrowdedEffectsList;
        public List<Injury> InjuryEffectsList;
        public List<Injury> InternalDmgInjuries;
        public List<BleedingEffect> BleedingEffectsList;
        public List<SimBleedingEffect> SimBleedingEffectList;
        public List<string> InjuryEffectIDs;

        public static PilotInjuryManager ManagerInstance
        {
            get
            {
                if (_instance == null) _instance = new PilotInjuryManager();
                return _instance;
            }
        }

        public SimGameEventResult processSimBleedingSettings(JObject jObject)
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

        public void Initialize()
        {
            OvercrowdedEffectsList = new List<OvercrowdedEffect>();
            ModInit.modLog.Info?.Write($"[InitializeEffects] Initializing effects for Overcrowding");
            foreach (var overcrowdEffect in ModInit.modSettings.OvercrowdingEffects)
            {
                ModInit.modLog?.Info?.Write($"Adding effects for {overcrowdEffect.ID}!");
                foreach (var jObject in overcrowdEffect.effectDataJO)
                {
                    var effectData = new EffectData();
                    effectData.FromJSON(jObject.ToString());
                    overcrowdEffect.effects.Add(effectData);
                }
                foreach (var jObject in overcrowdEffect.simResultJO)
                {
                    var simResult = processSimBleedingSettings(jObject);
                    ModInit.modLog?.Info?.Write($"TEMPORARY: {simResult.Scope}\n{simResult.Requirements}\n{simResult.AddedTags}\n{simResult.RemovedTags}\n{simResult.Stats} AND {JsonConvert.SerializeObject(simResult)}!");

                    overcrowdEffect.simResult.Add(simResult);
                }
                OvercrowdedEffectsList.Add(overcrowdEffect);
            }

            SimBleedingEffectList = new List<SimBleedingEffect>();
            foreach (var simBleedingEffect in ModInit.modSettings.SimBleedingEffects) 
            {
                ModInit.modLog?.Info?.Write($"Adding effects for {simBleedingEffect.simBleedingEffectID}!");
                foreach (var jObject in simBleedingEffect.simResultJO)
                {
                    var simResult = processSimBleedingSettings(jObject);
                    ModInit.modLog?.Info?.Write($"TEMPORARY: {simResult.Scope}\n{simResult.Requirements}\n{simResult.AddedTags}\n{simResult.RemovedTags}\n{simResult.Stats} AND {JsonConvert.SerializeObject(simResult)}!");

                    simBleedingEffect.simResult.Add(simResult);
                }
                SimBleedingEffectList.Add(simBleedingEffect);
            }

            SimBleedingEffectList = SimBleedingEffectList.OrderByDescending(x => x.bleedingEffectLvl).ToList();

            BleedingEffectsList = new List<BleedingEffect>();
            foreach (var bleedingEffect in ModInit.modSettings.BleedingEffects) 
            {
                ModInit.modLog?.Info?.Write($"Adding effects for {bleedingEffect.bleedingName}!");
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
            InjuryEffectIDs = new List<string>();
            foreach (var injuryEffect in ModInit.modSettings.InjuryEffectsList) 
            {
                ModInit.modLog?.Info?.Write($"Adding effects for {injuryEffect.injuryName}!");
                foreach (var jObject in injuryEffect.effectDataJO)
                {
                    var effectData = new EffectData();
                    effectData.FromJSON(jObject.ToString());
                    injuryEffect.effects.Add(effectData);
                    InjuryEffectIDs.Add(effectData.Description.Id);
                }
                InjuryEffectsList.Add(injuryEffect);
                
            }
            
            if (ModInit.modSettings.enableInternalDmgInjuries)
            {
                InternalDmgInjuries = new List<Injury>();

                foreach (var internalDmgEffect in ModInit.modSettings.InternalDmgInjuries)
                {
                    ModInit.modLog?.Info?.Write($"Adding effects for {internalDmgEffect.injuryName}!");
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
        public static void PreloadIcons()
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

        public void GatherAndApplyInjuries(AbstractActor actor)
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
                    ModInit.modLog?.Info?.Write($"Pilot {pKey} gets {resultINT} {statType} due to bleeding previously.");
                }
                else
                {
                    float.TryParse(statMod, out var resultFLT);
                    if (resultFLT != 0)
                    {
                        p?.StatCollection.ModifyStat<float>("TBAS_Injuries", 0, statType,
                            StatCollection.StatOperation.Float_Add, resultFLT);
                        ModInit.modLog?.Info?.Write($"Pilot {pKey} gets {resultFLT} {statType} due to bleeding previously.");
                    }
                }
            }


            foreach (var id in PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey])
            {
                foreach (Injury injury in ManagerInstance.InternalDmgInjuries.Where(x => x.injuryID == id))
                {
                    this.applyInjuryEffects(actor, injury);
                    ModInit.modLog?.Info?.Write($"Gathered {injury.injuryName} for {p.Description.Callsign}_{pKey}");
                }
                foreach (Injury injury in ManagerInstance.InjuryEffectsList.Where(x => x.injuryID == id))
                {
                    this.applyInjuryEffects(actor, injury);
                    ModInit.modLog?.Info?.Write($"Gathered {injury.injuryName} for {p.Description.Callsign}_{pKey}");
                }
            }
        }

        public void applyInjuryEffects(AbstractActor actor, Injury injury)
        {
            var p = actor.GetPilot();
            if (injury.InjuryTags.Count > 0)
            {
                p.pilotDef.PilotTags.AddRange(injury.InjuryTags);
                ModInit.modLog?.Info?.Write($"Adding injury tags {string.Join(", ", injury.InjuryTags)}");
            }
            if (actor.StatCollection.GetValue<bool>(ModInit.modSettings.NullifiesInjuryEffectsStat) || actor.GetStaticUnitTags().Contains(ModInit.modSettings.disableTBASTag) || (actor.UnitIsTrooperSquad() && ModInit.modSettings.disableTBASTroopers))
            {
                ModInit.modLog?.Info?.Write($"Found advanced life-support - {actor.StatCollection.GetValue<bool>(ModInit.modSettings.NullifiesInjuryEffectsStat)} or tag {ModInit.modSettings.disableTBASTag} and  - {actor.GetStaticUnitTags().Contains(ModInit.modSettings.disableTBASTag)} or is a trooper squad and disableTBASTroopers {ModInit.modSettings.disableTBASTroopers}:  nullifying injury effects for {p.Callsign}");
                return;
            }
            var pKey = p.FetchGUID();
            ModInit.modLog?.Info?.Write($"processing {injury.effects.Count} injury effects for {p.Description.Callsign}_{pKey}");
            foreach (EffectData effectData in injury.effects)
            {
                ModInit.modLog?.Info?.Write($"processing {effectData.Description.Name} for {p.Description.Callsign}_{pKey}");

                if (effectData.targetingData.effectTriggerType == EffectTriggerType.Passive &&
                    effectData.targetingData.effectTargetType == EffectTargetType.Creator)
                {
                    string id = ($"InjuryEffect_{p.Description.Callsign}_{effectData.Description.Id}");

                    ModInit.modLog?.Info?.Write($"Applying {id}");
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

                if (!injury.effects.Any(x =>
                    x.Description.Id.EndsWith(ModInit.modSettings.BleedingOutSuffix))) return;

                var baseRate = p.GetBleedingRate();
                var multi = p.GetBleedingRateMulti();
                var bleedRate = baseRate * multi;
                ModInit.modLog?.Info?.Write(
                    $"applyInjuryEffects: {p.Callsign}_{pKey} bleeding out at rate of {bleedRate}/activation from base {baseRate} * multi {multi}!");
                var durationInfo = Mathf.CeilToInt(p.GetBloodBank() / (bleedRate) - 1);

                ModInit.modLog?.Info?.Write(
                    $"At ApplyInjuryEffects: Found bleeding effect(s) for {actor.GetPilot().Callsign}, processing time to bleedout for display: {durationInfo} activations remain");
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


        public void rollInjury(Pilot pilot, int dmg, DamageType damageType, InjuryReason reason) //to be postfix patched into InjurePilot
        {
            InjuryLoc loc;
            var pKey = pilot.FetchGUID();

            if (!PilotInjuryHolder.HolderInstance.combatInjuriesMap.ContainsKey(pKey))
            {
                PilotInjuryHolder.HolderInstance.combatInjuriesMap.Add(pKey, new List<string>());
                ModInit.modLog?.Info?.Write($"{pilot.Callsign} missing, added to combatInjuriesMap");
            }

            if (!PilotInjuryHolder.HolderInstance.pilotInjuriesMap.ContainsKey(pKey))
            {
                PilotInjuryHolder.HolderInstance.pilotInjuriesMap.Add(pKey, new List<string>());
                ModInit.modLog?.Info?.Write($"{pilot.Callsign} missing, added to pilotInjuriesMap");
            }

            for (int i = 0; i < dmg; i++)
            {
                //adding locations weights for preexisting injuries

                //does pilot have existing injuries
                var injuryLocs = new List<int>(Enumerable.Range(2,6));
                if (PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey].Count > 0 || PilotInjuryHolder.HolderInstance.combatInjuriesMap[pKey].Count > 0)
                {
                    var curInjuryList = new List<Injury>();
                    ModInit.modLog?.Info?.Write($"{pilot?.Callsign} has preexisting conditions, processing location weight");
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
                        ModInit.modLog?.Info?.Write($"Found advanced life-support: no bleeding out injuries allowed for {pilot.Callsign}!");
                    }

                    if (ModInit.modSettings.reInjureWeightAppliesCurrentContract)
                    {
                        foreach (var inj in curInjuryList)
                        {
                            for (int t = 0; t < ModInit.modSettings.reInjureLocWeight; t++)
                            {
                                injuryLocs.Add((int)inj.injuryLoc);
                            }
                            ModInit.modLog?.Info?.Write($"{inj.injuryLoc.ToString()} has weight of {ModInit.modSettings.reInjureLocWeight}");
                        }
                    }

                    else
                    {
                        if (ModState.LastInjuryIDs.TryGetValue(pilot.FetchGUID(), out var lastInjuriesList))
                        {
                            foreach (var inj in curInjuryList.Where(x => !lastInjuriesList.Contains(x.injuryID)))
                            {
                                for (int t = 0; t < ModInit.modSettings.reInjureLocWeight; t++)
                                {
                                    injuryLocs.Add((int) inj.injuryLoc);
                                }

                                ModInit.modLog?.Info?.Write(
                                    $"{inj.injuryLoc.ToString()} has weight of {ModInit.modSettings.reInjureLocWeight}");
                            }
                        }
                        else ModState.LastInjuryIDs.Add(pilot.FetchGUID(), new List<string>());
                    }
                    ModInit.modLog?.Info?.Write($"Final list of injury location indices: {string.Join(",", injuryLocs)}");
                }

                var injuryList = new List<Injury>(ManagerInstance.InjuryEffectsList);

                var idx = UnityEngine.Random.Range(0, injuryLocs.Count);
                loc = (InjuryLoc)injuryLocs[idx];

//                loc = (InjuryLoc)UnityEngine.Random.Range(2, 8); // old

                if ((int) reason == 20)
                {
                    loc = InjuryLoc.Head;
                    injuryList.RemoveAll(x => x.injuryLoc != InjuryLoc.Head || !string.IsNullOrEmpty(x.injuryID_Post));
                }

                else if (damageType == DamageType.Overheat || damageType == DamageType.OverheatSelf || (int)reason ==  101 || (int)reason == 666)
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
                ModInit.modLog?.Info?.Write($"Injury Loc {loc} chosen for {pilot?.Callsign}");

                injuryList.RemoveAll(x => x.severity >= 100 || x.injuryLoc != loc);
                //                injuryList.RemoveAll(x => PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey].Contains(x.injuryID));
                //                ModInit.modLog?.Info?.Write($"Removed all injuries that {pilot?.Callsign} already has.");

                if (pilot != null && pilot.ParentActor.StatCollection.GetValue<bool>(ModInit.modSettings.DisableBleedingStat))
                {
                    injuryList.RemoveAll(x => !string.IsNullOrEmpty(x.injuryID_Post));
                    ModInit.modLog?.Info?.Write($"Found advanced life-support: no bleeding out injuries allowed for {pilot.Callsign}!");
                }

                var chosen = injuryList[UnityEngine.Random.Range(0, injuryList.Count)]; 
                ModInit.modLog?.Info?.Write($"Injury {chosen.injuryName} chosen for {pilot?.Callsign}");

                PilotInjuryHolder.HolderInstance.combatInjuriesMap[pKey].Add(chosen.injuryID);
                ModInit.modLog?.Info?.Write(
                    $"Adding {chosen.injuryName} to {pilot?.Callsign}'s combat injury map. PilotID: {pKey}");


                if (ModState.LastInjuryIDs.ContainsKey(pilot.FetchGUID()))
                {
                    ModState.LastInjuryIDs[pilot.FetchGUID()].Add(chosen.injuryID);
                }
                else
                {
                    ModState.LastInjuryIDs.Add(pilot.FetchGUID(), new List<string>(){chosen.injuryID});
                }
                //var newList = pilot?.StatCollection.GetValue<List<string>>("LastInjuryId");
                //newList?.Add(chosen.injuryID);
                //pilot?.StatCollection.ModifyStat<List<string>>("TBAS_Injuries", 0, "LastInjuryId",StatCollection.StatOperation.Set, newList);

                ModInit.modLog?.Info?.Write(
                    $"added {chosen.injuryName} to {pilot?.Callsign}'s LastInjuryId in modstate. PilotID: {pKey}");

                pilot?.StatCollection.ModifyStat<int>("TBAS_Injuries", 0, MissionKilledStat,
                    StatCollection.StatOperation.Int_Add, chosen.severity);

                var mkillStat = pilot?.StatCollection.GetValue<int>(MissionKilledStat);
                ModInit.modLog?.Info?.Write(
                    $"Adding {chosen.injuryName}'s severity value: {chosen.severity} to {pilot?.Callsign}'s MissionKilledStat. Total is now {mkillStat}");

                if (pilot?.StatCollection.GetValue<int>(MissionKilledStat) > 0 && ModInit.modSettings.enableConsciousness)
                {
                    var missionKill = new Text("<color=#C65102>Pilot's current Consciousness Threshold: {0} of {1}</color=#C65102>",
                        new object[]
                        {
                            mkillStat,
                            pilot?.StatCollection.GetValue<int>("MissionKilledThreshold")
                        });


                    pilot?.ParentActor.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(
                        new ShowActorInfoSequence(pilot?.ParentActor, missionKill, FloatieMessage.MessageNature.PilotInjury, false)));
                }

                if (!string.IsNullOrEmpty(chosen.injuryID_Post))
                {
                    var em = UnityGameInstance.BattleTechGame.Combat.EffectManager;
                    var effects = em.GetAllEffectsTargeting(pilot?.ParentActor); //targeting parent actor maybe?
                    var currentRate = pilot.GetBleedingRate();
                    if (currentRate == 0f)
                    {
                        pilot.SetBleedingRate(chosen.severity);
                        ModInit.modLog?.Info?.Write($"{pilot?.Callsign}'s Bleeding Rate was 0, now {currentRate}, {pilot.GetBleedingRateMulti() }is multi");
                    }
                    else
                    {
                        var continuator = false;
                        foreach (var effect in effects)
                        {
                            if (effect.EffectData?.Description?.Id == null)
                            {
                                ModInit.modLog?.Info?.Write(
                                    $"Effect {effect?.EffectData} had null description");
                                continue;
                            }

                            if (effect.EffectData.Description.Id.EndsWith(ModInit.modSettings.BleedingOutSuffix))
                            {
                                continuator = true;
                                break;
                            }
                        }
                        if (!continuator) return;

                        var addToRate = 0f;
                        if (ModInit.modSettings.additiveBleedingFactor < 0)
                        {
                            ModInit.modLog?.Info?.Write(
                                $"{pilot?.Callsign}'s Bleeding Rate {currentRate} before.");
                            addToRate = Math.Abs(ModInit.modSettings.additiveBleedingFactor);
                            currentRate += addToRate;
                            ModInit.modLog?.Info?.Write(
                                $"{pilot?.Callsign}'s Bleeding Rate have {addToRate} added to {currentRate}.");
                        }

                        else if (ModInit.modSettings.additiveBleedingFactor < 1 &&
                                 ModInit.modSettings.additiveBleedingFactor > 0)
                        {
                            ModInit.modLog?.Info?.Write(
                                $"{pilot?.Callsign}'s Bleeding Rate now {currentRate} before.");
                            addToRate = currentRate * ModInit.modSettings.additiveBleedingFactor;
                            currentRate += addToRate;
                            ModInit.modLog?.Info?.Write(
                                $"{pilot?.Callsign}'s Bleeding Rate have {addToRate} added to {currentRate}.");
                        }
                        pilot.SetBleedingRate(currentRate);
                        ModInit.modLog?.Info?.Write(
                            $"{pilot?.Callsign}'s Bleeding Rate set to {currentRate}.");
                    }

                    if (pilot.GetBleedingRate() * pilot.GetBleedingRateMulti() > 0f)
                    {
                        pilot.ApplyClosestBleedingEffect();
                    }
                }
                applyInjuryEffects(pilot?.ParentActor, chosen);
            }
        }

        public void rollInjuryFeedback(Pilot pilot, int dmg, DamageType damageType) //to be postfix patched into InjurePilot
        {
            var pKey = pilot.FetchGUID();

            if (!PilotInjuryHolder.HolderInstance.combatInjuriesMap.ContainsKey(pKey))
            {
                PilotInjuryHolder.HolderInstance.combatInjuriesMap.Add(pKey, new List<string>());
                ModInit.modLog?.Info?.Write($"{pilot.Callsign} missing, added to combatInjuriesMap");
            }

            if (!PilotInjuryHolder.HolderInstance.pilotInjuriesMap.ContainsKey(pKey))
            {
                PilotInjuryHolder.HolderInstance.pilotInjuriesMap.Add(pKey, new List<string>());
                ModInit.modLog?.Info?.Write($"{pilot.Callsign} missing, added to pilotInjuriesMap");
            }

            var loc = InjuryLoc.Head;

            for (int i = 0; i < dmg; i++)
            {
                var injuryList = new List<Injury>(ManagerInstance.InternalDmgInjuries);


                injuryList.RemoveAll(x => x.severity >= 100 || x.injuryLoc != loc);
                var chosen = injuryList[UnityEngine.Random.Range(0, injuryList.Count)];
                ModInit.modLog?.Info?.Write($"Feedback Injury {chosen.injuryName} chosen for {pilot?.Callsign}");
                

                PilotInjuryHolder.HolderInstance.combatInjuriesMap[pKey].Add(chosen.injuryID);
                ModInit.modLog?.Info?.Write(
                    $"Adding {chosen.injuryName} to {pilot?.Callsign}'s combat injury map. PilotID: {pKey}");

                pilot?.StatCollection.ModifyStat<int>("TBAS_Injuries", 0, MissionKilledStat,
                    StatCollection.StatOperation.Int_Add, chosen.severity);
                ModInit.modLog?.Info?.Write(
                    $"Adding {chosen.injuryName}'s severity value: {chosen.severity} to {pilot?.Callsign}'s MissionKilledStat");
                if (pilot?.StatCollection.GetValue<int>(MissionKilledStat) > 0 && ModInit.modSettings.enableConsciousness)
                {
                    var mknum = pilot?.StatCollection.GetValue<int>(MissionKilledStat);
                    var missionKill = new Text("<color=#C65102>Pilot's current Consciousness Threshold: {0} of {1}</color=#C65102>",
                        new object[]
                        {
                            mknum,
                            pilot?.StatCollection.GetValue<int>("MissionKilledThreshold")
                        });


                    pilot?.ParentActor.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(
                        new ShowActorInfoSequence(pilot?.ParentActor, missionKill, FloatieMessage.MessageNature.PilotInjury, false)));
                }
                applyInjuryEffects(pilot?.ParentActor, chosen);

            }
        }

        public void rollInjurySG(Pilot pilot, int dmg, DamageType damageType) //to be postfix patched into InjurePilot
        {
            var pKey = pilot.FetchGUID();


            if (!PilotInjuryHolder.HolderInstance.pilotInjuriesMap.ContainsKey(pKey))
            {
                PilotInjuryHolder.HolderInstance.pilotInjuriesMap.Add(pKey, new List<string>());
                ModInit.modLog?.Info?.Write($"{pilot.Callsign} missing, added to pilotInjuriesMap");
            }

            InjuryLoc loc;

            for (int i = 0; i < dmg; i++)
            {
                var injuryList = new List<Injury>(PilotInjuryManager.ManagerInstance.InjuryEffectsList);
                loc = (InjuryLoc)UnityEngine.Random.Range(2, 8);
                    ModInit.modLog?.Info?.Write($"Injury Loc {loc} chosen for {pilot?.Callsign}");


                injuryList.RemoveAll(x => x.severity >= 100 || x.injuryLoc != loc || !string.IsNullOrEmpty(x.injuryID_Post));
 //               injuryList.RemoveAll(x => PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey].Contains(x.injuryID));
                var chosen = injuryList[UnityEngine.Random.Range(0, injuryList.Count)];
                ModInit.modLog?.Info?.Write($"Injury {chosen.injuryName} chosen for {pilot?.Callsign}");

                PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey].Add(chosen.injuryID);
                ModInit.modLog?.Info?.Write(
                    $"Adding {chosen.injuryName} to {pilot?.Callsign}'s injury map. PilotID: {pKey}");
            }
        }
    }
}
