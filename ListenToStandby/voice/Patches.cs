// Yes, this should probably be seperate files.
// Yes, I know DRY exists, but I just want anyone reading KnobPatches to suffer.
// No, I don't apologise.
//
// TODO:
// - Make the knob patching not a horrible mess that not even satan will touch.

using System;
using System.IO;
using HarmonyLib;
using NAudio.Wave.SampleProviders;
using Steamworks;
using UnityEngine;
using UnityEngine.Audio;
using VTNetworking;
using VTOLVR.Multiplayer;
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
            if (in_channel == 0L || in_channel == ___customChannel)
            {
                return;
            }

            if (in_channel != ModdedStandbyChannel.Instance.standbyChannel)
            {
                return;
            }

            Logger.Log($"Received voice data for Standby from {incomingID}");

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

            if (VTAPI.GetVehicleEnum(__instance.gameObject) != VTOLVehicles.F45A)
            {
                return;
            }

            Logger.Log("AddKnobPatch.F45AddKnob");

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

        // DRY isn't real
        [HarmonyPatch(typeof(Actor))]
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        public static void EF24AddKnob(Actor __instance)
        {
            if (!__instance.isPlayer)
            {
                return;
            }

            if (VTAPI.GetVehicleEnum(__instance.gameObject) != VTOLVehicles.EF24G)
            {
                return;
            }

            Logger.Log("AddKnobPatch.EF24AddKnob");

            var localPlane = __instance.gameObject.transform.Find("PassengerOnlyObjs");
            if (localPlane == null || !localPlane.gameObject.activeInHierarchy)
            {
                Logger.Log("PassengerOnlyObjs gameobject either doesn't exist or isn't active");
                return;
            }

            var commsPanel = localPlane.Find("FrontCockpit/HUDDashTransform/CommsPanel_Front");
            if (commsPanel == null)
            {
                Logger.Log("Couldn't find CommsPanel");
                return;
            }

            var commsVolumeMP = commsPanel.Find("CommsVolumeMP");
            if (commsVolumeMP == null)
            {
                Logger.Log("Couldn't find CommsVolumeMP");
                return;
            }

            var label = commsVolumeMP.Find("Label (1)");
            if (label == null)
            {
                Logger.Log("Couldn't find Label (1)");
                return;
            }

            GameObject.Destroy(label.gameObject);

            var newCommsVolume = GameObject.Instantiate(commsVolumeMP.gameObject);
            newCommsVolume.transform.parent = commsPanel;
            newCommsVolume.transform.localPosition = commsVolumeMP.transform.localPosition + (Vector3.left * 50f);
            newCommsVolume.transform.localRotation = commsVolumeMP.transform.localRotation;
            newCommsVolume.transform.localScale = commsVolumeMP.transform.localScale;
            newCommsVolume.name = "StandbyCommsVolumeMP";

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

        // DRY isn't real
        [HarmonyPatch(typeof(Actor))]
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        public static void T55FrontAddKnob(Actor __instance)
        {
            if (!__instance.isPlayer)
            {
                return;
            }

            if (VTAPI.GetVehicleEnum(__instance.gameObject) != VTOLVehicles.T55)
            {
                return;
            }

            Logger.Log("AddKnobPatch.T55FrontAddKnob");

            var localPlane = __instance.gameObject.transform.Find("PassengerOnlyObjs");
            if (localPlane == null || !localPlane.gameObject.activeInHierarchy)
            {
                Logger.Log("PassengerOnlyObjs gameobject either doesn't exist or isn't active");
                return;
            }

            var commsPanel = localPlane.Find("DashCanvasFront/RightDash/CommsPanel_Front");
            if (commsPanel == null)
            {
                Logger.Log("Couldn't find CommsPanel");
                return;
            }

            var commsVolumeMP = commsPanel.Find("CommsVolumeMP");
            if (commsVolumeMP == null)
            {
                Logger.Log("Couldn't find CommsVolumeMP");
                return;
            }

            var label = commsVolumeMP.Find("Label (1)");
            if (label == null)
            {
                Logger.Log("Couldn't find Label (1)");
                return;
            }
            GameObject.Destroy(label.gameObject);

            var newCommsVolume = GameObject.Instantiate(commsVolumeMP.gameObject);
            newCommsVolume.transform.parent = commsPanel;
            newCommsVolume.transform.localPosition = commsVolumeMP.transform.localPosition + (Vector3.right * 35f);
            commsVolumeMP.transform.localPosition = commsVolumeMP.transform.localPosition + (Vector3.left * 10f);
            newCommsVolume.transform.localRotation = commsVolumeMP.transform.localRotation;
            newCommsVolume.transform.localScale = commsVolumeMP.transform.localScale;
            newCommsVolume.name = "StandbyCommsVolumeMP";

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

        // DRY isn't real
        [HarmonyPatch(typeof(Actor))]
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        public static void T55RearAddKnob(Actor __instance)
        {
            if (!__instance.isPlayer)
            {
                return;
            }

            if (VTAPI.GetVehicleEnum(__instance.gameObject) != VTOLVehicles.T55)
            {
                return;
            }

            Logger.Log("AddKnobPatch.T55RearAddKnob");

            var localPlane = __instance.gameObject.transform.Find("PassengerOnlyObjs");
            if (localPlane == null || !localPlane.gameObject.activeInHierarchy)
            {
                Logger.Log("PassengerOnlyObjs gameobject either doesn't exist or isn't active");
                return;
            }

            var commsPanel = localPlane.Find("DashCanvasRear/RightDash/CommsPanel_rear");
            if (commsPanel == null)
            {
                Logger.Log("Couldn't find CommsPanel");
                return;
            }

            var commsVolumeMP = commsPanel.Find("CommsVolumeMP");
            if (commsVolumeMP == null)
            {
                Logger.Log("Couldn't find CommsVolumeMP");
                return;
            }

            var label = commsVolumeMP.Find("Label (1)");
            if (label == null)
            {
                Logger.Log("Couldn't find Label (1)");
                return;
            }
            GameObject.Destroy(label.gameObject);

            var newCommsVolume = GameObject.Instantiate(commsVolumeMP.gameObject);
            newCommsVolume.transform.parent = commsPanel;
            newCommsVolume.transform.localPosition = commsVolumeMP.transform.localPosition + (Vector3.right * 35f);
            commsVolumeMP.transform.localPosition = commsVolumeMP.transform.localPosition + (Vector3.left * 10f);
            newCommsVolume.transform.localRotation = commsVolumeMP.transform.localRotation;
            newCommsVolume.transform.localScale = commsVolumeMP.transform.localScale;
            newCommsVolume.name = "StandbyCommsVolumeMP";

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

        // DRY isn't real
        [HarmonyPatch(typeof(Actor))]
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        public static void AV42AddKnob(Actor __instance)
        {
            if (!__instance.isPlayer)
            {
                return;
            }

            if (VTAPI.GetVehicleEnum(__instance.gameObject) != VTOLVehicles.AV42C)
            {
                return;
            }

            Logger.Log("AddKnobPatch.AV42AddKnob");

            var localPlane = __instance.gameObject.transform.Find("Local");
            if (localPlane == null || !localPlane.gameObject.activeInHierarchy)
            {
                Logger.Log("Local gameobject either doesn't exist or isn't active");
                return;
            }

            var commsPanel = localPlane.Find("DashCanvas/RightDash/CommsPanel");
            if (commsPanel == null)
            {
                Logger.Log("Couldn't find CommsPanel");
                return;
            }

            var commsVolumeMP = commsPanel.Find("CommsVolumeMP");
            if (commsVolumeMP == null)
            {
                Logger.Log("Couldn't find CommsVolumeMP");
                return;
            }

            var label = commsVolumeMP.Find("Label (1)");
            if (label == null)
            {
                Logger.Log("Couldn't find Label (1)");
                return;
            }
            GameObject.Destroy(label.gameObject);

            var newCommsVolume = GameObject.Instantiate(commsVolumeMP.gameObject);
            newCommsVolume.transform.parent = commsPanel;
            newCommsVolume.transform.localPosition = commsVolumeMP.transform.localPosition + (Vector3.right * 40f);
            newCommsVolume.transform.localRotation = commsVolumeMP.transform.localRotation;
            newCommsVolume.transform.localScale = commsVolumeMP.transform.localScale;
            newCommsVolume.name = "StandbyCommsVolumeMP";

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

        // DRY isn't real
        [HarmonyPatch(typeof(Actor))]
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        public static void F26AddKnob(Actor __instance)
        {
            if (!__instance.isPlayer)
            {
                return;
            }

            if (VTAPI.GetVehicleEnum(__instance.gameObject) != VTOLVehicles.FA26B)
            {
                return;
            }

            Logger.Log("AddKnobPatch.F26AddKnob");

            var localPlane = __instance.gameObject.transform.Find("Local");
            if (localPlane == null || !localPlane.gameObject.activeInHierarchy)
            {
                Logger.Log("Local gameobject either doesn't exist or isn't active");
                return;
            }

            var commsPanel = localPlane.Find("DashCanvas/RightDash/CommsPanel/CommsVolume");
            if (commsPanel == null)
            {
                Logger.Log("Couldn't find CommsPanel");
                return;
            }

            var commsVolumeMP = commsPanel.Find("CommsVolumeMP");
            if (commsVolumeMP == null)
            {
                Logger.Log("Couldn't find CommsVolumeMP");
                return;
            }

            var label = commsVolumeMP.Find("Label (1)");
            if (label == null)
            {
                Logger.Log("Couldn't find Label (1)");
                return;
            }
            GameObject.Destroy(label.gameObject);

            var newCommsVolume = GameObject.Instantiate(commsVolumeMP.gameObject);
            newCommsVolume.transform.parent = commsPanel;
            newCommsVolume.transform.localPosition = commsVolumeMP.transform.localPosition + (Vector3.right * 30f);
            commsVolumeMP.transform.localPosition = commsVolumeMP.transform.localPosition + (Vector3.left * 10f);
            newCommsVolume.transform.localRotation = commsVolumeMP.transform.localRotation;
            newCommsVolume.transform.localScale = commsVolumeMP.transform.localScale;
            newCommsVolume.name = "StandbyCommsVolumeMP";

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
