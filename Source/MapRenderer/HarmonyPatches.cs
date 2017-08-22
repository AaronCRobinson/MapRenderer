using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using RimWorld;
using Harmony;

namespace MapRenderer
{
    static class Helper
    {
        private static MethodInfo MI_CloseMainTab = AccessTools.Method(typeof(MainMenuDrawer), "CloseMainTab");
       
        public static void AddRenderOption(List<ListableOption> optList1)
        {
            if (Current.ProgramState == ProgramState.Playing)
            {
                ListableOption item = new ListableOption("RenderMap".Translate(), delegate
                {
                    MI_CloseMainTab.Invoke(null, new object[] { });
                    Find.WindowStack.Add(new Dialog_RenderMap());
                }, null);
                optList1.Add(item);
            }
        }
    }

    [StaticConstructorOnStartup]
    class HarmonyPatches
    {
        static HarmonyPatches()
        {
#if DEBUG
            HarmonyInstance.DEBUG = true;
#endif
            HarmonyInstance harmony = HarmonyInstance.Create("rimworld.whyisthat.maprenderer.main");

            harmony.Patch(AccessTools.Method(typeof(MainMenuDrawer), nameof(MainMenuDrawer.DoMainMenuControls)), null, null, new HarmonyMethod(typeof(HarmonyPatches), nameof(Transpiler)));
        }

        private static MethodInfo MI = AccessTools.Method(typeof(Helper), nameof(Helper.AddRenderOption));
        

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            foreach(CodeInstruction instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldarg_1)
                {
                    yield return new CodeInstruction(OpCodes.Ldloc_2); //optList1
                    yield return new CodeInstruction(OpCodes.Call, MI);
                    yield return instruction;
                }
                else
                    yield return instruction;
            }
        }
    }
}
