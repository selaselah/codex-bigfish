from pathlib import Path
import hashlib, json, subprocess
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
WORK = Path(__file__).resolve().parent
OUT = ROOT / 'outputs/CoFish-Demo-276'
OUT.mkdir(exist_ok=True)
FFMPEG = '/opt/homebrew/bin/ffmpeg'
FFPROBE = '/opt/homebrew/bin/ffprobe'
VIDEO = OUT / 'CoFish-Demo-1080.mp4'

def run(args, log):
    with (WORK / log).open('w') as f:
        subprocess.run(args, stdout=f, stderr=subprocess.STDOUT, check=True)

audit = json.loads((WORK / 'native-frame-audit.json').read_text())
assert len(audit) == 1800
assert [r['frame'] for r in audit] == list(range(1800))
assert all(r['nativeRegular'] for r in audit)
assert audit[1365]['expanded'] and audit[1366]['expanded'] and audit[1365]['cards'] == 2
assert audit[1080]['pending'] == 1 and audit[1200]['pending'] == 0
assert audit[1500]['jobs'] == 10 and audit[1560]['pending'] == 5
assert audit[1680]['jobs'] == 0 and audit[1680]['pending'] == 10
assert audit[360]['diagramOpacity'] == 0 and audit[375]['diagramOpacity'] == 1
assert audit[495]['diagramOpacity'] == 1 and audit[510]['diagramOpacity'] == 0
run([FFMPEG, '-y', '-hide_banner', '-loglevel', 'error', '-i', str(WORK/'video-only.mp4'),
     '-i', str(WORK/'audio/soundtrack.wav'), '-map', '0:v:0', '-map', '1:a:0',
     '-c:v', 'copy', '-c:a', 'aac', '-b:a', '192k', '-movflags', '+faststart', '-t', '60', str(VIDEO)], 'Mux.log')
probe = subprocess.run([FFPROBE, '-v', 'error', '-count_frames', '-show_streams', '-show_format', '-of', 'json', str(VIDEO)], capture_output=True, text=True, check=True)
details = json.loads(probe.stdout); (WORK/'FFprobe.json').write_text(probe.stdout)
v = next(x for x in details['streams'] if x['codec_type'] == 'video')
a = next(x for x in details['streams'] if x['codec_type'] == 'audio')
assert v['width'] == 1080 and v['height'] == 1080 and v['avg_frame_rate'] == '30/1'
assert int(v['nb_read_frames']) == 1800 and abs(float(details['format']['duration'])-60) < .001
assert a['codec_name'] == 'aac' and a['sample_rate'] == '48000' and a['channels'] == 2
run([FFMPEG, '-v', 'error', '-xerror', '-i', str(VIDEO), '-f', 'null', '-'], 'Full-Decode.log')
times = [0,4,8,11,15,18,19.2,20.5,23.2,25,26.5,29,32.5,36,38.5,40,44,46,47.5,50,52,55.8,57.5,59]
frames = OUT/'Frames'; frames.mkdir(exist_ok=True)
sheet = Image.new('RGB', (1440, 4*388), '#edf0f4'); draw = ImageDraw.Draw(sheet)
for i,t in enumerate(times):
    frame = frames/f'{t:05.1f}s.png'
    run([FFMPEG, '-y', '-v', 'error', '-ss', str(t), '-i', str(VIDEO), '-frames:v', '1', str(frame)], 'Extract.log')
    image = Image.open(frame).convert('RGB'); image.thumbnail((360,360))
    x=i%4*360; y=i//4*388
    if y+388>sheet.height:
        larger=Image.new('RGB',(1440,y+388),'#edf0f4');larger.paste(sheet,(0,0));sheet=larger;draw=ImageDraw.Draw(sheet)
    sheet.paste(image,(x,y));draw.text((x+10,y+365),f'{t:04.1f}s',fill='#1f2937')
sheet.save(OUT/'Storyboard.png')
Image.open(frames/'000.0s.png').save(OUT/'Poster.png')
report = dict(passed=True, video=str(VIDEO.relative_to(ROOT)), sha256=hashlib.sha256(VIDEO.read_bytes()).hexdigest(),
              bytes=VIDEO.stat().st_size, width=1080, height=1080, fps=30, frames=1800, duration=60,
              full_decode_clean=True, audio_codec='AAC192k stereo48k', encoded_probes=len(times),
              native_frames=len(audit), completed_click_preserves_expanded=True,
              original211_unchanged=hashlib.sha256((ROOT/'outputs/BigFish-Task-Demo-211/BigFish-Task-Demo-1080.mp4').read_bytes()).hexdigest()=='936c2cd3f186d47fa93a62440baeaed2634cc4da6e6b1ccceff40746e75a264b')
assert report['original211_unchanged']
(WORK/'Delivery-Validation.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
print(report)
