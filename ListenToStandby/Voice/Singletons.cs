using System;
using System.Collections.Generic;

using Steamworks;
using UnityEngine;
using VTOLVR.Multiplayer;

namespace ListenToStandby.Voice
{
    class ModdedStandbyChannel
    {
        public ulong standbyChannel;

        private static ModdedStandbyChannel instance = null;

        ModdedStandbyChannel()
        {
            standbyChannel = 0;
        }

        public static ModdedStandbyChannel Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new ModdedStandbyChannel();
                }
                return instance;
            }
        }
    }

    class StandbyAudioSources
    {
        private static StandbyAudioSources instance;

        private StandbyAudioSources() { }

        public static StandbyAudioSources Instance
        {
            get {
                if (instance == null) { instance = new StandbyAudioSources(); } return instance;
            }
        }

        public Dictionary<SteamId, StandbyAudioSource> sources = new();

        public void CreateForPlayer(PlayerInfo playerInfo, Transform parent)
        {
            Logger.Log($"Creating standby audio source for {playerInfo}. Under {parent.gameObject.name}");
            if (playerInfo.steamUser.IsMe)
            {
                return;
            }
            GameObject go = new GameObject($"{playerInfo.pilotName} standby voice");
            AudioSource audio = go.AddComponent<AudioSource>();
            go.transform.parent = parent;

            try
            {
                sources.Add(playerInfo.steamUser.Id, new StandbyAudioSource(audio));
            }
            catch (ArgumentException)
            {
                DestoryPlayer(playerInfo);
                sources.Add(playerInfo.steamUser.Id, new StandbyAudioSource(audio));
            }
        }

        public void DestoryPlayer(PlayerInfo playerInfo)
        {
            Logger.Log($"Destroying standby audio source for {playerInfo}.");
            StandbyAudioSource source;
            if (this.sources.TryGetValue(playerInfo.steamUser.Id, out source))
            {
                source.DestroyObjects();
                this.sources.Remove(playerInfo.steamUser.Id);
            }
        }
        
        public class StandbyAudioSource
        {
            private readonly AudioSource source;

            private readonly AudioClip incomingStreamClip;

            public Queue<float> sampleQueue = new();

            public object inStreamLock = new();

            private readonly int minQueueCount = 1000;

            public StandbyAudioSource(AudioSource source)
            {
                this.incomingStreamClip = AudioClip.Create("Steam Standby Voice", (int)SteamUser.SampleRate * 10, 1, (int)SteamUser.SampleRate, true, new AudioClip.PCMReaderCallback(this.OnAudioRead));

                this.source = source;
                this.source.maxDistance = 100.0f;
                this.source.minDistance = 1.5f;
                this.source.outputAudioMixerGroup = CommRadioManager.instance.opforMixerGroup;
                this.source.clip = this.incomingStreamClip;
                this.source.velocityUpdateMode = AudioVelocityUpdateMode.Dynamic;
                this.source.loop = true;
                this.source.dopplerLevel = 0f;
                this.source.Play();
            }

            public void DestroyObjects()
            {
                UnityEngine.Object.Destroy(this.incomingStreamClip);
                GameObject.Destroy(this.source.gameObject);;
            }

            private bool reading;

            private void OnAudioRead(float[] data)
            {
                lock(this.inStreamLock)
                {
                    for (int i = 0; i < data.Length; i++)
                    {
                        if ((this.reading && this.sampleQueue.Count > 0) || (!this.reading && this.sampleQueue.Count > this.minQueueCount))
                        {
                            data[i] = this.sampleQueue.Dequeue() * 2f;
                            this.reading = true;
                        } else
                        {
                            data[i] = 0f;
                            this.reading = false;
                        }
                    }
                }
            }
        }
    }
}
