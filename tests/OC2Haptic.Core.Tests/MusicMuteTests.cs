using System;
using System.Collections.Generic;
using OC2Haptic.Core;

internal static class MusicMuteTests
{
    internal static void Add(Dictionary<string,Action> tests)
    {
        tests.Add("music source mute leaves SFX and existing user mute unchanged", Restore);
        tests.Add("music source mute discovers new sources and restores departing sources", SourcesChange);
        tests.Add("music source changing to SFX is restored immediately", GroupChange);
        tests.Add("destroyed music source retires without native access", Destroyed);
        tests.Add("music source failures retain ownership while other sources restore", RestoreFailure);
        tests.Add("music source mute respects external unmute at release", ExternalChange);
        tests.Add("music source focus restore is independent of background feedback opt out", Focus);
    }
    private static void Check(bool value){if(!value)throw new Exception("music source mute mismatch");}
    private sealed class Source
    {
        internal bool Alive=true,Music=true,Muted,FailRead,FailWrite,ThrowRead,ThrowWrite;
        internal int Writes;
        internal bool? Read(){if(!Alive || ThrowRead)throw new InvalidOperationException("read unavailable");return FailRead?(bool?)null:Muted;}
        internal bool Write(bool value){if(!Alive || ThrowWrite)throw new InvalidOperationException("write unavailable");if(FailWrite)return false;Writes++;Muted=value;return true;}
    }
    private static TemporaryMusicMute<Source> Scope()
    {return new TemporaryMusicMute<Source>(s=>s.Alive,s=>s.Music,s=>s.Read(),(s,value)=>s.Write(value));}
    private static void Restore()
    {
        var a=new Source();var b=new Source{Muted=true};var sfx=new Source{Music=false};var scope=Scope();
        Check(scope.Apply(new[]{a,b,sfx}) && a.Muted && b.Muted && !sfx.Muted && scope.ActiveCount==2);
        Check(scope.End() && !a.Muted && b.Muted && !sfx.Muted && sfx.Writes==0 && scope.ActiveCount==0);
        int writes=a.Writes;Check(scope.End() && a.Writes==writes);
    }
    private static void SourcesChange()
    {
        var a=new Source();var b=new Source();var scope=Scope();Check(scope.Apply(new[]{a}));
        Check(scope.Apply(new[]{a,b}) && b.Muted);Check(scope.Apply(new[]{b}) && !a.Muted && b.Muted);
        Check(scope.End() && !b.Muted);
    }
    private static void GroupChange()
    {
        var a=new Source();var b=new Source{Music=false};var scope=Scope();Check(scope.Apply(new[]{a,b}));
        a.Music=false;b.Music=true;Check(scope.Apply(new[]{a,b}) && !a.Muted && b.Muted);
        Check(scope.End() && !a.Muted && !b.Muted);
    }
    private static void Destroyed()
    {
        var a=new Source();var scope=Scope();Check(scope.Apply(new[]{a}));a.Alive=false;int writes=a.Writes;
        Check(scope.End() && scope.ActiveCount==0 && a.Writes==writes);
    }
    private static void RestoreFailure()
    {
        for(int mode=0;mode<4;mode++)
        {
            var a=new Source();var b=new Source();var scope=Scope();Check(scope.Apply(new[]{a,b}));
            a.FailRead=mode==0;a.FailWrite=mode==1;a.ThrowRead=mode==2;a.ThrowWrite=mode==3;
            Check(!scope.End() && scope.ActiveCount==1 && a.Muted && !b.Muted);
            a.FailRead=a.FailWrite=a.ThrowRead=a.ThrowWrite=false;
            Check(scope.End() && scope.ActiveCount==0 && !a.Muted);
        }
    }
    private static void ExternalChange()
    {
        var a=new Source{Muted=true};var scope=Scope();Check(scope.Apply(new[]{a}));a.Muted=false;
        Check(scope.End() && !a.Muted);
        Check(scope.Apply(new[]{a}));a.Muted=false;Check(scope.Apply(new[]{a}) && a.Muted);
        Check(scope.End() && !a.Muted);
    }
    private static void Focus()
    {
        var a=new Source();var scope=Scope();int stops=0;Check(scope.Apply(new[]{a}));
        FeedbackFocus.Apply(false,false,value=>{scope.SetFocused(value);},()=>stops++);
        Check(!a.Muted && stops==0 && scope.Apply(new[]{a}) && !a.Muted);
        FeedbackFocus.Apply(true,false,value=>{scope.SetFocused(value);},()=>stops++);
        Check(scope.Apply(new[]{a}) && a.Muted && stops==0);
        FeedbackFocus.Apply(false,true,value=>{scope.SetFocused(value);},()=>stops++);
        Check(!a.Muted && stops==1);
    }
}
