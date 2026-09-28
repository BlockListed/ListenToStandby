using System;
using System.Collections.Generic;

using Steamworks;
using UnityEngine;
using VTOLVR.Multiplayer;

namespace ListenToStandby.Voice
{
    static class ModdedStandbyChannel
    {
        public static ulong standbyChannel;
        public static float volume = 1.0f;

        static float[] staticSamples;
        static int samplePos;

        class UserState { public float lp1, lp2, hp, lfoPhase; }
        static readonly Dictionary<ulong, UserState> users = new Dictionary<ulong, UserState>();

        public static void ApplyDSP(float[] samples, int count, ulong id)
        {
            if (staticSamples == null)
            {
                if (!CommRadioManager.instance) return;
                var clip = CommRadioManager.instance.radioStatic;
                var staticSamplesCount = clip.samples;
                staticSamples = new float[staticSamplesCount];
                clip.GetData(staticSamples, 0);
            }

            if (!users.TryGetValue(id, out UserState st)) users[id] = st = new UserState();

            for (int i = 0;  i < count; i++)
            {
                float x = samples[i];

                st.lp1 += 0.12f * (x - st.lp1);
                st.lp2 += 0.30f * (st.lp1 - st.lp2);
                st.hp += 0.02f * (st.lp2 - st.hp);
                float y = st.lp2 - st.hp;

                st.lfoPhase += 0.00125f; if (st.lfoPhase >= 1f) st.lfoPhase -= 1f;
                y *= 1f + 0.22f * Mathf.Sin(st.lfoPhase * 2f * Mathf.PI);

                y = Mathf.Lerp(y, staticSamples[samplePos], 0.06f);
                samplePos = (samplePos + 1) % staticSamples.Length;

                samples[i] = y * volume;
            }
        }
    }
}
