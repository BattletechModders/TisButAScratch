using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using UnityEngine;
using System.Threading.Tasks;
using BattleTech;
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

    public class PilotInjuryManager //need to rewrite aplyPassiveeffects from mechaffinity
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
 //           ManagerInstance.InjuryEffectsList.AddRange(ModInit.modSettings.InjuryEffectsList);
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
    

        internal void GatherAndApplyInjuries(AbstractActor actor)
        {
            var p = actor.GetPilot();
            var pKey = p.FetchGUID();
            foreach (var id in PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey])
            {
                foreach (Injury injury in ManagerInstance.InjuryEffectsList.Where(x => x.injuryID == id))
                {
                    this.applyInjuryEffects(actor, injury);
                    ModInit.modLog.LogMessage($"Gathered {injury.injuryName} for {p.Description.Callsign}");
                }
            }
        }

        protected void applyInjuryEffects(AbstractActor actor, Injury injury)
        {
            var p = actor.GetPilot();
            ModInit.modLog.LogMessage($"processing {injury.effects.Count} injury effects for {p.Description.Callsign}");
            foreach (EffectData effectData in injury.effects)
            {
                ModInit.modLog.LogMessage($"processing {effectData.Description.Name} for {p.Description.Callsign}");

                if (effectData.targetingData.effectTriggerType == EffectTriggerType.Passive &&
                    effectData.targetingData.effectTargetType == EffectTargetType.Creator)
                {
                    string id = ($"InjuryEffect_{p.Description.Callsign}_{effectData.Description.Id}");

                    ModInit.modLog.LogMessage($"Applying {id}");
                    actor.Combat.EffectManager.CreateEffect(effectData, id, -1, actor, actor, default(WeaponHitInfo), 1,
                        false);
                }
            }
        }


        internal void rollInjury(Pilot pilot, int dmg, DamageType damageType) //to be postfix patched into InjurePilot
        {
            var loc = InjuryLoc.NOT_SET;
            

            for (int i = 0; i < dmg; i++)
            {
                var injuryList = new List<Injury>(ManagerInstance.InjuryEffectsList);
                if (damageType == DamageType.Overheat || damageType == DamageType.OverheatSelf)
                {
                    loc = (InjuryLoc) UnityEngine.Random.Range(3, 8);
                    ModInit.modLog.LogMessage($"Injury Loc {loc} chosen for {pilot?.Callsign}");
                    injuryList.RemoveAll(x => x.couldBeThermal == false || x.severity >= 100);
                }

                else if (damageType == DamageType.Knockdown || damageType == DamageType.KnockdownSelf)
                {
                    loc = (InjuryLoc) UnityEngine.Random.Range(2, 8);
                    ModInit.modLog.LogMessage($"Injury Loc {loc} chosen for {pilot?.Callsign}");
                    injuryList.RemoveAll(x => x.couldBeThermal == true || x.severity >= 100);
                }
                else
                {
                    loc = (InjuryLoc) UnityEngine.Random.Range(2, 8);
                    ModInit.modLog.LogMessage($"Injury Loc {loc} chosen for {pilot?.Callsign}");
                }

                injuryList.RemoveAll(x => x.severity >= 100 || x.injuryLoc != loc);//this done to make sure CRIPPLED doesn't show up
                            //some kind of Guts check for severity maybe?
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


                injuryList.RemoveAll(x => x.severity >= 100 || x.injuryLoc != loc);//this done to make sure CRIPPLED doesn't show up
                                                                                   //some kind of Guts check for severity maybe?
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
            var loc = InjuryLoc.NOT_SET;


            for (int i = 0; i < dmg; i++)
            {
                var injuryList = new List<Injury>(ManagerInstance.InjuryEffectsList);
                if (damageType == DamageType.Overheat || damageType == DamageType.OverheatSelf)
                {
                    loc = (InjuryLoc)UnityEngine.Random.Range(3, 8);
                    ModInit.modLog.LogMessage($"Injury Loc {loc} chosen for {pilot?.Callsign}");
                    injuryList.RemoveAll(x => x.couldBeThermal == false || x.severity >= 100);
                }

                else if (damageType == DamageType.Knockdown || damageType == DamageType.KnockdownSelf)
                {
                    loc = (InjuryLoc)UnityEngine.Random.Range(2, 8);
                    ModInit.modLog.LogMessage($"Injury Loc {loc} chosen for {pilot?.Callsign}");
                    injuryList.RemoveAll(x => x.couldBeThermal == true || x.severity >= 100);
                }
                else
                {
                    loc = (InjuryLoc)UnityEngine.Random.Range(2, 8);
                    ModInit.modLog.LogMessage($"Injury Loc {loc} chosen for {pilot?.Callsign}");
                }

                injuryList.RemoveAll(x => x.severity >= 100 || x.injuryLoc != loc);//this done to make sure CRIPPLED doesn't show up
                                                                                   //some kind of Guts check for severity maybe?
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
