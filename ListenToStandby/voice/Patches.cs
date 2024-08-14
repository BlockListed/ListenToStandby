using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using HarmonyLib;
using NAudio.Wave.SampleProviders;
using Steamworks;
using VTNetworking;
using VTOLVR.Multiplayer;
using UnityEngine.Audio;
using VTOLAPI;

namespace ListenToStandby.voice
{
    class SetStandbyPatches
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

    class PlayStandbyPatches
    {
        [HarmonyPatch(typeof(VTNetworkVoice))]
        [HarmonyPatch("ReceiveVTNetVoiceData")]
        [HarmonyPrefix]
        // look, I apologise sincerely
        public static void PatchReceiveVoice(ulong ___customChannel, ulong incomingID, byte[] buffer, int offset, int count, ulong in_channel, ref byte[] ___voiceDownBuffer, ref MemoryStream ___voiceDownStream, ref MemoryStream ___voiceDecompressedStream, ref float[] ___inFloatBuffer, ref SampleChannel ___sampleProvider)
        {
            if (in_channel > 0UL && in_channel == ___customChannel)
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
                    int num = SteamUser.DecompressVoice(___voiceDownStream, count, ___voiceDecompressedStream);
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

    class AddKnobPatch
    {
        // ignore the fact that I'm basically using go error handling
        // I hate exceptions.
        // Anyone who disagrees is a fed.
        [HarmonyPatch(typeof(Actor))]
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        public static void F45AddKnob(Actor __instance)
        {
            if (!__instance.isPlayer)
            {
                return;
            }

            Logger.Log($"Player spawned in {__instance.gameObject.name}");

            if (!__instance.gameObject.name.StartsWith("SEVTF(Clone"))
            {
                return;
            }

            Logger.Log("Adding knob to f45");

            var localPlane = __instance.gameObject.transform.Find("Local");
            if (localPlane == null || !localPlane.gameObject.activeInHierarchy)
            {
                Logger.Log("Local gameobject either doesn't exist or isn't active");
                return;
            }

            var commsPanel = localPlane.Find("NonUIDash/CommsPanel");
            if (commsPanel == null)
            {
                Logger.Log("Couldn't find CommsPanel");
                return;
            }

            var commsVolumeMP = localPlane.Find("NonUIDash/CommsPanel/CommsVolumeMP");
            if (commsVolumeMP == null)
            {
                Logger.Log("Couldn't find CommsVolumeMP");
                return;
            }

            var newCommsVolume = GameObject.Instantiate(commsVolumeMP.gameObject);
            newCommsVolume.transform.parent = commsPanel;
            newCommsVolume.transform.localPosition = commsVolumeMP.transform.localPosition + (Vector3.back * 50f);
            newCommsVolume.transform.localRotation = commsVolumeMP.transform.localRotation;
            newCommsVolume.transform.localScale = commsVolumeMP.transform.localScale;
            newCommsVolume.name = "StandbyCommsVolumeMP";

            var label = newCommsVolume.GetComponentInChildren<VTText>();
            if (label == null)
            {
                Logger.Log("Couldn't find label of knob");
                GameObject.Destroy(newCommsVolume);
                return;
            }
            label.text = "stby\nVOL";

            var vr_inter = newCommsVolume.GetComponentInChildren<VRInteractable>();
            if (vr_inter == null)
            {
                Logger.Log("Couldn't find vr interactable");
                GameObject.Destroy(newCommsVolume);
                return; 
            }
            vr_inter.interactableName = "Standby Radio Volume";

            var vr_twist = newCommsVolume.GetComponentInChildren<VRTwistKnob>();
            if (vr_twist == null)
            {
                Logger.Log("Couldn't find vr twisty boi");
                GameObject.Destroy(newCommsVolume);
                return;
            }
            vr_twist.OnSetState = new FloatEvent();
            vr_twist.OnSetState.AddListener(s => {
                Logger.Log($"Updating Opfor volume to {s}.");
                OpForChangeVol.SetCommsVolumeOpforMP(s);
            });

            Logger.Log("Completed amogus");
        }
    }
}
