using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using OC2Haptic.Core;
using UnityEngine;
using UnityEngine.Audio;

namespace OC2Haptic.Audio
{
    internal sealed class UnityRewardAudioBackend : IRewardAudioBackend, IDisposable
    {
        private readonly Action<string> log;
        private readonly Func<int> maxDuration;
        private readonly Func<bool> manualPreview;
        private readonly Func<string> selectedPack;
        private readonly int mainThread=Thread.CurrentThread.ManagedThreadId;
        private readonly Dictionary<string,PreparedPack> prepared=new Dictionary<string,PreparedPack>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> unavailable=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly FieldInfo sfxGroup=typeof(AudioManager).GetField("m_sfxAudioGroup",BindingFlags.Instance|BindingFlags.NonPublic);
        private GameObject audioObject;
        private AudioSource source;
        private bool faulted;
        internal RewardSoundPack[] Packs {get;private set;}
        internal UnityRewardAudioBackend(Action<string> log,Func<int> maxDuration,Func<bool> manualPreview,Func<string> selectedPack)
        {this.log=log;this.maxDuration=maxDuration;this.manualPreview=manualPreview;this.selectedPack=selectedPack;}

        internal void Prepare(string root)
        {
            Packs=RewardSoundPack.Discover(root,log);
            foreach(var pack in Packs)
            {
                var loaded=new PreparedPack();prepared.Add(pack.Id,loaded);
                if(pack.Id==RewardSoundPack.BuiltinId){PrepareBuiltin();continue;}
                for(int star=1;star<=4;star++)
                    try
                    {
                        PcmWaveData pcm;
                        using(var file=File.OpenRead(pack.FileFor(star)))pcm=PcmWaveReader.Read(file);
                        loaded.Clips[star-1]=Upload(pack.Id,star,pcm.Samples,pcm.Channels,pcm.SampleRate);
                        loaded.Durations[star-1]=pcm.DurationMs;
                        log("Reward audio prepared | pack="+pack.Id+" star="+star+" durationMs="+pcm.DurationMs+" rate="+pcm.SampleRate+" channels="+pcm.Channels);
                    }
                    catch(Exception error){log("Reward audio asset unavailable; haptics continue | pack="+pack.Id+" star="+star+" reason="+error.Message);}
            }
        }
        internal void PrepareBuiltin()
        {
            PreparedPack pack=prepared[RewardSoundPack.BuiltinId];DestroyClips(pack);
            int limit=Math.Max(100,Math.Min(1000,maxDuration()));
            for(int star=1;star<=4;star++)
                try
                {
                    pack.Clips[star-1]=Upload(RewardSoundPack.BuiltinId,star,RewardTone.Generate(star,limit),1,RewardTone.SampleRate);
                    pack.Durations[star-1]=RewardTone.DurationMs(star,limit);
                }
                catch(Exception error){log("Generated reward audio unavailable; haptics continue: "+error.Message);}
        }
        private AudioClip Upload(string pack,int star,float[] pcm,int channels,int rate)
        {
            RequireMainThread();
            var clip=AudioClip.Create("OC2Haptic."+pack+"."+star,pcm.Length/channels,channels,rate,false);
            if(clip.SetData(pcm,0))return clip;
            UnityEngine.Object.Destroy(clip);throw new InvalidOperationException("PCM upload failed.");
        }
        internal int DurationMs(int star)
        {PreparedPack pack;return prepared.TryGetValue(selectedPack(),out pack)?pack.Durations[star-1]:0;}
        public bool Play(int star,float volume)
        {
            if(faulted || star<1 || star>4)return false;
            PreparedPack pack;string id=selectedPack();
            if(!prepared.TryGetValue(id,out pack) || pack.Clips[star-1]==null)
            {
                if(unavailable.Add(id+":"+star))log("Selected reward audio unavailable; haptics continue | pack="+id+" star="+star);
                return false;
            }
            try
            {
                RequireMainThread();
                AudioManager manager=GameUtils.RequestManager<AudioManager>();
                AudioMixerGroup group=manager==null || sfxGroup==null?null:sfxGroup.GetValue(manager) as AudioMixerGroup;
                if(group==null){if(unavailable.Add("mixer"))log("Reward audio unavailable: game's SFX mixer is not ready; haptics continue.");return false;}
                if(source==null)
                {
                    audioObject=new GameObject("OC2Haptic.RewardAudio");UnityEngine.Object.DontDestroyOnLoad(audioObject);
                    source=audioObject.AddComponent<AudioSource>();source.playOnAwake=false;source.loop=false;
                    source.spatialBlend=0;source.volume=1;source.ignoreListenerVolume=false;
                }
                source.outputAudioMixerGroup=group;source.ignoreListenerPause=manualPreview();
                source.Stop();source.PlayOneShot(pack.Clips[star-1],RewardOutput.SafeVolume(volume));
                log("Reward audio | pack="+id+" star="+star+" durationMs="+pack.Durations[star-1]+" volume="+volume+" manual="+manualPreview()+" accepted=True");
                return true;
            }
            catch(Exception error){faulted=true;Stop();log("Reward audio disabled; haptics continue: "+error);return false;}
        }
        private void RequireMainThread()
        {if(Thread.CurrentThread.ManagedThreadId!=mainThread)throw new InvalidOperationException("Reward audio requires Unity main thread.");}
        public void Stop(){if(source!=null)source.Stop();}
        private static void DestroyClips(PreparedPack pack)
        {foreach(var clip in pack.Clips)if(clip!=null)UnityEngine.Object.Destroy(clip);Array.Clear(pack.Clips,0,4);Array.Clear(pack.Durations,0,4);}
        public void Dispose(){Stop();foreach(var pack in prepared.Values)DestroyClips(pack);if(audioObject!=null)UnityEngine.Object.Destroy(audioObject);}
        private sealed class PreparedPack{internal readonly AudioClip[] Clips=new AudioClip[4];internal readonly int[] Durations=new int[4];}
    }
}
