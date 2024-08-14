global using static ListenToStandby.Logger;
using ModLoader.Framework;
using ModLoader.Framework.Attributes;

using HarmonyLib;
using VTNetworking;
using ListenToStandby.voice;

namespace ListenToStandby
{
    [ItemId("xyz.031410.listen2standby")] // Harmony ID for your mod, make sure this is unique
    public class Main : VtolMod
    {
        private void Awake()
        {
            Log("Mod started");

            Harmony.CreateAndPatchAll(typeof(SetStandbyPatches));
            Harmony.CreateAndPatchAll(typeof(PlayStandbyPatches));
            Harmony.CreateAndPatchAll(typeof(AddStandbyPatches));
            Harmony.CreateAndPatchAll(typeof(AddKnobPatch));
        }

        public override void UnLoad() { }

    }


}