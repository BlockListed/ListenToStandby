using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Audio;
using VTNetworking;
using VTOLVR.Multiplayer;

namespace ListenToStandby.Voice
{
    class DisableStandby : MonoBehaviour
    {
        public void OnEnable()
        {
            ModdedStandbyChannel.standbyChannel = 0;
        }
    }

    class SetStandbyPatches
    {

        [HarmonyPatch(typeof(ChannelRadioSystem))]
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        public static void PatchStart(ChannelRadioSystem __instance)
        {
            ModdedStandbyChannel.standbyChannel = (ulong)__instance.standbyChannel;
            if (__instance.gameObject.name == "LSOTeamRadio")
            {
                GameObject disableStandby = new GameObject();
                DisableStandby dis = disableStandby.AddComponent<DisableStandby>();
                disableStandby.transform.parent = __instance.transform;

                dis.OnEnable();
            }
        }

        [HarmonyPatch(typeof(ChannelRadioSystem))]
        [HarmonyPatch("SwapButton")]
        [HarmonyPostfix]
        public static void PatchSwapChannels(ChannelRadioSystem __instance)
        {
            ModdedStandbyChannel.standbyChannel = (ulong)__instance.standbyChannel;
        }

        [HarmonyPatch(typeof(ChannelRadioSystem))]
        [HarmonyPatch("SetStandbyRadioChannel")]
        [HarmonyPostfix]
        public static void PatchSetStandby(ChannelRadioSystem __instance)
        {
            ModdedStandbyChannel.standbyChannel = (ulong)__instance.standbyChannel;
        }

        [HarmonyPatch(typeof(ChannelRadioSystem))]
        [HarmonyPatch("RemoteSetFreqs")]
        [HarmonyPostfix]
        public static void PatchRemoteSetFreqs(ChannelRadioSystem __instance)
        {
            ModdedStandbyChannel.standbyChannel = (ulong)__instance.standbyChannel;
        }
    }

    class PlayStandbyPatches
    {
        static bool standbyActive;

        [HarmonyPatch(typeof(VTNetworkVoice))]
        [HarmonyPatch("ReceiveVTNetVoiceDataOpus")]
        [HarmonyPrefix]
        public static void PatchReceiveVoice(ref ulong in_channel, ulong ___customChannel)
        {
            standbyActive = false;
            if (in_channel == 0 || in_channel == ___customChannel) return;
            if (in_channel != ModdedStandbyChannel.standbyChannel) return;
            
            in_channel = ___customChannel;
            standbyActive = true;

            return;
        }

        [HarmonyPatch(typeof(VTNetworkVoice))]
        [HarmonyPatch("SendSamplesToVoice")]
        [HarmonyPrefix]
        public static void PatchSendSamples(float[] ___inFloatBuffer, ulong incomingID, int sampleCount)
        {
            if (!standbyActive || sampleCount <= 0) return;
            ModdedStandbyChannel.ApplyDSP(___inFloatBuffer, sampleCount, incomingID);
        }
    }
}
