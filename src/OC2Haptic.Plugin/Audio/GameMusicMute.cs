using System;
using System.Reflection;
using OC2Haptic.Core;
using UnityEngine.Audio;

namespace OC2Haptic.Audio
{
    // Inspected current game MusicVolume/SFXVolume getters expose independent names.
    // Only the live mixer parameter is overridden; no saved AudioVolume option is changed.
    internal sealed class GameMusicMute
    {
        private readonly Action<string> log;
        private readonly FieldInfo mixerField=typeof(AudioManager).GetField("m_audioMixer",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
        private readonly TemporaryMusicMute mute;
        private AudioMixer mixer;
        private bool requested,failed;
        internal GameMusicMute(Action<string> log){this.log=log;mute=new TemporaryMusicMute(ReadMusic,WriteMusic);}
        private float? ReadMusic()
        {float value;return mixer!=null && mixer.GetFloat("MusicVolume",out value)?(float?)value:null;}
        private bool WriteMusic(float value){return mixer!=null && mixer.SetFloat("MusicVolume",value);}
        internal void Begin(AudioManager manager)
        {
            End();if(failed)return;
            try
            {
                mixer=manager==null || mixerField==null?null:mixerField.GetValue(manager) as AudioMixer;
                requested=true;Tick();
                if(mute.Active)log("Game music override active | MusicVolume only; SFX preserved.");
            }
            catch(Exception error){Fail(error.Message);}
        }
        internal void Tick()
        {
            if(!requested || failed)return;
            try
            {
                float sfx;
                if(mixer==null || !mixer.GetFloat("SFXVolume",out sfx)){Fail("independent mixer controls unavailable");return;}
                // Do not silence BGM when the MOD's SFX bus is itself muted.
                if(sfx<=TemporaryMusicMute.MutedDb){mute.End();return;}
                if(!(mute.Active?mute.Tick():mute.Begin()))Fail("music override could not be applied");
            }
            catch(Exception error){Fail(error.Message);}
        }
        internal void End()
        {
            requested=false;
            try{if(mute.Active)log("Game music override released | success="+mute.End());}
            catch(Exception error){failed=true;log("Game music override cleanup unavailable: "+error.Message);}
        }
        private void Fail(string reason)
        {bool first=!failed;failed=true;End();if(first)log("Game music override unavailable; MOD audio continues | "+reason);}
    }
}
