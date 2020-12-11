using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using UnityEngine;
using System.Threading.Tasks;
using SVGImporter;
using BattleTech;
using Localize;
using static TisButAScratch.Framework.GlobalVars;
using Newtonsoft.Json.Linq;


namespace TisButAScratch.Framework
{
    public static class PilotExtensions
    {
        internal static string FetchGUID(this Pilot pilot)
        {
            var guid = pilot.pilotDef.PilotTags.FirstOrDefault(x => x.StartsWith(iGUID));
            if (string.IsNullOrEmpty(guid))
            {
                ModInit.modLog.LogMessage($"WTF IS GUID NULL?!");
            }
            return guid;
        }
    }

    public class PilotInjuryManager
    {
        private static PilotInjuryManager _instance;
        public List<Injury> InjuryEffectsList;
        public List<Injury> InternalDmgInjuries;

        public static PilotInjuryManager ManagerInstance
        {
            get
            {
                if (_instance == null) _instance = new PilotInjuryManager();
                return _instance;
            }
        }

        
        internal void Initialize()
        {
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
            foreach (var id in PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey])
            {
                foreach (Injury injury in ManagerInstance.InternalDmgInjuries.Where(x => x.injuryID == id))
                {
                    this.applyInjuryEffects(actor, injury);
                    ModInit.modLog.LogMessage($"Gathered {injury.injuryName} for {p.Description.Callsign}{pKey}");
                }
                foreach (Injury injury in ManagerInstance.InjuryEffectsList.Where(x => x.injuryID == id))
                {
                    this.applyInjuryEffects(actor, injury);
                    ModInit.modLog.LogMessage($"Gathered {injury.injuryName} for {p.Description.Callsign}{pKey}");
                }
            }
        }

        protected void applyInjuryEffects(AbstractActor actor, Injury injury)
        {
            var p = actor.GetPilot();
            var pKey = p.FetchGUID();
            ModInit.modLog.LogMessage($"processing {injury.effects.Count} injury effects for {p.Description.Callsign}{pKey}");
            foreach (EffectData effectData in injury.effects)
            {
                ModInit.modLog.LogMessage($"processing {effectData.Description.Name} for {p.Description.Callsign}{pKey}");

                if (effectData.targetingData.effectTriggerType == EffectTriggerType.Passive &&
                    effectData.targetingData.effectTargetType == EffectTargetType.Creator)
                {
                    string id = ($"InjuryEffect_{p.Description.Callsign}_{effectData.Description.Id}");

                    ModInit.modLog.LogMessage($"Applying {id}");
                    actor.Combat.EffectManager.CreateEffect(effectData, id, -1, actor, actor, default(WeaponHitInfo), 1,
                        false);
                }
            }
            //added to display floaties? need totest
            if (actor.Combat.TurnDirector.GameHasBegun)
            {
                actor.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(
                    new ShowActorInfoSequence(actor, new Text("{0}! Severity {1} Injury!", new object[] { injury.injuryName, injury.severity }), FloatieMessage.MessageNature.PilotInjury, true)));
                if (!string.IsNullOrEmpty(injury.injuryID_Post))
                {
                    var txt = new Text("<color=#FF0000>Pilot is bleeding out!</color=#FF0000>");

                    actor.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(
                        new ShowActorInfoSequence(actor, txt, FloatieMessage.MessageNature.PilotInjury, true)));
                }
            }
        }


        internal void rollInjury(Pilot pilot, int dmg, DamageType damageType) //to be postfix patched into InjurePilot
        {
            var loc = InjuryLoc.NOT_SET;

            for (int i = 0; i < dmg; i++)
            {
                //adding locations weights for preexisting injuries
                var pKey = pilot.FetchGUID();
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
                        foreach (var inj in curInjuryList.Where(x => !pilot.StatCollection.GetValue<List<string>>("LastInjuryId").Contains(x.injuryID)))
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
                    injuryList.RemoveAll(x => x.couldBeThermal == true || x.severity >= 100);
                }
                else
                {
                    injuryList.RemoveAll(x => x.severity >= 100);
                }
                ModInit.modLog.LogMessage($"Injury Loc {loc} chosen for {pilot?.Callsign}");

                injuryList.RemoveAll(x => x.severity >= 100 || x.injuryLoc != loc);
                var chosen = injuryList[UnityEngine.Random.Range(0, injuryList.Count)]; 
                ModInit.modLog.LogMessage($"Injury {chosen.injuryName} chosen for {pilot?.Callsign}");

                PilotInjuryHolder.HolderInstance.combatInjuriesMap[pKey].Add(chosen.injuryID);
                ModInit.modLog.LogMessage(
                    $"Adding {chosen.injuryName} to {pilot?.Callsign}'s combat injury map. PilotID: {pKey}");

                var newList = pilot.StatCollection.GetValue<List<string>>("LastInjuryId");
                newList.Add(chosen.injuryID);

                pilot.StatCollection.ModifyStat<List<string>>("TBAS_Injuries", 0, "LastInjuryId",
                    StatCollection.StatOperation.Set, newList, -1, true);

                ModInit.modLog.LogMessage(
                    $"Setting {chosen.injuryName} to {pilot?.Callsign}'s LastInjuryId stat. PilotID: {pKey}");

                pilot.StatCollection.ModifyStat<int>("TBAS_Injuries", 0, MissionKilledStat,
                    StatCollection.StatOperation.Int_Add, chosen.severity, -1, true);

                ModInit.modLog.LogMessage(
                    $"Adding {chosen.injuryName}'s severity value: {chosen.severity} to {pilot?.Callsign}'s MissionKilledStat");

                applyInjuryEffects(pilot.ParentActor, chosen);
                
            }
        }

        internal void rollInjuryFeedback(Pilot pilot, int dmg, DamageType damageType) //to be postfix patched into InjurePilot
        {
            var loc = InjuryLoc.Head;

            for (int i = 0; i < dmg; i++)
            {
                var injuryList = new List<Injury>(ManagerInstance.InternalDmgInjuries);


                injuryList.RemoveAll(x => x.severity >= 100 || x.injuryLoc != loc);
                var chosen = injuryList[UnityEngine.Random.Range(0, injuryList.Count)];
                ModInit.modLog.LogMessage($"Feedback Injury {chosen.injuryName} chosen for {pilot?.Callsign}");
                var pKey = pilot.FetchGUID();

                PilotInjuryHolder.HolderInstance.combatInjuriesMap[pKey].Add(chosen.injuryID);
                ModInit.modLog.LogMessage(
                    $"Adding {chosen.injuryName} to {pilot?.Callsign}'s combat injury map. PilotID: {pKey}");

                pilot.StatCollection.ModifyStat<int>("TBAS_Injuries", 0, MissionKilledStat,
                    StatCollection.StatOperation.Int_Add, chosen.severity, -1, true);
                ModInit.modLog.LogMessage(
                    $"Adding {chosen.injuryName}'s severity value: {chosen.severity} to {pilot?.Callsign}'s MissionKilledStat");

                applyInjuryEffects(pilot.ParentActor, chosen);

            }
        }

        internal void rollInjurySG(Pilot pilot, int dmg, DamageType damageType) //to be postfix patched into InjurePilot
        {
            InjuryLoc loc;

            for (int i = 0; i < dmg; i++)
            {
                var injuryList = new List<Injury>(PilotInjuryManager.ManagerInstance.InjuryEffectsList);
                loc = (InjuryLoc)UnityEngine.Random.Range(2, 8);
                    ModInit.modLog.LogMessage($"Injury Loc {loc} chosen for {pilot?.Callsign}");


                injuryList.RemoveAll(x => x.severity >= 100 || x.injuryLoc != loc || x.injuryID_Post != "");

                var chosen = injuryList[UnityEngine.Random.Range(0, injuryList.Count)];
                ModInit.modLog.LogMessage($"Injury {chosen.injuryName} chosen for {pilot?.Callsign}");
                var pKey = pilot.FetchGUID();

                PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey].Add(chosen.injuryID);
                ModInit.modLog.LogMessage(
                    $"Adding {chosen.injuryName} to {pilot?.Callsign}'s injury map. PilotID: {pKey}");
            }
        }
    }
}
