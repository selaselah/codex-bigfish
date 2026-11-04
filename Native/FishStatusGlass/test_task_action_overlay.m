// Covers intermediate ancestor clipping, not just containment in the window.
// Example tasks remain in this process and no real preferences are written.
#define main OriginalTaskStackMain
#import "test_task_stack.m"
#undef main

static void Capture(NSString *name) {
    const char *dir=getenv("FISH_ACTION_CAPTURE_DIR");if(!dir)return;
    NSView *view=taskPanel.contentView;NSBitmapImageRep *bitmap=[view bitmapImageRepForCachingDisplayInRect:view.bounds];[view cacheDisplayInRect:view.bounds toBitmapImageRep:bitmap];
    NSString *path=[[NSString stringWithUTF8String:dir] stringByAppendingPathComponent:[name stringByAppendingString:@".png"]];
    Require([[bitmap representationUsingType:NSBitmapImageFileTypePNG properties:@{}] writeToFile:path atomically:YES],@"Save actual ancestor-clipping review frame");
}
static void CheckControls(FishTaskRow *row) {
    for(NSButton *button in @[row.dismissButton,row.hideSessionButton]) {
        Require(button.superview && !button.hidden && button.enabled,@"Visible row actions remain attached and actionable");
        NSRect frame=[taskPanel.contentView convertRect:button.bounds fromView:button];
        Require(NSContainsRect(taskPanel.contentView.bounds,frame),@"Complete action remains in the existing window");
        Require(NSContainsRect(button.visibleRect,button.bounds),@"The complete circle survives card-ancestor clipping");
        Require([taskPanel.contentView hitTest:NSMakePoint(NSMidX(frame),NSMidY(frame))]==button,@"Overlay action retains hit priority");
        if (@available(macOS 26.0, *)) {
            FishTaskActionButton *action=(FishTaskActionButton *)button;
            NSGlassEffectView *glass=(NSGlassEffectView *)action.glass;
            Require([glass isKindOfClass:NSGlassEffectView.class] && glass.superview==button,@"Transparent task panel retains a standalone native lens for each action");
            Require(glass.style==NSGlassEffectViewStyleRegular && !glass.tintColor && glass.alphaValue==1,@"Action lens uses Regular with no painted fill, tint or reduced opacity");
            Require(!button.bordered && button.transparent && button.imagePosition==NSNoImage,@"The native button cell does not replace the lens or draw a duplicate glyph");
            Require(glass.contentView==action.symbolView && action.symbolView.image==button.image,@"The actual glyph is inside the native glass contentView");
            Require(NSWidth(glass.frame)==NSHeight(glass.frame) && glass.cornerRadius==NSWidth(glass.bounds)/2,@"The glass lens itself is circular");
            CGFloat scale=MAX(1,button.window.backingScaleFactor),edge=1/scale;
            Require(NSEqualRects(glass.frame,NSInsetRect(button.bounds,edge,edge)),@"Tiny circular lens keeps a complete backing-pixel raster gutter on all four edges");
            Require(NSMinX(glass.frame)*scale==1 && NSMinY(glass.frame)*scale==1 && (NSMaxX(button.bounds)-NSMaxX(glass.frame))*scale==1 && (NSMaxY(button.bounds)-NSMaxY(glass.frame))*scale==1,@"All four small-circle edges have exactly one physical pixel of room");
            Require(NSEqualSizes(button.bounds.size,NSMakeSize(20,20)),@"Pixel gutter does not shrink the20pt click target");
            Require(NSContainsRect(glass.visibleRect,glass.bounds),@"Complete native glass surface survives ancestor clipping");
        } else {
            Require(button.bordered && button.bezelStyle==NSBezelStyleCircular,@"Older systems use the native circular button fallback");
        }
    }
    Require(NSMinX(row.hideSessionButton.frame)-NSMaxX(row.dismissButton.frame)==8,@"Separate hide action keeps8pt spacing");
}
int main(void) { @autoreleasepool {
    [NSApplication.sharedApplication setActivationPolicy:NSApplicationActivationPolicyProhibited];
    NSWindow *window=[[NSWindow alloc] initWithContentRect:NSMakeRect(600,400,312,416) styleMask:NSWindowStyleMaskBorderless backing:NSBackingStoreBuffered defer:NO];window.title=@"BigFatBlueFish";[window orderFront:nil];
    FishGlassStatusUpdate("工作中 · 3 项","working");SetTasks(3,@"圆形按钮保持完整");
    Require(!taskPanel.opaque && taskPanel.backgroundColor.alphaComponent==0,@"Exercise the actual transparent task-panel configuration");
    taskStack.front.clipsToBounds=YES;taskStack.front.layer.masksToBounds=YES;
    Capture(@"folded-card-clips");CheckControls(taskStack.front);
    taskPanel.appearance=[NSAppearance appearanceNamed:NSAppearanceNameDarkAqua];
    [taskPanel.contentView displayIfNeeded];CheckControls(taskStack.front);Capture(@"folded-dark");
    taskPanel.appearance=[NSAppearance appearanceNamed:NSAppearanceNameAqua];
    [taskPanel.contentView displayIfNeeded];CheckControls(taskStack.front);Capture(@"folded-light");
    for(NSButton *button in @[taskStack.front.dismissButton,taskStack.front.hideSessionButton]) [button highlight:YES];
    Capture(@"folded-pressed");
    for(NSButton *button in @[taskStack.front.dismissButton,taskStack.front.hideSessionButton]) [button highlight:NO];
    taskPanel.appearance=nil;
    FishGlassStatusShowTasks();SettleTaskAnimation();
    for(FishTaskRow *row in taskRows.allValues) { row.clipsToBounds=YES;row.layer.masksToBounds=YES;CheckControls(row); }
    Capture(@"expanded-card-clips");
    SetTasksExpanded(NO,NO);SetTasks(1,@"单卡的完整圆形按钮");SettleTaskAnimation();
    taskStack.front.clipsToBounds=YES;taskStack.front.layer.masksToBounds=YES;CheckControls(taskStack.front);Capture(@"single-card-clips");
    SetTasks(12,@"滚动时不留半截按钮");SettleTaskAnimation();FishGlassStatusShowTasks();SettleTaskAnimation();
    for(NSNumber *offset in @[@0,@19,@80,@123]) {
        [taskScroll.contentView scrollToPoint:NSMakePoint(0,offset.doubleValue)];[taskScroll reflectScrolledClipView:taskScroll.contentView];
        for(NSButton *button in taskActionOverlay.subviews) {
            Require(NSContainsRect(taskPanel.contentView.bounds,button.frame) && NSContainsRect(button.visibleRect,button.bounds),@"Scroll overlay never displays a partly clipped circle");
        }
        NSPoint gap=NSMakePoint(200,20);
        Require([taskActionOverlay hitTest:gap]==nil,@"Empty overlay regions pass mouse events through to the card/list");
    }
    Capture(@"scrolled-actions");SetTasksExpanded(NO,NO);SettleTaskAnimation();
    FishGlassStatusShowTask([stackOrder.firstObject UTF8String]);
    if(taskStack.shakeActive) {
        [taskStack advanceShake:.025];
        Require(NSMinX(taskStack.front.dismissButton.frame)==NSMinX(taskStack.front.frame),@"The action group follows front-card shake instead of staying behind");
    }
    SettleTaskAnimation();SetTasks(2,@"移除旧行时清理浮层");SettleTaskAnimation();
    Require(taskActionOverlay.subviews.count==2,@"Retired rows do not leave stale action controls in the overlay");
    FishGlassStatusDestroy();Require(!taskActionOverlay && !taskActionScrollObserver,@"Destroy releases action overlay and scroll observation");[window orderOut:nil];
    NSLog(@"PASS: standalone Regular glass lenses in the real transparent task panel/light-dark appearance; contentView glyphs/circular one-backing-pixel gutters/unchanged20pt hit target; folded/expanded/single actions survive card view/layer clipping; complete circles/8pt spacing/hit priority; scrolling clips no partial controls; empty pass-through; shake tracking/retired-row/observer cleanup; no window enlargement");return 0;
} }
