using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;
using UnityEngine;
using VTOLAPI;
using VTOLVR.Multiplayer;

namespace ListenToStandby.Voice.Knobs
{
    class AddKnobPatch
    {
        #region plane_knobs
        [HarmonyPatch(typeof(Actor))]
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        public static void Addknobs(Actor __instance)
        {
            // Yes we are running this check on every fucking actor.
            // Should be acceptable???? for performance.
            switch (VTAPI.GetVehicleEnum(__instance.gameObject))
            {
                case VTOLVehicles.FA26B:
                    AddF26(__instance.gameObject);
                    break;
                case VTOLVehicles.F45A:
                    AddF45(__instance.gameObject);
                    break;
                case VTOLVehicles.EF24G:
                    AddEF24G(__instance.gameObject);
                    break;
                case VTOLVehicles.T55:
                    AddT55(__instance.gameObject);
                    break;
                case VTOLVehicles.AV42C:
                    AddAV42(__instance.gameObject);
                    break;
                default:
                    // This is removed, because now we'd be spamming the logs.
                    //Logger.LogWarn($"Not adding standby volume knob to {__instance.gameObject.name}");
                    break;
            };

        }
        private static void AddF26(GameObject planeRoot)
        {
            KnobAdder adder = new KnobAdder.KnobAdderBuilder() .SetRemoveLabels(true)
                .SetCommsVolumeMPPath("Local/DashCanvas/RightDash/CommsPanel/CommsVolume/CommsVolumeMP")
                .SetOffsetTeam(Vector3.right * 30f)
                .SetOffsetStandby(Vector3.left * 10f)
                .Build();

            adder.Run(planeRoot);
        }

        private static void AddF45(GameObject planeRoot)
        { 
            KnobAdder adder = new KnobAdder.KnobAdderBuilder()
                .SetRemoveLabels(false)
                .SetCommsVolumeMPPath("Local/NonUIDash/CommsPanel/CommsVolumeMP")
                .SetOffsetStandby(Vector3.back * 50f)
                .Build();

            adder.Run(planeRoot);
        }

        private static void AddEF24G(GameObject planeRoot)
        {
            KnobAdder frontAdder = new KnobAdder.KnobAdderBuilder()
                .SetRemoveLabels(true)
                .SetCommsVolumeMPPath("PassengerOnlyObjs/FrontCockpit/HUDDashTransform/CommsPanel_Front/CommsVolumeMP")
                .SetOffsetStandby(Vector3.left * 50f)
                .Build();

            KnobAdder rearAdder = new KnobAdder.KnobAdderBuilder()
                .SetRemoveLabels(true)
                .SetCommsVolumeMPPath("PassengerOnlyObjs/RearCockpit/CommsPanel_Rear/CommsVolumeMP")
                .SetOffsetStandby(Vector3.left * 10f)
                .SetOffsetTeam(Vector3.right * 30f)
                .Build();

            frontAdder.Run(planeRoot);
            rearAdder.Run(planeRoot);
        }

        private static void AddT55(GameObject planeRoot)
        {
            KnobAdder frontAdder = new KnobAdder.KnobAdderBuilder()
                .SetRemoveLabels(true)
                .SetCommsVolumeMPPath("PassengerOnlyObjs/DashCanvasFront/RightDash/CommsPanel_Front/CommsVolumeMP")
                .SetOffsetTeam(Vector3.right * 35f)
                .SetOffsetStandby(Vector3.left * 10f)
                .Build();

            KnobAdder rearAdder = new KnobAdder.KnobAdderBuilder()
                .SetRemoveLabels(true)
                .SetCommsVolumeMPPath("PassengerOnlyObjs/DashCanvasRear/RightDash/CommsPanel_rear/CommsVolumeMP")
                .SetOffsetTeam(Vector3.right * 35f)
                .SetOffsetStandby(Vector3.left * 10f)
                .Build();

            frontAdder.Run(planeRoot);
            rearAdder.Run(planeRoot);
        }

        private static void AddAV42(GameObject planeRoot)
        {
            KnobAdder adder = new KnobAdder.KnobAdderBuilder()
                .SetRemoveLabels(true)
                .SetCommsVolumeMPPath("Local/DashCanvas/RightDash/CommsPanel/CommsVolumeMP")
                .SetOffsetTeam(Vector3.right * 40f)
                .Build();

            adder.Run(planeRoot);
        }
        #endregion

        [HarmonyPatch(typeof(CockpitTeamRadioManager))]
        [HarmonyPatch("OnEnable")]
        [HarmonyPrefix]
        public static void AddAirbossKnobs(CockpitTeamRadioManager __instance)
        {
            GameObject parent = __instance.transform.parent.gameObject;

            if (parent.name != "AirbossObjs")
            {
                return;
            }

            if (parent.transform.Find("spectatorRadio/ui/StandbyCommsVolumeMP") != null)
            {
                Logger.Log("knob already exists on airboss, exiting");
                return;
            }

            KnobAdder adder = new KnobAdder.KnobAdderBuilder()
                .SetCommsVolumeMPPath("spectatorRadio/ui/VolumeKnob")
                .SetRemoveLabels(true)
                .SetOffsetStandby(Vector3.down * 10f)
                .SetOffsetTeam(Vector3.up * 15f)
                .Build();

            adder.Run(parent);
        }
    }
}
