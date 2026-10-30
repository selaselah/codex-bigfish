// Run in a logged-in macOS session; exercises the production AppKit views.
// xcrun clang -fobjc-arc -framework Cocoa -framework QuartzCore \
//   Native/FishStatusGlass/test_task_stack.m -o work/fish-task-stack-test
#import "FishGlassStatus.m"

static void Require(BOOL condition, NSString *message) {
    if (!condition) { NSLog(@"FAIL: %@",message); exit(1); }
}
static void SetTasks(NSUInteger count, NSString *summary) {
    NSMutableArray *items = [NSMutableArray new];
    for (NSUInteger i=0; i<count; i++) [items addObject:@{
        @"session_id":[NSString stringWithFormat:@"00000000-0000-4000-8000-%012lu",(unsigned long)i],
        @"title":[NSString stringWithFormat:@"任务 %lu",(unsigned long)i+1],
        @"summary":summary, @"message":summary,
        @"expires_at":@(NSDate.date.timeIntervalSince1970+3600)}];
    NSData *json = [NSJSONSerialization dataWithJSONObject:@{@"tasks":items} options:0 error:nil];
    FishGlassStatusUpdateTasks([[NSString alloc] initWithData:json encoding:NSUTF8StringEncoding].UTF8String);
}
static void CheckRowEdges(FishTaskRow *row) {
    [row layoutSubtreeIfNeeded];
    if (@available(macOS 26.0, *)) {
        Require(fabs(NSMinX(row.glass.frame)*MAX(1,row.window.backingScaleFactor)-.5)<1e-6, @"Left glass edge keeps half a backing pixel inside the row");
        Require(fabs((NSMaxX(row.bounds)-NSMaxX(row.glass.frame))*MAX(1,row.window.backingScaleFactor)-.5)<1e-6, @"Right glass edge keeps the same half-pixel margin");
        NSGlassEffectView *glass=(NSGlassEffectView *)row.glass;
        Require(glass.style==NSGlassEffectViewStyleClear && !glass.tintColor && glass.cornerRadius==29, @"Original native glass material and radius retained");
        Require(row.backdrop.state==NSVisualEffectStateActive && row.backdrop.alphaValue==.58, @"Active HUD backing retained");
    }
    Require(fabs([row convertPoint:NSZeroPoint fromView:row.titleLabel].x-18)<1e-6 && fabs([row convertPoint:NSZeroPoint fromView:row.messageLabel].x-18)<1e-6, @"Text stays at its original row coordinates");
    Require(NSEqualRects(row.button.frame,row.bounds), @"Hit target keeps the full original card bounds");
}
static void CheckCollapsed(NSUInteger count) {
    Require(!tasksExpanded, @"Data updates must not expand the list");
    Require(taskPanel.visible == (count>0), @"Collapsed stack visibility follows the task count");
    if (!count) return;
    Require(taskScroll.hidden && !taskStack.hidden, @"Collapsed content selection");
    Require(taskPanel.frame.size.height == 74+(MIN(count,3)-1)*6, @"Compact stack height");
    Require([taskStack.countLabel.stringValue isEqualToString:[NSString stringWithFormat:@"%lu 项",(unsigned long)count]], @"Actual count, including counts above three");
    NSUInteger backs=0;
    for (NSView *back in taskStack.backs) if (!back.hidden) {
        backs++;
        Require(NSContainsRect(taskStack.bounds,back.frame), @"Backing card stays inside the window");
        CheckRowEdges((FishTaskRow *)back);
    }
    Require(backs==MIN(count-1,2), @"At most two backing cards");
    Require(NSContainsRect(taskStack.bounds,taskStack.front.frame), @"Front card is not clipped");
    CheckRowEdges(taskStack.front);
    Require(taskStack.front.titleLabel.frame.size.width==260, @"Stack title preserves space for the task count");
    Require([petWindow.accessibilityChildren containsObject:taskStack.button] && ![petWindow.accessibilityChildren containsObject:taskScroll], @"Only the stack control is exposed while collapsed");
}
int main(void) { @autoreleasepool {
    [NSApplication.sharedApplication setActivationPolicy:NSApplicationActivationPolicyProhibited];
    NSWindow *window=[[NSWindow alloc] initWithContentRect:NSMakeRect(920,200,312,416)
        styleMask:NSWindowStyleMaskBorderless backing:NSBackingStoreBuffered defer:NO];
    window.title=@"BigFatBlueFish";
    [window orderFront:nil];
    // Data may arrive before Unity's first status/caption update.
    SetTasks(3,@"正在思考");
    FishGlassStatusUpdate("工作中 · 3 项","working");
    NSRect parentFrame=window.frame, capsuleFrame=statusPanel.frame;
    NSWindow *keyWindow=NSApp.keyWindow;
    for (NSNumber *count in @[@0,@1,@2,@3,@6,@64]) {
        SetTasks(count.unsignedIntegerValue,@"正在思考");
        CheckCollapsed(count.unsignedIntegerValue);
    }
    [taskStack.button performClick:nil];
    Require(tasksExpanded && taskStack.hidden && !taskScroll.hidden, @"Click stack to expand");
    Require(taskPanel.frame.size.height==342 && taskRows.count==64, @"Scrollable expanded list retains every task");
    for (FishTaskRow *row in taskRows.allValues) CheckRowEdges(row);
    Require([petWindow.accessibilityChildren containsObject:taskScroll], @"Expanded rows are accessible");
    CGFloat top=taskDocument.bounds.size.height-taskScroll.contentView.bounds.size.height;
    [taskScroll.contentView scrollToPoint:NSMakePoint(0,top-120)];
    ToggleTasks();
    SetTasks(64,@"新的公开进度");
    CheckCollapsed(64);
    Require([taskStack.front.messageLabel.stringValue isEqualToString:@"新的公开进度"], @"Collapsed preview refreshes");
    [taskStack.button performClick:nil];
    CGFloat distance=taskDocument.bounds.size.height-NSMaxY(taskScroll.contentView.bounds);
    Require(fabs(distance-120)<.001, @"Collapsing and live updates preserve scroll position");
    // A real AppKit local event takes the same outside-click monitor path.
    NSEvent *event=[NSEvent mouseEventWithType:NSEventTypeLeftMouseDown
        location:NSMakePoint(156,300) modifierFlags:0 timestamp:0
        windowNumber:window.windowNumber context:nil eventNumber:1 clickCount:1 pressure:1];
    [NSApp sendEvent:event];
    CheckCollapsed(64);
    Require(!taskPanel.canBecomeKeyWindow && !taskPanel.canBecomeMainWindow && NSApp.keyWindow==keyWindow, @"Task UI never steals keyboard focus");
    Require(NSEqualRects(window.frame,parentFrame) && NSEqualRects(statusPanel.frame,capsuleFrame), @"Avatar window and original status capsule geometry unchanged");
    FishGlassStatusSetVisible(0);
    Require(!statusPanel.visible && !taskPanel.visible, @"Hide also hides the stack");
    FishGlassStatusSetVisible(1);
    CheckCollapsed(64);
    SetTasks(0,@"");
    CheckCollapsed(0);
    FishGlassStatusShowTasks();
    Require(tasksExpanded && taskPanel.visible && taskRows.count==1, @"Explicit empty-list opening shows a placeholder");
    ToggleTasks();
    CheckCollapsed(0);
    FishGlassStatusDestroy();
    Require(!taskPanel && !taskStack && !statusPanel && !outsideClickMonitor && !localClickMonitor, @"Destroy releases views and event monitors");
    [window orderOut:nil];
    NSLog(@"PASS: default stack0/1/2/3/6/64, click expand, outside collapse, live preview, scroll, AX, focus, geometry, glass edge alignment, fixed text/hit targets, hide, empty, cleanup");
    return 0;
} }
