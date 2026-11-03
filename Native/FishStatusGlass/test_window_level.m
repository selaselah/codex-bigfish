// Isolated AppKit production-view fixture. Other windows below are test-owned.
#define main OriginalTaskStackMain
#import "test_task_stack.m"
#undef main

static void CheckLevels(NSWindow *window) {
    Require(window.level==NSFloatingWindowLevel,@"Avatar floats above ordinary apps without entering system/menu levels");
    Require(statusPanel.level==NSFloatingWindowLevel && taskPanel.level==NSFloatingWindowLevel,@"Status and task cards share the avatar floating level");
    Require(NSFloatingWindowLevel>NSNormalWindowLevel && NSFloatingWindowLevel<NSModalPanelWindowLevel && NSFloatingWindowLevel<NSStatusWindowLevel && NSFloatingWindowLevel<NSPopUpMenuWindowLevel,@"Floating policy preserves ordinary-window ordering and yields to modal/status/menu UI");
    Require(statusPanel.parentWindow==window && taskPanel.parentWindow==window,@"Accessories retain their child-window attachment");
}
int main(void) { @autoreleasepool {
    [NSApplication.sharedApplication setActivationPolicy:NSApplicationActivationPolicyProhibited];
    NSWindow *window=[[NSWindow alloc] initWithContentRect:NSMakeRect(700,400,312,416) styleMask:NSWindowStyleMaskBorderless backing:NSBackingStoreBuffered defer:NO];
    Class owner=object_getClass(window);IMP original=method_getImplementation(class_getInstanceMethod(owner,@selector(setLevel:)));
    window.title=@"BigFatBlueFish";window.level=NSPopUpMenuWindowLevel;[window orderFront:nil];
    FishGlassStatusUpdate("工作中 · 3 项","working");SetTasks(3,@"窗口层级验证");CheckLevels(window);
    Require(object_getClass(window)==owner,@"Window level handling never replaces the Unity/AppKit class identity");
    NSRect avatar=window.frame,dock=statusPanel.frame;NSWindow *key=NSApp.keyWindow;
    NSWindow *ordinary=[[NSWindow alloc] initWithContentRect:NSMakeRect(700,400,312,416) styleMask:NSWindowStyleMaskBorderless backing:NSBackingStoreBuffered defer:NO];ordinary.title=@"Ordinary fixture";
    ordinary.level=NSNormalWindowLevel;[ordinary orderFront:nil];
    NSPanel *systemPanel=[[NSPanel alloc] initWithContentRect:NSMakeRect(700,400,312,416) styleMask:NSWindowStyleMaskBorderless|NSWindowStyleMaskNonactivatingPanel backing:NSBackingStoreBuffered defer:NO];
    systemPanel.title=@"Modal-level fixture (not Control Center)";systemPanel.level=NSModalPanelWindowLevel;[systemPanel orderFront:nil];
    for(NSNumber *request in @[@(NSNormalWindowLevel),@(NSPopUpMenuWindowLevel),@(NSScreenSaverWindowLevel),@(NSFloatingWindowLevel)]) {
        window.level=request.integerValue;CheckLevels(window);
        LayoutStatus();[taskPanel orderFront:nil];CheckLevels(window);
        Require(ordinary.level==NSNormalWindowLevel && systemPanel.level==NSModalPanelWindowLevel,@"Same-class unrelated windows keep their own requested levels");
        // Let AppKit create/flush the test process's windows before asking the
        // separate WindowServer for their order; setters above are synchronous.
        PumpFor(.05);
        NSArray *windows=(__bridge_transfer NSArray *)CGWindowListCopyWindowInfo(kCGWindowListOptionOnScreenOnly,kCGNullWindowID);
        NSUInteger host=NSNotFound,modal=NSNotFound,normal=NSNotFound;
        for(NSUInteger i=0;i<windows.count;i++) {
            NSInteger number=[windows[i][(id)kCGWindowNumber] integerValue];
            if(number==window.windowNumber)host=i;
            if(number==systemPanel.windowNumber)modal=i;
            if(number==ordinary.windowNumber)normal=i;
        }
        Require(modal<host && host<normal,@"WindowServer keeps system-level stand-in above the pet and normal stand-in below it, even after orderFront");
    }
    ordinary.level=NSPopUpMenuWindowLevel;Require(ordinary.level==NSPopUpMenuWindowLevel,@"Level guard affects only the owned avatar instance");ordinary.level=NSNormalWindowLevel;
    FishGlassStatusShowTasks();SettleTaskAnimation();CheckLevels(window);
    SetTasksExpanded(NO,YES);SettleTaskAnimation();CheckLevels(window);
    FishGlassStatusShowTask("00000000-0000-4000-8000-000000000001");window.level=NSPopUpMenuWindowLevel;SettleTaskAnimation();CheckLevels(window);
    FishGlassStatusUpdate("工作中 · 3 项","working");RefreshActiveAppearance();CheckLevels(window);
    FishGlassStatusSetVisible(0);Require(window.level==NSFloatingWindowLevel,@"Hiding accessories does not raise the pet into system UI");FishGlassStatusSetVisible(1);SettleTaskAnimation();CheckLevels(window);
    Require(NSEqualRects(avatar,window.frame)&&NSEqualRects(dock,statusPanel.frame)&&NSApp.keyWindow==key,@"Level policy never changes geometry, dock or keyboard focus");
    FishGlassStatusDestroy();
    Require(object_getClass(window)==owner && method_getImplementation(class_getInstanceMethod(owner,@selector(setLevel:)))==original,@"Destroy restores the original level setter with class identity intact");
    Require(window.level==NSPopUpMenuWindowLevel,@"Teardown restores the avatar's original pre-plugin level");
    window.level=NSNormalWindowLevel;Require(window.level==NSNormalWindowLevel,@"No level override survives teardown");
    [systemPanel orderOut:nil];[ordinary orderOut:nil];[window orderOut:nil];
    NSLog(@"PASS: floating avatar/status/tasks, four vendor level requests, same-class isolation, real WindowServer ordering vs ordinary/modal stand-ins, list/placing/heartbeat/focus/hide and restored teardown; system stand-in is not live Control Center");return 0;
} }
