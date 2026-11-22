using System;
using System.Collections.Generic;
using OC2Haptic.Core;

internal static class MusicMuteTests
{
    internal static void Add(Dictionary<string,Action> tests)
    {
        tests.Add("music mute restores initial level and leaves SFX untouched", Restore);
        tests.Add("music mute tracks game updates without clobbering later external changes", Updates);
        tests.Add("music mute preserves initially muted level and isolates unavailable mixer", Unavailable);
    }
    private static void Check(bool value){if(!value)throw new Exception("temporary music mute mismatch");}
    private sealed class Mixer
    {
        internal float Music=-12,Sfx=-6;internal bool Available=true;internal int Writes;
        internal float? Read(){return Available?(float?)Music:null;}
        internal bool Write(float value){if(!Available)return false;Writes++;Music=value;return true;}
    }
    private static void Restore()
    {
        var mixer=new Mixer();var mute=new TemporaryMusicMute(mixer.Read,mixer.Write);
        Check(mute.Begin() && mixer.Music==-80 && mixer.Sfx==-6);Check(mute.Tick());
        Check(mute.End() && mixer.Music==-12 && mixer.Sfx==-6 && !mute.Active);
        int writes=mixer.Writes;Check(mute.End() && mixer.Writes==writes);
    }
    private static void Updates()
    {
        var mixer=new Mixer();var mute=new TemporaryMusicMute(mixer.Read,mixer.Write);Check(mute.Begin());
        mixer.Music=-20;Check(mute.Tick() && mixer.Music==-80);Check(mute.End() && mixer.Music==-20);
        Check(mute.Begin());mixer.Music=-5;Check(mute.End() && mixer.Music==-5);
    }
    private static void Unavailable()
    {
        var mixer=new Mixer{Music=-80};var mute=new TemporaryMusicMute(mixer.Read,mixer.Write);
        Check(mute.Begin());Check(mute.End() && mixer.Music==-80);
        mixer.Available=false;int writes=mixer.Writes;Check(!mute.Begin() && !mute.Active && mixer.Writes==writes);
    }
}
