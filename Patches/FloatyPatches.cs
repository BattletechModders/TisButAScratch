using BattleTech;
using Localize;
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

            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilGenerator)
            {
                List<CodeInstruction> codes = new List<CodeInstruction>(instructions);

                // Create a new label for our target point
                Label clearNeedsInjuryLabel = ilGenerator.DefineLabel();

                MethodInfo clearNeedsInjuryMI = AccessTools.DeclaredMethod(typeof(Pilot), "ClearNeedsInjury");

                int injuryStrIdx = 0, clearInjuryIdx = 0;
                for (int i = 0; i < codes.Count; i++)
                {
                    CodeInstruction instruction = codes[i];
                    if (instruction.opcode == OpCodes.Ldstr && "{0}: PILOT INJURED".Equals((string)instruction.operand, StringComparison.InvariantCultureIgnoreCase))
                    {
                        injuryStrIdx = i;
                        ModInit.modLog?.Info?.Write($"Found PILOT INJURED instruction at idx: {i}");
                    }
                    else if (instruction.opcode == OpCodes.Callvirt && (MethodInfo)instruction.operand == clearNeedsInjuryMI)
                    {
                        clearInjuryIdx = i;
                        ModInit.modLog?.Info?.Write($"Found Pilot.ClearNeedsInjury instruction at idx: {i}");
                    }
                }

                CodeInstruction cnjInstruction = codes[clearInjuryIdx - 1];
                cnjInstruction.labels.Add(clearNeedsInjuryLabel);

                codes.RemoveRange(injuryStrIdx, 15);
                codes.Insert(injuryStrIdx - 1, new CodeInstruction(OpCodes.Br_S, clearNeedsInjuryLabel));

                return codes;
            }
        }

        [HarmonyPatch(typeof(Mech), "CompleteKnockdown")]
        static class Mech_CompleteKnockdown
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> codes = new List<CodeInstruction>(instructions);

                int targetIdx = 0;
                for (int i = 0; i < codes.Count; i++)
                {
                    CodeInstruction instruction = codes[i];
                    if (instruction.opcode == OpCodes.Ldstr && "KNOCKDOWN: PILOT INJURED".Equals((string)instruction.operand, StringComparison.InvariantCultureIgnoreCase))
                    {
                        targetIdx = i;
                        ModInit.modLog?.Info?.Write($"KNOCKDOWN: PILOT INJURED instruction at idx: {i}");
                    }
                }
                codes.RemoveRange(targetIdx - 4, 12);

                return codes;
            }
        }
    }
}