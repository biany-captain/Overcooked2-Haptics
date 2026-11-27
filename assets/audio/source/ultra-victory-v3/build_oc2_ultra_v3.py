#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Original ultra energetic victory jingles for Overcooked2-Haptics.
The notes, structure, arrangements are composed for this pack.
WAV rendering uses the Debian TimGM6mb SoundFont (GPL-2.0); preserve license.
No third-party/commercial audio recordings or synthetic cheering in this pack.
"""
from __future__ import annotations
import sys, shutil, json, zipfile, subprocess, math, re, datetime
from pathlib import Path
import numpy as np
import soundfile as sf
sys.path.insert(0, '/mnt/data/Overcooked2_Haptics_Star_Celebration')
import build_music as B

OUT=Path('/mnt/data/Overcooked2_Haptics_Ultra_Victory_v3')
SR=B.SR
INFO={
 'turbo_swing': {'zh':'超燃卡通大乐队','en':'Turbo Swing','bpm':208,'desc':'闪亮小号、长号与萨克斯，急促的爵士鼓填充；最贴近胡闹厨房的欢乐热闹氛围'},
 'champion_fanfare': {'zh':'冠军加冕号角','en':'Champion Fanfare','bpm':188,'desc':'大铜管胜利号角、定音鼓与军鼓滚奏，接近闯关夺冠庆典'},
 'kitchen_rock': {'zh':'厨房摇滚派对','en':'Kitchen Rock Party','bpm':214,'desc':'电吉他、合成器、摇滚鼓组，跳跃而具有冲击力的胜利短曲'},
}
LENS={'turbo_swing':[1.05,1.57,2.30,3.85], 'champion_fanfare':[1.13,1.70,2.48,4.05], 'kitchen_rock':[1.01,1.53,2.35,3.80]}

def notes(s,ch,rows):
 for p,t,d,v in rows:s.n(ch,p,t,d,v)
def chords(s,ch,rows):
 for ns,t,d,v in rows:s.chord(ch,ns,t,d,v,spread=.018)

def turbo(level):
 B.BPM=208; B.BEAT=60/208
 s=B.Song(f'star_{level}_turbo_swing',0,LENS['turbo_swing'][level-1])
 for ch,pr,pan in [(0,56,84),(1,57,27),(2,65,66),(3,3,42),(4,35,54),(5,61,58),(6,13,86)]:s.inst(ch,pr,pan)
 # Concert Bb fanfare with syncopated swing, five clearly differentiated ending signatures.
 melody={
 1:[(74,0,.24,107),(77,.55,.23,111),(82,1.43,.78,121)],
 2:[(77,0,.21,106),(82,.45,.25,112),(79,1.10,.19,105),(86,1.52,.29,116),(89,2.73,.88,124)],
 3:[(70,0,.24,108),(74,.37,.22,108),(77,.79,.23,114),(82,1.26,.38,121),(86,2.05,.22,109),
    (82,2.48,.22,112),(89,3.02,.46,124),(86,4.00,.21,107),(82,4.52,.23,115),(94,5.45,.77,125)],
 4:[(70,0,.22,106),(74,.32,.21,110),(77,.65,.26,118),(82,1.08,.41,123),
    (79,1.83,.24,106),(82,2.21,.24,112),(86,2.66,.36,124),(89,3.24,.48,127),
    (86,4.12,.24,113),(89,4.51,.23,116),(91,5.00,.35,127),
    (94,5.67,.38,127),(89,6.44,.21,113),(91,6.76,.24,117),(94,7.16,.27,123),
    (98,7.72,.48,127),(94,8.62,.24,118),(101,9.18,.85,127)]}
 notes(s,0,melody[level]); last=melody[level][-1][1]+melody[level][-1][2]
 # Lower brass calls and sax responses, bass walk
 if level>=2:notes(s,2,[(70,.05,.3,84),(74,1.12,.24,84),(77,2.73,.53,96)])
 if level>=3:
  notes(s,1,[(58,0,.24,89),(62,.80,.27,91),(65,2.05,.25,98),(70,3.03,.5,112),(74,5.46,.46,109)])
  notes(s,5,[(70,.77,.20,78),(77,2.69,.20,81),(82,5.43,.35,92)])
 if level==4:
  notes(s,2,[(74,3.23,.3,102),(77,5.01,.29,98),(82,7.72,.42,108),(89,9.18,.73,115)])
  notes(s,5,[(77,3.22,.2,93),(82,5.67,.21,90),(86,7.73,.30,100),(89,9.18,.65,112)])
 chord_seq=[([58,62,65,70],0,.36,84),([60,65,69,72],1.07,.31,87),([62,65,70,74],2.65,.36,92),
            ([58,62,65,70],3.23,.44,102),([55,59,62,67],4.12,.29,83),([60,65,69,72],5.00,.36,98),
            ([58,62,65,70],7.71,.42,108),([58,62,65,70,74],9.18,.85,119)]
 chords(s,3,[q for q in chord_seq if q[1]<=last+.05])
 bass=[46,50,53,50,43,50,46,53,43,50,46,53]
 for i,t in enumerate(np.arange(0,last,.83)):s.n(4,bass[i%len(bass)],float(t),.34,79+level*2)
 # fills with increasingly dramatic pace
 for t in np.arange(0,last,.5):s.drum(42,float(t),51+level*2,.07)
 for t in np.arange(0,last,1.0):s.drum(36,float(t),79+level*4,.09)
 for t in np.arange(.5,last,1.0):s.drum(38,float(t),84+level*3,.10)
 if level>=3:
  for t in np.arange(.25,last,.5):s.drum(44,float(t),44+level*2,.06)
 if level==4:
  for i,t in enumerate([8.36,8.52,8.67,8.81,8.96]):s.drum(38,t,60+i*12,.08)
  s.drum(49,9.18,121,.7);s.drum(57,9.46,78,.18)
 else:s.drum(49,max(0,last-.12),75+level*7,.25)
 return s

def champion(level):
 B.BPM=188; B.BEAT=60/188
 s=B.Song(f'star_{level}_champion_fanfare',0,LENS['champion_fanfare'][level-1])
 for ch,pr,pan in [(0,56,78),(1,61,30),(2,60,64),(3,48,53),(4,47,54),(5,32,53),(6,11,84),(7,57,44)]:s.inst(ch,pr,pan)
 # C major triumphant fanfare with three-note salutes, octave jumps, heroic fourth-star coda.
 melody={
 1:[(76,0,.32,109),(79,.61,.28,111),(84,1.45,.92,122)],
 2:[(79,0,.25,102),(84,.58,.25,114),(88,1.23,.32,118),(86,2.16,.2,108),(91,3.1,.84,124)],
 3:[(72,0,.22,107),(79,.47,.28,113),(84,.91,.30,119),(88,1.55,.46,123),
    (84,2.33,.22,112),(88,2.80,.23,117),(91,3.28,.53,126),(88,4.20,.22,113),(96,5.46,.92,127)],
 4:[(72,0,.22,109),(76,.36,.25,113),(79,.73,.28,120),(84,1.18,.42,127),
    (88,1.93,.25,118),(91,2.31,.22,124),(88,2.76,.26,112),(96,3.21,.52,127),
    (91,4.16,.24,120),(88,4.54,.21,109),(91,4.91,.27,122),(96,5.34,.41,127),
    (95,6.10,.30,115),(96,6.60,.31,121),(100,7.19,.40,127),
    (103,7.89,.57,127),(100,8.85,.28,119),(96,9.39,.28,121),(108,10.03,.95,127)]}
 notes(s,0,melody[level]);last=melody[level][-1][1]+melody[level][-1][2]
 if level>=2:notes(s,2,[(64,0,.32,79),(67,1.2,.35,86),(72,3.10,.65,98)])
 if level>=3:
  notes(s,1,[(60,.0,.26,88),(67,.91,.29,98),(72,3.30,.45,110),(79,5.46,.80,116)])
  notes(s,6,[(88,1.55,.21,74),(91,3.25,.28,78),(96,5.45,.65,91)])
 if level==4:
  notes(s,7,[(67,1.18,.33,82),(72,3.22,.42,98),(76,5.34,.35,94),(79,7.90,.48,105),(84,10.03,.9,114)])
  notes(s,2,[(72,3.23,.44,87),(76,5.34,.25,95),(79,7.89,.50,101),(84,10.03,.91,118)])
 chords(s,3,[([48,52,55,60],0,.34,70),([53,57,60,65],1.2,.31,79),([55,59,62,67],3.20,.42,89),
              ([48,52,55,60],5.37,.4,99),([53,57,60,65],7.88,.5,110),([48,52,55,60,64],10.03,.95,120)][: (6 if level==4 else 4 if level==3 else 3 if level==2 else 1)])
 # dynamic pedal bass & timpani-like tom rolls
 for i,t in enumerate(np.arange(0,last,.87)):
  s.n(5,[36,43,40,43,41,48,43,48][i%8],float(t),.42,75+level*3)
 for t in np.arange(0,last,1):s.drum(36,float(t),85+level*3,.12)
 for t in np.arange(.50,last,1):s.drum(38,float(t),83+level*3,.12)
 for t in np.arange(0,last,.5):s.drum(42,float(t),45+level*3,.09)
 if level>=3:
  for t in np.arange(.25,last,.5):s.drum(47,float(t),61+level*3,.10)
 if level==4:
  for i,t in enumerate(np.linspace(9.12,9.98,7)):s.drum(38,float(t),47+i*11,.075)
  s.drum(49,10.03,126,.72);s.drum(57,10.36,84,.15)
 else:s.drum(49,max(0,last-.3),76+level*6,.28)
 return s

def kitchen(level):
 B.BPM=214; B.BEAT=60/214
 s=B.Song(f'star_{level}_kitchen_rock',0,LENS['kitchen_rock'][level-1])
 for ch,pr,pan in [(0,30,80),(1,81,31),(2,29,40),(3,62,74),(4,34,50),(5,10,84),(6,56,67)]:s.inst(ch,pr,pan)
 # A major stadium-rock-meets-cartoon synthesizer; each tier with own motif/cadence.
 melody={
 1:[(76,0,.23,107),(81,.53,.25,113),(85,1.50,.81,123)],
 2:[(73,0,.25,106),(76,.48,.23,111),(81,.96,.28,116),(78,1.79,.21,107),(88,2.73,.83,125)],
 3:[(69,0,.22,108),(76,.45,.23,114),(81,.94,.34,121),(85,1.56,.26,114),
    (88,2.10,.41,126),(85,3.04,.20,113),(81,3.46,.21,111),(88,4.05,.25,123),(93,5.28,.85,127)],
 4:[(69,0,.20,110),(73,.36,.20,114),(76,.72,.22,119),(81,1.18,.40,127),
    (78,2.0,.22,109),(81,2.40,.22,112),(85,2.86,.30,123),(88,3.37,.39,127),
    (85,4.2,.18,110),(88,4.55,.25,123),(93,5.05,.39,127),
    (90,5.87,.21,113),(93,6.25,.22,117),(97,6.73,.38,127),(100,7.50,.35,127),
    (97,8.31,.18,117),(93,8.64,.22,119),(100,9.04,.24,126),(105,9.65,.9,127)]}
 notes(s,1,melody[level]);last=melody[level][-1][1]+melody[level][-1][2]
 # Overdriven guitar riff, octave power chords, staccato offbeats.
 riffs=[([45,52,57],0,.25,93),([47,54,59],1.17,.23,88),([50,57,62],2.86,.29,102),
        ([45,52,57],3.37,.34,110),([47,54,59],5.04,.36,108),([52,59,64],7.50,.40,118),([45,52,57,64],9.65,.91,124)]
 chords(s,0,[r for r in riffs if r[1]<last+.1])
 if level>=2:notes(s,3,[(73,.48,.25,78),(76,1.79,.21,88),(81,2.73,.48,102)])
 if level>=3:
  notes(s,2,[(57,0,.22,83),(64,.94,.22,87),(69,2.10,.38,98),(76,4.05,.28,105),(81,5.28,.60,113)])
  notes(s,6,[(76,1.56,.19,82),(81,3.45,.21,93),(85,5.28,.41,99)])
 if level==4:
  notes(s,3,[(81,3.37,.33,100),(85,5.05,.32,103),(88,7.50,.32,110),(93,9.65,.82,118)])
  notes(s,6,[(81,3.38,.33,90),(85,6.72,.28,103),(93,9.65,.81,114)])
 for i,t in enumerate(np.arange(0,last,.69)):s.n(4,[45,52,47,52,50,57,52,57][i%8],float(t),.25,83+level*2)
 for t in np.arange(0,last,.5):s.drum(42,float(t),60+level*2,.07)
 for t in np.arange(0,last,1):s.drum(36,float(t),89+level*3,.11)
 for t in np.arange(.5,last,1):s.drum(38,float(t),89+level*3,.13)
 if level>=3:
  for t in np.arange(.25,last,.5):s.drum(46,float(t),53+level*2,.08)
 if level==4:
  for i,t in enumerate([8.99,9.13,9.26,9.39,9.51]):s.drum(38,t,62+i*10,.075)
  s.drum(49,9.65,125,.75)
 else:s.drum(49,max(.1,last-.14),74+level*5,.27)
 return s

def postprocess(a,level):
 x=B.filter_sos(a,55,'highpass',2)
 x=B.filter_sos(x,15700,'lowpass',2)
 # enhance broad stereo spread without over-long delay
 d=int(.044*SR);y=x.copy();y[d:,0]+=x[:-d,1]*.047;y[d:,1]+=x[:-d,0]*.047
 n=len(y); idx=np.arange(n)
 env=np.minimum(1,idx/(.009*SR))*np.minimum(1,(n-idx)/((.13 if level==4 else .085)*SR))
 y*=np.clip(env,0,1)[:,None]
 rms=float(np.sqrt(np.mean(y*y)))
 target=[.098,.112,.124,.132][level-1]
 if rms>1e-8:y*=min(target/rms,5)
 y=np.tanh(y*1.1)
 peak=float(np.max(abs(y)))
 if peak>.81:y*=.81/peak
 return np.clip(y,-1,1).astype(np.float32)

def wav(path,x):
 path.parent.mkdir(parents=True,exist_ok=True)
 sf.write(path,x,SR,subtype='PCM_16')

def ogg(wavfile,oggfile):
 oggfile.parent.mkdir(parents=True,exist_ok=True)
 subprocess.run(['ffmpeg','-hide_banner','-loglevel','error','-nostdin','-y','-i',str(wavfile),'-c:a','libvorbis','-q:a','6',str(oggfile)],check=True)

def html(config):
 # Fully offline, clickable selectors including audio preview. No commercial recordings or effects.
 jsconfig=json.dumps(config,ensure_ascii=False,separators=(',',':'))
 return '''<!doctype html><html lang="zh-CN"><head><meta charset="UTF-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Overcooked2-Haptics 超燃庆祝音效试听</title><style>
*{box-sizing:border-box}body{background:#10141e;color:#edf2f9;font:15px/1.65 system-ui,'Microsoft YaHei',sans-serif;margin:0}.wrap{max-width:1050px;margin:30px auto;padding:0 20px}.muted{color:#a2aec3}h1{font-size:29px;margin:6px 0}h2{font-size:20px;margin-top:0}.themes{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:13px}.card,.settings{background:#1b2332;border:1px solid #323e52;border-radius:14px;padding:18px;box-shadow:0 5px 18px #0002}.card p{color:#b9c5d6;font-size:13px}.card button,.button{background:#435dd0;border:0;color:#fff;border-radius:9px;padding:9px 13px;font-weight:700;cursor:pointer}.card button:hover,.button:hover{filter:brightness(1.1)}.stargrid{display:grid;grid-template-columns:repeat(2,1fr);gap:12px}.row{display:flex;gap:10px;align-items:center}.row select{flex:1}.row>*{min-width:0}select{background:#101726;color:#eff4ff;border:1px solid #44546b;border-radius:8px;padding:9px;max-width:100%}label{display:flex;gap:10px;align-items:center}section{margin-top:24px}.star{color:#f4bd51}.note{background:#24364b;padding:14px;border-radius:10px;color:#c8d6ea;margin-top:18px}a{color:#a2c1ff}@media(max-width:730px){.themes,.stargrid{grid-template-columns:1fr}} </style></head><body><main class="wrap"><p style="letter-spacing:2px;color:#96aaff">OVERCOOKED2-HAPTICS · ULTRA VICTORY v3.0</p><h1>超燃星级音乐试听与自定义</h1><p class="muted">三套全新高能短曲 · 12 段原创音乐 · 1–4 星独立选曲 · 48kHz WAV/OGG · 可导出配置</p><section><h2>选择整套风格</h2><div class="themes" id="themes"></div></section><section><h2>按星级分别选择音乐</h2><div class="stargrid" id="stars"></div></section><section class="settings"><h2>播放设置</h2><label>试听音量 <input type="range" id="vol" min="0" max="100" step="5" value="65"/><strong id="vl">65%</strong></label><p class="muted">4 星音乐默认<strong>不包含合成欢呼与掌声</strong>。真人掌声素材建议按 README 的 CC0 来源另行添加，避免旧版失真的合成拟声。</p><div class="row"><button class="button" id="stop">停止播放</button><button class="button" id="exp">导出 JSON 配置</button></div><p id="status" class="muted" role="status"></p></section><p class="note">请先完整解压压缩包，再打开本页面。这里只负责试听与导出设置；MOD 需要 Codex 接入音频加载与星级事件。原创旋律；乐器渲染使用 TimGM6mb（详见 licenses）。</p></main><script>
const DATA=__JSON__;
const NAMES=Object.keys(DATA.presets),CHOICE={1:NAMES[0],2:NAMES[0],3:NAMES[0],4:NAMES[0]};let a=null;
const $=id=>document.getElementById(id);const emoji=['','⭐','⭐⭐','⭐⭐⭐','⭐⭐⭐⭐'];
function stop(){if(a){a.pause();a.currentTime=0;a=null}};
function preview(n){stop();const o=DATA.presets[CHOICE[n]].stars[n];a=new Audio(encodeURI(o.ogg));a.volume=+$('vol').value/100;a.play().catch(()=>{$('status').textContent='播放失败，请确认 ZIP 已完整解压。'});$('status').textContent='播放中：'+n+' 星 / '+DATA.presets[CHOICE[n]].name;}
function draw(){ $('themes').innerHTML=NAMES.map(k=>`<article class="card"><b>${DATA.presets[k].name}</b><p>${DATA.presets[k].desc}</p><button data-preset="${k}">整套使用</button><button data-preview="${k}" style="background:#2e425f;margin-left:6px">听 1–4 星</button></article>`).join('');$('stars').innerHTML=[1,2,3,4].map(n=>`<article class="card"><h2><span class="star">${emoji[n]}</span> ${n} 星</h2><div class="row"><select id="s${n}">${NAMES.map(k=>`<option value="${k}" ${CHOICE[n]===k?'selected':''}>${DATA.presets[k].name}</option>`).join('')}</select><button data-star="${n}">▶ 播放</button></div><p>约 ${DATA.presets[CHOICE[n]].stars[n].duration}s</p></article>`).join('');document.querySelectorAll('[data-star]').forEach(el=>el.onclick=()=>preview(+el.dataset.star));for(let n=1;n<=4;n++)$('s'+n).onchange=e=>{CHOICE[n]=e.target.value;draw()};document.querySelectorAll('[data-preset]').forEach(el=>el.onclick=()=>{for(let n=1;n<=4;n++)CHOICE[n]=el.dataset.preset;draw();$('status').textContent='已选择 '+DATA.presets[el.dataset.preset].name});document.querySelectorAll('[data-preview]').forEach(el=>el.onclick=()=>{stop();const k=el.dataset.preview;a=new Audio(encodeURI(DATA.presets[k].preview));a.volume=+$('vol').value/100;a.play();$('status').textContent='串联试听 '+DATA.presets[k].name});}
$('stop').onclick=stop;$('vol').oninput=()=>{$('vl').textContent=$('vol').value+'%';if(a)a.volume=+$('vol').value/100};$('exp').onclick=()=>{const j={schemaVersion:2,pack:'Overcooked2_Haptics_Ultra_Victory_v3',defaultVolume:+$('vol').value/100,starTracks:Object.fromEntries([1,2,3,4].map(n=>{let x=DATA.presets[CHOICE[n]].stars[n];return [n,{style:CHOICE[n],wav:x.wav,ogg:x.ogg}]})),enableCheer:false,enableApplause:false,comment:'True CC0 recorded cheers may be added as optional user-provided files; not included in v3 music clips.'};let u=URL.createObjectURL(new Blob([JSON.stringify(j,null,2)],{type:'application/json'}));let l=document.createElement('a');l.href=u;l.download='overcooked2_haptics_ultra_selection.json';l.click();setTimeout(()=>URL.revokeObjectURL(u),1200);$('status').textContent='已导出 JSON，交给 Codex 读取';};draw();</script></body></html>'''.replace('__JSON__',jsconfig)

def main():
 OUT.mkdir(parents=True,exist_ok=True)
 (OUT/'docs').mkdir(exist_ok=True)
 config={'pack':'Overcooked2-Haptics Ultra Victory Music Expansion','version':'3.0','description':'12 additional original ultra-energetic star victory jingles','defaultVolume':.65,'effectsBundled':False,'presets':{},'rules':{'sameFrameStar':'highest_only','starVsCombo':'star_priority','emergency':'preempt_or_duck','resetOnLevelRestart':True}}
 for sty,fn in [('turbo_swing',turbo),('champion_fanfare',champion),('kitchen_rock',kitchen)]:
  config['presets'][sty]={'name':INFO[sty]['zh'],'nameEn':INFO[sty]['en'],'desc':INFO[sty]['desc'],'bpm':INFO[sty]['bpm'],'stars':{}}
  tracks=[]
  for lvl in range(1,5):
   song=fn(lvl)
   fs=B.FSynth();raw=fs.render(song);fs.close()
   music=postprocess(raw,lvl)
   base=OUT/'presets'/sty
   name=song.name
   midi=base/'midi'/f'{name}.mid';midi.parent.mkdir(parents=True,exist_ok=True);song.export_midi(str(midi))
   w=base/'wav'/f'{name}.wav';wav(w,music)
   o=base/'ogg'/f'{name}.ogg';ogg(w,o)
   info={'wav':w.relative_to(OUT).as_posix(),'ogg':o.relative_to(OUT).as_posix(),'midi':midi.relative_to(OUT).as_posix(),'duration':round(len(music)/SR,3),'containsCheerApplause':False}
   config['presets'][sty]['stars'][str(lvl)]=info; tracks.append(music)
   print(sty,lvl,info['duration'],round(20*np.log10(max(1e-6,float(np.max(np.abs(music))))),2),flush=True)
  sil=np.zeros((int(.48*SR),2),dtype=np.float32)
  preview=np.concatenate([q for i,t in enumerate(tracks) for q in ([t,sil] if i<3 else [t])])
  preview_path=OUT/'previews'/f'{sty}_stars_1_to_4.wav';wav(preview_path,preview)
  config['presets'][sty]['preview']=preview_path.relative_to(OUT).as_posix()
 cfg=OUT/'config'/'ultra_star_music_config.json';cfg.parent.mkdir(exist_ok=True);cfg.write_text(json.dumps(config,ensure_ascii=False,indent=2)+'\n','utf-8')
 (OUT/'试听与风格选择.html').write_text(html(config),encoding='utf-8')
 readme='''# Overcooked2-Haptics · 超燃星级音乐扩展 v3.0

这是 **全新额外音乐包**，不是 v2 原音乐包的替换品，也不是已加载进游戏的 EXE/DLL。

## 新增三套，每套四首，共 12 首

- **超燃卡通大乐队 (turbo_swing, 208 BPM)**：明亮铜管、急促爵士鼓和萨克斯回应；最接近《Fancy Day》欢乐方向但更燃。
- **冠军加冕号角 (champion_fanfare, 188 BPM)**：大铜管胜利号角、鼓点与强烈收尾；4 星尤其适合夺冠氛围。
- **厨房摇滚派对 (kitchen_rock, 214 BPM)**：电吉他、合成主旋律、摇滚鼓组；更现代，适合另类风格。

各套 1 星轻快、2 星热闹、3 星明显胜利、4 星最高档激昂，不是同一首简单提速。所有音乐都是本次创作的原创 MIDI 乐句与编排，通过 TimGM6mb 音色采样库渲染而成。不是市场歌曲截取，也没有声称为真人乐队实录。

## 使用

1. 解压 ZIP；打开 `试听与风格选择.html` 试听三套风格及各星级。
2. 点击“整套使用”或各星级独立选择，导出 JSON。
3. 将本包、JSON 与 `docs/CODEX_INTEGRATION.md` 交给 Codex，让现有 MOD 加载。不能仅将 ZIP 放入游戏目录就直接生效。
4. 预览音量默认 65%，真实 MOD 应保留玩家独立音量。

- `presets/<style>/wav/*.wav`：48kHz / 16bit / stereo / PCM
- `presets/<style>/ogg/*.ogg`：OGG Vorbis，可供 Unity MOD 加载
- `presets/<style>/midi/*.mid`：原始编曲，供更换音色、改谱
- `previews/`：每风格 1～4 星连续试听
- `config/ultra_star_music_config.json`：游戏插件接入映射数据

## 欢呼和掌声：关于 v2 反馈

**v2 的合成拟声在听感上不够自然，因此 v3 音乐片段没有继续混入那段效果。** 请优先使用真正录制、授权明确的短人群录音，分别对 cheer / applause 混音，和第 4 星音乐保持可独立调音和开关。

两条已查到 CC0 的真实录音（未混入本包，需打开页面获取授权文件）：

- Freesound Nox_Sound：Small Crowd Cheer and Applause，CC0  https://freesound.org/people/Nox_Sound/sounds/752709/
- Freesound Nox_Sound：Small Crowd Applause，CC0  https://freesound.org/people/Nox_Sound/sounds/752710/
- Freesound Sadiquecat：Applause Edited 1，CC0  https://freesound.org/people/Sadiquecat/sounds/777707/

请 **不要把这三条音乐文件当成已经随包附送**。不从未知来源拼凑真人录音。推荐选择 0.4～0.8 秒真实声音，音乐尾部 duck 约 3dB，欢呼与掌声各比音乐低 10～16dB，最终耳朵试听后调整。

## 商业音乐的版权

免费开源不意味着能无授权分发已有歌曲。本包没有附带未经授权的商业音乐。若以后购买了特定歌曲/录音的同步、复制、改编及软件分发所需权利，MOD 可通过外部音效包加载，保留授权凭证；用户本地选曲与项目官方发布区分处理。

## SoundFont 与版权

本项目编写了原创乐句/MIDI，但渲染时使用 Debian 的 `TimGM6mb.sf2` SoundFont。其包内声明的许可证为 GPL-2.0，**这不等于 CC0 或完全无约束**。为了可追溯性，本包保留相应 GPL-2.0 文本、Debian 版权说明；发布到 GitHub 时保留。有关该音色的录音输出究竟适用哪些条款应进一步核实，不能把本包误说成没有任何版权限制的商用成品。若希望完全规避这类不确定性，应采用许可边界清楚的 CC0 乐器音色重新渲染 MIDI。

## 质量边界

所有文件进行 WAV、OGG、MIDI 与 CRC 校验，检查时长、声道、峰值和配置路径。但还没有真人试听盲测或游戏内实际体验验证，不能把技术检查当作完成音质验收。
'''
 (OUT/'README_使用说明.md').write_text(readme,'utf-8')
 (OUT/'docs'/'CODEX_INTEGRATION.md').write_text('''# Codex 接入任务 · Ultra Victory v3

1. 不修改既有游戏事件/订单/Combo 判定。先核对现有工程、配置格式及 Unity AudioSource 播放器。
2. 实现 `MusicPresets` 多套预设，分别把 1–4 星映射到 `config/ultra_star_music_config.json` 的 OGG 路径，支持混搭。
3. 允许配置文件映射 v2 或 v3 的星级音频；通过相对路径加载，预加载/缓存音频避免首次播放卡顿。默认音量 0.65。
4. 四星只有音乐文件；不混入 v2 的模拟欢呼/掌声。真实录音作为可选 `CheerPath` 和 `ApplausePath`，路径为空时仅播放音乐。
5. 已验证的分数跨门槛才播放；单次上菜跨多星只播放最高等级；关卡重试重置。
6. 音乐到来时替代本次普通上菜与 Combo 震动；紧急危险警告拥有抢占权。暂停/退出关卡/模组卸载时停播。
7. 交付功能及单测/运行日志；没有真实游戏环境不可声称完成实机验收。
8. 保留与用户讨论更好听的旋律、分级时长和实际 4 星的音量关系；用户拥有最终方案决定权。
''','utf-8')
 base=Path('/mnt/data/Overcooked2_Haptics_Music_Style_Pack_v2.0/licenses')
 for x in ['GPL-2.0.txt','TimGM6mb_Debian_copyright.txt']:
  shutil.copy2(base/x,OUT/'licenses'/x) if (OUT/'licenses').exists() else ((OUT/'licenses').mkdir(parents=True),shutil.copy2(base/x,OUT/'licenses'/x))
 (OUT/'source').mkdir(exist_ok=True)
 shutil.copy2(__file__,OUT/'source'/'build_oc2_ultra_v3.py')
 # Validate all audio entries, MIDI, matching samples and never digitally clipped
 stats=[]
 for sty,rec in config['presets'].items():
  for level,p in rec['stars'].items():
   for k in ['wav','ogg','midi']:assert (OUT/p[k]).is_file(),(sty,level,k)
   a,rate=sf.read(OUT/p['wav'],dtype='float32',always_2d=True)
   b,br=sf.read(OUT/p['ogg'],dtype='float32',always_2d=True)
   assert rate==br==48000 and a.shape[1]==b.shape[1]==2
   assert abs(len(a)-len(b))<2048 and np.max(np.abs(a))<.99
   stats.append({'preset':sty,'star':int(level),'sec':round(len(a)/SR,3),'peak':round(float(np.max(abs(a))),4)})
 (OUT/'docs'/'validation.json').write_text(json.dumps(stats,ensure_ascii=False,indent=2),'utf-8')
 zp=Path('/mnt/data/Overcooked2_Haptics_Ultra_Victory_v3.zip')
 with zipfile.ZipFile(zp,'w',compression=zipfile.ZIP_DEFLATED,compresslevel=6) as z:
  for p in OUT.rglob('*'):
   if p.is_file():z.write(p,p.relative_to(OUT.parent))
 with zipfile.ZipFile(zp) as z:assert z.testzip() is None
 print('PACK',zp,'BYTES',zp.stat().st_size,'FILES',len(stats),'ZIP_MEMBERS',len(z.namelist()))

if __name__=='__main__':main()
