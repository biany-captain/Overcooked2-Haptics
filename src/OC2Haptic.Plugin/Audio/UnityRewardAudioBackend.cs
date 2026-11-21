using System;
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
        private readonly int mainThread=Thread.CurrentThread.ManagedThreadId;
        private readonly AudioClip[] clips=new AudioClip[4];
        private readonly FieldInfo sfxGroup=typeof(AudioManager).GetField("m_sfxAudioGroup",BindingFlags.Instance|BindingFlags.NonPublic);
        private GameObject audioObject;
        private AudioSource source;
        private int clipLimit;
        private bool faulted;
        internal UnityRewardAudioBackend(Action<string> log,Func<int> maxDuration,Func<bool> manualPreview)
        {this.log=log;this.maxDuration=maxDuration;this.manualPreview=manualPreview;}
        public bool Play(int star,float volume)
        {
            if(faulted || star<1 || star>4)return false;
            try
            {
                if(Thread.CurrentThread.ManagedThreadId!=mainThread)throw new InvalidOperationException("Reward audio requires Unity main thread.");
                AudioManager manager=GameUtils.RequestManager<AudioManager>();
                AudioMixerGroup group=manager==null || sfxGroup==null?null:sfxGroup.GetValue(manager) as AudioMixerGroup;
                if(group==null){log("Reward audio unavailable: game's SFX mixer is not ready; haptics continue.");return false;}
                if(source==null)
                {
                    audioObject=new GameObject("OC2Haptic.RewardAudio");UnityEngine.Object.DontDestroyOnLoad(audioObject);
                    source=audioObject.AddComponent<AudioSource>();source.playOnAwake=false;source.loop=false;
                    source.spatialBlend=0;source.volume=1;source.ignoreListenerVolume=false;
                }
                source.outputAudioMixerGroup=group;
                source.ignoreListenerPause=manualPreview();
                int limit=Math.Max(100,Math.Min(1000,maxDuration()));
                if(clipLimit!=limit){DestroyClips();clipLimit=limit;}
                if(clips[star-1]==null)
                {
                    float[] pcm=RewardTone.Generate(star,limit);
                    var clip=AudioClip.Create("OC2Haptic.CrispReward."+star,pcm.Length,1,RewardTone.SampleRate,false);
                    if(!clip.SetData(pcm,0)){UnityEngine.Object.Destroy(clip);throw new InvalidOperationException("Generated PCM upload failed.");}
                    clips[star-1]=clip;
                }
                source.Stop();source.PlayOneShot(clips[star-1],RewardOutput.SafeVolume(volume));
                log("Reward audio | clip=CrispReward"+star+" volume="+volume+" manual="+manualPreview()+" accepted=True");
                return true;
            }
            catch(Exception error){faulted=true;Stop();log("Reward audio disabled; haptics continue: "+error);return false;}
        }
        public void Stop(){if(source!=null)source.Stop();}
        private void DestroyClips(){foreach(var clip in clips)if(clip!=null)UnityEngine.Object.Destroy(clip);Array.Clear(clips,0,clips.Length);}
        public void Dispose(){Stop();DestroyClips();if(audioObject!=null)UnityEngine.Object.Destroy(audioObject);}
    }
}
