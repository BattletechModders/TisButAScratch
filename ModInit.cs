using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Harmony;
using System.Reflection;
using BattleTech;
using IRBTModUtils.Logging;
using TisButAScratch.Framework;

namespace TisButAScratch
{
    public static class ModInit
    {
        internal static DeferringLogger modLog;
        private static string modDir;


        internal static Settings modSettings;
        public const string HarmonyPackage = "us.tbone.TisButAScratch";
        public static void Init(string directory, string settingsJSON)
        {
            modDir = directory;
            Exception settingsException = null;
            try
            {
                using (StreamReader reader = new StreamReader($"{modDir}/settings.json"))
                {
                    string jsData = reader.ReadToEnd();
                    ModInit.modSettings = JsonConvert.DeserializeObject<Settings>(jsData);
                }
                
            }
            catch (Exception ex)
            {
                settingsException =ex;
                ModInit.modSettings = new Settings();
            }
            modLog = new DeferringLogger(modDir, "TBAS", false, modSettings.enableTrace);
            //HarmonyInstance.DEBUG = true;
            if (settingsException != null)
            {
                ModInit.modLog?.Error?.Write($"EXCEPTION while reading settings file! Error was: {settingsException}");
            }
            ModInit.modLog?.Info?.Write($"Initializing TisButAScratch - Version {typeof(Settings).Assembly.GetName().Version}");
            PilotInjuryManager.ManagerInstance.Initialize();
            PilotInjuryHolder.HolderInstance.Initialize();
            var harmony = HarmonyInstance.Create(HarmonyPackage);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            
        }

    }

    class Settings
    {
        public bool enableLogging = false;
        public bool enableTrace = false;
        public bool debugPatchEnabled = false;
        public bool enableConsciousness = true;
        public bool enableLethalTorsoHead = false;
        public bool debilIncapacitates = false;

        public bool BleedingOutLethal = false;
        public string BleedingOutSuffix = "_bleedout";

        public bool enableInternalDmgInjuries = false;
        public string internalDmgStatName = "InjureOnStructDmg";
        public int internalDmgInjuryLimit = -1;
        public float internalDmgLvlReq = 0f;
        public int missionKillSeverityThreshold = 20;
        public bool reInjureWeightAppliesCurrentContract = false;
        public int reInjureLocWeight = 0;

        public string injureVehiclePilotOnDestroy = "MAX"; // will check for "MAX", "HIGH", "SINGLE", "OFF". HIGH = MaxInjuries-1 (pilot f'd up, but not dead.)
        public List<string> crewOrCockpitCustomID = new List<string>();
        public List<string> lifeSupportCustomID = new List<string>();

        public bool disableTBASTroopers = false;
        public string disableTBASTag = "TBAS_Disabled";
        public string OverheatInjuryStat = "InjureOnOverheat";
        public string isTorsoMountStatName = "isTorsoMount";
        public bool lifeSupportSupportsLifeTM = false;

        public bool timeHealsAllWounds = false;
        public int debilSeverityThreshold = -1;
        public int severityCost = 360;
        public int debilitatedCost = 1080;
        public float medtechDebilMultiplier = 0.75f;

        public string pilotPainShunt = "pilot_PainShunt";

        public List<PilotingReq> pilotingReqs = new List<PilotingReq>();

        public float injuryHealTimeMultiplier = 0f;
        public List<ChassisLocations> internalDmgInjuryLocs = new List<ChassisLocations>();
        public List<Injury> InjuryEffectsList = new List<Injury>();
        public List<Injury> InternalDmgInjuries = new List<Injury>();

        public string DisableBleedingStat = "DisablesBleeding";
        public string NullifiesInjuryEffectsStat = "NullifiesInjuryEffects";

        public float additiveBleedingFactor = 0f; // if negative, subtract, if decimal multiply.
        public float minBloodBank = 1;
        public float baseBloodBankAdd = 0;
        public float factorBloodBankMult = 1.5f;
        public bool UseGutsForBloodBank = true; // if false, uses Health

        public bool UseBleedingEffects = true;
        public List<BleedingEffect> BleedingEffects = new List<BleedingEffect>();
        public bool UseSimBleedingEffects = true;
        public List<SimBleedingEffect> SimBleedingEffects = new List<SimBleedingEffect>();
        public bool CanIntentionallyHotBunk = false;
        public List<OvercrowdedEffect> OvercrowdingEffects = new List<OvercrowdedEffect>();
    }
}