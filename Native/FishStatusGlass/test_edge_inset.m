// Own-process native regression; no real task snapshots or preference changes.
#import "FishGlassStatus.m"
static void Require(BOOL yes,NSString *message) { if (!yes) { NSLog(@"FAIL: %@",message);exit(1); } }
static void Pump(NSTimeInterval duration) {
    NSDate *until=[NSDate dateWithTimeIntervalSinceNow:duration];
    while (until.timeIntervalSinceNow>0) [NSRunLoop.mainRunLoop runMode:NSDefaultRunLoopMode beforeDate:[NSDate dateWithTimeIntervalSinceNow:MIN(.01,until.timeIntervalSinceNow)]];
}
static void SetTasks(NSUInteger count) {
    NSMutableArray *tasks=[NSMutableArray new];for (NSUInteger i=0;i<count;i++) [tasks addObject:@{@"session_id":[NSString stringWithFormat:@"10000000-0000-4000-8000-%012lu",(unsigned long)i],@"title":@"边缘回弹测试",@"summary":@"仅此测试进程",@"expires_at":@(NSDate.date.timeIntervalSince1970+3600)}];
    NSData *data=[NSJSONSerialization dataWithJSONObject:@{@"tasks":tasks} options:0 error:nil];FishGlassStatusUpdateTasks([[NSString alloc] initWithData:data encoding:NSUTF8StringEncoding].UTF8String);
}
static NSEvent *Mouse(NSEventType type,NSPoint p) {
    return [NSEvent mouseEventWithType:type location:[statusPanel convertPointFromScreen:p] modifierFlags:0 timestamp:0 windowNumber:statusPanel.windowNumber context:nil eventNumber:1 clickCount:1 pressure:1];
}
static void CheckPureGeometry(void) {
    // Includes left/above and vertically arranged desktop coordinate spaces.
    for (NSValue *value in @[[NSValue valueWithRect:NSMakeRect(-1920,0,1920,1080)],[NSValue valueWithRect:NSMakeRect(0,-1200,1600,1160)],[NSValue valueWithRect:NSMakeRect(0,0,1728,1020)]]) {
        NSRect visible=value.rectValue;
        for (NSNumber *x in @[@(-4000),@(-20),@0,@100,@3000]) for (NSNumber *y in @[@(-3000),@(-10),@100,@3000]) {
            NSRect box=NSMakeRect(NSMinX(visible)+x.doubleValue,NSMinY(visible)+y.doubleValue,350,480);NSPoint correction=EdgeCorrection(box,visible);
            NSRect result=NSOffsetRect(box,correction.x,correction.y);
            Require(NSContainsRect(NSInsetRect(visible,16,16),result),@"All edges/corners/far-offscreen positions recover16pt in positive/negative desktop coordinates");
            Require(NSEqualPoints(EdgeCorrection(result,visible),NSZeroPoint),@"Settled correction is idempotent at native point precision");
        }
    }
    NSRect tiny=NSMakeRect(-20,40,300,220),large=NSMakeRect(-200,-200,350,480);NSPoint delta=EdgeCorrection(large,tiny);NSRect result=NSOffsetRect(large,delta.x,delta.y);
    Require(NSMidX(result)==NSMidX(tiny) && NSMinY(result)==NSMinY(tiny)+16,@"Oversized group is not scaled: centerX and reachable lower status edge");
}
static void CheckRecovered(NSWindow *window,NSScreen *screen,NSSize size,BOOL expanded,BOOL shown,CGFloat scroll) {
    Require(NSContainsRect(NSInsetRect(screen.visibleFrame,16,16),PetGroupBounds(window)),@"Whole avatar and unclamped accessories recover the16pt safe region");
    Require(NSEqualSizes(window.frame.size,size) && tasksExpanded==expanded && taskCardsShown==shown,@"Return keeps pet scale and card mode");
    Require(fabs(NSMinY(taskScroll.contentView.bounds)-scroll)<.001,@"Return preserves expanded scroll anchor");
    NSRect natural=NaturalStatusRect();Require(NSEqualRects(natural,statusPanel.frame),@"Status stays attached instead of being separately clamped");
    if (TaskCardsPresent()) Require(NSMidX(taskPanel.frame)==NSMidX(statusPanel.frame),@"Recovered cards remain centered on their status dock");
    Require(!edgeReturnTimer && !edgeReturnWindow && !applyingEdgeReturn,@"Return releases timer/window and movement guard");
}
int main(void) { @autoreleasepool {
    [NSApplication.sharedApplication setActivationPolicy:NSApplicationActivationPolicyProhibited];CheckPureGeometry();
    NSRect main=NSScreen.mainScreen.visibleFrame;
    NSWindow *window=[[NSWindow alloc] initWithContentRect:NSMakeRect(NSMidX(main)-156,NSMidY(main)-240,312,416) styleMask:NSWindowStyleMaskBorderless backing:NSBackingStoreBuffered defer:NO];window.title=@"BigFatBlueFish";[window orderFront:nil];
    FishGlassStatusUpdate("工作中 · 6 项","working");SetTasks(6);NSWindow *key=NSApp.keyWindow;NSUInteger cases=0;
    for (NSScreen *screen in NSScreen.screens) for (NSNumber *mode in @[@(FishTaskHidden),@(FishTaskFolded),@(FishTaskExpanded)]) {
        NSRect visible=screen.visibleFrame;if (NSWidth(visible)<400 || NSHeight(visible)<540) continue;
        SetTaskMode(mode.integerValue,NO);[window setFrameOrigin:NSMakePoint(NSMidX(visible)-156,NSMidY(visible)-208)];
        if (tasksExpanded) { [taskScroll.contentView scrollToPoint:NSMakePoint(0,24)];[taskScroll reflectScrolledClipView:taskScroll.contentView]; }
        NSSize size=window.frame.size;BOOL expanded=tasksExpanded,shown=taskCardsShown;CGFloat scroll=NSMinY(taskScroll.contentView.bounds);
        for (NSUInteger edge=0;edge<8;edge++) {
            NSPoint origin=NSMakePoint(NSMidX(visible)-156,NSMidY(visible)-208);
            if (edge==0 || edge==4 || edge==6) origin.x=NSMinX(visible)-80;
            if (edge==1 || edge==5 || edge==7) origin.x=NSMaxX(visible)-312+80;
            if (edge==2 || edge==4 || edge==5) origin.y=NSMinY(visible)-80;
            if (edge==3 || edge==6 || edge==7) origin.y=NSMaxY(visible)-416+80;
            [window setFrameOrigin:origin];NSRect from=window.frame;NSPoint correction=EdgeCorrection(PetGroupBounds(window),screen.visibleFrame);Require(!NSEqualPoints(correction,NSZeroPoint),@"Fixture actually overflows an edge");
            FishGlassStatusFinishDrag();
            if (!NSWorkspace.sharedWorkspace.accessibilityDisplayShouldReduceMotion) {
                Require(edgeReturnTimer!=nil && NSEqualRects(window.frame,from),@"Release starts animation without jumping to its endpoint");
                AdvanceEdgeReturn(.08);NSPoint moved=NSMakePoint(window.frame.origin.x-from.origin.x,window.frame.origin.y-from.origin.y);
                Require(hypot(moved.x,moved.y)>0 && hypot(moved.x,moved.y)<hypot(correction.x,correction.y),@"Return has a genuine intermediate frame");AdvanceEdgeReturn(.22);
            }
            CheckRecovered(window,screen,size,expanded,shown,scroll);cases++;
        }
        NSRect stable=window.frame;FinishPetDrag();Require(NSEqualRects(window.frame,stable)&&!edgeReturnTimer,@"Already-safe release is a no-op");
    }
    SetTaskMode(FishTaskFolded,NO);[window setFrameOrigin:NSMakePoint(NSMidX(main)-156,NSMidY(main)-208)];
    FishStatusDragButton *button=(FishStatusDragButton *)overlay.taskButton;NSPoint start=[statusPanel convertPointToScreen:[button convertPoint:NSMakePoint(NSMidX(button.bounds),20) toView:nil]];NSRect from=window.frame;BOOL mode=tasksExpanded;
    [button mouseDown:Mouse(NSEventTypeLeftMouseDown,start)];NSPoint end=NSMakePoint(start.x+NSMaxX(main)-NSMaxX(from)+80,start.y);
    [button mouseDragged:Mouse(NSEventTypeLeftMouseDragged,end)];Require(NSMaxX(PetGroupBounds(window))>NSMaxX(main),@"Held status drag remains free beyond the edge");[button mouseUp:Mouse(NSEventTypeLeftMouseUp,end)];
    if (edgeReturnTimer) AdvanceEdgeReturn(.22);Require(NSContainsRect(NSInsetRect(main,16,16),PetGroupBounds(window))&&tasksExpanded==mode,@"Status release shares the same whole-group return without toggling cards");
    [window setFrameOrigin:NSMakePoint(NSMaxX(main)-220,NSMidY(main)-208)];FinishPetDrag();
    if (edgeReturnTimer) { AdvanceEdgeReturn(.08);NSRect mid=window.frame;PreparePetDrag();Pump(.28);Require(NSEqualRects(window.frame,mid)&&!edgeReturnTimer,@"New drag interrupts from the displayed frame without late return callbacks"); }
    [window setFrameOrigin:NSMakePoint(NSMaxX(main)-220,NSMidY(main)-208)];FinishPetDrag();Pump(.32);Require(!edgeReturnTimer&&NSContainsRect(NSInsetRect(main,16,16),PetGroupBounds(window)),@"Actual common-mode timer completes the return");
    [window setFrameOrigin:NSMakePoint(NSMaxX(main)-220,NSMidY(main)-208)];FinishPetDrag();FishGlassStatusSetVisible(0);Require(!edgeReturnTimer&&!edgeReturnWindow,@"Hiding cancels the return");FishGlassStatusSetVisible(1);
    SetTasks(1);SetTaskMode(FishTaskFolded,NO);[window setFrameOrigin:NSMakePoint(NSMaxX(main)-220,NSMaxY(main)-300)];FinishPetDrag();
    if (edgeReturnTimer) { AdvanceEdgeReturn(.08);SetTasks(6);AdvanceEdgeReturn(.22); }
    Require(NSContainsRect(NSInsetRect(main,16,16),PetGroupBounds(window)),@"A growing task stack during return is refitted using its current footprint");
    [window setFrameOrigin:NSMakePoint(NSMaxX(main)-220,NSMidY(main)-208)];FinishPetDrag();FishGlassStatusDestroy();NSRect destroyed=window.frame;Pump(.30);Require(!edgeReturnTimer&&!edgeReturnWindow&&NSEqualRects(window.frame,destroyed),@"Destroy cancels return without late callbacks");Require(NSApp.keyWindow==key,@"Edge return never acquires keyboard focus");
    [window setFrameOrigin:NSMakePoint(NSMaxX(main)-220,NSMidY(main)-208)];FishGlassStatusFinishDrag();Pump(.32);
    Require(NSContainsRect(NSInsetRect(main,16,16),window.frame)&&!statusPanel&&FishGlassStatusIsReady()==0,@"Avatar-only drag also returns without creating a hidden status UI");
    [window orderOut:nil];
    NSLog(@"PASS: %lu native edge/corner/mode/display cases;16pt whole-group margin, no resize, centered cards, scroll/mode preservation, avatar ABI/status drag, free held drag, intermediate/live timer, interruption/hide/destroy; dynamic task count/hidden-status fallback; positive/negative/far-offscreen/oversize geometry",(unsigned long)cases);return 0;
} }
