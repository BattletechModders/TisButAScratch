using BattleTech;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace TisButAScratch.Patches
{
    public class FloatyPatches
    {

        [HarmonyPatch(typeof(AbstractActor), "CheckPilotStatusFromAttack")]
        static class AbstractActor_CheckPilotStatusFromAttack
        {
            [HarmonyPriority(Priority.Last)]
            public static bool Prepare() => false;
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilGenerator)
            {
                List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
                ModInit.modLog?.Info?.Write($"Dump code - pre\r");
                ModInit.modLog?.Info?.Write($"{string.Join("\r", codes)}\r");
                
                //return codes;
                // Create a new label for our target point
                Label clearNeedsInjuryLabel = ilGenerator.DefineLabel();

                MethodInfo clearNeedsInjuryMI = AccessTools.DeclaredMethod(typeof(Pilot), "ClearNeedsInjury");

                int injuryStrIdx = 0, clearInjuryIdx = 0;
                var foundIdx = false;
                var foundIdx2 = false;
                for (int i = 0; i < codes.Count; i++)
                {
                    CodeInstruction instruction = codes[i];
                    if (instruction.opcode == OpCodes.Ldstr && "{0}: PILOT INJURED".Equals((string)instruction.operand, StringComparison.InvariantCultureIgnoreCase))
                    {
                        injuryStrIdx = i;
                        foundIdx = true;
                        ModInit.modLog?.Info?.Write($"Found PILOT INJURED instruction at idx: {i}");
                    }
                    else if (instruction.opcode == OpCodes.Callvirt && (MethodInfo)instruction.operand == clearNeedsInjuryMI)
                    {
                        clearInjuryIdx = i;
                        foundIdx2 = true;
                        ModInit.modLog?.Info?.Write($"Found Pilot.ClearNeedsInjury instruction at idx: {i}");
                    }
                }

                if (foundIdx && foundIdx2)
                {
                    CodeInstruction cnjInstruction = codes[clearInjuryIdx - 1];
                    cnjInstruction.labels.Add(clearNeedsInjuryLabel);

                    codes.RemoveRange(injuryStrIdx, 15);
                    codes.Insert(injuryStrIdx - 1, new CodeInstruction(OpCodes.Br_S, clearNeedsInjuryLabel));
                }
                ModInit.modLog?.Info?.Write($"Dump code - post\r");
                ModInit.modLog?.Info?.Write($"{string.Join("\r", codes)}\r");
                return codes;

            }
        }

        [HarmonyPatch(typeof(Mech), "CompleteKnockdown")]
        static class Mech_CompleteKnockdown
        {
            [HarmonyPriority(Priority.Last)]
            public static bool Prepare() => false;
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> codes = new List<CodeInstruction>(instructions);

                int targetIdx = 0;
                var foundIdx = false;
                for (int i = 0; i < codes.Count; i++)
                {
                    CodeInstruction instruction = codes[i];
                    if (instruction.opcode == OpCodes.Ldstr && "KNOCKDOWN: PILOT INJURED".Equals((string)instruction.operand, StringComparison.InvariantCultureIgnoreCase))
                    {
                        targetIdx = i;
                        foundIdx = true;
                        ModInit.modLog?.Info?.Write($"KNOCKDOWN: PILOT INJURED instruction at idx: {i}");
                    }
                }

                if (foundIdx)
                {
                    codes.RemoveRange(targetIdx - 4, 12);
                }
                return codes;
            }
        }
    }
}