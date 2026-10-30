#import <AppKit/AppKit.h>
#import <QuartzCore/QuartzCore.h>
#import <stdatomic.h>
#import <objc/message.h>

#define FISH_EXPORT __attribute__((visibility("default")))
static atomic_int ready = 0;
static void ToggleTasks(void);
static void LayoutTasks(void);
static void UpdateTaskAccessibility(void);
static void EnsureTaskPanel(void);

@interface FishTaskButton : NSButton
@end
@implementation FishTaskButton
- (BOOL)acceptsFirstMouse:(NSEvent *)event { return YES; }
@end

@interface FishStatusOverlay : NSView
@property(nonatomic, strong) NSView *pill;
@property(nonatomic, strong) NSVisualEffectView *backdrop;
@property(nonatomic, strong) NSView *glass;
@property(nonatomic, strong) NSView *content;
@property(nonatomic, strong) NSTextField *caption;
@property(nonatomic, strong) NSView *dot;
@property(nonatomic, strong) NSButton *taskButton;
@property(nonatomic, copy) NSString *tone;
@property(nonatomic) BOOL liquidGlass;
- (void)updateCaption:(NSString *)caption tone:(NSString *)tone;
@end

@implementation FishStatusOverlay
- (BOOL)isFlipped { return NO; }
- (NSView *)hitTest:(NSPoint)point {
    return NSPointInRect(point, self.pill.frame) ? self.taskButton : nil;
}
- (void)toggleTasks:(id)sender { ToggleTasks(); }
- (instancetype)initWithFrame:(NSRect)frame {
    if (!(self = [super initWithFrame:frame])) return nil;
    self.autoresizingMask = NSViewWidthSizable | NSViewHeightSizable;
    self.wantsLayer = YES;
    self.layer.backgroundColor = NSColor.clearColor.CGColor;
    self.pill = [[NSView alloc] initWithFrame:NSZeroRect];
    self.pill.wantsLayer = YES;
    self.pill.layer.shadowColor = NSColor.blackColor.CGColor;
    self.pill.layer.shadowOpacity = .12f;
    self.pill.layer.shadowRadius = 4;
    self.pill.layer.shadowOffset = CGSizeMake(0, -1.5);
    [self addSubview:self.pill];

    // Behind-window vibrancy gives the floating pet a real desktop backdrop;
    // Liquid Glass then supplies the system refraction and edge treatment.
    self.backdrop = [[NSVisualEffectView alloc] initWithFrame:NSZeroRect];
    self.backdrop.blendingMode = NSVisualEffectBlendingModeBehindWindow;
    self.backdrop.material = NSVisualEffectMaterialHUDWindow;
    self.backdrop.state = NSVisualEffectStateActive;
    self.backdrop.wantsLayer = YES;
    self.backdrop.layer.cornerRadius = 20;
    self.backdrop.layer.masksToBounds = YES;
    [self.pill addSubview:self.backdrop];
    self.content = [[NSView alloc] initWithFrame:NSZeroRect];
    if (@available(macOS 26.0, *)) {
        NSGlassEffectView *glass = [[NSGlassEffectView alloc] initWithFrame:NSZeroRect];
        glass.style = NSGlassEffectViewStyleClear;
        glass.cornerRadius = 20;
        glass.contentView = self.content;
        self.glass = glass;
        self.liquidGlass = YES;
        self.backdrop.alphaValue = .58;
        [self.pill addSubview:glass];
    } else {
        self.glass = self.backdrop;
        [self.backdrop addSubview:self.content];
    }
    self.caption = [NSTextField labelWithString:@""];
    self.caption.font = [NSFont systemFontOfSize:13 weight:NSFontWeightMedium];
    self.caption.textColor = NSColor.labelColor;
    self.caption.alignment = NSTextAlignmentCenter;
    self.caption.lineBreakMode = NSLineBreakByTruncatingTail;
    self.caption.maximumNumberOfLines = 1;
    self.caption.selectable = NO;
    self.caption.accessibilityLabel = @"桌宠状态";
    [self.content addSubview:self.caption];
    self.dot = [[NSView alloc] initWithFrame:NSZeroRect];
    self.dot.wantsLayer = YES;
    self.dot.layer.cornerRadius = 3;
    [self.content addSubview:self.dot];
    self.taskButton = [[FishTaskButton alloc] initWithFrame:NSZeroRect];
    self.taskButton.bordered = NO;
    self.taskButton.transparent = YES;
    self.taskButton.target = self;
    self.taskButton.action = @selector(toggleTasks:);
    self.taskButton.accessibilityLabel = @"查看任务消息";
    self.taskButton.toolTip = @"查看每个任务的标题和最新消息";
    [self addSubview:self.taskButton];
    return self;
}
- (void)updateCaption:(NSString *)caption tone:(NSString *)tone {
    BOOL changed = ![tone isEqualToString:self.tone];
    self.caption.stringValue = [caption stringByReplacingOccurrencesOfString:@"\n" withString:@" "];
    self.caption.accessibilityValue = caption;
    self.taskButton.accessibilityValue = caption;
    self.tone = tone;
    NSColor *color = [NSColor colorWithSRGBRed:.29 green:.55 blue:.95 alpha:1];
    if ([tone isEqualToString:@"complete"]) color = [NSColor colorWithSRGBRed:.18 green:.66 blue:.50 alpha:1];
    else if ([tone isEqualToString:@"waiting"]) color = [NSColor colorWithSRGBRed:.91 green:.64 blue:.22 alpha:1];
    else if ([tone isEqualToString:@"error"]) color = [NSColor colorWithSRGBRed:.91 green:.33 blue:.39 alpha:1];
    else if ([tone isEqualToString:@"idle"]) color = [NSColor colorWithSRGBRed:.48 green:.56 blue:.67 alpha:1];
    self.dot.layer.backgroundColor = color.CGColor;
    if (changed) {
        [self.dot.layer removeAnimationForKey:@"activity"];
        if ([tone isEqualToString:@"working"] && !NSWorkspace.sharedWorkspace.accessibilityDisplayShouldReduceMotion) {
            CABasicAnimation *pulse = [CABasicAnimation animationWithKeyPath:@"opacity"];
            pulse.fromValue = @.55; pulse.toValue = @1;
            pulse.autoreverses = YES; pulse.duration = 1.0; pulse.repeatCount = HUGE_VALF;
            pulse.timingFunction = [CAMediaTimingFunction functionWithName:kCAMediaTimingFunctionEaseInEaseOut];
            [self.dot.layer addAnimation:pulse forKey:@"activity"];
        }
    }
    self.needsLayout = YES;
    [self layoutSubtreeIfNeeded];
}
- (void)layout {
    [super layout];
    CGFloat text = [self.caption.stringValue sizeWithAttributes:@{NSFontAttributeName:self.caption.font}].width;
    CGFloat width = MIN(MAX(140, ceil(text) + 48), MAX(140, self.bounds.size.width - 24));
    self.pill.frame = NSMakeRect(floor((self.bounds.size.width-width)/2), 5, width, 40);
    self.backdrop.frame = self.pill.bounds;
    // Keep the glass edge half a backing pixel inside the authored capsule.
    // Text and the activity dot retain their original screen coordinates.
    CGFloat edgeInset = self.liquidGlass ? .5 / MAX(1, self.window.backingScaleFactor) : 0;
    if (self.liquidGlass) self.glass.frame = NSInsetRect(self.pill.bounds, edgeInset, 0);
    self.content.frame = NSMakeRect(0, 0, width - 2 * edgeInset, 40);
    self.dot.frame = NSMakeRect(16 - edgeInset, 17, 6, 6);
    self.caption.frame = NSMakeRect(32 - edgeInset, 11, width - 48, 18);
    self.taskButton.frame = self.pill.frame;
    CGPathRef shadow = CGPathCreateWithRoundedRect(self.pill.bounds, 20, 20, NULL);
    self.pill.layer.shadowPath = shadow;
    CGPathRelease(shadow);
}
@end


// This passive accessory never takes real keyboard focus. The two AppKit SPI
// appearance queries mirror the active-material handling found in local Codex
// mini. They affect painting only; isKeyWindow / NSApp.keyWindow stay truthful.
@interface FishPassiveStatusPanel : NSPanel
@end
@implementation FishPassiveStatusPanel
- (BOOL)canBecomeKeyWindow { return NO; }
- (BOOL)canBecomeMainWindow { return NO; }
@end
@interface FishActiveStatusPanel : FishPassiveStatusPanel
@end
@implementation FishActiveStatusPanel
- (BOOL)_hasActiveAppearance { return YES; }
- (BOOL)_hasActiveAppearanceIgnoringKeyFocus { return YES; }
@end

static FishStatusOverlay *overlay;
static NSPanel *statusPanel;
static __weak NSWindow *petWindow;
static NSMutableArray *observers;
static BOOL wantsVisible = YES;
static BOOL layingOut = NO;
static BOOL refreshingAppearance = NO;
static NSPanel *taskPanel;
static NSScrollView *taskScroll;
static NSView *taskDocument;
static NSMutableDictionary *taskRows;
static NSArray<NSDictionary *> *taskData;
static BOOL tasksExpanded = NO;
static id outsideClickMonitor;
static id localClickMonitor;
static NSArray *originalAccessibilityChildren;

@interface FishTaskRow : NSView
@property(nonatomic, strong) NSVisualEffectView *backdrop;
@property(nonatomic, strong) NSView *glass;
@property(nonatomic, strong) NSView *content;
@property(nonatomic) BOOL liquidGlass;
@property(nonatomic, strong) NSTextField *titleLabel;
@property(nonatomic, strong) NSTextField *messageLabel;
@property(nonatomic, strong) NSButton *button;
@property(nonatomic, copy) NSString *sessionID;
- (void)updateTask:(NSDictionary *)task;
@end
@implementation FishTaskRow
- (instancetype)initWithFrame:(NSRect)frame {
    if (!(self = [super initWithFrame:frame])) return nil;
    self.wantsLayer = YES;
    NSVisualEffectView *backdrop = [[NSVisualEffectView alloc] initWithFrame:self.bounds];
    backdrop.autoresizingMask = NSViewWidthSizable | NSViewHeightSizable;
    backdrop.blendingMode = NSVisualEffectBlendingModeBehindWindow;
    backdrop.material = NSVisualEffectMaterialHUDWindow;
    backdrop.state = NSVisualEffectStateActive;
    backdrop.wantsLayer = YES;
    backdrop.layer.cornerRadius = 29;
    backdrop.layer.masksToBounds = YES;
    self.backdrop = backdrop;
    [self addSubview:backdrop];
    NSView *content = [[NSView alloc] initWithFrame:self.bounds];
    self.content = content;
    if (@available(macOS 26.0, *)) {
        NSGlassEffectView *glass = [[NSGlassEffectView alloc] initWithFrame:self.bounds];
        glass.autoresizingMask = NSViewWidthSizable | NSViewHeightSizable;
        glass.style = NSGlassEffectViewStyleClear;
        glass.cornerRadius = 29;
        glass.contentView = content;
        self.glass = glass;
        self.liquidGlass = YES;
        backdrop.alphaValue = .58;
        [self addSubview:glass];
    } else { self.glass = backdrop; [backdrop addSubview:content]; }
    self.titleLabel = [NSTextField labelWithString:@""];
    self.titleLabel.font = [NSFont systemFontOfSize:13 weight:NSFontWeightSemibold];
    self.titleLabel.textColor = NSColor.labelColor;
    self.titleLabel.frame = NSMakeRect(18, 31, frame.size.width-36, 18);
    self.messageLabel = [NSTextField labelWithString:@""];
    self.messageLabel.font = [NSFont systemFontOfSize:12 weight:NSFontWeightRegular];
    self.messageLabel.textColor = NSColor.secondaryLabelColor;
    self.messageLabel.frame = NSMakeRect(18, 12, frame.size.width-36, 17);
    for (NSTextField *label in @[self.titleLabel, self.messageLabel]) {
        label.maximumNumberOfLines = 1;
        label.lineBreakMode = NSLineBreakByTruncatingTail;
        [content addSubview:label];
    }
    self.button = [[FishTaskButton alloc] initWithFrame:self.bounds];
    self.button.bordered = NO;
    self.button.transparent = YES;
    self.button.target = self;
    self.button.action = @selector(openTask:);
    [self addSubview:self.button];
    return self;
}
- (void)viewDidMoveToWindow {
    [super viewDidMoveToWindow];
    self.needsLayout = YES;
}
- (void)viewDidChangeBackingProperties {
    [super viewDidChangeBackingProperties];
    self.needsLayout = YES;
}
- (void)layout {
    [super layout];
    self.backdrop.frame = self.bounds;
    // Match the accepted status capsule's half-backing-pixel alignment. Keep
    // native glass inside the row's edge without moving its text or hit target.
    CGFloat edgeInset = self.liquidGlass ? .5 / MAX(1,self.window.backingScaleFactor) : 0;
    if (self.liquidGlass) self.glass.frame = NSInsetRect(self.bounds,edgeInset,0);
    self.content.frame = NSMakeRect(0,0,self.bounds.size.width-2*edgeInset,self.bounds.size.height);
    NSRect title = self.titleLabel.frame;
    NSRect message = self.messageLabel.frame;
    title.origin.x = message.origin.x = 18-edgeInset;
    self.titleLabel.frame = title;
    self.messageLabel.frame = message;
    self.button.frame = self.bounds;
}
- (void)updateTask:(NSDictionary *)task {
    self.sessionID = task[@"session_id"];
    self.titleLabel.stringValue = task[@"title"] ?: @"未命名任务";
    self.messageLabel.stringValue = task[@"summary"] ?: @"正在思考";
    self.button.accessibilityLabel = self.titleLabel.stringValue;
    self.button.accessibilityValue = self.messageLabel.stringValue;
    NSString *message = [task[@"message"] length] ? task[@"message"] : self.messageLabel.stringValue;
    self.button.toolTip = [NSString stringWithFormat:@"%@\n%@", self.titleLabel.stringValue, message];
    self.button.enabled = [[NSUUID alloc] initWithUUIDString:self.sessionID] != nil;
}
- (void)openTask:(id)sender {
    if (![[NSUUID alloc] initWithUUIDString:self.sessionID]) return;
    tasksExpanded = NO;
    LayoutTasks();
    // Official local-chat deep link; opening never sends a message.
    NSURL *url = [NSURL URLWithString:[@"codex://threads/" stringByAppendingString:self.sessionID]];
    [NSWorkspace.sharedWorkspace openURL:url];
}
@end

@interface FishTaskStack : NSView
@property(nonatomic, strong) FishTaskRow *front;
@property(nonatomic, strong) NSArray<FishTaskRow *> *backs;
@property(nonatomic, strong) NSTextField *countLabel;
@property(nonatomic, strong) NSButton *button;
@property(nonatomic) NSUInteger count;
- (void)updateTasks:(NSArray<NSDictionary *> *)tasks;
@end
@implementation FishTaskStack
- (instancetype)initWithFrame:(NSRect)frame {
    if (!(self = [super initWithFrame:frame])) return nil;
    NSMutableArray *backs = [NSMutableArray new];
    // Farthest card first: the two native-glass edges peek below the front.
    for (NSUInteger i=0; i<2; i++) {
        FishTaskRow *row = [[FishTaskRow alloc] initWithFrame:NSMakeRect(0,0,340,58)];
        row.button.hidden = YES;
        row.accessibilityHidden = YES;
        [self addSubview:row];
        [backs addObject:row];
    }
    self.backs = backs;
    self.front = [[FishTaskRow alloc] initWithFrame:NSMakeRect(10,8,340,58)];
    self.front.button.hidden = YES;
    self.front.accessibilityHidden = YES;
    self.front.titleLabel.frame = NSMakeRect(18,31,260,18);
    [self addSubview:self.front];
    self.countLabel = [NSTextField labelWithString:@""];
    self.countLabel.font = [NSFont systemFontOfSize:11 weight:NSFontWeightMedium];
    self.countLabel.textColor = NSColor.secondaryLabelColor;
    self.countLabel.alignment = NSTextAlignmentRight;
    self.countLabel.accessibilityHidden = YES;
    [self addSubview:self.countLabel];
    self.button = [[FishTaskButton alloc] initWithFrame:self.bounds];
    self.button.bordered = NO;
    self.button.transparent = YES;
    self.button.target = self;
    self.button.action = @selector(expandTasks:);
    [self addSubview:self.button];
    self.accessibilityChildren = @[self.button];
    return self;
}
- (void)expandTasks:(id)sender { if (!tasksExpanded) ToggleTasks(); }
- (void)updateTasks:(NSArray<NSDictionary *> *)tasks {
    self.count = tasks.count;
    [self.front updateTask:tasks.firstObject ?: @{}];
    self.countLabel.stringValue = [NSString stringWithFormat:@"%lu 项",(unsigned long)self.count];
    self.button.accessibilityLabel = [NSString stringWithFormat:@"展开任务列表，共 %lu 项",(unsigned long)self.count];
    self.button.accessibilityValue = [NSString stringWithFormat:@"%@，%@",self.front.titleLabel.stringValue,self.front.messageLabel.stringValue];
    self.button.toolTip = [NSString stringWithFormat:@"点击展开 %lu 项任务\n%@",(unsigned long)self.count,self.front.button.toolTip];
    self.needsLayout = YES;
}
- (void)layout {
    [super layout];
    NSUInteger layers = MIN(MAX(self.count,1),3);
    CGFloat frontY = 8 + (layers-1)*6;
    for (NSUInteger i=0; i<self.backs.count; i++) {
        NSUInteger depth = 2-i;
        FishTaskRow *back = self.backs[i];
        back.hidden = depth >= layers;
        back.frame = NSMakeRect(10+depth*8,frontY-depth*6,340-depth*16,58);
    }
    self.front.frame = NSMakeRect(10,frontY,340,58);
    self.countLabel.frame = NSMakeRect(294,frontY+32,38,16);
    self.button.frame = self.bounds;
}
@end
static FishTaskStack *taskStack;

static NSPanel *NewTaskPanel(void) {
    Class cls = FishPassiveStatusPanel.class;
    if (@available(macOS 26.0, *)) cls = FishActiveStatusPanel.class;
    NSPanel *panel = [[cls alloc] initWithContentRect:NSMakeRect(0,0,360,84)
        styleMask:NSWindowStyleMaskBorderless | NSWindowStyleMaskNonactivatingPanel
        backing:NSBackingStoreBuffered defer:NO];
    panel.title = @"任务消息";
    panel.opaque = NO;
    panel.backgroundColor = NSColor.clearColor;
    panel.hasShadow = NO;
    panel.hidesOnDeactivate = NO;
    panel.floatingPanel = YES;
    panel.releasedWhenClosed = NO;
    panel.excludedFromWindowsMenu = YES;
    return panel;
}
static void UpdateTaskRows(void) {
    if (!taskPanel) return;
    CGFloat distanceFromTop = MAX(0, taskDocument.bounds.size.height-NSMaxY(taskScroll.contentView.bounds));
    NSArray *items = taskData.count ? taskData : @[@{@"session_id":@"", @"title":@"暂无进行中的任务", @"summary":@"新的任务开始后会显示在这里"}];
    CGFloat height = items.count * 66 + 12;
    taskDocument.frame = NSMakeRect(0,0,360,height);
    NSMutableSet *keep = [NSMutableSet new];
    for (NSUInteger i=0; i<items.count; i++) {
        NSDictionary *item = items[i];
        NSString *key = item[@"session_id"] ?: @"";
        [keep addObject:key];
        FishTaskRow *row = taskRows[key];
        if (!row) { row = [[FishTaskRow alloc] initWithFrame:NSMakeRect(10,0,340,58)]; taskRows[key]=row; [taskDocument addSubview:row]; }
        row.frame = NSMakeRect(10,height-8-(i+1)*66,340,58);
        [row updateTask:item];
    }
    for (NSString *key in [taskRows.allKeys copy]) if (![keep containsObject:key]) {
        [taskRows[key] removeFromSuperview]; [taskRows removeObjectForKey:key];
    }
    [taskStack updateTasks:taskData];
    LayoutTasks();
    [taskScroll.contentView scrollToPoint:NSMakePoint(0, MAX(0, height-taskScroll.contentView.bounds.size.height-distanceFromTop))];
    [taskScroll reflectScrolledClipView:taskScroll.contentView];
    LayoutTasks();
}
static void LayoutTasks(void) {
    if (!taskPanel || !petWindow) return;
    if ((!taskData.count && !tasksExpanded) || !wantsVisible || !petWindow.visible || petWindow.miniaturized) { [taskPanel orderOut:nil]; UpdateTaskAccessibility(); return; }
    NSRect screen = (petWindow.screen ?: NSScreen.mainScreen).visibleFrame;
    CGFloat height = tasksExpanded ? MIN(342, MAX(78, (taskData.count ?: 1)*66+12)) : 74+(MIN(taskData.count,3)-1)*6;
    CGFloat x = NSMinX(petWindow.frame)-366;
    if (x < NSMinX(screen)+8) x=MIN(NSMaxX(petWindow.frame)+6,NSMaxX(screen)-368);
    CGFloat y = MIN(MAX(NSMinY(statusPanel.frame)+5,NSMinY(screen)+8),NSMaxY(screen)-height-8);
    [taskPanel setFrame:NSMakeRect(MAX(NSMinX(screen)+8,x),y,360,height) display:YES];
    taskScroll.hidden = !tasksExpanded;
    taskStack.hidden = tasksExpanded;
    // Keep the hidden scroll viewport intact so collapsing/live updates do not
    // discard the user's distance from the top of the expanded list.
    if (tasksExpanded) taskScroll.frame = taskPanel.contentView.bounds;
    else { taskStack.frame = taskPanel.contentView.bounds; [taskStack layoutSubtreeIfNeeded]; }
    taskPanel.level = petWindow.level;
    taskPanel.collectionBehavior = petWindow.collectionBehavior | NSWindowCollectionBehaviorFullScreenAuxiliary | NSWindowCollectionBehaviorIgnoresCycle;
    if (taskPanel.parentWindow != petWindow) [petWindow addChildWindow:taskPanel ordered:NSWindowAbove];
    [taskPanel orderFront:nil];
    UpdateTaskAccessibility();
}
static void UpdateTaskAccessibility(void) {
    if (!petWindow) return;
    NSMutableArray *children = [NSMutableArray arrayWithArray:originalAccessibilityChildren ?: @[]];
    // Keep passive child-window controls reachable from the pet's main window
    // for VoiceOver and accessibility clients without taking keyboard focus.
    if (statusPanel.visible) [children addObject:overlay.taskButton];
    if (taskPanel.visible) [children addObject:tasksExpanded ? (id)taskScroll : taskStack.button];
    petWindow.accessibilityChildren = children;
    overlay.taskButton.accessibilityLabel = tasksExpanded ? @"收起任务列表" : @"查看任务消息";
    overlay.taskButton.toolTip = tasksExpanded ? @"收起为堆叠卡片" : @"展开每个任务的标题和最新消息";
}
static void EnsureTaskPanel(void) {
    if (!taskPanel) {
        taskPanel = NewTaskPanel();
        taskScroll = [[NSScrollView alloc] initWithFrame:NSMakeRect(0,0,360,342)];
        taskScroll.drawsBackground = NO;
        taskScroll.hasVerticalScroller = YES;
        taskScroll.autohidesScrollers = YES;
        taskScroll.scrollerStyle = NSScrollerStyleOverlay;
        taskScroll.contentView.drawsBackground = NO;
        taskDocument = [[NSView alloc] initWithFrame:NSMakeRect(0,0,360,78)];
        taskScroll.documentView = taskDocument;
        [taskPanel.contentView addSubview:taskScroll];
        taskRows = [NSMutableDictionary new];
        taskStack = [[FishTaskStack alloc] initWithFrame:taskPanel.contentView.bounds];
        [taskPanel.contentView addSubview:taskStack];
    }
}
static void ToggleTasks(void) {
    EnsureTaskPanel();
    tasksExpanded = !tasksExpanded;
    UpdateTaskRows();
}
static BOOL IsInsideStatus(NSPoint point) {
    if (!statusPanel.visible) return NO;
    NSRect pill = [statusPanel convertRectToScreen:[overlay convertRect:overlay.pill.frame toView:nil]];
    return NSPointInRect(point,pill);
}

static NSWindow *FindPetWindow(void) {
    for (NSWindow *window in NSApp.windows) {
        if ([window.title isEqualToString:@"BigFatBlueFish"] && window.contentView && window.visible && window != statusPanel) return window;
    }
    return nil;
}
static void OnMain(dispatch_block_t block) {
    if (NSThread.isMainThread) block();
    else dispatch_async(dispatch_get_main_queue(), block);
}
static void RefreshActiveAppearance(void) {
    if (!statusPanel || refreshingAppearance) return;
    refreshingAppearance = YES;
    @try {
        // Same guarded native refresh used by Codex mini. Its additional glass
        // selector is optional because it belongs to that app's window subclass.
        SEL force = NSSelectorFromString(@"_setForceActiveControls:");
        SEL glass = NSSelectorFromString(@"setNativeGlassForcesActiveAppearance:");
        SEL refresh = NSSelectorFromString(@"_sendForcedWindowChangedKeyState");
        for (NSPanel *panel in taskPanel ? @[statusPanel,taskPanel] : @[statusPanel]) {
            if ([panel respondsToSelector:force]) ((void(*)(id,SEL,BOOL))objc_msgSend)(panel, force, YES);
            if ([panel respondsToSelector:glass]) ((void(*)(id,SEL,BOOL))objc_msgSend)(panel, glass, YES);
            if ([panel respondsToSelector:refresh]) ((void(*)(id,SEL))objc_msgSend)(panel, refresh);
        }
    } @catch (NSException *exception) {
        NSLog(@"Desktop Fish active appearance refresh failed: %@", exception.name);
    } @finally {
        refreshingAppearance = NO;
    }
}
static void LayoutStatus(void) {
    if (!statusPanel || !petWindow || layingOut) return;
    layingOut = YES;
    NSView *content = petWindow.contentView;
    // Preserve the taller capsule's top edge below the feet. Its own transparent
    // accessory extends downwards; the model window and camera remain untouched.
    CGFloat textWidth = [overlay.caption.stringValue sizeWithAttributes:@{NSFontAttributeName:overlay.caption.font}].width;
    CGFloat capsuleWidth = MIN(MAX(140,ceil(textWidth)+48),MAX(140,content.bounds.size.width-24));
    CGFloat panelWidth = capsuleWidth+24;
    // Narrow only the transparent accessory bounds. The painted capsule keeps
    // exactly the same size and desktop coordinates as the accepted version.
    NSRect rect = [content convertRect:NSMakeRect(floor((content.bounds.size.width-capsuleWidth)/2)-12, -16, panelWidth, 60) toView:nil];
    rect = [petWindow convertRectToScreen:rect];
    if (!NSEqualRects(statusPanel.frame, rect)) [statusPanel setFrame:rect display:YES];
    statusPanel.level = petWindow.level;
    overlay.frame = statusPanel.contentView.bounds;
    overlay.needsLayout = YES;
    [overlay layoutSubtreeIfNeeded];
    if (wantsVisible && petWindow.visible && !petWindow.miniaturized) {
        if (statusPanel.parentWindow != petWindow) [petWindow addChildWindow:statusPanel ordered:NSWindowAbove];
        if (!statusPanel.visible) [statusPanel orderFront:nil];
    } else {
        [statusPanel orderOut:nil];
    }
    RefreshActiveAppearance();
    statusPanel.ignoresMouseEvents = NO;
    LayoutTasks();
    layingOut = NO;
}
static void InstallObservers(void) {
    observers = [NSMutableArray new];
    NSNotificationCenter *center = NSNotificationCenter.defaultCenter;
    void (^outsideClick)(NSEvent *) = ^(NSEvent *event) {
        NSPoint point = event.window ? [event.window convertPointToScreen:event.locationInWindow] : NSEvent.mouseLocation;
        if (tasksExpanded && !IsInsideStatus(point) && !NSPointInRect(point,taskPanel.frame)) {
            tasksExpanded=NO;
            LayoutTasks();
        }
    };
    outsideClickMonitor = [NSEvent addGlobalMonitorForEventsMatchingMask:NSEventMaskLeftMouseDown handler:outsideClick];
    localClickMonitor = [NSEvent addLocalMonitorForEventsMatchingMask:NSEventMaskLeftMouseDown handler:^NSEvent *(NSEvent *event) { outsideClick(event); return event; }];
    for (NSNotificationName name in @[NSWindowDidMoveNotification, NSWindowDidResizeNotification,
        NSWindowDidChangeScreenNotification, NSWindowDidMiniaturizeNotification, NSWindowDidDeminiaturizeNotification]) {
        [observers addObject:[center addObserverForName:name object:nil queue:NSOperationQueue.mainQueue usingBlock:^(NSNotification *note) {
            if (note.object == petWindow) LayoutStatus();
        }]];
    }
    for (NSNotificationName name in @[NSApplicationDidBecomeActiveNotification, NSApplicationDidResignActiveNotification,
        NSWindowDidBecomeKeyNotification, NSWindowDidResignKeyNotification]) {
        [observers addObject:[center addObserverForName:name object:nil queue:NSOperationQueue.mainQueue usingBlock:^(NSNotification *note) {
            if (note.object != NSApp && note.object != petWindow && note.object != statusPanel) return;
            if (refreshingAppearance || layingOut) return;
            LayoutStatus();
            NSLog(@"Desktop Fish original glass focus: appActive=%d petKey=%d panelKey=%d actualPanelKey=%d activePainting=1",
                NSApp.active, petWindow.keyWindow, statusPanel.keyWindow, NSApp.keyWindow == statusPanel);
        }]];
    }
}
FISH_EXPORT int FishGlassStatusIsReady(void) { return atomic_load(&ready); }
FISH_EXPORT int FishGlassStatusUpdate(const char *captionUTF8, const char *toneUTF8) {
    NSString *caption = captionUTF8 ? [NSString stringWithUTF8String:captionUTF8] : @"";
    NSString *tone = toneUTF8 ? [NSString stringWithUTF8String:toneUTF8] : @"idle";
    OnMain(^{
        NSWindow *window = FindPetWindow();
        if (!window) { atomic_store(&ready, 0); return; }
        BOOL created = NO;
        if (!statusPanel) {
            Class panelClass = FishPassiveStatusPanel.class;
            if (@available(macOS 26.0, *)) panelClass = FishActiveStatusPanel.class;
            statusPanel = [[panelClass alloc] initWithContentRect:NSMakeRect(0, 0, window.contentView.bounds.size.width, 60)
                styleMask:NSWindowStyleMaskBorderless | NSWindowStyleMaskNonactivatingPanel backing:NSBackingStoreBuffered defer:NO];
            statusPanel.title = @"桌宠状态";
            statusPanel.opaque = NO;
            statusPanel.backgroundColor = NSColor.clearColor;
            statusPanel.hasShadow = NO; // Keep the original152 layer-shadow profile.
            statusPanel.hidesOnDeactivate = NO;
            statusPanel.floatingPanel = YES;
            statusPanel.becomesKeyOnlyIfNeeded = YES;
            statusPanel.ignoresMouseEvents = NO;
            statusPanel.releasedWhenClosed = NO;
            statusPanel.excludedFromWindowsMenu = YES;
            overlay = [[FishStatusOverlay alloc] initWithFrame:statusPanel.contentView.bounds];
            statusPanel.contentView = overlay;
            InstallObservers();
            created = YES;
        }
        if (petWindow != window) {
            petWindow.accessibilityChildren = originalAccessibilityChildren;
            [petWindow removeChildWindow:statusPanel];
            petWindow = window;
            originalAccessibilityChildren = [window.accessibilityChildren copy];
            statusPanel.collectionBehavior = window.collectionBehavior | NSWindowCollectionBehaviorFullScreenAuxiliary | NSWindowCollectionBehaviorIgnoresCycle;
        }
        [overlay updateCaption:caption tone:tone];
        wantsVisible = YES;
        LayoutStatus();
        UpdateTaskAccessibility();
        atomic_store(&ready, overlay.liquidGlass ? 2 : 1);
        if (created) {
            NSRect capsule = [statusPanel convertRectToScreen:[overlay convertRect:overlay.pill.frame toView:nil]];
            NSLog(@"Desktop Fish glass geometry: parent=%@ panel=%@ capsule=%@ scale=%g", NSStringFromRect(petWindow.frame), NSStringFromRect(statusPanel.frame), NSStringFromRect(capsule), statusPanel.backingScaleFactor);
            NSLog(@"Desktop Fish original material restored: NSGlassEffectView.clear tint=nil; HUD alpha=.58; shadow=.12/4/-1.5; height=40 radius=20; activePainting=1");
            NSLog(@"Desktop Fish original glass initial focus: appActive=%d petKey=%d panelKey=%d actualPanelKey=%d; forceSelector=%d refreshSelector=%d",
                NSApp.active, petWindow.keyWindow, statusPanel.keyWindow, NSApp.keyWindow == statusPanel,
                [statusPanel respondsToSelector:NSSelectorFromString(@"_setForceActiveControls:")],
                [statusPanel respondsToSelector:NSSelectorFromString(@"_sendForcedWindowChangedKeyState")]);
        }
    });
    return atomic_load(&ready);
}
FISH_EXPORT void FishGlassStatusSetVisible(int visible) {
    OnMain(^{ wantsVisible = visible != 0; if (!wantsVisible) tasksExpanded=NO; LayoutStatus(); });
}
FISH_EXPORT void FishGlassStatusUpdateTasks(const char *jsonUTF8) {
    if (!jsonUTF8 || strlen(jsonUTF8)>256*1024) return;
    NSData *bytes = [[NSString stringWithUTF8String:jsonUTF8] dataUsingEncoding:NSUTF8StringEncoding];
    if (!bytes) return;
    id payload = [NSJSONSerialization JSONObjectWithData:bytes options:0 error:nil];
    if (![payload isKindOfClass:NSDictionary.class] || ![payload[@"tasks"] isKindOfClass:NSArray.class]) return;
    NSMutableArray *tasks = [NSMutableArray new];
    NSMutableSet *seen = [NSMutableSet new];
    for (id item in payload[@"tasks"]) {
        if (![item isKindOfClass:NSDictionary.class]) continue;
        NSString *sid=item[@"session_id"];
        if (![sid isKindOfClass:NSString.class] || ![[NSUUID alloc] initWithUUIDString:sid] || [seen containsObject:sid]) continue;
        if (![item[@"expires_at"] isKindOfClass:NSNumber.class] || [item[@"expires_at"] doubleValue]<=NSDate.date.timeIntervalSince1970) continue;
        NSMutableDictionary *safe = [NSMutableDictionary new];
        safe[@"session_id"]=sid;
        for (NSString *field in @[@"title",@"summary",@"message"]) {
            NSString *value=[item[field] isKindOfClass:NSString.class] ? item[field] : @"";
            NSUInteger limit=[field isEqualToString:@"title"] ? 160 : 600;
            if (value.length>limit) value=[value substringWithRange:[value rangeOfComposedCharacterSequencesForRange:NSMakeRange(0,limit)]];
            safe[field]=value;
        }
        [tasks addObject:safe]; [seen addObject:sid];
        if (tasks.count>=64) break;
    }
    OnMain(^{ if (![taskData isEqualToArray:tasks]) { taskData=[tasks copy]; if (taskData.count) EnsureTaskPanel(); UpdateTaskRows(); } });
}
FISH_EXPORT void FishGlassStatusShowTasks(void) {
    OnMain(^{ tasksExpanded=NO; ToggleTasks(); RefreshActiveAppearance(); });
}
FISH_EXPORT void FishGlassStatusDestroy(void) {
    OnMain(^{
        if (outsideClickMonitor) [NSEvent removeMonitor:outsideClickMonitor];
        if (localClickMonitor) [NSEvent removeMonitor:localClickMonitor];
        outsideClickMonitor=nil; localClickMonitor=nil;
        [petWindow removeChildWindow:taskPanel]; [taskPanel orderOut:nil]; [taskPanel close];
        taskPanel=nil; taskScroll=nil; taskDocument=nil; taskRows=nil; taskStack=nil; taskData=nil; tasksExpanded=NO;
        for (id token in observers) [NSNotificationCenter.defaultCenter removeObserver:token];
        observers = nil;
        [petWindow removeChildWindow:statusPanel];
        [statusPanel orderOut:nil];
        [statusPanel close];
        petWindow.accessibilityChildren=originalAccessibilityChildren;
        originalAccessibilityChildren=nil;
        overlay = nil; statusPanel = nil; petWindow = nil;
        wantsVisible = YES;
        atomic_store(&ready, 0);
    });
}
