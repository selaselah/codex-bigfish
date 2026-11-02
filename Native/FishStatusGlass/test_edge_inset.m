// Reference behavior is the user's Mini drag description, not218's rejected
// whole-group centering rule. Test display lists do not alter OS settings.
#define FISH_STATUS_DRAG_TESTING 1
#import "FishGlassStatus.m"
static void Require(BOOL yes,NSString *message) { if (!yes) { NSLog(@"FAIL: %@",message);exit(1); } }
static void Pump(NSTimeInterval duration) {
    NSDate *until=[NSDate dateWithTimeIntervalSinceNow:duration];
    while(until.timeIntervalSinceNow>0)[NSRunLoop.mainRunLoop runMode:NSDefaultRunLoopMode beforeDate:[NSDate dateWithTimeIntervalSinceNow:MIN(.005,until.timeIntervalSinceNow)]];
}
static void SetTasks(NSUInteger count) {
    NSMutableArray *tasks=[NSMutableArray new];for(NSUInteger i=0;i<count;i++)[tasks addObject:@{@"session_id":[NSString stringWithFormat:@"10000000-0000-4000-8000-%012lu",(unsigned long)i],@"title":@"拖动边界测试",@"summary":@"仅此测试进程",@"expires_at":@(NSDate.date.timeIntervalSince1970+3600)}];
    NSData *data=[NSJSONSerialization dataWithJSONObject:@{@"tasks":tasks} options:0 error:nil];FishGlassStatusUpdateTasks([[NSString alloc] initWithData:data encoding:NSUTF8StringEncoding].UTF8String);
}
static NSEvent *Mouse(NSEventType type,NSPoint p) {
    return [NSEvent mouseEventWithType:type location:[statusPanel convertPointFromScreen:p] modifierFlags:0 timestamp:0 windowNumber:statusPanel.windowNumber context:nil eventNumber:1 clickCount:1 pressure:1];
}
static NSRect Painted(void) { return [statusPanel convertRectToScreen:[overlay convertRect:overlay.pill.frame toView:nil]]; }
static void Park(NSWindow *window,NSScreen *screen,FishTaskMode mode) {
    CancelStatusDrag();CancelEdgeReturn();CancelTaskAnimation();statusDockOffset=NSZeroPoint;SetTaskMode(mode,NO);
    NSRect v=screen.visibleFrame;[window setFrameOrigin:NSMakePoint(NSMidX(v)-156,NSMidY(v)-208)];LayoutStatus();
}
static void PureGeometry(void) {
    for(NSValue *v in @[[NSValue valueWithRect:NSMakeRect(-1920,0,1920,1080)],[NSValue valueWithRect:NSMakeRect(0,-1200,1600,1160)],[NSValue valueWithRect:NSMakeRect(0,0,1728,1020)]]) {
        for(NSNumber *x in @[@(-4000),@(-20.125),@0,@100,@3000])for(NSNumber *y in @[@(-3000),@(-10.125),@100,@3000]) {
            NSRect b=NSMakeRect(NSMinX(v.rectValue)+x.doubleValue,NSMinY(v.rectValue)+y.doubleValue,146,40);NSPoint d=EdgeCorrection(b,v.rectValue);NSRect out=NSOffsetRect(b,d.x,d.y);
            Require(NSContainsRect(NSInsetRect(v.rectValue,16,16),out),@"Painted status clamp handles fractional/negative/far-offscreen desktop coordinates");Require(NSEqualPoints(EdgeCorrection(out,v.rectValue),NSZeroPoint),@"Correction is idempotent without integer overshoot");
        }
    }
}
static void DragCase(NSWindow *window,NSScreen *screen,BOOL single,NSInteger edge,FishTaskMode mode,NSUInteger *samples) {
    NSLog(@"CASE %@ single=%d mode=%ld edge=%ld",screen.localizedName,single,(long)mode,(long)edge);
    Park(window,screen,mode);if(tasksExpanded){[taskScroll.contentView scrollToPoint:NSMakePoint(0,24)];[taskScroll reflectScrolledClipView:taskScroll.contentView];}
    NSRect visible=screen.visibleFrame,safe=NSInsetRect(visible,16,16);BOOL expanded=tasksExpanded,shown=taskCardsShown;CGFloat scroll=NSMinY(taskScroll.contentView.bounds);NSSize size=window.frame.size;
    FishStatusDragButton *button=(FishStatusDragButton *)(edge%2?overlay.disclosureButton:overlay.taskButton);
    NSPoint start=[statusPanel convertPointToScreen:[button convertPoint:NSMakePoint(NSMidX(button.bounds),20) toView:nil]];
    NSPoint end=start;if(edge==0||edge==4||edge==6)end.x=NSMinX(visible)+4;if(edge==1||edge==5||edge==7)end.x=NSMaxX(visible)-4;if(edge==2||edge==4||edge==5)end.y=NSMinY(visible)+4;if(edge==3||edge==6||edge==7)end.y=NSMaxY(visible)-4;
    NSRect originalPainted=Painted();
    NSRect rawTarget=NSOffsetRect(originalPainted,end.x-start.x,end.y-start.y);NSPoint singleCorrection=EdgeCorrection(rawTarget,visible);NSRect singleExpected=NSOffsetRect(rawTarget,singleCorrection.x,singleCorrection.y);
    [button mouseDown:Mouse(NSEventTypeLeftMouseDown,start)];
    __block BOOL recording=single;__block NSUInteger notifications=0;
    id observer=[NSNotificationCenter.defaultCenter addObserverForName:NSWindowDidMoveNotification object:statusPanel queue:nil usingBlock:^(NSNotification *note){if(recording){notifications++;Require(NSContainsRect(safe,Painted()),@"No transient child-panel overshoot before layout correction on a held single-screen drag");}}];
    [button mouseDragged:Mouse(NSEventTypeLeftMouseDragged,end)];NSRect held=Painted(),parent=window.frame;NSPoint relative=NSMakePoint(NSMinX(statusPanel.frame)-NSMinX(parent),NSMinY(statusPanel.frame)-NSMinY(parent));
    if(single)Require(NSEqualRects(held,singleExpected),[NSString stringWithFormat:@"Single screen stops exactly at its nearest padded drop position while held: actual=%@ expected=%@",NSStringFromRect(held),NSStringFromRect(singleExpected)]);
    else Require(NSEqualRects(held,rawTarget)&&!NSContainsRect(safe,held)&&!edgeReturnTimer,[NSString stringWithFormat:@"Multiple screens allow held status overrun before release; held=%@ safe=%@ start=%@ end=%@ tracking=%d offset=%@",NSStringFromRect(held),NSStringFromRect(safe),NSStringFromPoint(start),NSStringFromPoint(end),statusDragActive,NSStringFromPoint(statusDockOffset)]);
    for(NSUInteger i=0;i<16;i++) {
        // Subpixel pointer pressure continues farther into the boundary on one
        // screen. Multi-screen repeats the same outside point while layout and
        // heartbeat run, exposing an independent child clamp/restore loop.
        NSPoint press=end;if(single){CGFloat extra=(i+1)*.125;if(edge==0||edge==4||edge==6)press.x-=extra;if(edge==1||edge==5||edge==7)press.x+=extra;if(edge==2||edge==4||edge==5)press.y-=extra;if(edge==3||edge==6||edge==7)press.y+=extra;}
        [button mouseDragged:Mouse(NSEventTypeLeftMouseDragged,press)];FishGlassStatusUpdate("工作中 · 6 项","working");LayoutStatus();Pump(.001);
        Require(NSEqualRects(Painted(),held)&&NSEqualRects(window.frame,parent),@"Held boundary pressure and periodic layout have zero plateau jitter");
        Require(NSMinX(statusPanel.frame)-NSMinX(window.frame)==relative.x&&NSMinY(statusPanel.frame)-NSMinY(window.frame)==relative.y,@"Parent and grabbed status retain one consistent drag origin");(*samples)++;
    }
    recording=NO;[NSNotificationCenter.defaultCenter removeObserver:observer];
    NSRect drop=Painted();NSPoint correction=EdgeCorrection(drop,visible);NSRect expected=NSOffsetRect(drop,correction.x,correction.y);
    [button mouseUp:Mouse(NSEventTypeLeftMouseUp,end)];
    if(single)Require(!edgeReturnTimer&&NSEqualRects(Painted(),held)&&NSEqualRects(window.frame,parent),@"Single-screen release neither springs nor recenters");
    else if(edgeReturnTimer){NSRect from=Painted();AdvanceEdgeReturn(.08);Require(!NSEqualRects(from,Painted())&&!NSEqualRects(expected,Painted()),@"Multi-screen return has an intermediate frame");AdvanceEdgeReturn(.22);}
    Require(NSEqualRects(Painted(),expected),@"Release fits only the dragged status, independent of card width");
    for(int i=0;i<12;i++)LayoutStatus();Require(NSEqualRects(Painted(),expected),@"Released status stays at its drop anchor instead of returning to a centered group");
    Require(NSContainsRect(safe,Painted())&&!edgeReturnTimer&&!statusDragActive&&!applyingStatusDrag,@"Final status and tracking lifecycle are safe");Require(NSEqualSizes(window.frame.size,size)&&tasksExpanded==expanded&&taskCardsShown==shown&&fabs(NSMinY(taskScroll.contentView.bounds)-scroll)<.001,@"Scale, card mode and scroll anchor are preserved");
    if(TaskCardsPresent()&&(edge==0||edge==1))Require(fabs(NSMidX(taskPanel.frame)-NSMidX(statusPanel.frame))>1,@"Wider cards slide independently; they do not pull the status into their center");
}
int main(void){@autoreleasepool{
    [NSApplication.sharedApplication setActivationPolicy:NSApplicationActivationPolicyProhibited];PureGeometry();NSArray<NSScreen*> *screens=NSScreen.screens;Require(screens.count>0,@"Native displays available");
    NSRect visible=screens.firstObject.visibleFrame;NSWindow *window=[[NSWindow alloc] initWithContentRect:NSMakeRect(NSMidX(visible)-156,NSMidY(visible)-208,312,416) styleMask:NSWindowStyleMaskBorderless backing:NSBackingStoreBuffered defer:NO];window.title=@"BigFatBlueFish";[window orderFront:nil];FishGlassStatusUpdate("工作中 · 6 项","working");SetTasks(6);NSWindow *key=NSApp.keyWindow;Class originalClass=freeFrameOriginalClass;Require(freeFrameClass&&object_getClass(window)==originalClass&&method_getImplementation(class_getInstanceMethod(freeFrameClass,@selector(constrainFrameRect:toScreen:)))==(IMP)StatusAnchoredFrame,@"Owned avatar keeps its original class while installing a restorable public frame-constraint method");NSUInteger cases=0,samples=0;
    if (!getenv("FISH_EDGE_FOCUS")) for(NSScreen *screen in screens)for(NSNumber *policy in @[@YES,@NO]){
        BOOL single=policy.boolValue;if(!single&&screens.count<2)continue;statusDragTestScreens=single?@[screen]:screens;
        for(NSNumber *mode in @[@(FishTaskHidden),@(FishTaskFolded),@(FishTaskExpanded)])for(NSInteger edge=0;edge<8;edge++){DragCase(window,screen,single,edge,mode.integerValue,&samples);cases++;}
    }
    statusDragTestScreens=screens;Park(window,screens.firstObject,FishTaskFolded);
    // Interior release is exactly the user's drop location, even after pumps.
    FishStatusDragButton *button=(FishStatusDragButton*)overlay.taskButton;NSPoint start=[statusPanel convertPointToScreen:[button convertPoint:NSMakePoint(NSMidX(button.bounds),20) toView:nil]],end=NSMakePoint(start.x+55,start.y+25);[button mouseDown:Mouse(NSEventTypeLeftMouseDown,start)];[button mouseDragged:Mouse(NSEventTypeLeftMouseDragged,end)];NSRect drop=Painted();[button mouseUp:Mouse(NSEventTypeLeftMouseUp,end)];Pump(.3);Require(!edgeReturnTimer&&NSEqualRects(Painted(),drop),@"Interior release does not animate or recenter");
    statusDragTestScreens=@[screens.firstObject];Park(window,screens.firstObject,FishTaskFolded);[window setFrameOrigin:NSMakePoint(NSMaxX(visible)-100,NSMidY(visible)-208)];LayoutStatus();NSRect offsetStart=Painted();
    start=[statusPanel convertPointToScreen:[button convertPoint:NSMakePoint(NSMidX(button.bounds),20) toView:nil]];end=NSMakePoint(start.x-55,start.y+15);[button mouseDown:Mouse(NSEventTypeLeftMouseDown,start)];Require(!NSEqualPoints(statusDockOffset,NSZeroPoint),@"Fixture starts with a separately clamped status offset");[button mouseDragged:Mouse(NSEventTypeLeftMouseDragged,end)];drop=Painted();[button mouseUp:Mouse(NSEventTypeLeftMouseUp,end)];if(edgeReturnTimer)AdvanceEdgeReturn(.22);Pump(.28);Require(NSEqualRects(Painted(),drop)&&fabs(NSMidX(Painted())-NSMidX(NaturalStatusRect()))>1,[NSString stringWithFormat:@"An independently offset status retains its safe release anchor through later layout: drop=%@ actual=%@ natural=%@ offset=%@",NSStringFromRect(drop),NSStringFromRect(Painted()),NSStringFromRect(NaturalStatusRect()),NSStringFromPoint(statusDockOffset)]);
    statusDragTestScreens=screens;
    if(screens.count>1){NSScreen *other=screens[1];Park(window,screens.firstObject,FishTaskFolded);start=[statusPanel convertPointToScreen:[button convertPoint:NSMakePoint(NSMidX(button.bounds),20) toView:nil]];end=NSMakePoint(NSMinX(other.visibleFrame)+4,NSMidY(other.visibleFrame));[button mouseDown:Mouse(NSEventTypeLeftMouseDown,start)];[button mouseDragged:Mouse(NSEventTypeLeftMouseDragged,end)];Require(statusDragScreen==other,@"Cross-screen held drag follows the actual cursor display");[button mouseUp:Mouse(NSEventTypeLeftMouseUp,end)];if(edgeReturnTimer)AdvanceEdgeReturn(.22);Require(NSContainsRect(NSInsetRect(other.visibleFrame,16,16),Painted()),@"Cross-screen release fits the destination instead of bouncing to the source display");}
    Park(window,screens.firstObject,FishTaskFolded);[window setFrameOrigin:NSMakePoint(NSMaxX(visible)-180,NSMidY(visible)-208)];BeginStatusDrag(NSMakePoint(NSMaxX(visible)-4,NSMidY(visible)));FinishPetDrag();CancelStatusDrag();if(edgeReturnTimer){AdvanceEdgeReturn(.08);NSRect mid=window.frame;BeginStatusDrag(NSMakePoint(NSMidX(Painted()),NSMidY(Painted())));Pump(.28);Require(!edgeReturnTimer&&NSEqualRects(window.frame,mid),@"New press interrupts a return from its displayed anchor without late movement");CancelStatusDrag();}
    Park(window,screens.firstObject,FishTaskFolded);[window setFrameOrigin:NSMakePoint(NSMaxX(visible)-180,NSMidY(visible)-208)];BeginStatusDrag(NSMakePoint(NSMaxX(visible)-4,NSMidY(visible)));FinishPetDrag();CancelStatusDrag();FishGlassStatusSetVisible(0);Require(!edgeReturnTimer&&!statusDragActive,@"Hide cleans return and held tracking");FishGlassStatusSetVisible(1);
    FishGlassStatusDestroy();NSRect destroyed=window.frame;Pump(.28);Require(!edgeReturnTimer&&!edgeReturnWindow&&!statusDragActive&&NSEqualPoints(statusDockOffset,NSZeroPoint)&&NSEqualRects(window.frame,destroyed),@"Destroy releases tracking, persistent offset and all late callbacks");Require(NSApp.keyWindow==key&&object_getClass(window)==originalClass&&method_getImplementation(class_getInstanceMethod(originalClass,@selector(constrainFrameRect:toScreen:)))==freeFrameOriginalIMP,@"Boundary handling never takes keyboard focus, changes class identity, or leaves the frame hook after destroy");[window orderOut:nil];
    NSLog(@"PASS: %lu native single/multi-screen edge/corner/mode cases; %lu repeated held-pressure/layout samples; transient overshoot guard, release anchor/no-card-recenter, actual cursor destination, interior no-op, scroll/scale/mode, interruption/hide/destroy; no OS display changes",(unsigned long)cases,(unsigned long)samples);return 0;
}}
