#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Overcooked2-Haptics original cartoon-jazz celebration jingles.

Offline: generate original MIDI scores, render using FluidSynth + TimGM6mb,
add original synthesized cartoon crowd/claps, mix/master to WAV & OGG.

SoundFont sample bank: TimGM6mb, GPL-2.0. Its .sf2 is not redistributed.
This generator and compositions: original for this project. See docs/LICENSE_NOTICES.md.
"""
from __future__ import annotations
import os, ctypes, ctypes.util, math, zipfile, json
from dataclasses import dataclass
import numpy as np
import soundfile as sf
import mido
from scipy.signal import butter, sosfilt, fftconvolve

ROOT=os.path.dirname(os.path.abspath(__file__))
SR=48000
BPM=168
BEAT=60.0/BPM
SOUNDFONT='/usr/share/sounds/sf2/TimGM6mb.sf2'

@dataclass
class Note:
    on:float
    dur:float
    pitch:int
    vel:int
    ch:int

class Song:
    def __init__(self,name,beats,duration):
        self.name=name
        self.beats=beats
        self.duration=duration
        self.events=[]
        self.programs={}
        self.pan={}
    def inst(self,ch,program,pan=64):
        self.programs[ch]=program
        self.pan[ch]=pan
    def n(self,ch,p,on,dur=.25,vel=85):
        self.events.append(Note(round(on*BEAT,6),round(dur*BEAT,6),p,vel,ch))
    def chord(self,ch,notes,on,dur=.5,vel=82,spread=.017):
        for i,p in enumerate(notes):
            self.n(ch,p,on+i*spread,dur,vel-max(i-1,0)*3)
    def drum(self,p,on,vel=75,dur=.09):
        self.n(9,p,on,dur,vel)
    def swing_hats(self,total,vel=45):
        pos=0
        while pos<total:
            self.drum(42,pos,vel,.08)
            self.drum(42,pos+0.66,vel-8,.07)
            pos+=1
    def export_midi(self,path):
        mid=mido.MidiFile(ticks_per_beat=960)
        trk=mido.MidiTrack();mid.tracks.append(trk)
        trk.append(mido.MetaMessage('set_tempo',tempo=mido.bpm2tempo(BPM),time=0))
        names={0:'Piano',1:'Xylophone',2:'Clarinet',3:'Trumpet',4:'Trombone',5:'Bass',6:'Saxophone',7:'Brass Section',8:'Mallets',9:'Drums',10:'Banjo'}
        eventlist=[]
        for ch,pr in self.programs.items():
            eventlist.append((0,0,mido.Message('program_change',channel=ch,program=pr)))
            eventlist.append((0,0,mido.Message('control_change',channel=ch,control=10,value=self.pan[ch])))
        for nd in self.events:
            at=max(0,round(nd.on/BEAT*960)); off=max(at+1,round((nd.on+nd.dur)/BEAT*960))
            eventlist.append((at,2,mido.Message('note_on',channel=nd.ch,note=nd.pitch,velocity=nd.vel)))
            eventlist.append((off,1,mido.Message('note_off',channel=nd.ch,note=nd.pitch,velocity=0)))
        eventlist.sort(key=lambda x:(x[0],x[1]))
        prev=0
        for tick,_,msg in eventlist:
            trk.append(msg.copy(time=tick-prev));prev=tick
        mid.save(path)


def song1():
    s=Song('star_1_playful_spark',2.55,.98)
    # Bright short upward piano + xylophone pop, tiny two-chord cadential smile.
    s.inst(0,3,55)     # honky-tonk piano
    s.inst(1,13,78)    # xylophone
    s.inst(5,32,51)
    s.inst(8,9,82)     # glockenspiel
    s.n(5,53,0,.5,74);s.n(5,48,1.15,.42,66)
    s.chord(0,[53,57,60,65],0,.27,66)
    s.n(1,77,0,.27,84)    # F5
    s.n(1,81,.52,.25,88) # A5
    s.n(1,84,1.16,.37,98)# C6
    s.n(8,89,1.65,.34,75) # F6 sparkle
    s.chord(0,[65,69,72,77],1.52,.57,83)
    s.drum(75,0,45,.06);s.drum(76,.55,43,.06)
    s.drum(42,1.5,52,.12)
    return s


def song2():
    s=Song('star_2_bouncy_skip',3.75,1.49)
    s.inst(0,0,47);s.inst(2,71,68) # clarinet leads
    s.inst(1,12,76) # marimba punctuation
    s.inst(5,32,55);s.inst(10,105,82)
    s.n(5,53,0,.42,70);s.n(5,57,.94,.34,66);s.n(5,60,1.75,.36,65);s.n(5,53,2.65,.65,70)
    s.chord(0,[57,60,65,69],0,.28,66)
    s.chord(0,[58,62,65,69],1.65,.25,75) # Dm6-ish
    s.chord(0,[53,57,60,65,69],2.8,.65,79)
    # recognizable, jaunty 3 + 1 notes with staggered up-down motion
    for n,t,d,v in [(72,0,.40,83),(77,.63,.35,91),(79,1.37,.34,91),(76,2.09,.26,85),(84,2.82,.5,103)]:
        s.n(2,n,t,d,v)
    s.n(1,72,1.42,.20,73);s.n(1,77,2.09,.19,68)
    s.n(10,72,2.82,.37,55)
    s.swing_hats(3.2,51);s.drum(38,1.95,63,.12);s.drum(36,0,66,.12);s.drum(49,2.85,52,.14)
    return s


def song3():
    s=Song('star_3_brass_victory',5.3,2.16)
    s.inst(0,3,44);s.inst(3,56,72);s.inst(4,57,38);s.inst(6,65,61)
    s.inst(5,32,55);s.inst(8,11,82)
    # louder brass announces theme; syncopated 3 fanfare stabs and descending jazz tail.
    for n,t,d,v in [(69,0,.29,88),(72,.42,.27,91),(77,.81,.49,106),(79,1.68,.3,93),(81,2.12,.38,101),(84,2.80,.63,110),(81,3.86,.26,97),(77,4.28,.55,102)]:
        s.n(3,n,t,d,v)
    for chord,t,d,vel in [([53,60,65],0,.28,74),([58,65,70],.78,.39,80),([60,65,72],2.13,.45,79),([53,60,65,69],3.45,.54,98)]:
        s.chord(4,chord,t,d,vel)
    s.n(6,69,.78,.25,71);s.n(6,72,1.68,.25,76);s.n(6,77,2.8,.6,84)
    s.chord(0,[53,57,60,65],0,.23,67);s.chord(0,[55,59,62,65],1.78,.21,74)
    s.chord(0,[53,57,60,65,69],3.45,.78,89)
    for pitch,t in [(41,0),(48,.8),(43,1.68),(48,2.8),(41,3.45)]:
        s.n(5,pitch,t,.42,74)
    s.swing_hats(4.8,49)
    for beat in [0,1.65,3.45]:s.drum(36,beat,72,.10)
    for beat in [1.05,2.47,3.10]:s.drum(38,beat,76,.13)
    s.drum(38,3.29,57,.10);s.drum(49,3.48,86,.38)
    s.drum(54,4.18,59,.09)
    s.n(8,89,3.50,.5,62)
    return s


def song4():
    s=Song('star_4_grand_kitchen_party',7.0,3.25)
    s.inst(0,3,42);s.inst(3,56,73);s.inst(4,57,26);s.inst(6,66,62)
    s.inst(7,61,55);s.inst(5,32,50);s.inst(8,11,80);s.inst(10,105,77)
    # distinctive opening syncopated horn motif, call-and-response, final brassy chord.
    trumpets=[(72,0,.22,101),(76,.33,.23,104),(79,.72,.31,106),
              (84,1.27,.34,115),(81,1.88,.26,100),
              (79,2.2,.22,102),(84,2.57,.26,111),
              (86,3.04,.44,113),(84,3.69,.22,102),(81,4.02,.25,98),
              (88,4.46,.35,116),(89,5.2,.76,120)]
    for p,t,d,v in trumpets:s.n(3,p,t,d,v)
    for pitches,t,d,v in [([53,60,65],0,.24,83),([58,65,70],.70,.32,86),([60,67,72],1.28,.28,89),
                           ([55,62,67],2.18,.3,85),([58,65,70],3.06,.36,93),([60,67,72],4.48,.28,92),
                           ([53,60,65,69],5.2,.89,108)]:
        s.chord(4,pitches,t,d,v,spread=.012)
    for pitches,t,d,vel in [([65,69,72,77],1.29,.42,76),([67,71,74,79],3.09,.33,82),([65,69,72,77,81],5.2,.92,105)]:
        s.chord(7,pitches,t,d,vel)
    # lively stride comping with offbeat rhythms
    for pitches,t,d,v in [([53,57,60,65],0,.16,76),([53,57,62,65],.71,.21,70),([55,59,62,67],1.67,.20,72),
                          ([57,60,65,69],2.58,.21,81),([55,59,62,67],3.44,.24,74),([55,59,65,70],4.52,.16,80),
                          ([53,57,60,65,69],5.2,1.09,100)]:s.chord(0,pitches,t,d,v)
    for p,t,d in [(72,.70,.18),(76,1.30,.25),(79,1.90,.16),(77,2.58,.3),(81,3.45,.2),(84,4.15,.23),(89,5.2,.61)]:
        s.n(6,p,t,d,86)
    for p,t,d in [(41,0,.35),(48,.7,.34),(50,1.33,.34),(53,2.08,.35),(43,2.83,.36),(48,3.5,.30),(50,4.25,.30),(41,5.2,.76)]:
        s.n(5,p,t,d,79)
    for p,t in [(84,.72),(81,2.58),(89,5.24)]:s.n(8,p,t,.29,72)
    for p,t in [(77,0),(79,2.17),(84,4.50)]:s.n(10,p,t,.23,58)
    s.swing_hats(6.3,56)
    for t in [0,1.33,2.56,3.87,5.19]:s.drum(36,t,78,.09)
    for t in [.68,1.95,3.16,4.48,5.24]:s.drum(38,t,80,.10)
    for t in [4.8,4.91,5.01,5.10]:s.drum(38,t,48+int((t-4.8)*110),.07)
    s.drum(49,5.20,100,.60);s.drum(54,5.48,69,.15)
    return s


class FSynth:
    def __init__(self):
        lib=ctypes.CDLL(ctypes.util.find_library('fluidsynth'))
        self.lib=lib
        def decl(k,restype,args):
            f=getattr(lib,k);f.restype=restype;f.argtypes=args;return f
        P=ctypes.c_void_p; I=ctypes.c_int;D=ctypes.c_double;S=ctypes.c_char_p
        self.newsettings=decl('new_fluid_settings',P,[])
        self.ssetnum=decl('fluid_settings_setnum',I,[P,S,D])
        self.ssetint=decl('fluid_settings_setint',I,[P,S,I])
        self.newsynth=decl('new_fluid_synth',P,[P])
        self.sfload=decl('fluid_synth_sfload',I,[P,S,I])
        self.prg=decl('fluid_synth_program_change',I,[P,I,I])
        self.cc=decl('fluid_synth_cc',I,[P,I,I,I])
        self.noteon=decl('fluid_synth_noteon',I,[P,I,I,I])
        self.noteoff=decl('fluid_synth_noteoff',I,[P,I,I])
        self.off=decl('fluid_synth_all_sounds_off',I,[P,I])
        self.write=decl('fluid_synth_write_float',I,[P,I,P,I,I,P,I,I])
        self.del_synth=decl('delete_fluid_synth',None,[P])
        self.del_settings=decl('delete_fluid_settings',None,[P])
        self.settings=self.newsettings()
        self.ssetnum(self.settings,b'synth.sample-rate',float(SR))
        self.ssetnum(self.settings,b'synth.gain',0.28)
        self.ssetnum(self.settings,b'synth.reverb.room-size',0.31)
        self.ssetnum(self.settings,b'synth.reverb.level',0.20)
        self.ssetnum(self.settings,b'synth.chorus.level',0.08)
        self.ssetint(self.settings,b'synth.polyphony',256)
        self.synth=self.newsynth(self.settings)
        res=self.sfload(self.synth,SOUNDFONT.encode(),1)
        if res<0:raise RuntimeError('SoundFont load failed')
    def close(self):
        self.del_synth(self.synth);self.del_settings(self.settings)
    def render(self,song):
        for ch,pr in song.programs.items():
            self.prg(self.synth,ch,pr)
            self.cc(self.synth,ch,10,song.pan[ch])
            self.cc(self.synth,ch,7,106 if ch not in [5] else 97)
        events=[]
        for n in song.events:
            events.append((int(n.on*SR),2,n.ch,n.pitch,n.vel))
            events.append((int((n.on+n.dur)*SR),1,n.ch,n.pitch,0))
        events.sort(key=lambda e:(e[0],e[1]))
        output=np.zeros((int(song.duration*SR),2),dtype=np.float32)
        cursor=0
        i=0
        while cursor<len(output):
            nxt=min(events[i][0],len(output)) if i<len(events) else len(output)
            if nxt>cursor:
                n=nxt-cursor
                left=np.empty(n,dtype=np.float32);right=np.empty(n,dtype=np.float32)
                self.write(self.synth,n,left.ctypes.data,0,1,right.ctypes.data,0,1)
                output[cursor:nxt,0]=left;output[cursor:nxt,1]=right
                cursor=nxt
            else:
                while i<len(events) and events[i][0]<=cursor:
                    _,kind,ch,p,vel=events[i]
                    if kind==2:self.noteon(self.synth,ch,p,vel)
                    else:self.noteoff(self.synth,ch,p)
                    i+=1
            if i>=len(events) and cursor>=len(output):break
        for ch in range(16):self.off(self.synth,ch)
        return output


def filter_sos(arr,cut,typ='lowpass',order=3):
    return sosfilt(butter(order,cut,btype=typ,fs=SR,output='sos'),arr,axis=0)


def clap_burst(n,seed=0):
    rng=np.random.default_rng(seed)
    white=rng.standard_normal(n).astype(np.float32)
    hi=filter_sos(white,900,'highpass',2)
    # sampled claps have several sharp transients, brief decaying noisy body
    t=np.arange(n)/SR
    env=np.zeros(n,dtype=np.float32)
    for delay,amp,dec in [(0,1,.012),(.013,.67,.016),(.024,.45,.02),(.045,.19,.045)]:
        env+=amp*np.where(t>=delay,np.exp(-(t-delay)/dec),0)
    return hi*env*.27


def formant_voice(duration=.55,f0=200,seed=0):
    """Original fully synthesized cartoon 'woo/yay' vowel, not a voice recording.
    Multi-formant spectral excitation, vibrato + happy rising pitch.
    """
    rng=np.random.default_rng(seed)
    n=int(duration*SR);t=np.arange(n)/SR
    xphase=2*np.pi*(f0*(1+.20*np.minimum(t/.14,1)) * t + 1.8/5*np.cos(2*np.pi*5*t))
    xphase+=rng.uniform(0,2*np.pi)
    # voiced signal with rich harmonics (cartoon chorus)
    harmonics=range(1,50)
    # 1st half 'oo', 2nd half 'ay' formant blend
    voice=np.zeros(n,dtype=np.float32)
    for h in harmonics:
        freq=f0*(1+.20*np.minimum(t/.14,1))*h
        step=np.maximum(0,np.minimum(1,(t-.15)/.18))
        # formants u / ae, move resonances from 'oo' to 'yay' gradually
        amps=(np.exp(-.5*((freq-(430*(1-step)+720*step))/115)**2)
             +.55*np.exp(-.5*((freq-(950*(1-step)+1350*step))/200)**2)
             +.23*np.exp(-.5*((freq-2550)/400)**2))
        voice+=(amps*np.sin(h*xphase)).astype(np.float32)/np.sqrt(h)
    v=np.max(np.abs(voice))
    if v>0:voice/=v
    env=np.minimum(1,t/.075)**1.25 * np.minimum(1,(duration-t)/.19)**1.6
    voice=voice*env.astype(np.float32)
    # soft excitation for breath, blended rather than clear speech
    breath=filter_sos(rng.standard_normal(n).astype(np.float32),2400,'lowpass',2)
    voice=voice*.82+breath*env*.018
    return voice


def cartoon_crowd(length,seed=144):
    """Tiny synthetic cheer + applause tail, generated in-house (no third-party recordings)."""
    rng=np.random.default_rng(seed)
    out=np.zeros((int(length*SR),2),dtype=np.float32)
    # 5 cartoon pitched voices, quiet and brief, staggered naturally
    voices=[(0.00,.57,185,.30,-.62),(.055,.53,239,.24,.38),(.13,.42,167,.21,.69),(.05,.47,292,.14,-.18),(.16,.39,209,.17,.04)]
    for i,(start,dur,freq,gain,pan) in enumerate(voices):
        v=formant_voice(dur,freq,seed+i)
        k=int(start*SR)
        if k+len(v)>len(out):v=v[:len(out)-k]
        out[k:k+len(v),0]+=v*gain*(.75-.38*pan)
        out[k:k+len(v),1]+=v*gain*(.75+.38*pan)
    # low amplitude overlapping claps, real rhythmic noise texture
    for j,start in enumerate([.18,.27,.36,.48,.59,.70]):
        if start>length-.045:continue
        cl=clap_burst(min(int(.12*SR),len(out)-int(start*SR)),seed+j+25)
        pan=float(rng.uniform(-1,1));gain=.22 + .12*rng.random()
        k=int(start*SR)
        out[k:k+len(cl),0]+=cl*gain*(1-pan*.5)
        out[k:k+len(cl),1]+=cl*gain*(1+pan*.5)
    # gentle early reflections without modulating music itself
    tail=np.zeros_like(out)
    for d,g in [(0.041,.14),(0.081,.09),(0.133,.06)]:
        k=int(d*SR)
        tail[k:,0]+=out[:-k,1]*g
        tail[k:,1]+=out[:-k,0]*g
    out+=tail
    return out


def master(song,x):
    # SoundFont already supplies stereo instruments. For consistent mastering,
    # remove infrabass and tiny click artifacts, add custom small-room reflections.
    x=filter_sos(x,58,'highpass',2)
    x=filter_sos(x,15100,'lowpass',2)
    # Simple dedicated stereo slap reflections, at much lower level.
    y=x.copy()
    for dt,level,flip in [(.045,.054,True),(.088,.034,False)]:
        k=int(dt*SR)
        if flip:
            y[k:,0]+=x[:-k,1]*level;y[k:,1]+=x[:-k,0]*level
        else:y[k:]+=x[:-k]*level
    x=y
    if song.name.startswith('star_4'):
        # Applause+cheer enters toward the end (separately synthesised, no sampled voices).
        cheer=cartoon_crowd(.82)
        at=int(2.31*SR)
        count=min(len(cheer),len(x)-at)
        x[at:at+count]+=cheer[:count]*.24
    # Start/end envelopes and tame peaks: safety for continuous game play.
    n=len(x);t=np.arange(n)/SR
    env=np.minimum(1,np.maximum(0,t)/.018)
    fade=.10 if song.name.startswith('star_1') else (.19 if song.name.startswith('star_4') else .15)
    env*=np.minimum(1,np.maximum(0,(song.duration-t))/fade)**.8
    x=x*env[:,None]
    # Soft clip and LUFS: modest headroom. Since these are very short, integrated
    # loudness approximations fluctuate; use trimmed RMS to equalize.
    rms=np.sqrt(np.mean(x*x))
    target={1:.106,2:.115,3:.128,4:.128}[int(song.name[5])]
    if rms>1e-10:x*=min(target/rms,4.0)
    x=np.tanh(x*1.15)/1.15
    peak=float(np.max(np.abs(x)))
    if peak>.85:x*=.85/peak
    x=np.nan_to_num(x)
    return np.clip(x,-1,1).astype(np.float32)


def main():
    if not os.path.isfile(SOUNDFONT):raise RuntimeError(f'Missing {SOUNDFONT}')
    wavdir=os.path.join(ROOT,'audio_wav');oggdir=os.path.join(ROOT,'audio_ogg');mididir=os.path.join(ROOT,'midi')
    for p in [wavdir,oggdir,mididir]:os.makedirs(p,exist_ok=True)
    songs=[song1(),song2(),song3(),song4()]
    data=[]
    for idx,s in enumerate(songs,1):
        fs=FSynth()
        s.export_midi(os.path.join(mididir,s.name+'.mid'))
        x=fs.render(s)
        fs.close()
        x=master(s,x)
        path=os.path.join(wavdir,s.name+'.wav')
        sf.write(path,x,SR,subtype='PCM_16')
        data.append({'star':idx,'file':s.name+'.wav','duration_sec':len(x)/SR,'sample_rate':SR,
                     'channels':2,'peak_dbfs':round(float(20*np.log10(max(1e-10,np.max(abs(x))))),2),
                     'rms_dbfs':round(float(20*np.log10(max(1e-10,np.sqrt(np.mean(x*x))))),2)})
        print(data[-1])
    import subprocess
    for s in songs:
        cmd=['ffmpeg','-nostdin','-hide_banner','-loglevel','error','-y','-i',os.path.join(wavdir,s.name+'.wav'),
             '-c:a','libvorbis','-q:a','6',os.path.join(oggdir,s.name+'.ogg')]
        subprocess.run(cmd,check=True)
    with open(os.path.join(ROOT,'docs','audio_analysis.json'),'w',encoding='utf-8') as f:
        json.dump(data,f,ensure_ascii=False,indent=2)
if __name__=='__main__':main()
