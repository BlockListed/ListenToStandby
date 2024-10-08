﻿using UnityEngine;

namespace ListenToStandby.voice
{
    class OpForChangeVol
    {
        public static void SetCommsVolumeOpforMP(float t)
        {
            float num = Mathf.Lerp(-30f, 8, Mathf.Sqrt(t));
            CommRadioManager.instance.opforMixerGroup.audioMixer.SetFloat("CommAttenuationOpfor", num);
        }
    }
}
