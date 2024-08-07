global using static ListenToStandby.Logger;
using System.Reflection;
using ModLoader.Framework;
using ModLoader.Framework.Attributes;

using HarmonyLib;
using VTOLVR.Multiplayer;
using System;
using VTNetworking;

namespace ListenToStandby
{
    [ItemId("xyz.031410.listen2standby")] // Harmony ID for your mod, make sure this is unique
    public class Main : VtolMod
    {
        private void Awake()
        {
            Log("Mod started");

            Harmony.CreateAndPatchAll(typeof(ChannelRadioPatches));
            Harmony.CreateAndPatchAll(typeof(VTNetworkVoicePatches));
        }

        public override void UnLoad() { }

    }

    class ChannelRadioPatches
    {

        [HarmonyPatch(typeof(ChannelRadioSystem))]
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        public static void PatchStart(ChannelRadioSystem __instance)
        {
            ModdedStandbyChannel.Instance.standbyChannel = (ulong)__instance.standbyChannel;
        }

        [HarmonyPatch(typeof(ChannelRadioSystem))]
        [HarmonyPatch("SwapButton")]
        [HarmonyPostfix]
        public static void PatchSwapChannels(ChannelRadioSystem __instance)
        {
            ModdedStandbyChannel.Instance.standbyChannel = (ulong)__instance.standbyChannel;
        }

        [HarmonyPatch(typeof(ChannelRadioSystem))]
        [HarmonyPatch("SetStandbyRadioChannel")]
        [HarmonyPostfix]
        public static void PatchSetStandby(ChannelRadioSystem __instance)
        {
            ModdedStandbyChannel.Instance.standbyChannel = (ulong)__instance.standbyChannel;
        }

        [HarmonyPatch(typeof(ChannelRadioSystem))]
        [HarmonyPatch("RemoteSetFreqs")]
        [HarmonyPostfix]
        public static void PatchRemoteSetFreqs(ChannelRadioSystem __instance)
        {
            ModdedStandbyChannel.Instance.standbyChannel = (ulong)__instance.standbyChannel;
        }
    }

    class VTNetworkVoicePatches
    {
        [HarmonyPatch(typeof(VTNetworkVoice))]
        [HarmonyPatch("ReceiveVTNetVoiceData")]
        [HarmonyPrefix]
        public static void PatchReceiveVoice(ref ulong in_channel, ulong ___customChannel)
        {
            if (in_channel > 0UL && in_channel == ModdedStandbyChannel.Instance.standbyChannel)
            {
                in_channel = ___customChannel;
            }
        }
    }

    class ModdedStandbyChannel
    {
        public ulong standbyChannel;

        private static ModdedStandbyChannel instance = null;

        ModdedStandbyChannel()
        {
            standbyChannel = 0;
        }

        public static ModdedStandbyChannel Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new ModdedStandbyChannel();
                }
                return instance;
            }
        }
    }
}