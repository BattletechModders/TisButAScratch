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
            }
        }


        internal void rollInjury(Pilot pilot, int dmg, DamageType damageType) //to be postfix patched into InjurePilot
        {
            var loc = InjuryLoc.NOT_SET;

            for (int i = 0; i < dmg; i++)
            {
                var injuryList = new List<Injury>(ManagerInstance.InjuryEffectsList);
                loc = (InjuryLoc)UnityEngine.Random.Range(2, 8);
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
                var pKey = pilot.FetchGUID();

                PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey].Add(chosen.injuryID);
                ModInit.modLog.LogMessage(
                    $"Adding {chosen.injuryName} to {pilot?.Callsign}'s injury map. PilotID: {pKey}");

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

                PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey].Add(chosen.injuryID);
                ModInit.modLog.LogMessage(
                    $"Adding {chosen.injuryName} to {pilot?.Callsign}'s injury map. PilotID: {pKey}");

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
