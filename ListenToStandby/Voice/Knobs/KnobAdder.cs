using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ListenToStandby.voice;
using UnityEngine;
using VTOLAPI;

namespace ListenToStandby.Voice.Knobs
{

    class KnobAdder
    {
        public class KnobAdderBuilder
        {
            private bool _removeLabels;
            private Vector3 _offsetOld;
            private Vector3 _offsetNew;
            private string _commsVolumeMPPath;

            public KnobAdderBuilder()
            {
                this._removeLabels = false;
                this._offsetOld = Vector3.zero;
                this._offsetNew = Vector3.back * 50f;
            }

            public KnobAdderBuilder SetRemoveLabels(bool removeLabels)
            {
                this._removeLabels = removeLabels;
                return this;
            }

            public KnobAdderBuilder SetOffsetOld(Vector3 offsetOld)
            {
                this._offsetOld = offsetOld;
                return this;
            }

            public KnobAdderBuilder SetOffsetNew(Vector3 offsetNew)
            {
                this._offsetNew = offsetNew;
                return this;
            }

            public KnobAdderBuilder SetCommsVolumeMPPath(string commsVolumeMPPath)
            {
                this._commsVolumeMPPath = commsVolumeMPPath;
                return this;
            }

            public KnobAdder Build()
            {
                return new KnobAdder(this._removeLabels, this._offsetOld, this._offsetNew, this._commsVolumeMPPath);
            }
        }

        private readonly bool _removeLabels;
        private readonly Vector3 _offsetOld;
        private readonly Vector3 _offsetNew;
        private readonly string _commsVolumeMPPath;

        private KnobAdder(bool removeLabels, Vector3 offsetOld, Vector3 offsetNew, string commsVolumeMPPath)
        {
            this._removeLabels = removeLabels;
            this._offsetOld = offsetOld;
            this._offsetNew = offsetNew;
            this._commsVolumeMPPath = commsVolumeMPPath;
        }

        public void Run(GameObject planeRoot)
        {
            Logger.Log($"Adding standby volume to {planeRoot.name}");

            GameObject commsVolumeMP = planeRoot.transform.Find(this._commsVolumeMPPath)?.gameObject;
            if (commsVolumeMP == null)
            {
                Logger.LogError($"Couldn't find comms volume knob on {planeRoot.name}. Path={this._commsVolumeMPPath}");
                return;
            }

            GameObject commsPanel = commsVolumeMP.transform.parent?.gameObject;
            if ( commsPanel == null )
            {
                Logger.LogError($"CommsVolumeMP on {planeRoot.name} did not have parent. Path={this._commsVolumeMPPath}");
                return;
            }

            if (this._removeLabels)
            {
                // this should work (tm).
                GameObject.Destroy(commsVolumeMP.transform.Find("Label (1)")?.gameObject);
            }

            GameObject standbyCommsVolumeMP = GameObject.Instantiate(commsVolumeMP);

            if (!this._removeLabels)
            {
                VTText label = standbyCommsVolumeMP.GetComponentInChildren<VTText>();

                if (label == null)
                {
                    Logger.LogError("Couldn't find label on standbyCommsVolumeMP");
                    GameObject.Destroy(standbyCommsVolumeMP);
                    return;
                }

                label.text = "stby\nVOL";
            }

            VRInteractable interactable = standbyCommsVolumeMP.GetComponentInChildren<VRInteractable>();
            if (interactable == null)
            {
                Logger.LogError("Couldn't find VRInteractable on standbyCommsVolumeMP");
                GameObject.Destroy(standbyCommsVolumeMP);
                return;
            }
            interactable.interactableName = "Standby Comms Volume";

            VRTwistKnob twistKnob = standbyCommsVolumeMP.GetComponentInChildren<VRTwistKnob>();
            if (twistKnob == null)
            {
                Logger.LogError("Couldn't find twistKnob on standbyCommsVolumeMP");
                GameObject.Destroy(standbyCommsVolumeMP);
                return;
            }
            twistKnob.OnSetState = new FloatEvent();
            twistKnob.OnSetState.AddListener(f =>
            {
                OpForChangeVol.SetCommsVolumeOpforMP(f);
            });

            standbyCommsVolumeMP.name = "StandbyCommsVolumeMP";

            standbyCommsVolumeMP.transform.parent = commsPanel.transform;
            standbyCommsVolumeMP.transform.localPosition = commsVolumeMP.transform.localPosition;
            standbyCommsVolumeMP.transform.localScale = commsVolumeMP.transform.localScale;
            standbyCommsVolumeMP.transform.localRotation = commsVolumeMP.transform.localRotation;

            commsVolumeMP.transform.localPosition += this._offsetOld;
            standbyCommsVolumeMP.transform.localPosition += this._offsetNew;
        }
    }
}
