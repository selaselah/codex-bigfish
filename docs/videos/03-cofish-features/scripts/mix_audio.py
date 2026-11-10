from pathlib import Path
import hashlib, json, math, wave
import numpy as np

ROOT = Path(__file__).resolve().parents[2]
OUT = Path(__file__).resolve().parent
SR, DURATION = 48000, 60
N = SR * DURATION

def read(path):
    with wave.open(str(path), 'rb') as f:
        assert f.getframerate() == SR and f.getsampwidth() == 2
        return np.frombuffer(f.readframes(f.getnframes()), dtype='<i2').astype(np.float64).reshape(-1, f.getnchannels()) / 32768

def write(name, data):
    with wave.open(str(OUT / 'audio' / name), 'wb') as f:
        f.setnchannels(2); f.setsampwidth(2); f.setframerate(SR)
        f.writeframes((data * 32767).round().astype('<i2').tobytes())

(OUT / 'audio').mkdir(exist_ok=True)
old = ROOT / 'work/demo-voice-191'
source = read(old / 'audio/natural-young.wav')[:, 0]
source *= .90 / max(np.max(np.abs(source)), .01)
clips = []
for e in json.loads((old / 'audio/natural-young.json').read_text())['events']:
    a = max(0, round((e['offset'] / 1e7 - .045) * SR))
    b = min(len(source), round((e['offset'] / 1e7 + e['duration'] / 1e7 + .065) * SR))
    x = source[a:b].copy(); edge = min(300, len(x) // 6)
    x[:edge] *= np.linspace(0, 1, edge); x[-edge:] *= np.linspace(1, 0, edge)
    window = round(SR * .012)
    energy = np.concatenate(([0.], np.cumsum(x ** 2)))
    rms = np.sqrt((energy[window:] - energy[:-window]) / window)
    accent = (int(np.argmax(rms)) + window // 2) / SR
    clips.append((x, accent))

meta = [json.loads(line) for line in (OUT / 'metadata.jsonl').read_text().splitlines()]
assert len(meta) == 1800
times = np.array([r['time'] for r in meta]); weights = np.array([r['weight'] for r in meta])
phases = np.unwrap(np.array([r['phase'] for r in meta]))
voice = np.zeros((N, 2)); schedule = []
beat = math.pi / 4.4; last = -100; pair = 0; selected = 0; order = [0, 2, 1, 3]
for half in range(math.ceil((100 * 4.4 - math.pi / 2) / math.pi), math.floor((160 * 4.4 - math.pi / 2) / math.pi) + 1):
    target = (math.pi / 2 + half * math.pi) / 4.4 - 100
    word = half % 2
    weight = float(np.interp(target, times, weights))
    if not 3 <= target < 55 or weight < .9: continue
    if target - last > beat * 1.6 and word == 1: continue
    if word == 0: selected = order[pair % 4]; pair += 1
    x, accent = clips[selected * 2 + word]
    start = target - accent; i = round(start * SR)
    length = min(N - i, len(x))
    # Never spill a chant into a completion celebration.
    for boundary in [35, 43, 55]:
        if start < boundary < start + length / SR: length = min(length, round((boundary - start) * SR))
    y = x[:length].copy(); edge = min(400, len(y) // 5)
    y[-edge:] *= np.linspace(1, 0, edge)
    pan = -.025 if word == 0 else .025
    gain = [.54, .50, .57, .52][selected] * .5
    g = np.sqrt(np.array([(1 - pan) * .5, (1 + pan) * .5])) * gain
    voice[i:i + length] += y[:, None] * g
    measured = float(np.interp(target, times, phases)); desired = math.pi / 2 + half * math.pi
    delta = math.atan2(math.sin(measured - desired), math.cos(measured - desired))
    schedule.append(dict(word='嘿' if word == 0 else '哟', variant=selected, accent=target,
                         start=start, duration=length/SR, weight=weight, phase_error_degrees=abs(math.degrees(delta))))
    last = target

music_path = ROOT / 'work/demo-video-190/audio/instrumental.wav'
instrumental = read(music_path); assert len(instrumental) == 50 * SR
# Keep the accepted accompaniment and level; extend it with a .25s crossfade.
cross = SR // 4; join = len(instrumental) - cross
music = np.zeros((N, 2)); music[:len(instrumental)] = instrumental
tail = np.tile(instrumental, (2, 1))[:N - join]
ramp = np.linspace(0, 1, cross)
music[join:join + cross] = music[join:join + cross] * (1-ramp[:,None]) + tail[:cross] * ramp[:,None]
music[join + cross:] = tail[cross:]
music *= .8
fade = np.minimum(np.arange(N) / SR / .35, 1) * np.minimum((DURATION - np.arange(N) / SR) / 1.25, 1)
voice = (voice - voice.mean(axis=0)) * fade[:, None]
music = (music - music.mean(axis=0)) * fade[:, None]
mix = voice + music
assert schedule and max(e['phase_error_degrees'] for e in schedule) < .01
peak = float(np.max(np.abs(mix))); assert peak < .95
for name, data in [('soundtrack.wav', mix), ('voice-half.wav', voice), ('music-preserved.wav', music)]: write(name, data)
report = dict(duration=DURATION, sample_rate=SR, voice_source='accepted191 stock synthetic youthful feminine AnaNeural; four context variants',
              voice_gain_relative191=.5, music_source_sha256=hashlib.sha256(music_path.read_bytes()).hexdigest(),
              music_gain=.8, music_extension='same source loop, .25s crossfade at49.75s', fades=[.35,1.25],
              no_global_normalization=True, peak=peak, max_phase_error_degrees=max(e['phase_error_degrees'] for e in schedule),
              no_chant_idle_or_cheer=True, syllables=len(schedule), schedule=schedule)
(OUT / 'audio-validation.json').write_text(json.dumps(report, ensure_ascii=False, indent=2)+'\n')
print({k:v for k,v in report.items() if k != 'schedule'})
