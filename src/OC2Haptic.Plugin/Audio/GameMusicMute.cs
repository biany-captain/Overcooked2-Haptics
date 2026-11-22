using System;
using System.Collections.Generic;
using System.Reflection;
using OC2Haptic.Core;
using UnityEngine;
using UnityEngine.Audio;

namespace OC2Haptic.Audio
{
    // Current AudioManager.AddSource creates children under m_sharedSource and sets
    // their mixer group. Only those Music sources are muted; volume options stay untouched.
    internal sealed class GameMusicMute
    {
        private const BindingFlags Fields=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        private readonly FieldInfo rootField=typeof(AudioManager).GetField("m_sharedSource",Fields);
        private readonly FieldInfo groupField=typeof(AudioManager).GetField("m_musAudioGroup",Fields);
        private readonly FieldInfo mixerField=typeof(AudioManager).GetField("m_audioMixer",Fields);
        private readonly Action<string> log;
        private readonly TemporaryMusicMute<AudioSource> mute;
        private readonly List<AudioSource> sources=new List<AudioSource>();
        private GameObject root;
        private AudioMixerGroup group;
        private AudioMixer mixer;
        private bool requested,failed,restoreWarning;
        private float nextRestoreAt;
        internal GameMusicMute(Action<string> log)
        {
            this.log=log;
            mute=new TemporaryMusicMute<AudioSource>(source=>source!=null,source=>source.outputAudioMixerGroup==group,
                source=>source==null?(bool?)null:source.mute,
                (source,value)=>{if(source!=null)source.mute=value;return true;});
        }
        internal void Begin(AudioManager manager,AudioMixerGroup sfxGroup)
        {
            End();if(failed || mute.ActiveCount>0)return;
            try
            {
                root=manager==null || rootField==null?null:rootField.GetValue(manager) as GameObject;
                group=manager==null || groupField==null?null:groupField.GetValue(manager) as AudioMixerGroup;
                mixer=manager==null || mixerField==null?null:mixerField.GetValue(manager) as AudioMixer;
                if(group!=null && group==sfxGroup){Fail("music and SFX groups are not distinct");return;}
                requested=true;Tick();
                if(!failed)log("Game BGM source mute active | count="+mute.ActiveCount+"; volume parameters/SFX unchanged.");
            }
            catch(Exception error){Fail(error.Message);}
        }
        internal void Tick()
        {
            if(!requested || failed || !mute.Focused){Restore(false);return;}
            try
            {
                float sfx;
                if(root==null || group==null || mixer==null || !mixer.GetFloat("SFXVolume",out sfx))
                {Fail("game audio source bindings unavailable");return;}
                if(sfx<=-80){Restore(false);return;}
                sources.Clear();root.GetComponentsInChildren<AudioSource>(true,sources);
                if(!mute.Apply(sources))Fail(mute.LastError);else restoreWarning=false;
            }
            catch(Exception error){Fail(error.Message);}
        }
        internal void End(){requested=false;Restore(true);}
        internal void SetFocused(bool focused)
        {
            bool active=mute.ActiveCount>0;
            if(!mute.SetFocused(focused))Restore(true);
            else if(!focused && active)log("Game BGM sources restored on focus loss.");
        }
        internal void Idle(){requested=false;Restore(false);}
        private void Restore(bool force)
        {
            if(mute.ActiveCount==0){restoreWarning=false;nextRestoreAt=0;return;}
            float now=Time.realtimeSinceStartup;if(!force && now<nextRestoreAt)return;
            nextRestoreAt=now+.25f;
            if(mute.End()){restoreWarning=false;nextRestoreAt=0;log("Game BGM sources restored | success=True");return;}
            if(!restoreWarning)log("Game BGM source restore pending; retrying on main thread | "+mute.LastError);
            restoreWarning=true;
        }
        private void Fail(string reason)
        {bool first=!failed;failed=true;End();if(first)log("Game BGM source mute unavailable; MOD audio continues | "+reason);}
    }
}
