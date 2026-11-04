// Isolated production UI fixture; no live tasks, files or Codex chats changed.
#define main OriginalTaskStackMain
#import "test_task_stack.m"
#undef main

static NSUInteger navigations;
static BOOL ObserveNavigation(id workspace,SEL command,NSURL *url) { navigations++;return YES; }
static NSString *ID(NSUInteger i) { return [NSString stringWithFormat:@"00000000-0000-4000-8000-%012lu",(unsigned long)i]; }
static NSMutableArray *Items(NSUInteger count,NSString *turn,NSString *state) {
    NSMutableArray *items=[NSMutableArray new];
    for(NSUInteger i=0;i<count;i++)[items addObject:[@{@"session_id":ID(i),@"turn_id":turn,@"state":state,
        @"title":[NSString stringWithFormat:@"任务%lu",(unsigned long)i],@"summary":@"本轮最新消息",
        @"expires_at":@(NSDate.date.timeIntervalSince1970+3600)} mutableCopy]];
    return items;
}
static void Publish(NSArray *items) {
    NSData *json=[NSJSONSerialization dataWithJSONObject:@{@"tasks":items} options:0 error:nil];
    FishGlassStatusUpdateTasks([[NSString alloc] initWithData:json encoding:NSUTF8StringEncoding].UTF8String);
    SettleTaskAnimation();
}
static void Capture(NSString *name) {
    const char *directory=getenv("FISH_DISMISS_CAPTURE_DIR");if(!directory)return;
    NSView *view=taskPanel.contentView;
    NSBitmapImageRep *bitmap=[view bitmapImageRepForCachingDisplayInRect:view.bounds];
    [view cacheDisplayInRect:view.bounds toBitmapImageRep:bitmap];
    NSString *path=[[NSString stringWithUTF8String:directory] stringByAppendingPathComponent:[name stringByAppendingString:@".png"]];
    Require([[bitmap representationUsingType:NSBitmapImageFileTypePNG properties:@{}] writeToFile:path atomically:YES],@"Save native control layout for visual review");
}
static void CheckClose(FishTaskRow *row) {
    Require(row.dismissButton.enabled && !row.dismissButton.hidden,@"A live card exposes its close control");
    NSRect close=[taskPanel.contentView convertRect:row.dismissButton.bounds fromView:row.dismissButton];
    Require(NSContainsRect(taskPanel.contentView.bounds,close),@"The entire circular close control fits the existing window");
    NSPoint hit=NSMakePoint(NSMidX(close),NSMidY(close));
    Require([taskPanel.contentView hitTest:hit]==row.dismissButton,@"Close wins hit testing over the glass and card navigation button");
    Require([row.dismissButton.accessibilityLabel isEqualToString:@"关闭本轮通知"],@"Close announces turn-scoped notification dismissal");
}
int main(void) { @autoreleasepool {
    [NSApplication.sharedApplication setActivationPolicy:NSApplicationActivationPolicyProhibited];
    Class cls=object_getClass(NSWorkspace.sharedWorkspace);Method open=class_getInstanceMethod(cls,@selector(openURL:));IMP originalOpen=method_getImplementation(open);
    class_replaceMethod(cls,@selector(openURL:),(IMP)ObserveNavigation,method_getTypeEncoding(open));
    NSWindow *window=[[NSWindow alloc] initWithContentRect:NSMakeRect(600,400,312,416) styleMask:NSWindowStyleMaskBorderless backing:NSBackingStoreBuffered defer:NO];
    window.title=@"BigFatBlueFish";[window orderFront:nil];
    FishGlassStatusUpdate("工作中 · 3 项","working");
    NSMutableArray *items=Items(3,@"turn-a",@"working");Publish(items);
    NSRect dock=statusPanel.frame;NSString *caption=overlay.caption.stringValue;
    CheckClose(taskStack.front);Capture(@"folded-close");
    for(FishTaskRow *back in taskStack.backs)Require(back.dismissButton.hidden,@"Decorative rear lenses have no actionable close controls");
    [taskStack.front.dismissButton performClick:nil];SettleTaskAnimation();
    Require(taskSnapshot.count==3 && taskData.count==2 && !TaskWithID(ID(0)),@"Dismiss only filters the notification, preserving every lifecycle item");
    Require([taskStack.front.sessionID isEqualToString:ID(1)],@"Next existing card becomes front without deleting the stack");
    Require([overlay.caption.stringValue isEqualToString:caption] && NSEqualRects(statusPanel.frame,dock),@"Dismiss does not change working count or status dock");
    Require([petWindow.accessibilityChildren containsObject:taskStack.front.dismissButton],@"Folded close is reachable through the passive pet window");
    items[0][@"summary"]=@"同一轮继续更新";items[0][@"state"]=@"waiting";Publish(items);
    Require(!TaskWithID(ID(0)),@"Progress and waiting updates cannot resurrect a dismissed turn");
    items[0][@"state"]=@"complete";Publish(items);Require(!TaskWithID(ID(0)),@"Completion of the same turn remains dismissed");
    FishTaskRow *stale=[[FishTaskRow alloc] initWithFrame:NSMakeRect(0,0,330,56)];[stale updateTask:items[0]];
    items[0][@"turn_id"]=@"turn-b";items[0][@"state"]=@"working";Publish(items);
    Require(taskData.count==3 && TaskWithID(ID(0)) && !dismissedTaskTurns[ID(0)],@"A new turn restores its notification even without an intervening empty snapshot");
    [stale.dismissButton performClick:nil];Require(TaskWithID(ID(0))!=nil,@"Queued close from the old turn cannot dismiss the new turn");
    FishGlassStatusShowTasks();SettleTaskAnimation();CheckClose(taskRows[ID(1)]);Capture(@"expanded-close");
    [((FishTaskRow *)taskRows[ID(1)]).dismissButton performClick:nil];SettleTaskAnimation();
    Require(tasksExpanded && taskData.count==2 && taskRows.count==2,@"Closing one expanded card keeps the remaining multi-task list");
    [((FishTaskRow *)taskRows[ID(2)]).dismissButton performClick:nil];SettleTaskAnimation();
    Require(!tasksExpanded && taskData.count==1 && taskPanel.visible,@"One remaining notification uses the existing single-card presentation");
    CheckClose(taskStack.front);[taskStack.front.dismissButton performClick:nil];SettleTaskAnimation();
    Require(!taskPanel.visible && taskData.count==0 && taskSnapshot.count==3,@"Closing the last notification hides cards while all tasks continue");
    Publish(items);Require(!taskPanel.visible && taskData.count==0,@"Identical periodic snapshots preserve all turn dismissals");
    FishGlassStatusShowTasks();SettleTaskAnimation();
    Require(taskPanel.visible && ((FishTaskRow *)taskRows[@""]).dismissButton.hidden && !((FishTaskRow *)taskRows[@""]).button.enabled,@"All-dismissed placeholder is informative and non-actionable");
    FishGlassStatusShowTask(ID(2).UTF8String);SettleTaskAnimation();
    Require(taskData.count==1 && [taskStack.front.sessionID isEqualToString:ID(2)] && !tasksExpanded,@"Clicking its cooker explicitly restores only that notification");
    CheckClose(taskStack.front);Require(navigations==0,@"Closing/revealing notifications never navigates to or stops a chat");
    Publish(@[]);Require(dismissedTaskTurns.count==0,@"Completed/expired sessions release their dismissal records");
    items=Items(2,@"",@"working");Publish(items);
    [taskStack.front.dismissButton performClick:nil];SettleTaskAnimation();Publish(items);
    Require(!TaskWithID(ID(0)),@"Legacy snapshots without turn IDs still preserve dismissal through messages");
    items[0][@"state"]=@"complete";Publish(items);items[0][@"state"]=@"working";Publish(items);
    Require(TaskWithID(ID(0))!=nil,@"Legacy completed-to-working boundary restores a notification");
    FishGlassStatusShowTask(ID(1).UTF8String);
    if(taskStack.insertionActive)Require(taskStack.front.dismissButton.hidden && taskStack.incoming.dismissButton.hidden,@"Moving lenses cannot receive stale dismiss clicks");
    SettleTaskAnimation();CheckClose(taskStack.front);
    FishGlassStatusDestroy();Require(!taskSnapshot && !dismissedTaskTurns,@"Destroy releases notification bookkeeping");
    class_replaceMethod(cls,@selector(openURL:),originalOpen,method_getTypeEncoding(open));[window orderOut:nil];
    NSLog(@"PASS: close hit-testing/full bounds/AX; folded and expanded dismissal; unchanged lifecycle/count/dock; progress/wait/completion suppression; new-turn/stale-click guard; explicit cooker restore; all-muted placeholder; legacy boundaries/expiry/animation/teardown; no navigation");return 0;
} }
