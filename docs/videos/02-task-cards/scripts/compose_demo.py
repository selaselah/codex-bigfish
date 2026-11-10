"""Local media assembly for the 1080-square task-card demo; no live bridge writes."""
from pathlib import Path
import csv, hashlib, json, math, subprocess, sys, wave
import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
WORK = ROOT / "work/demo-native-211"
OUT = ROOT / "outputs/BigFish-Task-Demo-211"
SR = 48000

def storyboard_font():
    # Native captions use AppKit. Only the review sheet needs a FreeType font;
    # the current system PingFangUI collection is not readable by FreeType.
    return ImageFont.truetype("/System/Library/Fonts/STHeiti Medium.ttc", 20)

def read_wave(path):
    with wave.open(str(path), "rb") as f:
        assert f.getframerate() == SR and f.getnchannels() == 2 and f.getsampwidth() == 2
        return np.frombuffer(f.readframes(f.getnframes()), "<i2").astype(np.float32).reshape(-1, 2) / 32768

def write_wave(path, data):
    with wave.open(str(path), "wb") as f:
        f.setnchannels(2); f.setsampwidth(2); f.setframerate(SR)
        f.writeframes((np.clip(data, -.999, .999) * 32767).astype("<i2").tobytes())

def audio():
    folder = WORK / "audio"; folder.mkdir(exist_ok=True)
    original = read_wave(ROOT / "work/demo-voice-191/audio/voice-only.wav")
    instrumental_path = ROOT / "work/demo-video-190/audio/instrumental.wav"
    instrumental = read_wave(instrumental_path)
    assert len(original) == len(instrumental) == 50 * SR
    full = original.copy()
    timing = list(csv.DictReader((WORK / "motion-timing.csv").open()))
    times = np.array([float(x["time"]) for x in timing])
    weights = np.array([float(x["workWeight"]) for x in timing])
    phases = np.unwrap(np.array([float(x["crankPhase"]) for x in timing]))
    old = json.loads((ROOT / "work/demo-voice-191/voice-sync-validation.json").read_text())
    shift = 3 * 2 * math.pi / 4.4  # Whole crank revolutions keep the accents aligned.
    extra = []
    for s in old["schedule"]:
        accent = s["accent_seconds"] - shift
        if not (3 <= accent < 7) or np.interp(accent, times, weights) < .9: continue
        if not extra and s["syllable"] == "哟": continue
        a = round(s["start_seconds"] * SR); b = round((s["start_seconds"] - shift) * SR)
        length = min(round(s["clip_duration"] * SR), 7 * SR - b)
        clip = original[a:a+length].copy()
        if b + length == 7 * SR:
            edge = min(400, length); clip[-edge:] *= np.linspace(1, 0, edge)[:, None]
        full[b:b+length] += clip
        extra.append({"syllable": s["syllable"], "accent_seconds": accent})
    assert np.array_equal(full[7*SR:], original[7*SR:])
    fade = np.minimum(np.arange(len(full)) / SR / .35, 1) * np.minimum((50 - np.arange(len(full)) / SR) / 1.25, 1)
    reference = (full - full.mean(axis=0)) * fade[:, None]
    voice = reference * .5
    # Same source, 0.80 gain and fade as the accepted191 accompaniment.
    music_raw = instrumental * .80
    music = (music_raw - music_raw.mean(axis=0)) * fade[:, None]
    mix = music + voice
    assert np.max(np.abs(voice - reference*.5)) == 0 and np.max(np.abs(mix)) < .95
    schedule = old["schedule"] + extra
    errors = []
    for s in schedule:
        t = s["accent_seconds"]; assert np.interp(t, times, weights) >= .9
        phase = np.interp(t, times, phases)
        errors.append(abs(math.degrees(math.remainder(phase-math.pi/2, math.pi))))
    assert max(errors) < .01
    for name, data in [("soundtrack.wav", mix), ("voice-half.wav", voice), ("music-preserved.wav", music)]: write_wave(folder / name, data)
    report = {"voice_source": "accepted191 voice-only.wav; same synthetic youthful feminine voice", "voice_gain": .5,
              "voice_change_db": 20*math.log10(.5), "voice_ratio_exact_before_quantization": True,
              "original_voice_samples_after7s_unchanged_before_gain": True, "early_extra_syllables": extra,
              "music_source_sha256": hashlib.sha256(instrumental_path.read_bytes()).hexdigest(), "music_relative191_gain": 1,
              "music_original_gain": .80, "music_fades_unchanged": [.35, 1.25], "no_global_normalization": True,
              "max_crank_phase_error_degrees": max(errors), "mix_peak": float(np.max(np.abs(mix))), "duration": 50}
    (WORK / "audio-validation.json").write_text(json.dumps(report, ensure_ascii=False, indent=2)+"\n")
    print(json.dumps(report, ensure_ascii=False))

def finish():
    OUT.mkdir(parents=True, exist_ok=True)
    plan = json.loads((WORK / "plan.json").read_text())
    model = json.loads((WORK / "model-validation.json").read_text())
    assert model["passed"] and model["frames"] == 1500 and model["triangles"] == 4999
    assert model["bones"] == 58 and model["completedCookers"] == 1
    assert "Native task movie capture passed: 1500 frames" in (WORK / "native-full.log").read_text()
    audit = json.loads((WORK / "native-frame-audit.json").read_text())
    assert len(audit) == 1500
    for i, frame in enumerate(audit):
        t = i / 30
        jobs = 0 if t < 3 else 1 if t < 11 else 2 if t < 19 else 3 if t < 37 else 2
        mode = 0 if t < 29 else 1 if t < 31 else 2 if t < 34 else 0
        assert frame["frame"] == i and frame["jobs"] == jobs and frame["mode"] == mode
        assert frame["diagram"] == (24 <= t < 29) and frame["nativeRegular"]
        opacity = max(0, min(1, (t-24)/.5, (29-t)/.5))
        assert abs(frame["diagramOpacity"]-opacity) < 1e-6
        assert not frame["diagram"] or frame["mode"] == 0
        assert frame["statusY"] == 112 and frame["statusCenterX"] == 270
    assert any(x["animation"] and 8 < x["petOffset"] < 76 for x in audit)
    assert all(audit[i]["petOffset"] == 76 for i in (900, 960, 1000))
    assert all(audit[i]["petOffset"] == 8 for i in (60, 1050, 1499))
    (WORK / "native-validation.json").write_text(json.dumps({"passed": True, "frames": len(audit),
        "native_regular_all_frames": True, "statusY_pt": 112, "statusCenterX_pt": 270,
        "diagram_frames": sum(x["diagram"] for x in audit), "diagram_seconds": 5,
        "diagram_fade_in_seconds": .5, "diagram_fade_out_seconds": .5,
        "diagram_before_folded": True, "animation_has_intermediate_reservation": True,
        "hidden_offset_pt": 8, "folded_expanded_offset_pt": 76}, indent=2)+"\n")
    text = ";FFMETADATA1\n"
    for stage in plan["timeline"]:
        title = stage.get("caption", stage["stage"])
        text += f"[CHAPTER]\nTIMEBASE=1/1000\nSTART={stage['start']*1000}\nEND={stage['end']*1000}\ntitle={title}\n"
    (WORK / "chapters.ffmeta").write_text(text)
    video = OUT / "BigFish-Task-Demo-1080.mp4"
    with (WORK / "mux.log").open("w") as log:
        subprocess.run(["/opt/homebrew/bin/ffmpeg", "-y", "-i", str(WORK/"video-only.mp4"), "-i", str(WORK/"audio/soundtrack.wav"), "-i", str(WORK/"chapters.ffmeta"),
                        "-map", "0:v", "-map", "1:a", "-map_metadata", "2", "-map_chapters", "2", "-c:v", "copy", "-c:a", "aac", "-b:a", "256k", "-t", "50", "-movflags", "+faststart", str(video)], stdout=log, stderr=subprocess.STDOUT, check=True)
    result = subprocess.run(["/opt/homebrew/bin/ffprobe", "-v", "error", "-show_streams", "-show_format", "-show_chapters", "-of", "json", str(video)], capture_output=True, text=True, check=True)
    (WORK/"ffprobe.json").write_text(result.stdout); probe=json.loads(result.stdout); v=next(x for x in probe["streams"] if x["codec_type"]=="video")
    assert (v["width"],v["height"],v["nb_frames"]) == (1080,1080,"1500") and v["r_frame_rate"]=="30/1" and abs(float(probe["format"]["duration"])-50)<.05
    with (WORK/"decode.log").open("w") as log: subprocess.run(["/opt/homebrew/bin/ffmpeg","-v","error","-i",str(video),"-f","null","-"],stdout=log,stderr=subprocess.STDOUT,check=True)
    assert (WORK/"decode.log").stat().st_size == 0
    samples=[(2,"Idle · 3 秒"),(7,"一个任务"),(15,"两个任务"),(23,"三个任务"),(26,"精美的交互设计（胡说）"),(30,"折叠"),(32,"展开"),(35,"收起"),(37.4,"完成一个任务"),(43,"还剩两个任务")]
    (WORK/"encoded-samples").mkdir(exist_ok=True)
    sheet=Image.new("RGB",(1080,4*390),"white"); font=storyboard_font()
    for i,(t,label) in enumerate(samples):
        p=WORK/"encoded-samples"/f"sample{i:02d}.png"
        subprocess.run(["/opt/homebrew/bin/ffmpeg","-v","error","-y","-ss",str(t),"-i",str(video),"-frames:v","1",str(p)],check=True)
        frame=Image.open(p).convert("RGB");x=(i%3)*360;y=(i//3)*390;sheet.paste(frame.resize((360,360),Image.Resampling.LANCZOS),(x,y+30));ImageDraw.Draw(sheet).text((x+10,y+3),label,font=font,fill="#252525")
        if t==30:frame.save(OUT/"Poster.png")
    sheet.save(OUT/"Storyboard.png")
    (WORK/"media-validation.json").write_text(json.dumps({"passed":True,"video":str(video.relative_to(ROOT)),"sha256":hashlib.sha256(video.read_bytes()).hexdigest(),"bytes":video.stat().st_size,"width":1080,"height":1080,"fps":30,"frames":1500,"duration":50,"idle_seconds":3,"full_decode_clean":True,"stages":plan["timeline"]},ensure_ascii=False,indent=2)+"\n")
    print(video)

if __name__ == "__main__":
    {"audio":audio,"finish":finish}[sys.argv[1]]()
