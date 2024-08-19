using System;
using System.IO;
using HarmonyLib;
using NAudio.Wave.SampleProviders;
using Steamworks;
using UnityEngine;
using UnityEngine.Audio;
using VTNetworking;
using VTOLVR.Multiplayer;

namespace ListenToStandby.Voice
{
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
                ModdedStandbyChannel.Instance.standbyChannel = 0;
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
        [HarmonyPatch("ReceiveVTNetVoiceData")]
        [HarmonyPrefix]
        // look, I apologise sincerely
        public static void PatchReceiveVoice(ulong ___customChannel, ulong incomingID, byte[] buffer, int offset, int count, ulong in_channel, ref byte[] ___voiceDownBuffer, ref MemoryStream ___voiceDownStream, ref MemoryStream ___voiceDecompressedStream, ref float[] ___inFloatBuffer, ref SampleChannel ___sampleProvider)
        {
            if (in_channel == 0L || in_channel == ___customChannel)
            {
                return;
            }

            if (in_channel != ModdedStandbyChannel.Instance.standbyChannel)
            {
                return;
            }

            // this literally just copies the current code for doing this, but plays it on standbySource instead.
            StandbyAudioSources.StandbyAudioSource standbySource;
            if (StandbyAudioSources.Instance.sources.TryGetValue(incomingID, out standbySource) && (VTNetworkVoice.mutes == null || !VTNetworkVoice.mutes.Contains(incomingID)))
            {
                Buffer.BlockCopy(buffer, offset, ___voiceDownBuffer, 0, count);
                lock (standbySource.inStreamLock)
                {
                    ___voiceDownStream.Position = 0L;
                    ___voiceDecompressedStream.Position = 0L;
                    int num = SteamUser.DecompressVoice(___voiceDownStream, count, ___voiceDecompressedStream) / 2;
                    ___voiceDecompressedStream.Position = 0L;
                    if (___inFloatBuffer.Length < num)
                    {
                        ___inFloatBuffer = new float[num];
                        Debug.Log(string.Format("VTNetworkVoice: new float buffer length: {0}", num));
                    }
                    ___sampleProvider.Read(___inFloatBuffer, 0, num);
                    for (int i = 0; i < num; i++)
                    {
                        standbySource.sampleQueue.Enqueue(___inFloatBuffer[i]);
                    }
                }
            }

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
            StandbyAudioSources.Instance.DestoryPlayer(player);
        }
    }

    class DontChangeOpforVolumePatch
    {
        [HarmonyPatch(typeof(CommRadioManager))]
        [HarmonyPatch("SetCommsVolumeMP")]
        [HarmonyPostfix]
        public static bool DisableChangeOpfor(float t, AudioMixerGroup ___mpAlliedMixerGroup)
        {
            float num = Mathf.Lerp(-30f, 8f, Mathf.Sqrt(t));
            ___mpAlliedMixerGroup.audioMixer.SetFloat("CommAttenuationAllied", num);

            return false;
        }
    }
}
