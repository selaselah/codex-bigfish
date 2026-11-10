from pathlib import Path
r=Path.cwd();w=r/'work/demo-cofish-276';s=(r/'Native/FishStatusGlass/capture_demo.m').read_text()
s=s.replace('#import FISH_SOURCE','#import <Cocoa/Cocoa.h>\nstatic NSPoint movieMouse={-1000000,-1000000};\nstatic NSURL *movieOpenedURL;\nstatic BOOL MovieOpenURL(NSURL *url){movieOpenedURL=url;return YES;}\n#define FISH_TASK_ACTION_MOUSE_LOCATION() movieMouse\n#define FISH_TASK_OPEN_URL(url) MovieOpenURL(url)\n#import FISH_SOURCE')
s=s.replace('@property BOOL diagramVisible;','@property BOOL diagramVisible,overlayOnly,menuDemo,cursorVisible,brand;\n@property NSImage *appIcon,*menuIcon;\n@property NSString *caption;\n@property NSPoint cursor;\n@property CGFloat click;')
a=s.index('- (void)drawRect:');b=s.index('\n@end',a)
s=s[:a]+'''- (void)drawRect:(NSRect)rect {
    if(!self.overlayOnly){[NSColor.whiteColor setFill];NSRectFill(self.bounds);
      for(int row=-1;row<7;row++)for(int col=-1;col<6;col++){
        CGFloat x=col*112+(row%2?56:0),y=row*88;
        [self.whale drawInRect:NSMakeRect(x+8,y+6,88,59) fromRect:NSZeroRect operation:NSCompositingOperationSourceOver fraction:.20 respectFlipped:NO hints:@{NSImageHintInterpolation:@(NSImageInterpolationHigh)}];}
      if(self.caption.length){NSDictionary *style=@{NSFontAttributeName:[NSFont systemFontOfSize:17 weight:NSFontWeightSemibold],NSForegroundColorAttributeName:[NSColor colorWithWhite:.17 alpha:1]};NSSize z=[self.caption sizeWithAttributes:style];[self.caption drawAtPoint:NSMakePoint((540-z.width)/2,502) withAttributes:style];}
    }
    if(self.brand){
      [[NSColor colorWithWhite:1 alpha:.96]setFill];[[NSBezierPath bezierPathWithRoundedRect:NSMakeRect(54,190,432,260) xRadius:24 yRadius:24]fill];
      [self.appIcon drawInRect:NSMakeRect(166,305,104,104) fromRect:NSZeroRect operation:NSCompositingOperationSourceOver fraction:1];
      [self.menuIcon drawInRect:NSMakeRect(308,334,42,42) fromRect:NSZeroRect operation:NSCompositingOperationSourceOver fraction:1];
      NSString *title=@"CoFish";NSDictionary *font=@{NSFontAttributeName:[NSFont systemFontOfSize:32 weight:NSFontWeightBold],NSForegroundColorAttributeName:NSColor.labelColor};NSSize z=[title sizeWithAttributes:font];[title drawAtPoint:NSMakePoint((540-z.width)/2,252) withAttributes:font];
      NSString *sub=@"陪你把任务，一锅锅完成";NSDictionary *small=@{NSFontAttributeName:[NSFont systemFontOfSize:16],NSForegroundColorAttributeName:NSColor.secondaryLabelColor};z=[sub sizeWithAttributes:small];[sub drawAtPoint:NSMakePoint((540-z.width)/2,219) withAttributes:small];
    }else if(!self.overlayOnly && [self.caption isEqualToString:@"CoFish · 全新图标"]){
      [self.appIcon drawInRect:NSMakeRect(176,420,62,62) fromRect:NSZeroRect operation:NSCompositingOperationSourceOver fraction:1];
      [self.menuIcon drawInRect:NSMakeRect(312,436,30,30) fromRect:NSZeroRect operation:NSCompositingOperationSourceOver fraction:1];
    }
    if(self.diagramVisible){
      [NSColor.whiteColor setFill];NSRectFill(self.bounds);
      [self.diagram drawInRect:NSMakeRect(14,155,512,512*936/2258.0) fromRect:NSZeroRect operation:NSCompositingOperationSourceOver fraction:1];
      NSString *caption=@"精美的交互设计（胡说）";NSDictionary *style=@{NSFontAttributeName:[NSFont systemFontOfSize:22 weight:NSFontWeightSemibold],NSForegroundColorAttributeName:NSColor.labelColor};NSSize z=[caption sizeWithAttributes:style];[caption drawAtPoint:NSMakePoint((540-z.width)/2,414) withAttributes:style];
    }
    if(self.menuDemo){
      [[NSColor colorWithWhite:1 alpha:.96]setFill];NSBezierPath *panel=[NSBezierPath bezierPathWithRoundedRect:NSMakeRect(316,348,200,118) xRadius:13 yRadius:13];[panel fill];[[NSColor colorWithWhite:.8 alpha:.8]setStroke];[panel stroke];
      [self.menuIcon drawInRect:NSMakeRect(329,431,20,20) fromRect:NSZeroRect operation:NSCompositingOperationSourceOver fraction:1];
      NSDictionary *font=@{NSFontAttributeName:[NSFont systemFontOfSize:14 weight:NSFontWeightMedium],NSForegroundColorAttributeName:NSColor.labelColor};
      [@"CoFish" drawAtPoint:NSMakePoint(360,432) withAttributes:font];[@"设置…                 ⌘," drawAtPoint:NSMakePoint(335,397) withAttributes:font];[@"退出 CoFish        ⌘Q" drawAtPoint:NSMakePoint(335,364) withAttributes:font];
    }
    if(self.cursorVisible){
      NSAffineTransform *tr=[NSAffineTransform transform];[tr translateXBy:self.cursor.x yBy:self.cursor.y];
      NSBezierPath *p=[NSBezierPath bezierPath];[p moveToPoint:NSMakePoint(0,0)];[p lineToPoint:NSMakePoint(0,-17)];[p lineToPoint:NSMakePoint(5,-12)];[p lineToPoint:NSMakePoint(9,-20)];[p lineToPoint:NSMakePoint(13,-18)];[p lineToPoint:NSMakePoint(8,-10)];[p lineToPoint:NSMakePoint(16,-10)];[p closePath];[p transformUsingAffineTransform:tr];[NSColor.whiteColor setStroke];p.lineWidth=2;[p stroke];[[NSColor colorWithWhite:.18 alpha:.9]setFill];[p fill];
      if(self.click>0){[[NSColor colorWithRed:.22 green:.55 blue:1 alpha:self.click]setStroke];NSBezierPath *ring=[NSBezierPath bezierPathWithOvalInRect:NSMakeRect(self.cursor.x-14,self.cursor.y-14,28,28)];ring.lineWidth=2;[ring stroke];}
    }
}'''+s[b:]
s=s.replace('if(t<24||t>=29)return 0;\n    return MIN(1,MIN((t-24)/.5,(29-t)/.5));','if(t<12||t>=17)return 0;\n    return MIN(1,MIN((t-12)/.5,(17-t)/.5));')
s=s.replace('@property NSString *lastCaption;','@property NSString *lastCaption;\n@property NSArray *metadata,*timeline;\n@property NSTimeInterval insertionStart,shakeStart;\n@property NSPoint dragOrigin;')
s=s.replace('self.audit=[NSMutableArray new];self.lastCount=-1;self.lastMode=-1;','''self.audit=[NSMutableArray new];self.lastCount=-1;self.lastMode=-1;
    notificationPreferences=[[NSUserDefaults alloc]initWithSuiteName:@"local.desktop.cofish.offline276"];permanentlyHiddenSessions=[NSMutableSet new];
    NSMutableArray *meta=[NSMutableArray new];NSString *lines=[NSString stringWithContentsOfFile:[self.folder stringByAppendingPathComponent:@"metadata.jsonl"] encoding:NSUTF8StringEncoding error:nil];
    for(NSString *line in [lines componentsSeparatedByString:@"\\n"])if(line.length){id row=[NSJSONSerialization JSONObjectWithData:[line dataUsingEncoding:NSUTF8StringEncoding] options:0 error:nil];MovieRequire(row!=nil,@"Bad metadata");[meta addObject:row];}self.metadata=meta;MovieRequire(meta.count==1800,@"Expected1800current-model metadata rows");
    NSDictionary *plan=[NSJSONSerialization JSONObjectWithData:[NSData dataWithContentsOfFile:[self.folder stringByAppendingPathComponent:@"plan.json"]] options:0 error:nil];self.timeline=plan[@"timeline"];''')
a=s.index('    self.probes=');b=s.index('\n    NSNumber *expiry',a)
s=s[:a]+'''    NSMutableSet *probes=[NSMutableSet new];for(NSString *name in [NSFileManager.defaultManager contentsOfDirectoryAtPath:[self.folder stringByAppendingPathComponent:@"preview-model"] error:nil])if([name hasPrefix:@"frame"])[probes addObject:@([[name substringWithRange:NSMakeRange(5,4)] integerValue])];self.probes=probes;'''+s[b:]
s=s.replace('self.boardWindow.contentView=self.board;','self.board.appIcon=[[NSImage alloc]initWithContentsOfFile:[self.folder stringByAppendingPathComponent:@"assets/App-Icon.png"]];self.board.menuIcon=[[NSImage alloc]initWithContentsOfFile:[self.folder stringByAppendingPathComponent:@"assets/Menu-Icon.pdf"]];self.boardWindow.contentView=self.board;')
s=s.replace('self.diagramBoard.diagramVisible=YES;self.diagramWindow.contentView=self.diagramBoard;','self.diagramBoard.overlayOnly=YES;self.diagramBoard.menuIcon=self.board.menuIcon;self.diagramBoard.appIcon=self.board.appIcon;self.diagramWindow.opaque=NO;self.diagramWindow.backgroundColor=NSColor.clearColor;self.diagramWindow.contentView=self.diagramBoard;')
a=s.index('- (void)nextFrame {');b=s.index('\n@end',a)
s=s[:a]+'''- (void)nextFrame { @autoreleasepool {
    if(self.frame>=1800){
        if(self.encoder){[self.encoderInput closeFile];[self.encoder waitUntilExit];MovieRequire(self.encoder.terminationStatus==0,@"Video encoder failed");}
        NSData *audit=[NSJSONSerialization dataWithJSONObject:self.audit options:NSJSONWritingPrettyPrinted error:nil];[audit writeToFile:[self.folder stringByAppendingPathComponent:self.preview?@"native-preview-audit.json":@"native-frame-audit.json"] atomically:YES];
        [notificationPreferences removePersistentDomainForName:@"local.desktop.cofish.offline276"];FishGlassStatusDestroy();[self.avatar orderOut:nil];[self.boardWindow orderOut:nil];[self.diagramWindow orderOut:nil];NSLog(@"PASS: CoFish native movie capture %lu frames, Regular glass, isolated own-app only",(unsigned long)self.audit.count);exit(0);
    }
    CGFloat t=self.frame/30.0;NSDictionary *meta=self.metadata[self.frame];NSInteger count=[meta[@"jobs"]integerValue];
    // Execute the completed-card action before the new acknowledged snapshot.
    if(self.frame==1365){MovieRequire(tasksExpanded&&taskRows[@"00000000-0000-4000-8000-000000000002"]!=nil,@"Expanded completion row missing");[taskRows[@"00000000-0000-4000-8000-000000000002"] openTask:nil];MovieRequire(tasksExpanded,@"Completed click folded remaining list");}
    NSMutableDictionary *payload=[NSJSONSerialization JSONObjectWithData:[meta[@"taskJson"]dataUsingEncoding:NSUTF8StringEncoding] options:NSJSONReadingMutableContainers error:nil];
    for(NSMutableDictionary *task in payload[@"tasks"])task[@"expires_at"]=@(NSDate.date.timeIntervalSince1970+3600);
    NSData *liveJSON=[NSJSONSerialization dataWithJSONObject:payload options:0 error:nil];
    FishGlassStatusUpdateTasks([[NSString alloc]initWithData:liveJSON encoding:NSUTF8StringEncoding].UTF8String);self.lastCount=count;
    NSString *caption=count?[NSString stringWithFormat:@"工作中 · %ld 项",(long)count]:t>=55?@"本回合结束":@"休息中";
    if(![caption isEqualToString:self.lastCaption]){FishGlassStatusUpdate(caption.UTF8String,count?"working":"idle");self.lastCaption=caption;}
    FishTaskMode mode=t<17?FishTaskHidden:t<18.5?FishTaskFolded:t<20?FishTaskExpanded:t<24?FishTaskFolded:t<28?FishTaskExpanded:t<34?FishTaskFolded:t<35?FishTaskExpanded:t<37?FishTaskFolded:t<49?FishTaskExpanded:FishTaskHidden;
    if(mode!=self.lastMode){SetTaskMode(mode,YES);[taskAnimationTimer invalidate];taskAnimationTimer=nil;self.transitionStart=t;self.lastMode=mode;}
    if(taskAnimationActive)AdvanceTaskAnimation(t-self.transitionStart);
    if(self.frame==600||self.frame==690||self.frame==840){FishGlassStatusShowTask("00000000-0000-4000-8000-000000000002");[taskAnimationTimer invalidate];taskAnimationTimer=nil;self.transitionStart=t;[taskStack.insertionTimer invalidate];taskStack.insertionTimer=nil;[taskStack.shakeTimer invalidate];taskStack.shakeTimer=nil;self.insertionStart=t;self.shakeStart=t;}
    if(taskStack.insertionActive)[taskStack advanceInsertion:t-self.insertionStart];if(taskStack.shakeActive)[taskStack advanceShake:t-self.shakeStart];
    if(self.frame==1035){FishGlassStatusShowTask("00000000-0000-4000-8000-000000000001");[taskAnimationTimer invalidate];taskAnimationTimer=nil;self.transitionStart=t;[taskStack.insertionTimer invalidate];taskStack.insertionTimer=nil;self.insertionStart=t;}
    if(self.frame==780)DismissTaskNotification(@"00000000-0000-4000-8000-000000000002",@"first-turn");
    if(self.frame==930)self.dragOrigin=self.avatar.frame.origin;
    if(t>=31&&t<=34){CGFloat u=MIN(1,(t-31)/3);[self.avatar setFrameOrigin:NSMakePoint(self.dragOrigin.x+24*sin(M_PI*u),self.dragOrigin.y+9*sin(M_PI*u))];LayoutStatus();}
    movieMouse=NSMakePoint(-1000000,-1000000);self.diagramBoard.cursorVisible=NO;self.diagramBoard.click=0;
    if((t>=20&&t<20.6)||(t>=23&&t<23.5)||(t>=28&&t<28.6)){
        NSPoint p=[self.avatar convertPointToScreen:NSMakePoint([meta[@"potX"]doubleValue]*312,[meta[@"potY"]doubleValue]*416)];self.diagramBoard.cursor=[self.boardWindow convertPointFromScreen:p];self.diagramBoard.cursorVisible=YES;self.diagramBoard.click=1;
    }
    if(t>=24&&t<26){FishTaskRow *row=taskRows[@"00000000-0000-4000-8000-000000000002"];NSPoint point=NSMakePoint(t<25?100:10,t<25?28:53);movieMouse=[taskPanel convertPointToScreen:[row convertPoint:point toView:nil]];self.diagramBoard.cursor=[self.boardWindow convertPointFromScreen:movieMouse];self.diagramBoard.cursorVisible=YES;}
    if(t>=31&&t<34){NSPoint p=[statusPanel convertPointToScreen:[overlay convertPoint:NSMakePoint(NSMidX(overlay.pill.frame),NSMidY(overlay.pill.frame)) toView:nil]];self.diagramBoard.cursor=[self.boardWindow convertPointFromScreen:p];self.diagramBoard.cursorVisible=YES;self.diagramBoard.click=.7;}
    if(t>=45&&t<45.5){FishTaskRow *row=taskRows[@"00000000-0000-4000-8000-000000000002"];if(row){NSPoint p=[taskPanel convertPointToScreen:[row convertPoint:NSMakePoint(120,28) toView:nil]];self.diagramBoard.cursor=[self.boardWindow convertPointFromScreen:p];self.diagramBoard.cursorVisible=YES;self.diagramBoard.click=1;}}
    LayoutTaskActions();for(NSDictionary *stage in self.timeline)if(t>=[stage[@"start"]doubleValue]&&t<[stage[@"end"]doubleValue])self.board.caption=stage[@"caption"];
    self.board.brand=NO;self.diagramBoard.brand=t>=57;self.imageView.alphaValue=t>=57?MAX(0,1-(t-57)/.5):1;statusPanel.alphaValue=self.imageView.alphaValue;
    CGFloat diagram=MovieDiagramOpacity(t);self.diagramBoard.diagramVisible=diagram>0;self.diagramBoard.menuDemo=t>=46.5&&t<49;
    self.diagramWindow.alphaValue=diagram>0?diagram:(self.diagramBoard.brand?MIN(1,(t-57)/.5):(self.diagramBoard.menuDemo||self.diagramBoard.cursorVisible?1:0));
    if(self.diagramWindow.alphaValue>0)[self.diagramWindow orderFront:nil];else[self.diagramWindow orderOut:nil];
    taskPanel.ignoresMouseEvents=YES;
    NSInteger expected=count+[meta[@"pending"]integerValue]-(t>=26&&t<28?1:0);MovieRequire(taskData.count==expected,[NSString stringWithFormat:@"Frame %ld expected%ldvisible got%lu",(long)self.frame,(long)expected,(unsigned long)taskData.count]);
    BOOL capture=!self.preview||[self.probes containsObject:@(self.frame)];if(!capture){self.frame++;dispatch_async(dispatch_get_main_queue(),^{[self nextFrame];});return;}
    NSString *path=[self.folder stringByAppendingPathComponent:[NSString stringWithFormat:@"%@/frame%04ld.png",self.preview?@"preview-model":@"model-frames",(long)self.frame]];NSImage *model=[[NSImage alloc]initWithContentsOfFile:path];MovieRequire(model!=nil,path);self.imageView.image=model;
    self.board.needsDisplay=YES;self.diagramBoard.needsDisplay=YES;[self.avatar.contentView displayIfNeeded];[self.boardWindow.contentView displayIfNeeded];[self.diagramWindow.contentView displayIfNeeded];[CATransaction flush];
    dispatch_after(dispatch_time(DISPATCH_TIME_NOW,(int64_t)(.045*NSEC_PER_SEC)),dispatch_get_main_queue(),^{
      [SCScreenshotManager captureImageWithFilter:self.filter configuration:self.configuration completionHandler:^(CGImageRef image,NSError *error){
        MovieRequire(!error&&image,error.description?:@"Native screenshot missing");CGImageRef owned=CGImageRetain(image);
        dispatch_async(dispatch_get_main_queue(),^{@autoreleasepool{
          MovieRequire(CGImageGetWidth(owned)==1080&&CGImageGetHeight(owned)==1080,@"Native output size mismatch");NSBitmapImageRep *rep=[[NSBitmapImageRep alloc]initWithCGImage:owned];NSData *png=[rep representationUsingType:NSBitmapImageFileTypePNG properties:@{}];CGImageRelease(owned);
          if(self.preview){NSString *out=[self.folder stringByAppendingPathComponent:[NSString stringWithFormat:@"native-preview/frame%04ld.png",(long)self.frame]];MovieRequire([png writeToFile:out atomically:YES],@"Preview write failed");}else[self.encoderInput writeData:png];
          MovieRequire(overlay.liquidGlass&&taskStack.front.liquidGlass,@"Native Regular glass missing");
          [self.audit addObject:@{@"frame":@(self.frame),@"time":@(t),@"jobs":@(count),@"pending":meta[@"pending"],@"cards":@(taskData.count),@"expanded":@(tasksExpanded),@"mode":@(self.lastMode),@"diagramOpacity":@(diagram),@"nativeRegular":@YES,@"caption":self.board.caption?:@""}];
          if(self.frame%150==0||self.preview)NSLog(@"Movie frame %ld/1800",(long)self.frame);self.frame++;dispatch_async(dispatch_get_main_queue(),^{[self nextFrame];});
        }});
      }];
    });
}}
'''+s[b:]
(w/'capture_movie.m').write_text(s)
