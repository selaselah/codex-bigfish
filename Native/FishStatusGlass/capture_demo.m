// Offline movie compositor. Build with -DFISH_SOURCE='"/absolute/FishGlassStatus.m"'.
// Uses the production native views/animation driver; never reads the live bridge.
#import FISH_SOURCE
#import <ScreenCaptureKit/ScreenCaptureKit.h>
#import <unistd.h>

@interface MovieBoard : NSView
@property NSImage *whale;
@property NSImage *diagram;
@property BOOL diagramVisible;
@end
@implementation MovieBoard
- (void)drawRect:(NSRect)rect {
    [NSColor.whiteColor setFill];NSRectFill(self.bounds);
    for(int row=-1;row<7;row++)for(int col=-1;col<6;col++){
        CGFloat x=col*112+(row%2?56:0),y=row*88;
        [self.whale drawInRect:NSMakeRect(x+8,y+6,88,59) fromRect:NSZeroRect operation:NSCompositingOperationSourceOver fraction:.20 respectFlipped:NO hints:@{NSImageHintInterpolation:@(NSImageInterpolationHigh)}];
    }
    if(self.diagramVisible){
        [NSColor.whiteColor setFill];[[NSBezierPath bezierPathWithRoundedRect:NSMakeRect(8,141,524,250) xRadius:12 yRadius:12]fill];
        [self.diagram drawInRect:NSMakeRect(14,155,512,512*936/2258.0) fromRect:NSZeroRect operation:NSCompositingOperationSourceOver fraction:1];
        NSString *caption=@"精美的交互设计（胡说）";
        NSDictionary *style=@{NSFontAttributeName:[NSFont systemFontOfSize:22 weight:NSFontWeightSemibold],NSForegroundColorAttributeName:NSColor.labelColor};
        NSSize size=[caption sizeWithAttributes:style];[caption drawAtPoint:NSMakePoint((540-size.width)/2,414) withAttributes:style];
    }
}
@end

static void MovieRequire(BOOL condition,NSString *message){if(!condition){NSLog(@"MOVIE FAIL: %@",message);exit(1);}}
@interface MovieCapture:NSObject
@property NSString *folder;
@property BOOL preview;
@property NSWindow *boardWindow,*avatar,*diagramWindow;
@property MovieBoard *board,*diagramBoard;
@property NSImageView *imageView;
@property SCContentFilter *filter;
@property SCStreamConfiguration *configuration;
@property NSTask *encoder;
@property NSFileHandle *encoderInput;
@property NSMutableArray *audit;
@property NSArray *sampleTasks;
@property NSSet *probes;
@property NSInteger frame,lastCount,lastMode;
@property NSTimeInterval transitionStart;
@property NSString *lastCaption;
- (void)start;
@end
@implementation MovieCapture
- (void)start {
    self.audit=[NSMutableArray new];self.lastCount=-1;self.lastMode=-1;
    self.probes=[NSSet setWithArray:@[@0,@240,@510,@720,@825,@900,@960,@1050,@1122,@1260,@1499]];
    NSNumber *expiry=@(NSDate.date.timeIntervalSince1970+3600);
    self.sampleTasks=@[@{@"session_id":@"00000000-0000-4000-8000-000000000001",@"expires_at":expiry,@"title":@"绘制鲸鱼背景",@"summary":@"正在检查图案"},@{@"session_id":@"00000000-0000-4000-8000-000000000002",@"expires_at":expiry,@"title":@"优化发电动画",@"summary":@"正在摇动发电机"},@{@"session_id":@"00000000-0000-4000-8000-000000000003",@"expires_at":expiry,@"title":@"准备演示视频",@"summary":@"正在生成画面"}];
    NSRect area=NSMakeRect(100,200,540,540);
    self.boardWindow=[[NSWindow alloc]initWithContentRect:area styleMask:NSWindowStyleMaskBorderless backing:NSBackingStoreBuffered defer:NO];self.boardWindow.title=@"Offline movie background";self.boardWindow.hasShadow=NO;self.boardWindow.ignoresMouseEvents=YES;self.boardWindow.level=NSFloatingWindowLevel;
    self.board=[[MovieBoard alloc]initWithFrame:NSMakeRect(0,0,540,540)];self.board.whale=[[NSImage alloc]initWithContentsOfFile:[self.folder stringByAppendingPathComponent:@"assets/Whale-Icon.png"]];MovieRequire(self.board.whale!=nil,@"Whale icon missing");self.boardWindow.contentView=self.board;
    self.avatar=[[NSWindow alloc]initWithContentRect:NSMakeRect(area.origin.x+114,area.origin.y+48,312,416) styleMask:NSWindowStyleMaskBorderless backing:NSBackingStoreBuffered defer:NO];self.avatar.title=@"BigFatBlueFish";self.avatar.opaque=NO;self.avatar.backgroundColor=NSColor.clearColor;self.avatar.hasShadow=NO;self.avatar.ignoresMouseEvents=YES;self.avatar.level=NSFloatingWindowLevel;
    // The fixed movie camera has more foot padding than the live app camera.
    // Calibrate only this composition's coordinate origin; keep the model's
    // position/scale and the production fixed-dock/animation logic unchanged.
    NSView *avatarCanvas=[[NSView alloc]initWithFrame:NSMakeRect(0,0,312,416)];self.avatar.contentView=avatarCanvas;avatarCanvas.bounds=NSMakeRect(0,-40,312,416);
    self.imageView=[[NSImageView alloc]initWithFrame:avatarCanvas.bounds];self.imageView.imageScaling=NSImageScaleAxesIndependently;[avatarCanvas addSubview:self.imageView];
    [self.boardWindow orderFront:nil];[self.boardWindow addChildWindow:self.avatar ordered:NSWindowAbove];[self.avatar orderFront:nil];
    FishGlassStatusUpdate("休息中","idle");
    // Only this offline host's fixed timeline controls the demonstration.
    if(outsideClickMonitor)[NSEvent removeMonitor:outsideClickMonitor];outsideClickMonitor=nil;
    if(localClickMonitor)[NSEvent removeMonitor:localClickMonitor];localClickMonitor=nil;
    statusPanel.ignoresMouseEvents=YES;
    self.diagramWindow=[[NSWindow alloc]initWithContentRect:area styleMask:NSWindowStyleMaskBorderless backing:NSBackingStoreBuffered defer:NO];self.diagramWindow.title=@"Offline interaction diagram";self.diagramWindow.hasShadow=NO;self.diagramWindow.ignoresMouseEvents=YES;self.diagramWindow.level=NSFloatingWindowLevel+1;
    self.diagramBoard=[[MovieBoard alloc]initWithFrame:NSMakeRect(0,0,540,540)];self.diagramBoard.whale=self.board.whale;self.diagramBoard.diagram=[[NSImage alloc]initWithContentsOfFile:[self.folder stringByAppendingPathComponent:@"assets/Interaction-Diagram.png"]];self.diagramBoard.diagramVisible=YES;self.diagramWindow.contentView=self.diagramBoard;
    if(self.preview)[NSFileManager.defaultManager createDirectoryAtPath:[self.folder stringByAppendingPathComponent:@"native-preview"] withIntermediateDirectories:YES attributes:nil error:nil];
    else{
        self.encoder=[NSTask new];self.encoder.executableURL=[NSURL fileURLWithPath:@"/opt/homebrew/bin/ffmpeg"];
        self.encoder.arguments=@[@"-y",@"-hide_banner",@"-loglevel",@"error",@"-f",@"image2pipe",@"-vcodec",@"png",@"-framerate",@"30",@"-i",@"pipe:0",@"-an",@"-c:v",@"libx264",@"-preset",@"medium",@"-crf",@"18",@"-pix_fmt",@"yuv420p",@"-movflags",@"+faststart",[self.folder stringByAppendingPathComponent:@"video-only.mp4"]];
        NSPipe *pipe=[NSPipe pipe];self.encoder.standardInput=pipe;self.encoderInput=pipe.fileHandleForWriting;
        NSString *log=[self.folder stringByAppendingPathComponent:@"native-encode.log"];[NSFileManager.defaultManager createFileAtPath:log contents:nil attributes:nil];self.encoder.standardError=[NSFileHandle fileHandleForWritingAtPath:log];self.encoder.standardOutput=NSFileHandle.fileHandleWithNullDevice;
        NSError *error=nil;MovieRequire([self.encoder launchAndReturnError:&error],error.description ?: @"Encoder launch failed");
    }
    [SCShareableContent getShareableContentExcludingDesktopWindows:YES onScreenWindowsOnly:NO completionHandler:^(SCShareableContent *content,NSError *error){dispatch_async(dispatch_get_main_queue(),^{
        MovieRequire(!error,error.description);NSArray *apps=[content.applications filteredArrayUsingPredicate:[NSPredicate predicateWithBlock:^BOOL(SCRunningApplication *app,NSDictionary *bindings){return app.processID==getpid();}]];
        MovieRequire(apps.count==1,@"Own movie app missing from capture inventory");
        NSScreen *movieScreen=self.boardWindow.screen;NSNumber *displayID=movieScreen.deviceDescription[@"NSScreenNumber"];SCDisplay *display=nil;for(SCDisplay *candidate in content.displays)if(candidate.displayID==displayID.unsignedIntValue)display=candidate;MovieRequire(display!=nil,@"Movie display missing");
        self.filter=[[SCContentFilter alloc]initWithDisplay:display includingApplications:apps exceptingWindows:@[]];self.filter.includeMenuBar=NO;
        self.configuration=[SCStreamConfiguration new];self.configuration.width=1080;self.configuration.height=1080;self.configuration.showsCursor=NO;self.configuration.ignoreShadowsDisplay=YES;self.configuration.includeChildWindows=YES;
        NSRect screen=movieScreen.frame;self.configuration.sourceRect=CGRectMake(area.origin.x-screen.origin.x,NSMaxY(screen)-NSMaxY(area),540,540);self.configuration.destinationRect=CGRectMake(0,0,1080,1080);NSLog(@"Movie screen %@, board %@, source %@, output1080",NSStringFromRect(screen),NSStringFromRect(area),NSStringFromRect(self.configuration.sourceRect));
        dispatch_after(dispatch_time(DISPATCH_TIME_NOW,NSEC_PER_SEC),dispatch_get_main_queue(),^{[self nextFrame];});
    });}];
}
- (void)nextFrame { @autoreleasepool {
    if(self.frame>=1500){
        if(self.encoder){[self.encoderInput closeFile];[self.encoder waitUntilExit];MovieRequire(self.encoder.terminationStatus==0,@"Video encoder failed");}
        NSData *audit=[NSJSONSerialization dataWithJSONObject:self.audit options:NSJSONWritingPrettyPrinted error:nil];[audit writeToFile:[self.folder stringByAppendingPathComponent:self.preview?@"native-preview-audit.json":@"native-frame-audit.json"] atomically:YES];
        FishGlassStatusDestroy();[self.avatar orderOut:nil];[self.boardWindow orderOut:nil];[self.diagramWindow orderOut:nil];NSLog(@"Native task movie capture passed: %lu frames, real Regular glass, own app only",(unsigned long)self.audit.count);exit(0);
    }
    CGFloat t=self.frame/30.0;NSInteger count=t<3?0:t<11?1:t<19?2:t<37?3:2;FishTaskMode mode=t<27?FishTaskHidden:t<31?FishTaskFolded:t<34?FishTaskExpanded:FishTaskHidden;
    if(count!=self.lastCount){NSArray *items=[self.sampleTasks subarrayWithRange:NSMakeRange(count==2&&t>=37?1:0,count)];NSData *data=[NSJSONSerialization dataWithJSONObject:@{@"tasks":items} options:0 error:nil];FishGlassStatusUpdateTasks([[NSString alloc]initWithData:data encoding:NSUTF8StringEncoding].UTF8String);self.lastCount=count;}
    NSString *caption=count?[NSString stringWithFormat:@"工作中 · %ld 项",(long)count]:@"休息中";if(t>=37&&t<39.4)caption=@"一个任务结束啦";
    if(![caption isEqualToString:self.lastCaption]){FishGlassStatusUpdate(caption.UTF8String,count?"working":"idle");self.lastCaption=caption;}
    MovieRequire(taskData.count==count,@"Offline task fixture was rejected by production validation");
    if(mode!=self.lastMode){SetTaskMode(mode,YES);[taskAnimationTimer invalidate];taskAnimationTimer=nil;self.transitionStart=t;self.lastMode=mode;}
    if(taskAnimationActive)AdvanceTaskAnimation(t-self.transitionStart);
    taskPanel.ignoresMouseEvents=YES;
    BOOL diagram=t>=29&&t<31;if(diagram)[self.diagramWindow orderFront:nil];else[self.diagramWindow orderOut:nil];
    BOOL capture=!self.preview||[self.probes containsObject:@(self.frame)];
    if(!capture){self.frame++;dispatch_async(dispatch_get_main_queue(),^{[self nextFrame];});return;}
    NSString *path=[self.folder stringByAppendingPathComponent:[NSString stringWithFormat:@"%@/frame%04ld.png",self.preview?@"preview-model":@"model-frames",(long)self.frame]];NSImage *model=[[NSImage alloc]initWithContentsOfFile:path];MovieRequire(model!=nil,[NSString stringWithFormat:@"Model frame missing: %@",path]);self.imageView.image=model;
    [self.avatar.contentView displayIfNeeded];[self.boardWindow.contentView displayIfNeeded];[self.diagramWindow.contentView displayIfNeeded];[CATransaction flush];
    dispatch_after(dispatch_time(DISPATCH_TIME_NOW,(int64_t)(.045*NSEC_PER_SEC)),dispatch_get_main_queue(),^{
        [SCScreenshotManager captureImageWithFilter:self.filter configuration:self.configuration completionHandler:^(CGImageRef image,NSError *error){
            MovieRequire(!error&&image,error.description ?: @"Native screenshot missing");CGImageRef owned=CGImageRetain(image);
            dispatch_async(dispatch_get_main_queue(),^{@autoreleasepool{
                MovieRequire(CGImageGetWidth(owned)==1080&&CGImageGetHeight(owned)==1080,@"Native output size mismatch");NSBitmapImageRep *rep=[[NSBitmapImageRep alloc]initWithCGImage:owned];NSData *png=[rep representationUsingType:NSBitmapImageFileTypePNG properties:@{}];CGImageRelease(owned);
                if(self.preview){NSString *out=[self.folder stringByAppendingPathComponent:[NSString stringWithFormat:@"native-preview/frame%04ld.png",(long)self.frame]];MovieRequire([png writeToFile:out atomically:YES],@"Preview write failed");}else[self.encoderInput writeData:png];
                MovieRequire(overlay.liquidGlass&&taskStack.front.liquidGlass&&!overlay.backdrop&&!taskStack.front.backdrop,@"Movie lost native glass");
                [self.audit addObject:@{@"frame":@(self.frame),@"time":@(self.frame/30.0),@"jobs":@(self.lastCount),@"mode":@(self.lastMode),@"diagram":@(self.frame>=870&&self.frame<930),@"petOffset":@(petTaskOffset),@"statusY":@(statusPanel.frame.origin.y-self.boardWindow.frame.origin.y),@"statusCenterX":@(NSMidX(statusPanel.frame)-self.boardWindow.frame.origin.x),@"animation":@(taskAnimationActive),@"nativeRegular":@YES}];
                if(self.frame%150==0||self.preview)NSLog(@"Native movie frame %ld/1500",(long)self.frame);self.frame++;dispatch_async(dispatch_get_main_queue(),^{[self nextFrame];});
            }});
        }];
    });
}}
@end
int main(int argc,const char *argv[]){@autoreleasepool{
    MovieRequire(argc==3,@"Usage: capture_demo /absolute/work/folder preview|full");[NSApplication.sharedApplication setActivationPolicy:NSApplicationActivationPolicyProhibited];MovieCapture *movie=[MovieCapture new];movie.folder=[NSString stringWithUTF8String:argv[1]];movie.preview=strcmp(argv[2],"preview")==0;[movie start];[NSApp run];
}}
