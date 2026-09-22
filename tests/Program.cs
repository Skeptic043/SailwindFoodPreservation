using System;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using SailwindFoodPreservation;

static void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}

static void Near(float actual, float expected, string message)
{
    Check(Math.Abs(actual - expected) < 0.00001f, $"{message}: expected {expected}, got {actual}");
}

Near(PreservationMath.ScaleSmoking(0.02f, 1f), 0.02f, "default smoking");
Near(PreservationMath.ScaleSmoking(0.02f, 5f), 0.1f, "faster smoking");
Near(PreservationMath.ScaleSmoking(0.02f, 0.1f), 0.002f, "slower smoking");
Near(PreservationMath.ScaleAmbientDrying(0.02f, 1f, 1f, 1f), 0.02f, "default drying");
Near(PreservationMath.ScaleAmbientDrying(0.02f, 2f, 3f, 0f), 0.04f, "unsalted drying");
Near(PreservationMath.ScaleAmbientDrying(0.02f, 2f, 3f, 0.5f), 0.08f, "partially salted drying");
Near(PreservationMath.ScaleAmbientDrying(0.02f, 2f, 3f, 1f), 0.12f, "fully salted drying");
Near(PreservationMath.ScaleAmbientDrying(-0.02f, 5f, 5f, 1f), -0.02f, "water drying loss");
Near(PreservationMath.ScaleAmbientDrying(0f, 5f, 5f, 1f), 0f, "suppressed drying");
Check(FoodClassifier.ClassifyWhole(true, 9f, 1f, true) == FoodCategory.Fish,
    "Fish catalog identity must win over vitamins");
Check(FoodClassifier.ClassifyWhole(false, 24f, 0f, true) == FoodCategory.Meat,
    "Raw protein food with no vitamins is meat");
Check(FoodClassifier.ClassifyWhole(false, 1f, 2f, false) == FoodCategory.Produce,
    "Vitamin food is produce");
Check(FoodClassifier.ClassifyWhole(false, 0f, 0f, true) == FoodCategory.Produce,
    "Raw protein-free produce is classified");
Check(FoodClassifier.ClassifyWhole(false, 0f, 0f, false) == FoodCategory.Unknown,
    "Ambiguous processed food stays unknown");

string gameDir = args.Length == 0 ? @"C:\Steam Games\steamapps\common\Sailwind" : args[0];
string gameAssembly = Path.Combine(gameDir, "Sailwind_Data", "Managed", "Assembly-CSharp.dll");
using var module = ModuleDefinition.ReadModule(gameAssembly);
var foodState = module.Types.Single(t => t.Name == "FoodState");
var smoked = foodState.Methods.Single(m => m.Name == "AddSmoked");
Check(smoked.Parameters.Count == 1 && smoked.Parameters[0].ParameterType.FullName == "System.Single",
    "FoodState.AddSmoked(float) changed");
var update = foodState.Methods.Single(m => m.Name == "Update" && m.Parameters.Count == 0);
var code = update.Body.Instructions;
int ambientAdds = 0;
for (int i = 3; i < code.Count; i++)
{
    if (code[i].OpCode != OpCodes.Stfld ||
        (code[i].Operand as FieldReference)?.Name != "dried" ||
        code[i - 1].OpCode != OpCodes.Add ||
        !IsLocalLoad(code[i - 2].OpCode) ||
        code[i - 3].OpCode != OpCodes.Ldfld ||
        (code[i - 3].Operand as FieldReference)?.Name != "dried") continue;
    ambientAdds++;
}
Check(ambientAdds == 1, $"Expected one ambient dried += local site, found {ambientAdds}");
Check(code.Any(i => (i.OpCode == OpCodes.Call || i.OpCode == OpCodes.Callvirt) &&
                    (i.Operand as MethodReference)?.Name == "GetCurrentHeat"),
    "Separate smoker heat path was not found");
Console.WriteLine("Preservation math and installed assembly hook checks passed.");

static bool IsLocalLoad(OpCode opcode)
{
    return opcode == OpCodes.Ldloc || opcode == OpCodes.Ldloc_S ||
           opcode == OpCodes.Ldloc_0 || opcode == OpCodes.Ldloc_1 ||
           opcode == OpCodes.Ldloc_2 || opcode == OpCodes.Ldloc_3;
}
