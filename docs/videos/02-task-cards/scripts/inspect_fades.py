from pathlib import Path
import json,subprocess,hashlib
from PIL import Image,ImageDraw,ImageFont
r=Path('/Users/chendx/mydev/codex-bigfish');w=r/'work/demo-native-211'
v=r/'outputs/BigFish-Task-Demo-211/BigFish-Task-Demo-1080.mp4'
steps=[23.966667,24,24.233333,24.5,26,28.5,28.733333,28.966667,29,29.2,30,32]
sheet=Image.new('RGB',(1200,990),'white');font=ImageFont.truetype('/System/Library/Fonts/STHeiti Medium.ttc',18)
for i,t in enumerate(steps):
 p=w/'encoded-samples'/f'fade{i:02d}.png'
 subprocess.run(['/opt/homebrew/bin/ffmpeg','-v','error','-y','-ss',str(t),'-i',str(v),'-frames:v','1',str(p)],check=True)
 x=(i%4)*300;y=(i//4)*330
 sheet.paste(Image.open(p).convert('RGB').resize((300,300)),(x,y+30));ImageDraw.Draw(sheet).text((x+8,y+3),f'{t:.3f}s',font=font,fill='black')
sheet.save(w/'Encoded-Fades.png')
audio_hashes={}
for version in [210,211]:
 p=w/f'encoded-audio{version}.aac'
 subprocess.run(['/opt/homebrew/bin/ffmpeg','-v','error','-y','-i',str(r/f'outputs/BigFish-Task-Demo-{version}/BigFish-Task-Demo-1080.mp4'),'-map','0:a','-c:a','copy','-f','adts',str(p)],check=True)
 audio_hashes[str(version)]=hashlib.sha256(p.read_bytes()).hexdigest()
assert audio_hashes['210']==audio_hashes['211']
(w/'encoded-audio-validation.json').write_text(json.dumps({'passed':True,'AAC_payload_same210':True,'sha256':audio_hashes},indent=2)+'\n')
print('12 encoded fade/order samples ready; encoded audio byteexact210')
