using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;
using UnityEngine;
using VTOLAPI;

namespace ListenToStandby.Voice.Knobs
{
    class AddKnobPatch
    {
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
                    //Logger.LogWarn($"Not adding standby volume knob to {__instance.gameObject.name}");
                    break;
            };

        }
        private static void AddF26(GameObject planeRoot)
        {
            KnobAdder adder = new KnobAdder.KnobAdderBuilder() .SetRemoveLabels(true)
                .SetCommsVolumeMPPath("Local/DashCanvas/RightDash/CommsPanel/CommsVolume/CommsVolumeMP")
                .SetOffsetNew(Vector3.right * 30f)
                .SetOffsetOld(Vector3.left * 10f) 
                .Build();

            adder.Run(planeRoot);
        }

        private static void AddF45(GameObject planeRoot)
        { 
            KnobAdder adder = new KnobAdder.KnobAdderBuilder()
                .SetRemoveLabels(false)
                .SetCommsVolumeMPPath("Local/NonUIDash/CommsPanel/CommsVolumeMP")
                .SetOffsetNew(Vector3.back * 50f)
                .Build();

            adder.Run(planeRoot);
        }

        private static void AddEF24G(GameObject planeRoot)
        {
            KnobAdder frontAdder = new KnobAdder.KnobAdderBuilder()
                .SetRemoveLabels(true)
                .SetCommsVolumeMPPath("PassengerOnlyObjs/FrontCockpit/HUDDashTransform/CommsPanel_Front/CommsVolumeMP")
                .SetOffsetNew(Vector3.left * 50f)
                .Build();

            KnobAdder rearAdder = new KnobAdder.KnobAdderBuilder()
                .SetRemoveLabels(true)
                .SetCommsVolumeMPPath("PassengerOnlyObjs/RearCockpit/CommsPanel_Rear/CommsVolumeMP")
                .SetOffsetOld(Vector3.left * 10f)
                .SetOffsetNew(Vector3.right * 30f)
                .Build();

            frontAdder.Run(planeRoot);
            rearAdder.Run(planeRoot);
        }

        private static void AddT55(GameObject planeRoot)
        {
            KnobAdder frontAdder = new KnobAdder.KnobAdderBuilder()
                .SetRemoveLabels(true)
                .SetCommsVolumeMPPath("PassengerOnlyObjs/DashCanvasFront/RightDash/CommsPanel_Front/CommsVolumeMP")
                .SetOffsetNew(Vector3.right * 35f)
                .SetOffsetOld(Vector3.left * 10f)
                .Build();

            KnobAdder rearAdder = new KnobAdder.KnobAdderBuilder()
                .SetRemoveLabels(true)
                .SetCommsVolumeMPPath("PassengerOnlyObjs/DashCanvasRear/RightDash/CommsPanel_rear/CommsVolumeMP")
                .SetOffsetNew(Vector3.right * 35f)
                .SetOffsetOld(Vector3.left * 10f)
                .Build();

            frontAdder.Run(planeRoot);
            rearAdder.Run(planeRoot);
        }

        private static void AddAV42(GameObject planeRoot)
        {
            KnobAdder adder = new KnobAdder.KnobAdderBuilder()
                .SetRemoveLabels(true)
                .SetCommsVolumeMPPath("Local/DashCanvas/RightDash/CommsPanel/CommsVolumeMP")
                .SetOffsetNew(Vector3.right * 40f)
                .Build();

            adder.Run(planeRoot);
        }
    }
}
