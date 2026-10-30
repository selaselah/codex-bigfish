// Run in a logged-in macOS session; exercises the production AppKit views.
// xcrun clang -fobjc-arc -framework Cocoa -framework QuartzCore \
//   Native/FishStatusGlass/test_task_stack.m -o work/fish-task-stack-test
#import "FishGlassStatus.m"

static void Require(BOOL condition, NSString *message) {
    if (!condition) { NSLog(@"FAIL: %@",message); exit(1); }
}
static void PumpFor(NSTimeInterval seconds) {
    NSDate *until=[NSDate dateWithTimeIntervalSinceNow:seconds];
    while (until.timeIntervalSinceNow>0) [NSRunLoop.mainRunLoop runMode:NSDefaultRunLoopMode beforeDate:[NSDate dateWithTimeIntervalSinceNow:MIN(.01,until.timeIntervalSinceNow)]];
}
static void SettleTaskAnimation(void) {
    CFTimeInterval limit=CACurrentMediaTime()+1;
    while(taskAnimationActive && CACurrentMediaTime()<limit) PumpFor(.01);
    Require(!taskAnimationActive && !taskAnimationTimer && !taskAnimationHost, @"Task transition completes and releases its driver and temporary views");
}
static void ClickControl(NSButton *button) {
    // Legacy list tests now open the folded stack, while the status arrow's
    // separate three-state cycle is exercised explicitly below.
    if (button==overlay.disclosureButton && TaskCardsPresent() && !tasksExpanded) button=taskStack.button;
    [button performClick:nil];SettleTaskAnimation();
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
    SettleTaskAnimation();
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
    Require([overlay.disclosureButton.accessibilityValue isEqualToString:tasksExpandUpward ? @"卡片向上展开" : @"卡片向下展开"], @"VoiceOver reports the actual expansion direction");
    Require(overlay.disclosureButton.image && NSContainsRect(overlay.pill.bounds,arrow) && arrow.size.width==24 && arrow.size.height==24 && NSMidY(arrow)==20, @"Visible native chevron stays centered inside the status pill");
    Require([overlay.disclosureButton.accessibilityLabel isEqualToString:tasksExpanded ? @"折叠任务列表" : (TaskCardsPresent() ? @"收缩任务卡片" : @"显示任务卡片")], @"Disclosure label follows actual expanded state");
    NSPoint arrowCenter=[overlay convertPoint:NSMakePoint(12,12) fromView:overlay.disclosureButton];
    Require([overlay hitTest:arrowCenter]==overlay.disclosureButton, @"Disclosure receives its own clicks despite the caption hit target");
    Require([petWindow.accessibilityChildren containsObject:overlay.disclosureButton], @"Disclosure is directly accessible from the pet window");
    Require(NSEqualRects(overlay.taskButton.frame,overlay.pill.frame), @"Status hit target retains the complete capsule");
}
static void CheckPlacement(void) {
    NSRect pill=[statusPanel convertRectToScreen:[overlay convertRect:overlay.pill.frame toView:nil]];
    NSRect screen=PetDockScreen().visibleFrame;
    Require(tasksExpandUpward==!statusAtTop, @"Expansion direction points toward the desktop interior");
    Require(statusAtTop ? NSMidY(pill)>NSMidY(PetDockFrame()) : NSMidY(pill)<NSMidY(PetDockFrame()), @"Status follows the top or bottom of the pet");
    Require(fabs(tasksExpandUpward ? NSMinY(taskPanel.frame)-NSMaxY(pill) : NSMaxY(taskPanel.frame)-NSMinY(pill))<1e-6, @"Task panel attaches to the correct status edge");
    Require(NSContainsRect(NSInsetRect(screen,8,8),pill), @"Painted status remains inside the usable desktop");
    Require(NSContainsRect(NSInsetRect(screen,8,8),taskPanel.frame), @"Task cards fit within the usable desktop");
    if (NSMidX(pill)-175>=NSMinX(screen)+8 && NSMidX(pill)+175<=NSMaxX(screen)-8)
        Require(fabs(NSMidX(taskPanel.frame)-NSMidX(pill))<1e-6, @"Task panel centers on the status capsule whenever space allows");
    if (tasksExpanded) {
        NSString *key=taskData.firstObject[@"session_id"] ?: @"";
        FishTaskRow *first=taskRows[key];
        Require(first && (tasksExpandUpward ? NSMinY(first.frame)==8 : NSMaxY(first.frame)==NSMaxY(taskDocument.bounds)-8), @"First task stays nearest the status anchor in either direction");
    }
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
    SettleTaskAnimation();
    Require(!tasksExpanded, @"Data updates must not expand the list");
    Require(taskPanel.visible == (count>0 && taskCardsShown), @"Folded visibility follows the task count and explicit shown preference");
    if (!count || !taskCardsShown) return;
    CheckPlacement();
    Require(taskScroll.hidden && !taskStack.hidden, @"Collapsed content selection");
    Require(taskStack.expandsUpward==tasksExpandUpward, @"Stack backing layers follow the expansion direction");
    Require(tasksExpandUpward ? NSMinY(taskStack.front.frame)==8 : NSMaxY(taskStack.front.frame)==NSMaxY(taskStack.bounds)-8, @"Front card stays nearest the status bar");
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
static NSEvent *StatusMouse(NSEventType type, NSPoint screen) {
    return [NSEvent mouseEventWithType:type location:[statusPanel convertPointFromScreen:screen]
        modifierFlags:0 timestamp:0 windowNumber:statusPanel.windowNumber context:nil
        eventNumber:1 clickCount:1 pressure:1];
}
static void CheckStatusDrag(NSButton *control) {
    Require([control isKindOfClass:FishStatusDragButton.class], @"Both caption and chevron support dragging");
    FishStatusDragButton *button=(FishStatusDragButton *)control;
    NSRect parent=petWindow.frame, status=statusPanel.frame, cards=taskPanel.frame, dock=PetDockFrame();
    BOOL expanded=tasksExpanded;
    NSPoint start=[statusPanel convertPointToScreen:[button convertPoint:NSMakePoint(NSMidX(button.bounds),NSMidY(button.bounds)) toView:nil]];
    [button mouseDown:StatusMouse(NSEventTypeLeftMouseDown,start)];
    [button mouseDragged:StatusMouse(NSEventTypeLeftMouseDragged,NSMakePoint(start.x+2,start.y+1))];
    Require(NSEqualRects(petWindow.frame,parent) && tasksExpanded==expanded, @"Minor pointer jitter neither moves the pet nor toggles before release");
    [button mouseDragged:StatusMouse(NSEventTypeLeftMouseDragged,NSMakePoint(start.x+40,start.y+20))];
    [button mouseDragged:StatusMouse(NSEventTypeLeftMouseDragged,NSMakePoint(start.x+65,start.y+35))];
    [button mouseUp:StatusMouse(NSEventTypeLeftMouseUp,NSMakePoint(start.x+65,start.y+35))];
    Require(NSEqualRects(petWindow.frame,NSOffsetRect(parent,65,35)), @"Two drag events use absolute desktop delta without cumulative feedback");
    Require(NSEqualRects(statusPanel.frame,NSOffsetRect(status,65,35)) && NSEqualRects(taskPanel.frame,NSOffsetRect(cards,65,35)), @"Status and collapsed or expanded cards move with the entire pet");
    Require(tasksExpanded==expanded && !button.pressed && !button.dragWindow, @"Drag release never toggles cards and releases tracking");
    CheckPlacement();
    [petWindow setFrameOrigin:parent.origin];
    start=[statusPanel convertPointToScreen:[button convertPoint:NSMakePoint(NSMidX(button.bounds),NSMidY(button.bounds)) toView:nil]];
    [button mouseDown:StatusMouse(NSEventTypeLeftMouseDown,start)];
    [button mouseDragged:StatusMouse(NSEventTypeLeftMouseDragged,NSMakePoint(start.x+2,start.y+1))];
    [button mouseUp:StatusMouse(NSEventTypeLeftMouseUp,NSMakePoint(start.x+2,start.y+1))];
    SettleTaskAnimation();
    Require(!tasksExpanded && taskCardsShown==expanded && NSEqualRects(PetDockFrame(),dock) && NSEqualRects(statusPanel.frame,status), @"A click with minor jitter steps expanded to folded or folded to hidden with a fixed dock/status");
    SetTaskMode(expanded ? FishTaskExpanded : FishTaskFolded,YES);SettleTaskAnimation();
    Require(tasksExpanded==expanded && NSEqualRects(petWindow.frame,parent), @"Restoring the previous card mode restores the exact reserved avatar frame");
}
static FishTaskAnimationEntry *MotionEntry(NSString *sid) {
    for (FishTaskAnimationEntry *entry in taskAnimationEntries) if ([entry.row.sessionID isEqualToString:sid]) return entry;
    return nil;
}
static void CheckTaskMotion(BOOL upward) {
    if (tasksExpanded) ClickControl(overlay.disclosureButton);
    NSRect screen=PetDockScreen().visibleFrame;
    NSPoint position=NSMakePoint(NSMinX(screen)+400,upward ? NSMinY(screen)+20 : NSMaxY(screen)-petWindow.frame.size.height-20);
    [petWindow setFrameOrigin:position];
    SetTasks(6,@"动画检查");
    CGFloat maxOffset=MAX(0,taskDocument.bounds.size.height-taskScroll.contentView.bounds.size.height);
    [taskScroll.contentView scrollToPoint:NSMakePoint(0,upward ? 0 : maxOffset)];
    NSRect avatar=petWindow.frame;
    ToggleTasks();
    if (NSWorkspace.sharedWorkspace.accessibilityDisplayShouldReduceMotion) {
        Require(!taskAnimationActive && tasksExpanded, @"Reduce Motion switches immediately without moving cards");
        ClickControl(overlay.disclosureButton); return;
    }
    Require(taskAnimationActive && taskAnimationTimer && taskAnimationHost && taskAnimationEntries.count<=8, @"Opening uses a bounded live native transition and timer");
    Require(taskScroll.hidden && taskStack.front.hidden && taskStack.button.hidden, @"Only transition rows and blank backing edges are drawn while unfolding");
    Require(![petWindow.accessibilityChildren containsObject:taskScroll], @"Temporary animated controls are not duplicated in VoiceOver");
    NSString *second=@"00000000-0000-4000-8000-000000000001";
    FishTaskAnimationEntry *entry=MotionEntry(second);
    Require(entry && !NSEqualRects(entry.fromFrame,entry.toFrame), @"A secondary card unfolds from the stack into its list position");
    CheckRowEdges(entry.row);
    NSRect from=entry.fromFrame,to=entry.toFrame;
    AdvanceTaskAnimation(.10);
    NSRect middle=entry.row.frame;CGFloat alpha=entry.row.alphaValue;
    Require(!NSEqualRects(middle,from) && !NSEqualRects(middle,to) && alpha>0 && alpha<1, @"The intermediate frame moves and fades rather than jumping directly to its destination");
    Require(upward ? middle.origin.y>from.origin.y && middle.origin.y<to.origin.y : middle.origin.y<from.origin.y && middle.origin.y>to.origin.y, @"Unfolding moves in the actual upper or lower expansion direction");
    ToggleTasks();
    entry=MotionEntry(second);
    Require(entry && NSEqualRects(entry.row.frame,middle) && entry.row.alphaValue==alpha, @"Rapid reversal begins at the current native frame and opacity without snapping");
    SettleTaskAnimation();
    CheckCollapsed(6);
    Require(NSEqualRects(petWindow.frame,avatar), @"Animation never moves or resizes the avatar");
    ToggleTasks();
    entry=MotionEntry(second);
    NSRect timerStart=entry.row.frame;
    PumpFor(.06);NSRect timerFirst=entry.row.frame;
    PumpFor(.06);NSRect timerSecond=entry.row.frame;
    Require(taskAnimationActive && !NSEqualRects(timerStart,timerFirst) && !NSEqualRects(timerFirst,timerSecond), @"The real run-loop timer advances multiple intermediate native frames before finishing");
    NSLog(@"Motion %@ live y: %g -> %g -> %g",upward ? @"up" : @"down",timerStart.origin.y,timerFirst.origin.y,timerSecond.origin.y);
    SettleTaskAnimation();
    ClickControl(overlay.disclosureButton);
    ToggleTasks();
    SetTasks(3,@"动画期间的新消息");
    Require(taskAnimationActive && taskAnimationNeedsRefresh, @"A live snapshot is deferred rather than interrupting moving cards");
    SettleTaskAnimation();
    Require(tasksExpanded && taskRows.count==3 && [taskStack.front.messageLabel.stringValue isEqualToString:@"动画期间的新消息"], @"The newest snapshot is applied at transition completion");
    CheckPlacement();
    ToggleTasks();
    [petWindow setFrameOrigin:NSMakePoint(position.x+10,position.y)];
    Require(!taskAnimationActive && !taskAnimationTimer && !taskAnimationHost, @"Dragging the pet cancels the transition and releases temporary views immediately");
    CheckCollapsed(3);
    ToggleTasks();
    FishGlassStatusSetVisible(0);
    Require(!taskAnimationActive && !taskPanel.visible && !statusPanel.visible, @"Hiding during animation cancels and hides all surfaces");
    FishGlassStatusSetVisible(1);
    CheckCollapsed(3);
}
static void ArrowClick(void) { [overlay.disclosureButton performClick:nil];SettleTaskAnimation(); }
static void CheckThreeStates(BOOL upward) {
    SetTaskMode(FishTaskHidden,NO);
    NSRect screen=PetDockScreen().visibleFrame;
    [petWindow setFrameOrigin:NSMakePoint(NSMinX(screen)+400,upward ? NSMinY(screen)+60 : NSMaxY(screen)-petWindow.frame.size.height-100)];
    SetTasks(3,@"三态检查");
    NSRect hiddenPet=petWindow.frame,bar=statusPanel.frame;
    Require(!TaskCardsPresent() && !taskPanel.visible && petTaskOffset==0, @"Hidden state shows no cards and restores the unreserved avatar");
    [overlay.disclosureButton performClick:nil];
    if (!NSWorkspace.sharedWorkspace.accessibilityDisplayShouldReduceMotion) {
        PumpFor(.08);
        Require(taskAnimationActive && fabs(petTaskOffset)>0 && fabs(petTaskOffset)<fabs(DesiredPetTaskOffset()), @"Showing the folded stack animates the avatar's reserved space");
        Require(NSEqualRects(statusPanel.frame,bar), @"Status anchor stays fixed during reservation animation");
    }
    SettleTaskAnimation();
    NSRect foldedPet=petWindow.frame,foldedCards=taskPanel.frame;
    Require(taskCardsShown && !tasksExpanded && taskPanel.visible && fabs(petTaskOffset)==92, @"Folded state reserves one stack height plus its gap");
    Require(upward ? foldedPet.origin.y==hiddenPet.origin.y+92 : foldedPet.origin.y==hiddenPet.origin.y-92, @"The avatar moves away from the fixed status toward the desktop interior");
    Require(NSEqualRects(statusPanel.frame,bar), @"Hidden and folded status frames are identical");
    ClickControl(taskStack.button);
    Require(tasksExpanded && taskPanel.frame.size.height>foldedCards.size.height, @"Clicking the stack expands the full list");
    Require(NSEqualRects(petWindow.frame,foldedPet) && NSEqualRects(statusPanel.frame,bar), @"Expansion keeps both the reserved avatar and status completely fixed");
    Require(upward ? NSMinY(taskPanel.frame)==NSMinY(foldedCards) : NSMaxY(taskPanel.frame)==NSMaxY(foldedCards), @"Folded and expanded cards share the same near-status edge");
    ArrowClick();
    Require(taskCardsShown && !tasksExpanded && taskPanel.visible && NSEqualRects(taskPanel.frame,foldedCards), @"First arrow click from expanded folds the list, without hiding it");
    Require(NSEqualRects(petWindow.frame,foldedPet) && NSEqualRects(statusPanel.frame,bar), @"Expanded-to-folded does not move the avatar or status");
    ArrowClick();
    Require(!taskCardsShown && !taskPanel.visible && NSEqualRects(petWindow.frame,hiddenPet) && NSEqualRects(statusPanel.frame,bar), @"Second arrow click hides the stack and restores the exact hidden-state avatar while keeping status fixed");
    SetTasks(6,@"收缩时的新消息");
    Require(!taskPanel.visible && petTaskOffset==0 && NSEqualRects(petWindow.frame,hiddenPet), @"Live messages respect the explicitly hidden preference");
    [overlay.disclosureButton performClick:nil];
    if (!NSWorkspace.sharedWorkspace.accessibilityDisplayShouldReduceMotion) {
        AdvanceTaskAnimation(.10);
        CGFloat offset=petTaskOffset;
        [overlay.disclosureButton performClick:nil];
        Require(petTaskOffset==offset, @"A rapid show/hide reversal starts from the current avatar offset without snapping");
    } else [overlay.disclosureButton performClick:nil];
    SettleTaskAnimation();
    Require(!TaskCardsPresent() && NSEqualRects(petWindow.frame,hiddenPet) && NSEqualRects(statusPanel.frame,bar), @"Interrupted reservation finishes back at the original hidden geometry");
    SetTaskMode(FishTaskFolded,NO);
    CheckCollapsed(6);
}
int main(void) { @autoreleasepool {
    [NSApplication.sharedApplication setActivationPolicy:NSApplicationActivationPolicyProhibited];
    NSWindow *window=[[NSWindow alloc] initWithContentRect:NSMakeRect(920,550,312,416)
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
    ClickControl(taskStack.button);
    Require(tasksExpanded && taskStack.hidden && !taskScroll.hidden, @"Click stack to expand");
    CheckPlacement();
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
    ClickControl(overlay.disclosureButton);
    SetTasks(64,@"新的公开进度");
    CheckCollapsed(64);
    Require([taskStack.front.messageLabel.stringValue isEqualToString:@"新的公开进度"], @"Collapsed preview refreshes");
    ClickControl(overlay.disclosureButton);
    Require(tasksExpanded, @"Status chevron expands the task list");
    CheckPlacement();
    CGFloat distance=taskDocument.bounds.size.height-NSMaxY(taskScroll.contentView.bounds);
    Require(fabs(distance-120)<.001, @"Collapsing and live updates preserve scroll position");
    // A real AppKit local event takes the same outside-click monitor path.
    NSEvent *event=[NSEvent mouseEventWithType:NSEventTypeLeftMouseDown
        location:NSMakePoint(156,20) modifierFlags:0 timestamp:0
        windowNumber:window.windowNumber context:nil eventNumber:1 clickCount:1 pressure:1];
    [NSApp sendEvent:event];
    SettleTaskAnimation();
    CheckCollapsed(64);
    CheckStatusDrag(overlay.taskButton);
    ClickControl(overlay.disclosureButton);
    CheckStatusDrag(overlay.disclosureButton);
    ClickControl(overlay.disclosureButton);
    CheckCollapsed(64);
    Require(!taskPanel.canBecomeKeyWindow && !taskPanel.canBecomeMainWindow && NSApp.keyWindow==keyWindow, @"Task UI never steals keyboard focus");
    Require(NSEqualRects(window.frame,parentFrame) && NSEqualRects(statusPanel.frame,capsuleFrame), @"Expanding, collapsing and same-half drags leave the original avatar geometry intact");
    NSRect screen=window.screen.visibleFrame;
    NSPoint low=NSMakePoint(NSMinX(screen)+10,NSMinY(screen)+20);
    [window setFrameOrigin:low];
    CheckCollapsed(64);
    Require(tasksExpandUpward && !statusAtTop && NSEqualPoints(window.frame.origin,low), @"Low pet keeps its position, status below, cards above");
    ClickControl(overlay.disclosureButton);
    CheckPlacement();
    Require(tasksExpanded && NSEqualPoints(window.frame.origin,low), @"Expanding upward never lifts the pet");
    [taskScroll.contentView scrollToPoint:NSMakePoint(0,120)];
    NSPoint high=NSMakePoint(NSMaxX(screen)-window.frame.size.width-10,NSMaxY(screen)-window.frame.size.height-20);
    [window setFrameOrigin:high];
    CheckPlacement();
    Require(statusAtTop && !tasksExpandUpward && tasksExpanded && NSEqualPoints(window.frame.origin,high), @"High pet keeps its position, status above, cards below");
    CGFloat distanceFromTop=taskDocument.bounds.size.height-NSMaxY(taskScroll.contentView.bounds);
    Require(fabs(distanceFromTop-120)<.001, @"Direction flip preserves logical scroll distance from the status anchor");
    [window setFrameOrigin:low];
    CheckPlacement();
    Require(fabs(NSMinY(taskScroll.contentView.bounds)-120)<.001, @"Returning to upward expansion restores the same scroll distance");
    ClickControl(overlay.disclosureButton);
    CheckCollapsed(64);
    Require(NSEqualSizes(window.frame.size,parentFrame.size), @"Adaptive placement never shrinks the pet");
    // While the status is held, it must not jump to the other end of the pet.
    // Release applies the new half-screen direction without changing the drag.
    NSPoint nearMiddle=NSMakePoint(920,NSMidY(screen)-window.frame.size.height/2-50);
    [window setFrameOrigin:nearMiddle];
    FishStatusDragButton *drag=(FishStatusDragButton *)overlay.taskButton;
    NSPoint start=[statusPanel convertPointToScreen:[drag convertPoint:NSMakePoint(NSMidX(drag.bounds),20) toView:nil]];
    [drag mouseDown:StatusMouse(NSEventTypeLeftMouseDown,start)];
    [drag mouseDragged:StatusMouse(NSEventTypeLeftMouseDragged,NSMakePoint(start.x,start.y+100))];
    Require(!statusAtTop && tasksExpandUpward, @"Crossing midpoint while held keeps the grabbed status on its current side");
    [drag mouseUp:StatusMouse(NSEventTypeLeftMouseUp,NSMakePoint(start.x,start.y+100))];
    Require(statusAtTop && !tasksExpandUpward && !tasksExpanded && NSEqualPoints(window.frame.origin,NSMakePoint(nearMiddle.x,nearMiddle.y+100)), @"Release flips sides while preserving the exact drag origin and collapsed state");
    CheckCollapsed(64);
    FishGlassStatusSetVisible(0);
    Require(!statusPanel.visible && !taskPanel.visible, @"Hide also hides the stack");
    FishGlassStatusSetVisible(1);
    CheckStatusGlass();
    CheckCollapsed(64);
    SetTasks(0,@"");
    CheckCollapsed(0);
    FishGlassStatusShowTasks();
    if (!NSWorkspace.sharedWorkspace.accessibilityDisplayShouldReduceMotion) {
        FishTaskAnimationEntry *placeholder=taskAnimationEntries.firstObject;
        Require(taskAnimationActive && placeholder.row.alphaValue==0, @"Opening a previously hidden empty list fades its placeholder in");
    }
    SettleTaskAnimation();
    Require(tasksExpanded && taskPanel.visible && taskRows.count==1, @"Explicit empty-list opening shows a placeholder");
    ToggleTasks();
    if (!NSWorkspace.sharedWorkspace.accessibilityDisplayShouldReduceMotion) Require(((FishTaskAnimationEntry *)taskAnimationEntries.firstObject).toAlpha==0, @"An empty list fades out when folding leaves no visible cards");
    SettleTaskAnimation();
    CheckCollapsed(0);
    CheckTaskMotion(NO);
    CheckTaskMotion(YES);
    CheckThreeStates(NO);
    CheckThreeStates(YES);
    SetTasksExpanded(YES,NO);
    Require(tasksExpanded && !taskAnimationActive && !taskScroll.hidden, @"The nonanimated/reduced-motion path uses final native views directly");
    ClickControl(overlay.disclosureButton);
    ToggleTasks();
    NSRect dockBeforeDestroy=PetDockFrame();
    FishGlassStatusDestroy();
    Require(NSEqualRects(window.frame,dockBeforeDestroy), @"Destroy restores the unreserved avatar frame");
    PumpFor(.35);
    Require(!taskPanel && !taskStack && !taskListSurface && !statusPanel && !outsideClickMonitor && !localClickMonitor && !taskAnimationActive && !taskAnimationTimer && !taskAnimationHost && !taskAnimationEntries, @"Destroy releases views, event monitors and in-flight animation without late callbacks");
    [window orderOut:nil];
    NSLog(@"PASS: three states hidden/folded/expanded, fixed status and shared card edge, animated avatar reservation only for folded row, arrow expanded-folded-hidden cycle, hidden-update preference and reversible reservation; live native unfold/refold in both directions, intermediate frames, continuous reversal, queued snapshots, drag/hide/destroy cancellation and immediate path; lower status/cards up, upper status/cards down, anchor-aware stack/order/scroll, no avatar relocation or resizing, held-side until drag release; caption/chevron drag and click,0/1/2/3/6/64, outside collapse/AX/focus/Regular glass/half-pixel edges/hide/empty/cleanup");
    return 0;
} }
