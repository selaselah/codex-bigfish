// Production-view fixture. URL opening is observed in this process only;
// fake tasks never enter the live bridge and no Codex chat is actually opened.
#define main OriginalTaskStackMain
#import "test_task_stack.m"
#undef main

static NSMutableArray<NSURL *> *openedURLs;
static BOOL ObserveOpen(id workspace,SEL command,NSURL *url) { [openedURLs addObject:url];return YES; }
static NSString *SID(NSUInteger n) { return [NSString stringWithFormat:@"00000000-0000-4000-8000-%012lu",(unsigned long)n]; }
static void CaptureAction(NSString *name) {
    const char *folder=getenv("FISH_STACK_CAPTURE_DIR");if(!folder)return;
    NSView *view=taskPanel.contentView;
    NSBitmapImageRep *bitmap=[view bitmapImageRepForCachingDisplayInRect:view.bounds];
    [view cacheDisplayInRect:view.bounds toBitmapImageRep:bitmap];
    NSString *path=[[NSString stringWithUTF8String:folder] stringByAppendingPathComponent:[name stringByAppendingString:@".png"]];
    Require([[bitmap representationUsingType:NSBitmapImageFileTypePNG properties:@{}] writeToFile:path atomically:YES],@"Save actual action frame");
}
int main(void) { @autoreleasepool {
    [NSApplication.sharedApplication setActivationPolicy:NSApplicationActivationPolicyProhibited];
    openedURLs=[NSMutableArray new];Class workspaceClass=object_getClass(NSWorkspace.sharedWorkspace);
    Method open=class_getInstanceMethod(workspaceClass,@selector(openURL:));IMP originalOpen=method_getImplementation(open);
    class_replaceMethod(workspaceClass,@selector(openURL:),(IMP)ObserveOpen,method_getTypeEncoding(open));
    NSWindow *window=[[NSWindow alloc] initWithContentRect:NSMakeRect(600,400,312,416) styleMask:NSWindowStyleMaskBorderless backing:NSBackingStoreBuffered defer:NO];
    window.title=@"BigFatBlueFish";[window orderFront:nil];
    FishGlassStatusUpdate("工作中 · 3 项","working");SetTasks(3,@"当前任务");
    NSRect dock=statusPanel.frame,parent=window.frame,canvas=taskPanel.frame;
    FishTaskRow *front=taskStack.front;NSArray *order=[stackOrder copy];CaptureAction(@"shake-before");
    FishGlassStatusShowTask(SID(0).UTF8String);
    if(!NSWorkspace.sharedWorkspace.accessibilityDisplayShouldReduceMotion) {
        Require(taskStack.shakeActive && taskStack.shakeTimer && !taskStack.insertionActive,@"Already-front cooker shakes without placing another card");
        [taskStack advanceShake:.025];
        Require(taskStack.shakeOffset>1 && taskStack.shakeOffset<=4 && taskStack.front.frame.origin.x>10,@"Front glass moves visibly within the existing side padding");
        Require(taskStack.front==front && [stackOrder isEqualToArray:order],@"Shake preserves the lens, task order and rear stack");
        Require(NSContainsRect(taskStack.bounds,taskStack.front.frame),@"Shaking glass stays within its canvas");CaptureAction(@"shake-right");
        [taskStack advanceShake:.07];Require(taskStack.shakeOffset<0,@"Damped shake changes direction");CaptureAction(@"shake-left");
        NSUInteger generation=taskStack.shakeGeneration;SetTasks(3,@"消息继续刷新");
        Require(taskStack.shakeGeneration==generation,@"Ordinary message updates do not restart the shake");
        FishGlassStatusShowTask(SID(0).UTF8String);Require(taskStack.shakeGeneration>generation,@"Repeated same-pot click safely restarts feedback");
        PumpFor(.025);Require(taskStack.shakeActive && taskStack.shakeOffset!=0,@"The real run-loop timer advances the shake");
    }
    SettleTaskAnimation();CheckCollapsed(3);CaptureAction(@"shake-settled");
    Require(NSEqualRects(dock,statusPanel.frame)&&NSEqualRects(parent,window.frame)&&NSEqualRects(canvas,taskPanel.frame),@"Shake leaves every window and final dock unchanged");
    FishGlassStatusShowTask(SID(2).UTF8String);
    FishGlassStatusShowTask(SID(2).UTF8String);
    if(taskStack.insertionActive)Require(taskStack.pendingShake,@"Same-pot click during placing queues feedback after landing");
    SettleTaskAnimation();CheckCollapsed(3);
    FishGlassStatusShowTasks();SettleTaskAnimation();
    FishGlassStatusShowTask(SID(2).UTF8String);SettleTaskAnimation();CheckCollapsed(3);
    SetTasks(1,@"唯一任务");SettleTaskAnimation();CheckCollapsed(1);CaptureAction(@"single-card");
    Require([taskStack.button.accessibilityLabel containsString:@"Codex"] && ![taskStack.button.accessibilityLabel containsString:@"展开"],@"Single card announces its direct chat action");
    [taskStack.button performClick:nil];SettleTaskAnimation();
    Require(openedURLs.count==1 && [openedURLs.lastObject.absoluteString isEqualToString:[@"codex://threads/" stringByAppendingString:SID(0)]],@"One card click opens exactly its matching Codex deep link");
    Require(!tasksExpanded && NSHeight(taskPanel.frame)==72 && taskScroll.hidden,@"Single-card click never opens a duplicate list representation");
    FishGlassStatusShowTasks();SettleTaskAnimation();Require(!tasksExpanded && taskPanel.visible,@"Explicit task view also retains the one-card representation");
    CycleTaskCards();SettleTaskAnimation();Require(!taskCardsShown&&!taskPanel.visible,@"Single card can still be fully hidden");
    CycleTaskCards();SettleTaskAnimation();CheckCollapsed(1);Require(openedURLs.count==1,@"Status arrow toggles card visibility without navigating");
    [taskStack.button performClick:nil];SettleTaskAnimation();Require(openedURLs.count==2,@"Restored single card keeps direct chat navigation");
    SetTasks(2,@"新增第二项");SettleTaskAnimation();[taskStack.button performClick:nil];SettleTaskAnimation();
    Require(tasksExpanded && taskRows.count==2 && openedURLs.count==2,@"Two tasks regain full-list expansion without opening a chat");
    SetTasks(1,@"多项列表只剩一项");SettleTaskAnimation();CheckCollapsed(1);
    FishGlassStatusShowTask(SID(0).UTF8String);FishGlassStatusSetVisible(0);
    Require(!taskStack.shakeActive&&!taskStack.shakeTimer&&!taskStack.pendingShake,@"Hiding cancels all shake feedback");
    FishGlassStatusSetVisible(1);SettleTaskAnimation();
    FishGlassStatusShowTask(SID(0).UTF8String);FishGlassStatusShowTask(SID(99).UTF8String);
    FishGlassStatusDestroy();PumpFor(.3);Require(!taskPanel&&!taskStack&&!taskAnimationTimer,@"Destroy leaves no feedback callbacks or views");
    class_replaceMethod(workspaceClass,@selector(openURL:),originalOpen,method_getTypeEncoding(open));[window orderOut:nil];
    NSLog(@"PASS: current-front damped shake/real timer/repeat/message stability/queued landing/window invariance/cleanup; single card exact Codex deep link/no expanded duplicate/hidden toggle; one-two-one task transitions");return 0;
} }
