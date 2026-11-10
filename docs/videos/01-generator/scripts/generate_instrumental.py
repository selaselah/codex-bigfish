"""Reproduction helper derived from compose_original_audio.py; identical music, no legacy vocal input."""
from pathlib import Path
import json,wave,math
import numpy as np
w=Path('work/demo-video-190');sr=48000;duration=50.;n=int(sr*duration);rng=np.random.default_rng(190)
music=np.zeros((n,2),np.float32);vocal=np.zeros((n,2),np.float32)
def add(track,sig,start,gain=1,pan=0):
 i=round(start*sr);j=max(0,-i);i=max(i,0);m=min(len(sig)-j,n-i)
 if m<=0:return
 g=np.array([math.sqrt((1-pan)*.5),math.sqrt((1+pan)*.5)])*gain
 track[i:i+m]+=sig[j:j+m,None]*g

def midi(note):return 440*2**((note-69)/12)
def pluck(note,seconds=.6,soft=False):
 t=np.arange(round(seconds*sr))/sr;f=midi(note);x=np.zeros(len(t))
 for h in range(1,6 if soft else 9):x+=np.sin(2*np.pi*f*h*t+(.03*h))*np.exp(-t*(7 if soft else 11)*(1+h*.12))/(h**(1.8 if soft else 1.2))
 return (x*np.minimum(t/.006,1)*np.minimum((seconds-t)/.025,1)).astype(np.float32)
def bell(note):
 t=np.arange(round(.65*sr))/sr;f=midi(note);return ((np.sin(2*np.pi*f*t)*np.exp(-t*8)+.25*np.sin(2*np.pi*f*2.76*t)*np.exp(-t*14))*np.minimum(t/.005,1)).astype(np.float32)
beat=np.pi/4.4;phase=((math.ceil((100*4.4-math.pi/2)/(2*math.pi))*2*math.pi+math.pi/2)/4.4)-100
chords=[[60,64,67,71],[57,60,64,67],[53,57,60,64],[55,59,62,67]]
for k in range(-2,math.ceil(duration/beat)+2):
 start=phase+k*beat
 if start<0:continue
 chord=chords[(k//8)%4]
 if k%2==0:
  for j,note in enumerate(chord):add(music,pluck(note,.5,True),start+j*.018,.050,(-.25+j*.16))
  add(music,pluck(chord[0]-12,.4,True),start,.085,-.08)
 melody=[0,2,1,3,2,1,0,2][k%8];add(music,bell(chord[melody]+12),start+beat*.5,.037,.30 if k%2 else -.3)
 t=np.arange(int(.11*sr))/sr;tap=rng.normal(0,1,len(t))*np.exp(-t*55)*np.sin(np.pi*np.minimum(t/.003,1))
 add(music,tap,start,.008,.35 if k%2 else -.35)
 if k%2==0:
  t=np.arange(int(.14*sr))/sr;kick=np.sin(2*np.pi*(95*t-110*t*t))*np.exp(-t*32);add(music,kick,start,.045,0)
# Light completion pop and a rising toy-bell flourish.
t=np.arange(int(.2*sr))/sr;pop=(np.sin(2*np.pi*(280*t-450*t*t))*.7+rng.normal(0,1,len(t))*.10)*np.exp(-t*30)*np.minimum(t/.004,1)
add(music,pop,37.14,.17,-.35)
for j,note in enumerate([76,79,84]):add(music,bell(note),37.28+j*.09,.055,-.1+j*.1)
add(music,bell(84),38.06,.045,.1)
w.mkdir(parents=True,exist_ok=True);(w/'audio').mkdir(exist_ok=True)
with wave.open(str(w/'audio/instrumental.wav'),'wb') as f:
 f.setnchannels(2);f.setsampwidth(2);f.setframerate(sr);f.writeframes((np.clip(music,-.999,.999)*32767).astype('<i2').tobytes())
print(w/'audio/instrumental.wav')
