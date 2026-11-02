// Standalone production-view regression; never reads/writes live task files.
#define main OriginalTaskStackMain
#import "test_task_stack.m"
#undef main

static NSString *ID(NSUInteger index) {
    return [NSString stringWithFormat:@"00000000-0000-4000-8000-%012lu",(unsigned long)index];
}
static void Select(NSUInteger index) {
    FishGlassStatusShowTask(ID(index).UTF8String);SettleTaskAnimation();
    Require(tasksExpanded && [selectedTaskID isEqualToString:ID(index)],@"Cooker opens its exact task ID");
    Require(taskRows.count==1 && taskRows[ID(index)] && DisplayedTaskItems().count==1,@"Only the selected card is present, not the complete task list");
    Require(taskData.count==3 && taskPanel.frame.size.height==76,@"Single-card view retains all tasks and uses one-card height");
    CheckRowEdges(taskRows[ID(index)]);CheckStatusGlass();
}
int main(void) { @autoreleasepool {
    [NSApplication sharedApplication];[NSApp setActivationPolicy:NSApplicationActivationPolicyAccessory];
    NSWindow *window=[[NSWindow alloc] initWithContentRect:NSMakeRect(500,400,312,416) styleMask:NSWindowStyleMaskBorderless backing:NSBackingStoreBuffered defer:NO];
    window.title=@"BigFatBlueFish";[window orderFront:nil];
    FishGlassStatusUpdate("工作中 · 3 项","working");SetTasks(3,@"原消息");
    float x,y,sx,sy;
    for(NSNumber *type in @[@(NSEventTypeLeftMouseDown),@(NSEventTypeLeftMouseUp)]) {
        NSEvent *event=[NSEvent mouseEventWithType:type.unsignedIntegerValue location:NSMakePoint(40,50) modifierFlags:0 timestamp:NSProcessInfo.processInfo.systemUptime windowNumber:window.windowNumber context:nil eventNumber:1 clickCount:1 pressure:1];
        CaptureAvatarPointer(event);
    }
    Require(FishGlassStatusPollAvatarPointer(&x,&y,&sx,&sy)==1 && fabs(x-40.0/312)<1e-6 && fabs(y-50.0/416)<1e-6,@"Queued down preserves normalized viewport coordinates without sampling the later cursor");
    Require(FishGlassStatusPollAvatarPointer(&x,&y,&sx,&sy)==3 && fabs(sx-(window.frame.origin.x+40))<.001 && fabs(sy-(window.frame.origin.y+50))<.001,@"Queued up preserves original desktop coordinates and order for fast clicks");
    Require(!FishGlassStatusPollAvatarPointer(&x,&y,&sx,&sy),@"Pointer queue drains exactly once");
    NSRect status=statusPanel.frame;NSSize size=window.frame.size;
    Select(2);Select(0);Select(1);
    NSEvent *outside=[NSEvent mouseEventWithType:NSEventTypeLeftMouseDown location:NSMakePoint(4,400) modifierFlags:0 timestamp:NSProcessInfo.processInfo.systemUptime windowNumber:window.windowNumber context:nil eventNumber:2 clickCount:1 pressure:1];
    HandleTaskOutsideClick(outside);SettleTaskAnimation();
    Require(tasksExpanded && [selectedTaskID isEqualToString:ID(1)],@"Selected cooker card stays pinned while using another part of the desktop");
    Require(NSEqualRects(statusPanel.frame,status)&&NSEqualSizes(size,window.frame.size),@"Selecting different cookers does not move the status dock or resize the avatar");
    SetTasks(3,@"最新消息");SettleTaskAnimation();
    Require(taskRows.count==1 && [((FishTaskRow *)taskRows[ID(1)]).messageLabel.stringValue isEqualToString:@"最新消息"],@"Live messages update only the selected task");
    FishGlassStatusShowTask("not-a-task");FishGlassStatusShowTask(ID(50).UTF8String);
    Require([selectedTaskID isEqualToString:ID(1)]&&taskRows.count==1,@"Invalid or stale cooker IDs never open another task");
    CycleTaskCards();SettleTaskAnimation();
    Require(!tasksExpanded && !selectedTaskID && taskStack.count==3,@"First arrow folds back to the complete three-task stack");
    CycleTaskCards();SettleTaskAnimation();Require(!taskCardsShown,@"Second arrow hides the cards");
    Select(0);FishGlassStatusShowTasks();SettleTaskAnimation();
    Require(!selectedTaskID && tasksExpanded && taskRows.count==3,@"Explicit normal list opening restores all tasks");
    HandleTaskOutsideClick(outside);SettleTaskAnimation();Require(!tasksExpanded,@"Ordinary complete list still folds on outside click");
    Select(2);SetTasks(2,@"剩余任务");SettleTaskAnimation();
    Require(!selectedTaskID && !tasksExpanded && taskStack.count==2,@"Completed/expired selected task closes instead of showing a different task");
    SetTasks(3,@"继续");Select(1);FishGlassStatusSetVisible(0);
    Require(!selectedTaskID && !tasksExpanded,@"Hiding clears selection");
    FishGlassStatusSetVisible(1);Select(1);FishGlassStatusDestroy();PumpFor(.35);
    Require(!selectedTaskID&&!taskPanel&&!taskAnimationTimer&&!FishGlassStatusPollAvatarPointer(&x,&y,&sx,&sy),@"Destroy clears selection, pointer queue and timers");
    [window orderOut:nil];
    NSLog(@"PASS: cooker ID selection/switch/latest message/single-card height/Regular edges/stable dock/stale ID/arrow fold-hide/full-list restore/completion/hide/destroy");return 0;
} }
