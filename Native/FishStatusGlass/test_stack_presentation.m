// Observe production presentation boundaries without forcing layout first.
// This isolated fixture never reads or writes live task snapshots.
#define main OriginalTaskStackMain
#import "test_task_stack.m"
#undef main

static IMP originalFrame,originalDisplay;
static NSUInteger presentations,shrinks;
static void CheckPresentation(void) {
    if (!taskPanel.visible || tasksExpanded || taskAnimationActive || preparingTaskAnimation || !taskCardsShown || !taskData.count) return;
    presentations++;
    Require(NSEqualRects(taskStack.frame,taskPanel.contentView.bounds),@"Window must never present a new canvas size with the old stack size");
    for (FishTaskRow *row in taskStack.subviews) {
        if (![row isKindOfClass:FishTaskRow.class] || row.hidden || row.alphaValue==0) continue;
        NSRect frame=[taskPanel.contentView convertRect:row.bounds fromView:row];
        Require(NSContainsRect(taskPanel.contentView.bounds,frame),@"Every visible glass lens is inside the presented window");
        if (row.titleLabel.hidden || row.content.alphaValue==0) continue;
        for (NSTextField *label in @[row.titleLabel,row.messageLabel]) {
            NSRect text=[taskPanel.contentView convertRect:label.bounds fromView:label];
            Require(NSContainsRect(taskPanel.contentView.bounds,text),@"Both text lines remain inside the presented window");
        }
    }
}
static void SetFrameProbe(id window,SEL command,NSRect frame,BOOL display) {
    BOOL own=window==taskPanel;
    if (own && NSHeight(frame)<NSHeight(taskPanel.frame)) shrinks++;
    ((void(*)(id,SEL,NSRect,BOOL))originalFrame)(window,command,frame,display);
    if (own && display) CheckPresentation();
}
static void DisplayProbe(id window,SEL command) {
    if(window==taskPanel) CheckPresentation();
    ((void(*)(id,SEL))originalDisplay)(window,command);
}
int main(void) { @autoreleasepool {
    [NSApplication.sharedApplication setActivationPolicy:NSApplicationActivationPolicyProhibited];
    NSWindow *window=[[NSWindow alloc] initWithContentRect:NSMakeRect(700,400,312,416) styleMask:NSWindowStyleMaskBorderless backing:NSBackingStoreBuffered defer:NO];
    window.title=@"BigFatBlueFish";[window orderFront:nil];
    FishGlassStatusUpdate("工作中 · 5 项","working");SetTasks(5,@"两行内容完整显示");
    Class cls=object_getClass(taskPanel);
    Method frame=class_getInstanceMethod(cls,@selector(setFrame:display:));originalFrame=method_getImplementation(frame);
    Method display=class_getInstanceMethod(cls,@selector(displayIfNeeded));originalDisplay=method_getImplementation(display);
    class_replaceMethod(cls,@selector(setFrame:display:),(IMP)SetFrameProbe,method_getTypeEncoding(frame));
    class_replaceMethod(cls,@selector(displayIfNeeded),(IMP)DisplayProbe,method_getTypeEncoding(display));
    for(NSUInteger i=0;i<9;i++) {
        NSString *sid=taskData[(i+1)%taskData.count][@"session_id"];
        FishGlassStatusShowTask(sid.UTF8String);
        FishTaskRow *incoming=taskStack.incoming;
        if(incoming) {
            [taskStack advanceInsertion:.15];[taskPanel displayIfNeeded];
            [taskStack advanceInsertion:.36];
            Require(taskStack.front==incoming,@"Completed insertion keeps the already-visible glass lens instead of replacing it");
        }
        SetTasks(i%2 ? 5 : 6,@"动画交接时的最新消息");
        SettleTaskAnimation();[taskPanel displayIfNeeded];CheckCollapsed(taskData.count);
    }
    FishGlassStatusShowTask([taskData[0][@"session_id"] UTF8String]);
    if(taskStack.insertionActive)[taskStack advanceInsertion:.18];
    FishGlassStatusShowTask([taskData[1][@"session_id"] UTF8String]);
    SettleTaskAnimation();CheckCollapsed(taskData.count);
    FishGlassStatusShowTask(taskData.lastObject[@"session_id"] ? [taskData.lastObject[@"session_id"] UTF8String] : "");
    FishGlassStatusShowTasks();SettleTaskAnimation();
    SetTasksExpanded(NO,YES);SettleTaskAnimation();[taskPanel displayIfNeeded];
    Require(NSWorkspace.sharedWorkspace.accessibilityDisplayShouldReduceMotion || shrinks>=9,@"Exercise repeated insertion-canvas shrink boundaries");
    Require(presentations>=20,@"Observe actual presentation requests, not a layout-forced screenshot");
    class_replaceMethod(cls,@selector(setFrame:display:),originalFrame,method_getTypeEncoding(frame));
    class_replaceMethod(cls,@selector(displayIfNeeded),originalDisplay,method_getTypeEncoding(display));
    FishGlassStatusDestroy();[window orderOut:nil];
    NSLog(@"PASS: %lu presentation boundaries, %lu canvas shrinks; complete glass/text bounds and same-lens handoff; count changes/list interruption",(unsigned long)presentations,(unsigned long)shrinks);
    return 0;
} }
