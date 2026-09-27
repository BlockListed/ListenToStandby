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
            ModdedStandbyChannel.Instance.standbyChannel = 0;
        }
    }

    class SetStandbyPatches
    {

        [HarmonyPatch(typeof(ChannelRadioSystem))]
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        public static void PatchStart(ChannelRadioSystem __instance)
        {
            ModdedStandbyChannel.Instance.standbyChannel = (ulong)__instance.standbyChannel;
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

    class PlayStandbyPatches
    {
        [HarmonyPatch(typeof(VTNetworkVoice))]
        [HarmonyPatch("ReceiveVTNetVoiceDataOpus")]
        [HarmonyPrefix]
        public static void PatchReceiveVoice(ref ulong in_channel, ulong ___customChannel)
        {
            if (in_channel == ModdedStandbyChannel.Instance.standbyChannel)
            {
                in_channel = ___customChannel;
            }
            return;
        }

        [HarmonyPatch(typeof(VTNetworkVoice))]
        [HarmonyPatch("ReceiveVTNetVoiceDataOpus")]
        [HarmonyPostfix]
        public static void Amongus(ulong in_channel)
        {
            return;
        }
    }

    class AddStandbyPatches
    {
        [HarmonyPatch(typeof(CockpitTeamRadioManager))]
        [HarmonyPatch("SetupVoiceSource")]
        [HarmonyPostfix]
        public static void AddStandbyVoice(PlayerInfo player, Transform ___opforSourcePosition)
        {
            StandbyAudioSources.Instance.CreateForPlayer(player, ___opforSourcePosition);
        }

        [HarmonyPatch(typeof(CockpitTeamRadioManager))]
        [HarmonyPatch("RemovePlayer")]
        [HarmonyPostfix]
        public static void RemovePlayer(PlayerInfo player)
        {
            if (player == null)
            {
                return;
            }
            StandbyAudioSources.Instance.DestroyPlayer(player);
        }
    }
}
