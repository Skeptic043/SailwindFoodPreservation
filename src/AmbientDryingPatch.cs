using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace SailwindFoodPreservation
{
    internal static class AmbientDryingPatch
    {
        internal static bool Matched { get; private set; }

        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var code = new List<CodeInstruction>(instructions);
            FieldInfo dried = AccessTools.Field(typeof(FoodState), nameof(FoodState.dried));
            MethodInfo scale = AccessTools.Method(typeof(Plugin), nameof(Plugin.ScaleAmbientDrying));
            int match = -1;

            // Only rewrite dried += the local ambient increment. The later
            // smoker-heat addition to dried has a method call, not a local load.
            for (int i = 3; i < code.Count; i++)
            {
                if (code[i].opcode != OpCodes.Stfld || !Equals(code[i].operand, dried) ||
                    code[i - 1].opcode != OpCodes.Add || !IsLocalLoad(code[i - 2].opcode) ||
                    code[i - 3].opcode != OpCodes.Ldfld || !Equals(code[i - 3].operand, dried))
                    continue;
                if (match != -1)
                {
                    Matched = false;
                    return code;
                }
                match = i - 2;
            }

            if (match < 0 || scale == null)
            {
                Matched = false;
                return code;
            }

            code.Insert(match, new CodeInstruction(OpCodes.Ldarg_0));
            code.Insert(match + 2, new CodeInstruction(OpCodes.Call, scale));
            Matched = true;
            return code;
        }

        private static bool IsLocalLoad(OpCode opcode)
        {
            return opcode == OpCodes.Ldloc || opcode == OpCodes.Ldloc_S ||
                   opcode == OpCodes.Ldloc_0 || opcode == OpCodes.Ldloc_1 ||
                   opcode == OpCodes.Ldloc_2 || opcode == OpCodes.Ldloc_3;
        }
    }
}
