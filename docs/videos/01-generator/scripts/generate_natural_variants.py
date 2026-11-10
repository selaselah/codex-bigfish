import asyncio,json
from pathlib import Path
import edge_tts
async def main():
 w=Path('work/demo-voice-191/audio');text='Hey, yo! Hey, yo. Hey... yo! Hey, yo!';voice='en-US-AnaNeural';c=edge_tts.Communicate(text,voice,rate='-8%',pitch='+0Hz',boundary='WordBoundary');events=[]
 with (w/'natural-young.mp3').open('wb') as f:
  async for e in c.stream():
   if e['type']=='audio':f.write(e['data'])
   else:events.append(e)
 (w/'natural-young.json').write_text(json.dumps({'voice':voice,'text':text,'rate':'-8%','pitch':'+0Hz','events':events},ensure_ascii=False,indent=2)+'\n');print(events)
asyncio.run(main())
