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
static void CheckStatusGlass(void) {
    [overlay layoutSubtreeIfNeeded];
    Require(overlay.pill.frame.size.height==40 && overlay.pill.frame.size.width>=140, @"Status capsule keeps its accepted dimensions");
    if (@available(macOS 26.0, *)) {
        Require([overlay.glass isKindOfClass:NSGlassEffectView.class] && !overlay.backdrop, @"Status capsule also uses direct native glass without HUD");
        NSGlassEffectView *glass=(NSGlassEffectView *)overlay.glass;
        Require(glass.style==NSGlassEffectViewStyleRegular && !glass.tintColor && glass.alphaValue==1 && glass.cornerRadius==20, @"Status and task surfaces share Regular Liquid Glass");
        Require(glass.contentView==overlay.content, @"Status text receives native glass content treatment");
        CGFloat scale=MAX(1,overlay.window.backingScaleFactor);
        Require(fabs(NSMinX(glass.frame)*scale-.5)<1e-6 && fabs((NSMaxX(overlay.pill.bounds)-NSMaxX(glass.frame))*scale-.5)<1e-6, @"Status edges retain symmetric half-pixel margins");
    }
    Require(overlay.caption.frame.size.width>=overlay.caption.cell.cellSize.width, @"Short status caption fits alongside its chevron without truncation");
    Require(overlay.content.subviews.count==2 && [overlay.content.subviews containsObject:overlay.caption] && [overlay.content.subviews containsObject:overlay.disclosureButton], @"Status has only its caption and disclosure button, no activity dot");
    Require(fabs([overlay.pill convertPoint:NSZeroPoint fromView:overlay.caption].x-16)<1e-6, @"Caption reclaims the dot's space with compensated16pt inset");
    NSRect arrow=[overlay.pill convertRect:overlay.disclosureButton.bounds fromView:overlay.disclosureButton];
    Require(overlay.disclosureButton.image && NSContainsRect(overlay.pill.bounds,arrow) && arrow.size.width==24 && arrow.size.height==24 && NSMidY(arrow)==20, @"Visible native chevron stays centered inside the status pill");
    Require([overlay.disclosureButton.accessibilityLabel isEqualToString:tasksExpanded ? @"收起任务卡片" : @"展开任务卡片"], @"Disclosure label follows actual expanded state");
    NSPoint arrowCenter=[overlay convertPoint:NSMakePoint(12,12) fromView:overlay.disclosureButton];
    Require([overlay hitTest:arrowCenter]==overlay.disclosureButton, @"Disclosure receives its own clicks despite the caption hit target");
    Require([petWindow.accessibilityChildren containsObject:overlay.disclosureButton], @"Disclosure is directly accessible from the pet window");
    Require(NSEqualRects(overlay.taskButton.frame,overlay.pill.frame), @"Status hit target retains the complete capsule");
}
static void CheckBelowStatus(void) {
    NSRect pill=[statusPanel convertRectToScreen:[overlay convertRect:overlay.pill.frame toView:nil]];
    NSRect screen=petWindow.screen.visibleFrame;
    Require(fabs(NSMaxY(taskPanel.frame)-NSMinY(pill))<1e-6, @"Task panel is anchored below the status capsule");
    Require(NSContainsRect(NSInsetRect(screen,8,8),taskPanel.frame), @"Task cards fit within the usable desktop");
    if (NSMidX(pill)-175>=NSMinX(screen)+8 && NSMidX(pill)+175<=NSMaxX(screen)-8)
        Require(fabs(NSMidX(taskPanel.frame)-NSMidX(pill))<1e-6, @"Task panel centers under the status capsule whenever space allows");
    CheckStatusGlass();
}
static void CheckRowEdges(FishTaskRow *row) {
    [row layoutSubtreeIfNeeded];
    if (@available(macOS 26.0, *)) {
        Require(fabs(NSMinX(row.glass.frame)*MAX(1,row.window.backingScaleFactor)-.5)<1e-6, @"Left glass edge keeps half a backing pixel inside the row");
        Require(fabs((NSMaxX(row.bounds)-NSMaxX(row.glass.frame))*MAX(1,row.window.backingScaleFactor)-.5)<1e-6, @"Right glass edge keeps the same half-pixel margin");
        NSGlassEffectView *glass=(NSGlassEffectView *)row.glass;
        Require(glass.style==NSGlassEffectViewStyleRegular && !glass.tintColor && glass.cornerRadius==28, @"Native Regular Liquid Glass matches the slimmer capsule");
        // AppKit can render this view without exposing a CALayer.
        Require(!row.backdrop && glass.alphaValue==1 && (!glass.layer || glass.layer.opacity==1), @"Direct system glass has no extra HUD backing or opacity override");
        Require(glass.contentView==row.content, @"Text receives the native glass content treatment");
        for (NSView *view in row.subviews) Require(![view isKindOfClass:NSVisualEffectView.class], @"No legacy material sibling on the Liquid Glass path");
    }
    Require(fabs([row convertPoint:NSZeroPoint fromView:row.titleLabel].x-16)<1e-6 && fabs([row convertPoint:NSZeroPoint fromView:row.messageLabel].x-16)<1e-6, @"Half-pixel compensation preserves the compact16pt text inset");
    Require(NSEqualRects(row.button.frame,row.bounds), @"Hit target keeps the full original card bounds");
}
static void CheckCollapsed(NSUInteger count) {
    Require(!tasksExpanded, @"Data updates must not expand the list");
    Require(taskPanel.visible == (count>0), @"Collapsed stack visibility follows the task count");
    if (!count) return;
    CheckBelowStatus();
    Require(taskScroll.hidden && !taskStack.hidden, @"Collapsed content selection");
    Require(taskPanel.frame.size.width==350 && taskPanel.frame.size.height == 72+(MIN(count,3)-1)*6, @"Slimmer stack canvas");
    Require(taskStack.front.frame.size.width==330 && taskStack.front.frame.size.height==56, @"Slimmer330x56pt task card");
    for (NSView *child in taskStack.subviews) Require(![child isKindOfClass:NSTextField.class], @"No separate visual task count");
    Require([taskStack.button.accessibilityLabel containsString:[NSString stringWithFormat:@"%lu 项",(unsigned long)count]], @"Task count remains available to VoiceOver");
    NSUInteger backs=0;
    for (NSView *back in taskStack.backs) if (!back.hidden) {
        backs++;
        Require(NSContainsRect(taskStack.bounds,back.frame), @"Backing card stays inside the window");
        CheckRowEdges((FishTaskRow *)back);
    }
    Require(backs==MIN(count-1,2), @"At most two backing cards");
    Require(NSContainsRect(taskStack.bounds,taskStack.front.frame), @"Front card is not clipped");
    CheckRowEdges(taskStack.front);
    Require(taskStack.front.titleLabel.frame.size.width==298, @"Title reclaims the space previously reserved for a count");
    Require([petWindow.accessibilityChildren containsObject:taskStack.button] && ![petWindow.accessibilityChildren containsObject:taskScroll], @"Only the stack control is exposed while collapsed");
}
int main(void) { @autoreleasepool {
    [NSApplication.sharedApplication setActivationPolicy:NSApplicationActivationPolicyProhibited];
    NSWindow *window=[[NSWindow alloc] initWithContentRect:NSMakeRect(920,450,312,416)
        styleMask:NSWindowStyleMaskBorderless backing:NSBackingStoreBuffered defer:NO];
    window.title=@"BigFatBlueFish";
    [window orderFront:nil];
    // Data may arrive before Unity's first status/caption update.
    SetTasks(3,@"正在思考");
    FishGlassStatusUpdate("工作中 · 3 项","working");
    CheckStatusGlass();
    NSRect parentFrame=window.frame, capsuleFrame=statusPanel.frame;
    NSWindow *keyWindow=NSApp.keyWindow;
    for (NSNumber *count in @[@0,@1,@2,@3,@6,@64]) {
        SetTasks(count.unsignedIntegerValue,@"正在思考");
        CheckCollapsed(count.unsignedIntegerValue);
    }
    [taskStack.button performClick:nil];
    Require(tasksExpanded && taskStack.hidden && !taskScroll.hidden, @"Click stack to expand");
    CheckBelowStatus();
    Require(taskPanel.frame.size.height==332 && taskRows.count==64, @"Scrollable expanded list retains every task");
    if (@available(macOS 26.0, *)) {
        Require([taskScroll.documentView isKindOfClass:NSGlassEffectContainerView.class], @"Expanded cards share native glass sampling");
        NSGlassEffectContainerView *group=(NSGlassEffectContainerView *)taskScroll.documentView;
        Require(group.contentView==taskDocument && group.spacing==0, @"Batch sampling preserves the separate expanded card gaps");
    }
    Require(NSEqualRects(taskListSurface.bounds,taskDocument.frame), @"Glass sampling surface follows the full scroll document");
    for (FishTaskRow *row in taskRows.allValues) CheckRowEdges(row);
    Require([petWindow.accessibilityChildren containsObject:taskScroll], @"Expanded rows are accessible");
    CGFloat top=taskDocument.bounds.size.height-taskScroll.contentView.bounds.size.height;
    [taskScroll.contentView scrollToPoint:NSMakePoint(0,top-120)];
    [overlay.disclosureButton performClick:nil];
    SetTasks(64,@"新的公开进度");
    CheckCollapsed(64);
    Require([taskStack.front.messageLabel.stringValue isEqualToString:@"新的公开进度"], @"Collapsed preview refreshes");
    [overlay.disclosureButton performClick:nil];
    Require(tasksExpanded, @"Status chevron expands the task list");
    CheckBelowStatus();
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
    // A low placement must move the group up, not clip the list or shrink the
    // character. Collapsing must not snap the character back down afterwards.
    NSRect screen=window.screen.visibleFrame;
    [window setFrameOrigin:NSMakePoint(NSMinX(screen)+10,NSMinY(screen)+20)];
    CheckCollapsed(64);
    Require(NSEqualSizes(window.frame.size,parentFrame.size), @"Screen-edge correction never shrinks the pet");
    [overlay.disclosureButton performClick:nil];
    CheckBelowStatus();
    Require(tasksExpanded && NSMinY(window.frame)>NSMinY(screen)+20, @"Opening a low-positioned list makes room below the pet");
    NSRect lifted=window.frame;
    [overlay.disclosureButton performClick:nil];
    CheckCollapsed(64);
    Require(NSEqualRects(window.frame,lifted), @"Collapse keeps the current pet position instead of snapping it back");
    FishGlassStatusSetVisible(0);
    Require(!statusPanel.visible && !taskPanel.visible, @"Hide also hides the stack");
    FishGlassStatusSetVisible(1);
    CheckStatusGlass();
    CheckCollapsed(64);
    SetTasks(0,@"");
    CheckCollapsed(0);
    FishGlassStatusShowTasks();
    Require(tasksExpanded && taskPanel.visible && taskRows.count==1, @"Explicit empty-list opening shows a placeholder");
    ToggleTasks();
    CheckCollapsed(0);
    FishGlassStatusDestroy();
    Require(!taskPanel && !taskStack && !taskListSurface && !statusPanel && !outsideClickMonitor && !localClickMonitor, @"Destroy releases views and event monitors");
    [window orderOut:nil];
    NSLog(@"PASS: below-status stack0/1/2/3/6/64, no dot, native chevron toggle/hit/AX, screen-edge fit without pet resizing, click expand, outside collapse, live preview, scroll, focus, Regular glass/half-pixel edges, hide, empty, cleanup");
    return 0;
} }
