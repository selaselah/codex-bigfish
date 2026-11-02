// Isolated production-view fixture; never reads/writes live task files.
#define main OriginalTaskStackMain
#import "test_task_stack.m"
#undef main

static NSString *ID(NSUInteger index) {
    return [NSString stringWithFormat:@"00000000-0000-4000-8000-%012lu",(unsigned long)index];
}
static void CheckStack(NSUInteger top,NSUInteger count) {
    Require(!tasksExpanded && taskCardsShown && !taskStack.hidden && taskScroll.hidden,@"Cooker selection retains the visible folded stack");
    Require([taskStack.front.sessionID isEqualToString:ID(top)] && [selectedTaskID isEqualToString:ID(top)],@"Requested task is the front card");
    Require(taskData.count==count && taskStack.count==count && taskRows.count==count,@"All tasks remain in the stack and complete list");
    Require(taskPanel.frame.size.height==TaskCardHeight+16+(MIN(count,3)-1)*6,@"Settled stack retains its original height and rear-card geometry");
    NSUInteger visible=0;for(FishTaskRow *row in taskStack.backs) {
        if(!row.hidden)visible++;
        Require(row.titleLabel.hidden && row.messageLabel.hidden,@"Rear glass layers never leak duplicate text through the front");
    }
    Require(visible==MIN(count-1,2),@"Other cards stay visibly stacked behind the selected card");
    CheckRowEdges(taskStack.front);CheckStatusGlass();
}
static void Select(NSUInteger top,NSUInteger count) {
    FishGlassStatusShowTask(ID(top).UTF8String);SettleTaskAnimation();CheckStack(top,count);
}
static void Capture(NSString *name) {
    const char *directory=getenv("FISH_STACK_CAPTURE_DIR");if(!directory)return;
    NSView *view=taskPanel.contentView;[view layoutSubtreeIfNeeded];
    NSBitmapImageRep *bitmap=[view bitmapImageRepForCachingDisplayInRect:view.bounds];
    [view cacheDisplayInRect:view.bounds toBitmapImageRep:bitmap];
    NSString *path=[[NSString stringWithUTF8String:directory] stringByAppendingPathComponent:[name stringByAppendingString:@".png"]];
    Require([[bitmap representationUsingType:NSBitmapImageFileTypePNG properties:@{}] writeToFile:path atomically:YES],@"Save insertion review frame");
}
static void Reorder(void) {
    NSMutableArray *rows=[NSMutableArray new];
    for(NSNumber *n in @[@2,@1,@0,@3])[rows addObject:@{@"session_id":ID(n.unsignedIntegerValue),@"title":[NSString stringWithFormat:@"任务 %@",n],@"summary":@"更新后的消息",@"expires_at":@(NSDate.date.timeIntervalSince1970+3600)}];
    NSData *data=[NSJSONSerialization dataWithJSONObject:@{@"tasks":rows} options:0 error:nil];
    FishGlassStatusUpdateTasks([[NSString alloc] initWithData:data encoding:NSUTF8StringEncoding].UTF8String);
}
int main(void) { @autoreleasepool {
    [NSApplication sharedApplication];[NSApp setActivationPolicy:NSApplicationActivationPolicyProhibited];
    NSWindow *window=[[NSWindow alloc] initWithContentRect:NSMakeRect(500,400,312,416) styleMask:NSWindowStyleMaskBorderless backing:NSBackingStoreBuffered defer:NO];
    window.title=@"BigFatBlueFish";[window orderFront:nil];
    FishGlassStatusUpdate("工作中 · 3 项","working");SetTasks(3,@"原消息");
    Require(!taskStack.insertionActive,@"Initial existing snapshot does not pretend tasks just arrived");CheckStack(0,3);
    float x,y,sx,sy;
    for(NSNumber *type in @[@(NSEventTypeLeftMouseDown),@(NSEventTypeLeftMouseUp)])CaptureAvatarPointer([NSEvent mouseEventWithType:type.unsignedIntegerValue location:NSMakePoint(40,50) modifierFlags:0 timestamp:NSProcessInfo.processInfo.systemUptime windowNumber:window.windowNumber context:nil eventNumber:1 clickCount:1 pressure:1]);
    Require(FishGlassStatusPollAvatarPointer(&x,&y,&sx,&sy)==1 && fabs(x-40.0/312)<1e-6 && fabs(y-50.0/416)<1e-6,@"Queued down retains original normalized viewport");
    Require(FishGlassStatusPollAvatarPointer(&x,&y,&sx,&sy)==3 && fabs(sx-window.frame.origin.x-40)<.001 && fabs(sy-window.frame.origin.y-50)<.001,@"Queued up retains original desktop position and order");
    Require(!FishGlassStatusPollAvatarPointer(&x,&y,&sx,&sy),@"Pointer queue drains exactly once");
    NSRect dock=statusPanel.frame;NSSize size=window.frame.size;Capture(@"before");
    FishGlassStatusShowTask(ID(2).UTF8String);
    if(!NSWorkspace.sharedWorkspace.accessibilityDisplayShouldReduceMotion) {
        Require(taskStack.insertionActive && taskStack.incoming && [taskStack.incoming.sessionID isEqualToString:ID(2)],@"Selecting another cooker starts an incoming front card");
        Require([taskStack.front.sessionID isEqualToString:ID(0)] && taskStack.oldFrontVisible,@"Original front remains visible while moving behind the new card");
        Require(taskPanel.frame.size.height==124 && !taskStack.hidden,@"Temporary transparent headroom prevents incoming glass from clipping");
        CGFloat startY=taskStack.incoming.frame.origin.y;Capture(@"incoming-start");PumpFor(.10);
        Require(taskStack.insertionActive && taskStack.incoming.frame.origin.y>8 && taskStack.incoming.frame.origin.y<startY,@"Real timer moves the incoming card down toward the front");
        Require(taskStack.front.frame.size.width<330 && taskStack.front.frame.size.width>314,@"Old front visibly recedes into the rear layer");
        Require(taskStack.front.content.alphaValue<.05,@"Receding text fades before it can ghost through the incoming glass");
        Require(NSContainsRect(taskStack.bounds,taskStack.incoming.frame),@"Incoming card remains fully inside its transparent canvas");
        CheckRowEdges(taskStack.incoming);Capture(@"incoming-middle");
        NSUInteger generation=taskStack.insertionGeneration;SetTasks(3,@"最新消息");
        Require(taskStack.insertionGeneration==generation,@"Message update does not restart the placing animation");
    }
    SettleTaskAnimation();CheckStack(2,3);Capture(@"incoming-end");
    Require(NSEqualRects(dock,statusPanel.frame)&&NSEqualSizes(size,window.frame.size),@"Cooker promotion never shifts the status or resizes the mascot");
    FishGlassStatusShowTask(ID(2).UTF8String);Require(!taskStack.insertionActive,@"Clicking the already-front task does not replay an unnecessary insertion");
    Select(0,3);Select(1,3);
    FishGlassStatusShowTask("not-a-task");FishGlassStatusShowTask(ID(50).UTF8String);CheckStack(1,3);
    SetTasks(4,@"新任务");
    if(!NSWorkspace.sharedWorkspace.accessibilityDisplayShouldReduceMotion)Require(taskStack.insertionActive && [taskStack.incoming.sessionID isEqualToString:ID(3)],@"A new task is placed on top with the same animation");
    SettleTaskAnimation();CheckStack(3,4);
    Reorder();Require(!taskStack.insertionActive,@"Recency-only reordering and latest-message updates do not masquerade as new cards");CheckStack(3,4);
    Require([taskStack.front.messageLabel.stringValue isEqualToString:@"更新后的消息"],@"Front card receives current messages without moving");
    FishGlassStatusShowTask(ID(1).UTF8String);FishGlassStatusShowTask(ID(2).UTF8String);SettleTaskAnimation();CheckStack(2,4);
    Require(!taskStack.incoming && !taskStack.insertionTimer,@"Rapid cooker switches settle to the last requested task without leaked overlays");
    FishGlassStatusShowTasks();SettleTaskAnimation();Require(tasksExpanded && taskRows.count==4 && taskStack.hidden,@"Explicit full-list expansion still shows every task");
    Select(1,4);
    CycleTaskCards();SettleTaskAnimation();Require(!taskCardsShown&&!taskPanel.visible,@"Arrow hides the folded stack");
    SetTasks(5,@"隐藏时的新任务");Require(!taskStack.insertionActive&&!taskPanel.visible,@"A new task does not override the user's hidden preference");
    Select(1,5);
    SetTasks(1,@"仅剩任务");SettleTaskAnimation();CheckStack(0,1);
    SetTasks(0,@"");SettleTaskAnimation();Require(!taskPanel.visible,@"Empty stack disappears normally");
    SetTasks(1,@"首个新任务");
    if(!NSWorkspace.sharedWorkspace.accessibilityDisplayShouldReduceMotion)Require(taskStack.insertionActive&&!taskStack.oldFrontVisible,@"First new task enters without a phantom old front");
    SettleTaskAnimation();CheckStack(0,1);
    SetTasks(2,@"第二项");FishGlassStatusSetVisible(0);
    Require(!taskStack.insertionActive&&!taskStack.insertionTimer&&!taskStack.incoming,@"Hide cancels insertion and releases temporary views");
    FishGlassStatusSetVisible(1);SettleTaskAnimation();CheckStack(1,2);
    [taskStack insertTasks:StackTaskItems() animated:NO];Require(!taskStack.insertionActive,@"Immediate/reduced-motion path uses the final stack directly");
    FishGlassStatusShowTask(ID(0).UTF8String);FishGlassStatusDestroy();PumpFor(.4);
    Require(!selectedTaskID&&!stackOrder&&!stackInsertionPending&&!taskPanel&&!taskAnimationTimer&&!FishGlassStatusPollAvatarPointer(&x,&y,&sx,&sy),@"Destroy clears stack order, insertion, pointer events and timers");
    [window orderOut:nil];
    NSLog(@"PASS: cooker promotion retains all rear cards; placing animation for cooker/new task; real intermediate frames/headroom/Regular edges/stable dock; no recency retrigger; rapid switch/full list/hidden preference/first task/removal/immediate/hide/destroy; original pointer FIFO");return 0;
} }
