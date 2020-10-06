﻿using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Harmony;
using System.Reflection;
using static TisButAScratch.Framework.GlobalVars;
using TisButAScratch.Framework;

namespace TisButAScratch
{
    public static class ModInit
    {
        internal static Logger modLog;
        internal static string modDir;


        internal static Settings modSettings;
        public const string HarmonyPackage = "us.tbone.TisButAScratch";
        public static void Init(string directory, string settingsJSON)
        {
            modDir = directory;
            try
            {
                using (StreamReader reader = new StreamReader($"{modDir}/settings.json"))
                {
                    string jsData = reader.ReadToEnd();
                    ModInit.modSettings = JsonConvert.DeserializeObject<Settings>(jsData);
                }
                
            }
            catch (Exception)
            {

                ModInit.modSettings = new Settings();
            }
            modLog = new Logger(modDir, "TBAS", modSettings.enableLogging);
            PilotInjuryManager.ManagerInstance.Initialize();
            PilotInjuryHolder.HolderInstance.Initialize();
            var harmony = HarmonyInstance.Create(HarmonyPackage);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

    }

    class Settings
    {
        public bool enableLogging = false;
        public bool enableFatigue = false;
        public bool enablePsych = false;
        public bool enableLethalTorsoHead = false;
        public int cripplingInjuriesThreshold = -1;
        public int severityCost = 360;
        public float injuryHealTimeMultiplier = 0f;
        public List<Injury> InjuryEffectsList = new List<Injury>();
    }
}