from pathlib import Path
import csv,json,math,wave
import numpy as np
w=Path('work/demo-voice-191');sr=48000;duration=50;n=int(sr*duration)
def read(p):
 with wave.open(str(p),'rb') as f:
  assert f.getframerate()==sr;channels=f.getnchannels();x=np.frombuffer(f.readframes(f.getnframes()),dtype='<i2').astype(np.float32)/32768
  return x.reshape((-1,channels))
source=read(w/'audio/natural-young.wav')[:,0];meta=json.loads((w/'audio/natural-young.json').read_text());events=meta['events'];assert len(events)==8
source*=.90/max(np.max(np.abs(source)),.01)
clips=[];descriptions=[]
for i,e in enumerate(events):
 a=max(0,round((e['offset']/1e7-.045)*sr));b=min(len(source),round((e['offset']/1e7+e['duration']/1e7+.065)*sr));x=source[a:b].copy()
 edge=min(300,len(x)//6);x[:edge]*=np.linspace(0,1,edge);x[-edge:]*=np.linspace(1,0,edge)
 window=round(sr*.012);energy=np.concatenate(([0.],np.cumsum(x.astype(np.float64)**2)));rms=np.sqrt((energy[window:]-energy[:-window])/window)
 accent=(int(np.argmax(rms))+window//2)/sr
 clips.append((x,accent));descriptions.append({'word':e['text'],'context':i//2,'duration':len(x)/sr,'accent_from_clip_start':accent})
motion=list(csv.DictReader((w/'motion-timing.csv').open()));times=np.array([float(x['time']) for x in motion]);weights=np.array([float(x['workWeight']) for x in motion]);phases=np.unwrap(np.array([float(x['crankPhase']) for x in motion]));assert len(times)==1500
vocal=np.zeros((n,2),np.float32);scheduled=[];beat=math.pi/4.4;last=-100;pair=0;selected=0;order=[0,2,1,3]
for half in range(math.ceil((100*4.4-math.pi/2)/math.pi),math.floor((150*4.4-math.pi/2)/math.pi)+1):
 target=(math.pi/2+half*math.pi)/4.4-100;word=half%2;weight=float(np.interp(target,times,weights))
 if not(7<=target<37 or 39.4<target<49.6) or weight<.90:continue
 if target-last>beat*1.6 and word==1:continue
 if word==0:selected=order[pair%4];pair+=1
 x,accent=clips[selected*2+word];start=target-accent;i=round(start*sr);end=min(n,i+len(x));length=end-i
 gain=[.54,.50,.57,.52][selected];pan=-.025 if word==0 else .025;g=np.array([math.sqrt((1-pan)*.5),math.sqrt((1+pan)*.5)])*gain
 if start<37 and start+length/sr>37:
  cut=max(0,round((37-start)*sr));length=min(length,cut)
 y=x[:length].copy();edge=min(400,len(y)//5);y[-edge:]*=np.linspace(1,0,edge)
 vocal[i:i+length]+=y[:,None]*g;last=target
 measured_phase=float(np.interp(target,times,phases));desired=math.pi/2+half*math.pi;delta=math.atan2(math.sin(measured_phase-desired),math.cos(measured_phase-desired))
 scheduled.append({'syllable':'嘿' if word==0 else '哟','variant':selected,'start_seconds':start,'accent_seconds':target,'work_weight':weight,'crank_phase_error_degrees':abs(math.degrees(delta)),'clip_duration':length/sr})
assert scheduled and all(s['work_weight']>=.9 for s in scheduled)
assert all(not(37<=s['accent_seconds']<39.4) for s in scheduled)
assert max(s['crank_phase_error_degrees'] for s in scheduled)<.01
instrumental=read(Path('work/demo-video-190/audio/instrumental.wav'));assert len(instrumental)==n
music=instrumental*.80;mix=music+vocal;mix-=mix.mean(axis=0)
fade=np.minimum(np.arange(n)/sr/.35,1)*np.minimum((duration-np.arange(n)/sr)/1.25,1);mix*=fade[:,None]
peak=float(np.max(np.abs(mix)));assert peak<.95
for name,data in [('soundtrack.wav',mix),('voice-only.wav',vocal)]:
 with wave.open(str(w/'audio'/name),'wb') as f:f.setnchannels(2);f.setsampwidth(2);f.setframerate(sr);f.writeframes((np.clip(data,-.999,.999)*32767).astype('<i2').tobytes())
report={'voice_model':'en-US-AnaNeural','voice_category':'Female; Cartoon, Conversation; Cute (service inventory)','source_type':'Stock synthetic youthful feminine voice; no real person cloned; no post-generation pitch shifting','phonetic_lyrics':'Hey / yo, matching 嘿 / 哟','source_context_variants':4,'selected_syllables':len(scheduled),'tempo_bpm':60/beat,'max_crank_phase_error_degrees':max(s['crank_phase_error_degrees'] for s in scheduled),'workWeight_min':min(s['work_weight'] for s in scheduled),'idle_and_cheer_no_chant':True,'mix_peak':peak,'sr':sr,'duration':duration,'clips':descriptions,'schedule':scheduled}
(w/'voice-sync-validation.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print({k:v for k,v in report.items() if k not in ['clips','schedule']})
