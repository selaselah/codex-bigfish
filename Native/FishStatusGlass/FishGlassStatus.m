#import <AppKit/AppKit.h>
#import <QuartzCore/QuartzCore.h>
#import <stdatomic.h>
#import <objc/message.h>

#define FISH_EXPORT __attribute__((visibility("default")))
typedef NS_ENUM(NSInteger,FishTaskMode) { FishTaskHidden, FishTaskFolded, FishTaskExpanded };
static atomic_int ready = 0;
static void ToggleTasks(void);
static void LayoutTasks(void);
static void UpdateTaskAccessibility(void);
static void EnsureTaskPanel(void);
static void LayoutStatus(void);
static void SetTasksExpanded(BOOL expanded, BOOL animated);
static void CancelTaskAnimation(void);
static void CycleTaskCards(void);
static void PreparePetDrag(void);
static void SetTaskMode(FishTaskMode mode, BOOL animated);

@interface FishTaskButton : NSButton
@end
@implementation FishTaskButton
- (BOOL)acceptsFirstMouse:(NSEvent *)event { return YES; }
@end

// A status click still opens the task list. Crossing the same 4pt threshold as
// the pet's own drag gesture instead moves the parent, never just its accessory.
@interface FishStatusDragButton : FishTaskButton
@property(nonatomic, weak) NSWindow *dragWindow;
@property(nonatomic) NSPoint downScreen;
@property(nonatomic) NSPoint downOrigin;
@property(nonatomic) BOOL pressed;
@property(nonatomic) BOOL dragged;
@end
@implementation FishStatusDragButton
- (BOOL)mouseDownCanMoveWindow { return NO; }
- (void)mouseDown:(NSEvent *)event {
    self.dragWindow = self.window.parentWindow;
    self.downScreen = [event.window convertPointToScreen:event.locationInWindow];
    self.downOrigin = self.dragWindow.frame.origin;
    self.pressed = YES;
    self.dragged = NO;
    [self highlight:YES];
}
- (void)mouseDragged:(NSEvent *)event {
    if (!self.pressed) return;
    NSPoint screen = [event.window convertPointToScreen:event.locationInWindow];
    NSPoint delta = NSMakePoint(screen.x-self.downScreen.x,screen.y-self.downScreen.y);
    if (!self.dragged && hypot(delta.x,delta.y) <= 4) return;
    if (!self.dragged) { PreparePetDrag(); self.downOrigin=self.dragWindow.frame.origin; }
    self.dragged = YES;
    [self highlight:NO];
    // Absolute desktop coordinates avoid feedback as this button's own window
    // moves. Existing move observers keep the status and all task cards attached.
    [self.dragWindow setFrameOrigin:NSMakePoint(self.downOrigin.x+delta.x,self.downOrigin.y+delta.y)];
}
- (void)mouseUp:(NSEvent *)event {
    if (!self.pressed) return;
    NSPoint screen = [event.window convertPointToScreen:event.locationInWindow];
    NSPoint local = [self convertPoint:[self.window convertPointFromScreen:screen] fromView:nil];
    BOOL click = !self.dragged && NSPointInRect(local,self.bounds);
    BOOL moved = self.dragged;
    self.pressed = NO;
    self.dragWindow = nil;
    [self highlight:NO];
    if (click) [self sendAction:self.action to:self.target];
    // Keep the grabbed bar on its current side until release; only then may it
    // switch ends of the character when a drag crosses the desktop midpoint.
    if (moved) LayoutStatus();
}
- (void)viewWillMoveToWindow:(NSWindow *)window {
    if (!window) { self.pressed=NO; self.dragWindow=nil; [self highlight:NO]; }
    [super viewWillMoveToWindow:window];
}
@end

@interface FishStatusOverlay : NSView
@property(nonatomic, strong) NSView *pill;
@property(nonatomic, strong) NSVisualEffectView *backdrop;
@property(nonatomic, strong) NSView *glass;
@property(nonatomic, strong) NSView *content;
@property(nonatomic, strong) NSTextField *caption;
@property(nonatomic, strong) NSButton *taskButton;
@property(nonatomic, strong) NSButton *disclosureButton;
@property(nonatomic) BOOL liquidGlass;
- (void)updateCaption:(NSString *)caption tone:(NSString *)tone;
- (void)updateDisclosure:(BOOL)expanded shown:(BOOL)shown upward:(BOOL)upward;
@end

@implementation FishStatusOverlay
- (BOOL)isFlipped { return NO; }
- (NSView *)hitTest:(NSPoint)point {
    if (NSPointInRect(point, [self convertRect:self.disclosureButton.bounds fromView:self.disclosureButton])) return self.disclosureButton;
    return NSPointInRect(point, self.pill.frame) ? self.taskButton : nil;
}
- (void)toggleTasks:(id)sender { CycleTaskCards(); }
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

    self.content = [[NSView alloc] initWithFrame:NSZeroRect];
    if (@available(macOS 26.0, *)) {
        // Use the same direct native Liquid Glass surface as task cards.
        NSGlassEffectView *glass = [[NSGlassEffectView alloc] initWithFrame:NSZeroRect];
        glass.style = NSGlassEffectViewStyleRegular;
        glass.cornerRadius = 20;
        glass.contentView = self.content;
        self.glass = glass;
        self.liquidGlass = YES;
        [self.pill addSubview:glass];
    } else {
        self.backdrop = [[NSVisualEffectView alloc] initWithFrame:NSZeroRect];
        self.backdrop.blendingMode = NSVisualEffectBlendingModeBehindWindow;
        self.backdrop.material = NSVisualEffectMaterialHUDWindow;
        self.backdrop.state = NSVisualEffectStateActive;
        self.backdrop.wantsLayer = YES;
        self.backdrop.layer.cornerRadius = 20;
        self.backdrop.layer.masksToBounds = YES;
        [self.pill addSubview:self.backdrop];
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
    self.disclosureButton = [[FishStatusDragButton alloc] initWithFrame:NSZeroRect];
    self.disclosureButton.bordered = NO;
    self.disclosureButton.imagePosition = NSImageOnly;
    self.disclosureButton.contentTintColor = NSColor.labelColor;
    self.disclosureButton.target = self;
    self.disclosureButton.action = @selector(toggleTasks:);
    [self.content addSubview:self.disclosureButton];
    [self updateDisclosure:NO shown:NO upward:YES];
    self.taskButton = [[FishStatusDragButton alloc] initWithFrame:NSZeroRect];
    self.taskButton.bordered = NO;
    self.taskButton.transparent = YES;
    self.taskButton.target = self;
    self.taskButton.action = @selector(toggleTasks:);
    self.taskButton.accessibilityLabel = @"查看任务消息";
    self.taskButton.toolTip = @"点击查看任务；拖动移动桌宠";
    [self addSubview:self.taskButton];
    return self;
}
- (void)updateCaption:(NSString *)caption tone:(NSString *)tone {
    self.caption.stringValue = [caption stringByReplacingOccurrencesOfString:@"\n" withString:@" "];
    self.caption.accessibilityValue = caption;
    self.taskButton.accessibilityValue = caption;
    self.needsLayout = YES;
    [self layoutSubtreeIfNeeded];
}
- (void)updateDisclosure:(BOOL)expanded shown:(BOOL)shown upward:(BOOL)upward {
    BOOL pointsUp = shown ? !upward : upward;
    NSImage *image = [NSImage imageWithSystemSymbolName:pointsUp ? @"chevron.up" : @"chevron.down" accessibilityDescription:nil];
    self.disclosureButton.image = [image imageWithSymbolConfiguration:[NSImageSymbolConfiguration configurationWithPointSize:12 weight:NSFontWeightSemibold]];
    self.disclosureButton.accessibilityLabel = expanded ? @"折叠任务列表" : (shown ? @"收缩任务卡片" : @"显示任务卡片");
    self.disclosureButton.accessibilityValue = upward ? @"卡片向上展开" : @"卡片向下展开";
    self.disclosureButton.toolTip = expanded ? @"点击折叠列表；拖动移动桌宠" : (shown ? @"点击完全收缩卡片；拖动移动桌宠" : @"点击显示折叠卡片；拖动移动桌宠");
}
- (void)layout {
    [super layout];
    // Include NSTextField padding and use an even point width so the 350pt
    // task window shares this capsule's center after AppKit window snapping.
    CGFloat text = self.caption.cell.cellSize.width;
    CGFloat width = MIN(MAX(140, 2*ceil((text+60)/2)), MAX(140, 2*floor((self.bounds.size.width-24)/2)));
    self.pill.frame = NSMakeRect(floor((self.bounds.size.width-width)/2), 5, width, 40);
    self.backdrop.frame = self.pill.bounds;
    // Keep the glass edge half a backing pixel inside the authored capsule.
    // Compensate content coordinates for the half-pixel glass inset.
    CGFloat edgeInset = self.liquidGlass ? .5 / MAX(1, self.window.backingScaleFactor) : 0;
    if (self.liquidGlass) self.glass.frame = NSInsetRect(self.pill.bounds, edgeInset, 0);
    self.content.frame = NSMakeRect(0, 0, width - 2 * edgeInset, 40);
    self.caption.frame = NSMakeRect(16 - edgeInset, 11, width - 60, 18);
    self.disclosureButton.frame = NSMakeRect(width - 36 - edgeInset, 8, 24, 24);
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
static BOOL layingOutTasks = NO;
static BOOL refreshingAppearance = NO;
static NSPanel *taskPanel;
static NSScrollView *taskScroll;
static NSView *taskDocument;
static NSView *taskListSurface;
static NSMutableDictionary *taskRows;
static NSArray<NSDictionary *> *taskData;
static BOOL tasksExpanded = NO;
static BOOL taskCardsShown = YES;
static CGFloat petTaskOffset = 0;
static BOOL applyingPetOffset = NO;
static BOOL statusAtTop = NO;
static BOOL petNeedsSideSelection = YES;
static BOOL tasksExpandUpward = YES;
static BOOL taskRowsExpandUpward = YES;
static id outsideClickMonitor;
static id localClickMonitor;
static NSArray *originalAccessibilityChildren;
static BOOL taskAnimationActive = NO;
static BOOL preparingTaskAnimation = NO;
static BOOL taskAnimationNeedsRefresh = NO;
static BOOL taskAnimationUpward = YES;
static NSPoint taskAnimationAnchor;
static NSTimer *taskAnimationTimer;
static NSView *taskAnimationHost;
static NSArray *taskAnimationEntries;
static CFTimeInterval taskAnimationStart;
static NSUInteger taskAnimationGeneration;
static CGFloat taskAnimationStackFrom;
static CGFloat taskAnimationStackTo;
static CGFloat taskAnimationPetFrom;
static CGFloat taskAnimationPetTo;
static BOOL taskAnimationListTransition;

static const CGFloat TaskCardWidth = 330;
static const CGFloat TaskCardHeight = 56;
static const CGFloat TaskPanelWidth = 350;
static const CGFloat TaskRowStep = 64;
static const CGFloat TaskListMaxHeight = 332;

static BOOL TaskCardsPresent(void) { return taskCardsShown && (taskData.count>0 || tasksExpanded); }
static NSRect PetDockFrame(void) {
    NSRect frame=petWindow.frame;
    frame.origin.y-=petTaskOffset;
    return frame;
}
static NSScreen *PetDockScreen(void) {
    NSPoint center=NSMakePoint(NSMidX(PetDockFrame()),NSMidY(PetDockFrame()));
    for (NSScreen *screen in NSScreen.screens) if (NSPointInRect(center,screen.frame)) return screen;
    return petWindow.screen ?: NSScreen.mainScreen;
}
static CGFloat DesiredPetTaskOffset(void) {
    if (!wantsVisible || !TaskCardsPresent()) return 0;
    CGFloat reserve=TaskCardHeight+16+(MIN(MAX(taskData.count,1),3)-1)*6+8;
    return tasksExpandUpward ? reserve : -reserve;
}
static void ApplyPetTaskOffset(CGFloat offset) {
    if (!petWindow || fabs(offset-petTaskOffset)<.001) return;
    NSRect dock=PetDockFrame(),status=statusPanel.frame,cards=taskPanel.frame;
    applyingPetOffset=YES;
    [petWindow setFrameOrigin:NSMakePoint(dock.origin.x,dock.origin.y+offset)];
    // NSWindow snaps positions to screen points. Keep the actual applied offset
    // so repeated animation ticks never accumulate a virtual-anchor drift.
    petTaskOffset=petWindow.frame.origin.y-dock.origin.y;
    if (statusPanel) [statusPanel setFrame:status display:NO];
    if (taskPanel) [taskPanel setFrame:cards display:NO];
    applyingPetOffset=NO;
}

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
    NSView *content = [[NSView alloc] initWithFrame:self.bounds];
    self.content = content;
    if (@available(macOS 26.0, *)) {
        NSGlassEffectView *glass = [[NSGlassEffectView alloc] initWithFrame:self.bounds];
        glass.autoresizingMask = NSViewWidthSizable | NSViewHeightSizable;
        // Let AppKit render the complete adaptive lens and its content. A
        // separate HUD material washes out the glass instead of replacing it.
        glass.style = NSGlassEffectViewStyleRegular;
        glass.cornerRadius = TaskCardHeight/2;
        glass.contentView = content;
        self.glass = glass;
        self.liquidGlass = YES;
        [self addSubview:glass];
    } else {
        // Older systems retain vibrancy as an explicit compatibility fallback.
        NSVisualEffectView *backdrop = [[NSVisualEffectView alloc] initWithFrame:self.bounds];
        backdrop.autoresizingMask = NSViewWidthSizable | NSViewHeightSizable;
        backdrop.blendingMode = NSVisualEffectBlendingModeBehindWindow;
        backdrop.material = NSVisualEffectMaterialHUDWindow;
        backdrop.state = NSVisualEffectStateActive;
        backdrop.wantsLayer = YES;
        backdrop.layer.cornerRadius = TaskCardHeight/2;
        backdrop.layer.masksToBounds = YES;
        self.backdrop = backdrop;
        self.glass = backdrop;
        [self addSubview:backdrop];
        [backdrop addSubview:content];
    }
    self.titleLabel = [NSTextField labelWithString:@""];
    self.titleLabel.font = [NSFont systemFontOfSize:13 weight:NSFontWeightSemibold];
    self.titleLabel.textColor = NSColor.labelColor;
    self.titleLabel.frame = NSMakeRect(16, 28, frame.size.width-32, 18);
    self.messageLabel = [NSTextField labelWithString:@""];
    self.messageLabel.font = [NSFont systemFontOfSize:12 weight:NSFontWeightRegular];
    self.messageLabel.textColor = NSColor.secondaryLabelColor;
    self.messageLabel.frame = NSMakeRect(16, 10, frame.size.width-32, 17);
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
    title.origin.x = message.origin.x = 16-edgeInset;
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
    SetTasksExpanded(NO,YES);
    // Official local-chat deep link; opening never sends a message.
    NSURL *url = [NSURL URLWithString:[@"codex://threads/" stringByAppendingString:self.sessionID]];
    [NSWorkspace.sharedWorkspace openURL:url];
}
@end

@interface FishTaskStack : NSView
@property(nonatomic, strong) FishTaskRow *front;
@property(nonatomic, strong) NSArray<FishTaskRow *> *backs;
@property(nonatomic, strong) NSButton *button;
@property(nonatomic) NSUInteger count;
@property(nonatomic) BOOL expandsUpward;
- (void)updateTasks:(NSArray<NSDictionary *> *)tasks;
@end
@implementation FishTaskStack
- (instancetype)initWithFrame:(NSRect)frame {
    if (!(self = [super initWithFrame:frame])) return nil;
    NSMutableArray *backs = [NSMutableArray new];
    // Farthest card first: the two native-glass edges peek below the front.
    for (NSUInteger i=0; i<2; i++) {
        FishTaskRow *row = [[FishTaskRow alloc] initWithFrame:NSMakeRect(0,0,TaskCardWidth,TaskCardHeight)];
        row.button.hidden = YES;
        row.accessibilityHidden = YES;
        [self addSubview:row];
        [backs addObject:row];
    }
    self.backs = backs;
    self.front = [[FishTaskRow alloc] initWithFrame:NSMakeRect(10,8,TaskCardWidth,TaskCardHeight)];
    self.front.button.hidden = YES;
    self.front.accessibilityHidden = YES;
    [self addSubview:self.front];
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
    self.button.accessibilityLabel = [NSString stringWithFormat:@"展开任务列表，共 %lu 项",(unsigned long)self.count];
    self.button.accessibilityValue = [NSString stringWithFormat:@"%@，%@",self.front.titleLabel.stringValue,self.front.messageLabel.stringValue];
    self.button.toolTip = [NSString stringWithFormat:@"点击展开 %lu 项任务\n%@",(unsigned long)self.count,self.front.button.toolTip];
    self.needsLayout = YES;
}
- (void)layout {
    [super layout];
    NSUInteger layers = MIN(MAX(self.count,1),3);
    CGFloat frontY = self.expandsUpward ? 8 : 8 + (layers-1)*6;
    for (NSUInteger i=0; i<self.backs.count; i++) {
        NSUInteger depth = 2-i;
        FishTaskRow *back = self.backs[i];
        back.hidden = depth >= layers;
        back.frame = NSMakeRect(10+depth*8,frontY+(self.expandsUpward ? 1.0 : -1.0)*depth*6,TaskCardWidth-depth*16,TaskCardHeight);
    }
    self.front.frame = NSMakeRect(10,frontY,TaskCardWidth,TaskCardHeight);
    self.button.frame = self.bounds;
}
@end
static FishTaskStack *taskStack;

@interface FishTaskAnimationEntry : NSObject
@property(nonatomic, strong) FishTaskRow *row;
@property(nonatomic) NSRect fromFrame;
@property(nonatomic) NSRect toFrame;
@property(nonatomic) CGFloat fromAlpha;
@property(nonatomic) CGFloat toAlpha;
@property(nonatomic) NSTimeInterval delay;
@end
@implementation FishTaskAnimationEntry @end

static NSPanel *NewTaskPanel(void) {
    Class cls = FishPassiveStatusPanel.class;
    if (@available(macOS 26.0, *)) cls = FishActiveStatusPanel.class;
    NSPanel *panel = [[cls alloc] initWithContentRect:NSMakeRect(0,0,TaskPanelWidth,TaskCardHeight+28)
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
    // A live snapshot may arrive during the 300ms transition. Apply it once the
    // current visual gesture settles instead of snapping its moving cards.
    if (taskAnimationActive) { taskAnimationNeedsRefresh=YES; return; }
    CGFloat distanceFromAnchor = MAX(0, taskRowsExpandUpward ? NSMinY(taskScroll.contentView.bounds) : taskDocument.bounds.size.height-NSMaxY(taskScroll.contentView.bounds));
    NSArray *items = taskData.count ? taskData : @[@{@"session_id":@"", @"title":@"暂无进行中的任务", @"summary":@"新的任务开始后会显示在这里"}];
    CGFloat height = items.count * TaskRowStep + 12;
    taskListSurface.frame = NSMakeRect(0,0,TaskPanelWidth,height);
    taskDocument.frame = taskListSurface.bounds;
    NSMutableSet *keep = [NSMutableSet new];
    for (NSUInteger i=0; i<items.count; i++) {
        NSDictionary *item = items[i];
        NSString *key = item[@"session_id"] ?: @"";
        [keep addObject:key];
        FishTaskRow *row = taskRows[key];
        if (!row) { row = [[FishTaskRow alloc] initWithFrame:NSMakeRect(10,0,TaskCardWidth,TaskCardHeight)]; taskRows[key]=row; [taskDocument addSubview:row]; }
        row.frame = NSMakeRect(10,tasksExpandUpward ? 8+i*TaskRowStep : height-(i+1)*TaskRowStep,TaskCardWidth,TaskCardHeight);
        [row updateTask:item];
    }
    for (NSString *key in [taskRows.allKeys copy]) if (![keep containsObject:key]) {
        [taskRows[key] removeFromSuperview]; [taskRows removeObjectForKey:key];
    }
    [taskStack updateTasks:taskData];
    taskRowsExpandUpward = tasksExpandUpward;
    if (!layingOut) LayoutStatus();
    LayoutTasks();
    CGFloat maxOffset = MAX(0,height-taskScroll.contentView.bounds.size.height);
    CGFloat offset = MIN(distanceFromAnchor,maxOffset);
    [taskScroll.contentView scrollToPoint:NSMakePoint(0,tasksExpandUpward ? offset : maxOffset-offset)];
    [taskScroll reflectScrolledClipView:taskScroll.contentView];
    LayoutTasks();
}
static NSRect TaskPanelRect(BOOL expanded) {
    NSRect screen = PetDockScreen().visibleFrame;
    CGFloat height = expanded ? MIN(TaskListMaxHeight, MAX(TaskRowStep+12, (taskData.count ?: 1)*TaskRowStep+12)) : TaskCardHeight+16+(MIN(MAX(taskData.count,1),3)-1)*6;
    NSRect pill = [statusPanel convertRectToScreen:[overlay convertRect:overlay.pill.frame toView:nil]];
    // Grow toward the desktop interior without moving or resizing the avatar.
    CGFloat available = tasksExpandUpward ? NSMaxY(screen)-8-NSMaxY(pill) : NSMinY(pill)-NSMinY(screen)-8;
    height = MIN(height,MAX(1,available));
    CGFloat x = MIN(MAX(NSMidX(pill)-TaskPanelWidth/2, NSMinX(screen)+8), NSMaxX(screen)-TaskPanelWidth-8);
    CGFloat y = tasksExpandUpward ? NSMaxY(pill) : NSMinY(pill)-height;
    return NSMakeRect(x,y,TaskPanelWidth,height);
}
static void LayoutTasks(void) {
    if (!taskPanel || !petWindow || !statusPanel || layingOutTasks || preparingTaskAnimation) return;
    if ((!TaskCardsPresent() && !taskAnimationActive) || !wantsVisible || !petWindow.visible || petWindow.miniaturized) {
        CancelTaskAnimation(); [taskPanel orderOut:nil]; UpdateTaskAccessibility(); return;
    }
    NSRect frame=TaskPanelRect(tasksExpanded);
    if (taskAnimationActive) {
        NSPoint anchor=NSMakePoint(NSMinX(frame),tasksExpandUpward ? NSMinY(frame) : NSMaxY(frame));
        if (taskAnimationUpward==tasksExpandUpward && NSEqualPoints(anchor,taskAnimationAnchor)) { UpdateTaskAccessibility(); return; }
        BOOL refresh=taskAnimationNeedsRefresh;
        CancelTaskAnimation();
        if (refresh) { UpdateTaskRows(); return; }
    }
    layingOutTasks = YES;
    [taskPanel setFrame:frame display:YES];
    taskScroll.hidden = !tasksExpanded;
    taskStack.hidden = tasksExpanded;
    // Keep the hidden scroll viewport intact so collapsing/live updates do not
    // discard the user's distance from the top of the expanded list.
    if (tasksExpanded) taskScroll.frame = taskPanel.contentView.bounds;
    else { taskStack.expandsUpward=tasksExpandUpward; taskStack.frame = taskPanel.contentView.bounds; taskStack.needsLayout=YES; [taskStack layoutSubtreeIfNeeded]; }
    taskPanel.level = petWindow.level;
    taskPanel.collectionBehavior = petWindow.collectionBehavior | NSWindowCollectionBehaviorFullScreenAuxiliary | NSWindowCollectionBehaviorIgnoresCycle;
    if (taskPanel.parentWindow != petWindow) [petWindow addChildWindow:taskPanel ordered:NSWindowAbove];
    [taskPanel orderFront:nil];
    layingOutTasks = NO;
    UpdateTaskAccessibility();
}
static void UpdateTaskAccessibility(void) {
    if (!petWindow) return;
    NSMutableArray *children = [NSMutableArray arrayWithArray:originalAccessibilityChildren ?: @[]];
    // Keep passive child-window controls reachable from the pet's main window
    // for VoiceOver and accessibility clients without taking keyboard focus.
    if (statusPanel.visible) [children addObjectsFromArray:@[overlay.taskButton,overlay.disclosureButton]];
    if (taskPanel.visible && !taskAnimationActive) [children addObject:tasksExpanded ? (id)taskScroll : taskStack.button];
    petWindow.accessibilityChildren = children;
    BOOL shown=TaskCardsPresent();
    overlay.taskButton.accessibilityLabel = tasksExpanded ? @"折叠任务列表" : (shown ? @"收缩任务卡片" : @"显示任务卡片");
    overlay.taskButton.toolTip = tasksExpanded ? @"点击折叠列表；拖动移动桌宠" : (shown ? @"点击完全收缩卡片；拖动移动桌宠" : @"点击显示折叠卡片；拖动移动桌宠");
    [overlay updateDisclosure:tasksExpanded shown:shown upward:tasksExpandUpward];
}
static void EnsureTaskPanel(void) {
    if (!taskPanel) {
        taskPanel = NewTaskPanel();
        taskScroll = [[NSScrollView alloc] initWithFrame:NSMakeRect(0,0,TaskPanelWidth,TaskListMaxHeight)];
        taskScroll.drawsBackground = NO;
        taskScroll.hasVerticalScroller = YES;
        taskScroll.autohidesScrollers = YES;
        taskScroll.scrollerStyle = NSScrollerStyleOverlay;
        taskScroll.contentView.drawsBackground = NO;
        taskDocument = [[NSView alloc] initWithFrame:NSMakeRect(0,0,TaskPanelWidth,TaskRowStep+12)];
        if (@available(macOS 26.0, *)) {
            // Expanded cards share background sampling without merging across
            // their gaps. Keep the overlapping stack outside this container:
            // merging its silhouettes would erase the separate card edges.
            NSGlassEffectContainerView *group = [[NSGlassEffectContainerView alloc] initWithFrame:taskDocument.frame];
            group.spacing = 0;
            group.contentView = taskDocument;
            taskListSurface = group;
        } else taskListSurface = taskDocument;
        taskScroll.documentView = taskListSurface;
        [taskPanel.contentView addSubview:taskScroll];
        taskRows = [NSMutableDictionary new];
        taskStack = [[FishTaskStack alloc] initWithFrame:taskPanel.contentView.bounds];
        [taskPanel.contentView addSubview:taskStack];
    }
}
static void CancelTaskAnimation(void) {
    taskAnimationGeneration++;
    [taskAnimationTimer invalidate]; taskAnimationTimer=nil;
    [taskAnimationHost removeFromSuperview]; taskAnimationHost=nil;
    taskAnimationEntries=nil;
    taskAnimationActive=NO; taskAnimationNeedsRefresh=NO;
    taskStack.alphaValue=1; taskStack.front.hidden=NO; taskStack.button.hidden=NO;
}
static void AdvanceTaskAnimation(NSTimeInterval elapsed) {
    if (!taskAnimationActive) return;
    CGFloat t=MIN(1,MAX(0,elapsed/.28));
    CGFloat ease=1-pow(1-t,3);
    ApplyPetTaskOffset(taskAnimationPetFrom+(taskAnimationPetTo-taskAnimationPetFrom)*ease);
    for (FishTaskAnimationEntry *entry in taskAnimationEntries) {
        CGFloat t=MIN(1,MAX(0,(elapsed-entry.delay)/.24));
        CGFloat ease=1-pow(1-t,3);
        NSRect a=entry.fromFrame,b=entry.toFrame;
        entry.row.frame=NSMakeRect(a.origin.x+(b.origin.x-a.origin.x)*ease,a.origin.y+(b.origin.y-a.origin.y)*ease,
            a.size.width+(b.size.width-a.size.width)*ease,a.size.height+(b.size.height-a.size.height)*ease);
        entry.row.alphaValue=entry.fromAlpha+(entry.toAlpha-entry.fromAlpha)*ease;
        entry.row.needsLayout=YES; [entry.row layoutSubtreeIfNeeded];
    }
    taskStack.alphaValue=taskAnimationStackFrom+(taskAnimationStackTo-taskAnimationStackFrom)*ease;
    if (elapsed>=.30) {
        BOOL refresh=taskAnimationNeedsRefresh;
        CancelTaskAnimation();
        if (refresh) UpdateTaskRows(); else LayoutTasks();
    }
}
static void SetTaskMode(FishTaskMode mode, BOOL animated) {
    EnsureTaskPanel();
    BOOL expanded=mode==FishTaskExpanded,shown=mode!=FishTaskHidden;
    if (tasksExpanded==expanded && taskCardsShown==shown) return;
    BOOL wasExpanded=tasksExpanded,wasShown=TaskCardsPresent(),interrupted=taskAnimationActive,wasVisible=taskPanel.visible;
    BOOL listTransition=wasExpanded || expanded || (interrupted && taskAnimationListTransition);
    NSRect oldCanvas=taskPanel.frame;
    CGFloat oldStackAlpha=taskStack.alphaValue;
    NSMutableDictionary *previous=[NSMutableDictionary new];
    for (FishTaskAnimationEntry *entry in taskAnimationEntries) previous[entry.row.sessionID ?: @""]=@{
        @"frame":[NSValue valueWithRect:entry.row.frame],@"alpha":@(entry.row.alphaValue)};
    CancelTaskAnimation();
    tasksExpanded=expanded;
    taskCardsShown=shown;
    BOOL animate=animated && wantsVisible && petWindow.visible && statusPanel.visible &&
        !NSWorkspace.sharedWorkspace.accessibilityDisplayShouldReduceMotion;
    preparingTaskAnimation=animate;
    LayoutStatus();
    if (!animate) { UpdateTaskRows(); return; }
    taskAnimationPetFrom=petTaskOffset;
    taskAnimationPetTo=DesiredPetTaskOffset();
    CGFloat distance=MAX(0,taskRowsExpandUpward ? NSMinY(taskScroll.contentView.bounds) : taskDocument.bounds.size.height-NSMaxY(taskScroll.contentView.bounds));
    preparingTaskAnimation=YES;
    UpdateTaskRows();
    NSRect canvas=TaskPanelRect(listTransition),collapsed=TaskPanelRect(NO);
    [taskPanel setFrame:canvas display:NO];
    taskScroll.frame=taskPanel.contentView.bounds;
    [taskScroll tile];
    CGFloat maxOffset=MAX(0,taskDocument.bounds.size.height-taskScroll.contentView.bounds.size.height);
    CGFloat offset=MIN(distance,maxOffset);
    [taskScroll.contentView scrollToPoint:NSMakePoint(0,tasksExpandUpward ? offset : maxOffset-offset)];
    [taskScroll reflectScrolledClipView:taskScroll.contentView];
    taskScroll.hidden=YES;
    taskStack.hidden=NO;
    taskStack.expandsUpward=tasksExpandUpward;
    taskStack.frame=NSMakeRect(0,collapsed.origin.y-canvas.origin.y,TaskPanelWidth,collapsed.size.height);
    taskStack.needsLayout=YES; [taskStack layoutSubtreeIfNeeded];
    NSRect front=[taskPanel.contentView convertRect:taskStack.front.bounds fromView:taskStack.front];
    taskStack.front.hidden=YES; taskStack.button.hidden=YES;
    taskAnimationStackFrom=interrupted ? oldStackAlpha : (wasShown && !wasExpanded && wasVisible ? 1 : 0);
    BOOL targetPresent=TaskCardsPresent();
    taskAnimationStackTo=targetPresent && !expanded ? 1 : 0;
    taskStack.alphaValue=taskAnimationStackFrom;
    taskAnimationHost=[[NSView alloc] initWithFrame:taskPanel.contentView.bounds];
    taskAnimationHost.wantsLayer=YES; taskAnimationHost.layer.masksToBounds=YES;
    taskAnimationHost.accessibilityHidden=YES;
    [taskPanel.contentView addSubview:taskAnimationHost];
    NSArray *items=taskData.count ? taskData : @[@{@"session_id":@"",@"title":@"暂无进行中的任务",@"summary":@"新的任务开始后会显示在这里"}];
    NSMutableArray *entries=[NSMutableArray new];
    // Farther cards are behind the front card as they leave or rejoin the stack.
    for (NSInteger i=(NSInteger)items.count-1;i>=0;i--) {
        NSDictionary *item=items[i];
        FishTaskRow *source=taskRows[item[@"session_id"] ?: @""];
        NSRect target=[taskPanel.contentView convertRect:source.bounds fromView:source];
        BOOL visible=NSIntersectsRect(target,taskAnimationHost.bounds);
        if (i!=0 && (!listTransition || !visible)) continue;
        CGFloat depth=MIN(i,2);
        NSRect folded=NSMakeRect(front.origin.x+depth*8,front.origin.y+(tasksExpandUpward ? 1.0 : -1.0)*depth*6,TaskCardWidth-depth*16,TaskCardHeight);
        FishTaskAnimationEntry *entry=[FishTaskAnimationEntry new];
        entry.row=[[FishTaskRow alloc] initWithFrame:NSMakeRect(0,0,TaskCardWidth,TaskCardHeight)];
        [entry.row updateTask:item]; entry.row.button.hidden=YES; entry.row.accessibilityHidden=YES;
        NSDictionary *prior=previous[item[@"session_id"] ?: @""];
        entry.fromFrame=prior ? NSOffsetRect([prior[@"frame"] rectValue],oldCanvas.origin.x-canvas.origin.x,oldCanvas.origin.y-canvas.origin.y) : (wasExpanded ? target : folded);
        entry.toFrame=expanded ? target : folded;
        entry.fromAlpha=prior ? [prior[@"alpha"] doubleValue] : (wasExpanded ? visible : (i==0 && wasShown && wasVisible ? 1 : 0));
        entry.toAlpha=!targetPresent ? 0 : (expanded ? visible : (i==0 ? 1 : 0));
        entry.delay=interrupted ? 0 : MIN(i,4)*.014;
        [taskAnimationHost addSubview:entry.row]; [entries addObject:entry];
    }
    taskAnimationEntries=entries;
    taskAnimationListTransition=listTransition;
    taskAnimationUpward=tasksExpandUpward;
    taskAnimationAnchor=NSMakePoint(NSMinX(canvas),tasksExpandUpward ? NSMinY(canvas) : NSMaxY(canvas));
    preparingTaskAnimation=NO;
    taskAnimationActive=YES;
    AdvanceTaskAnimation(0);
    taskPanel.level=petWindow.level;
    taskPanel.collectionBehavior=petWindow.collectionBehavior | NSWindowCollectionBehaviorFullScreenAuxiliary | NSWindowCollectionBehaviorIgnoresCycle;
    if (taskPanel.parentWindow!=petWindow) [petWindow addChildWindow:taskPanel ordered:NSWindowAbove];
    [taskPanel orderFront:nil];
    UpdateTaskAccessibility();
    taskAnimationStart=CACurrentMediaTime();
    NSUInteger generation=taskAnimationGeneration;
    taskAnimationTimer=[NSTimer timerWithTimeInterval:1.0/60 repeats:YES block:^(NSTimer *timer) {
        if (generation==taskAnimationGeneration) AdvanceTaskAnimation(CACurrentMediaTime()-taskAnimationStart);
    }];
    taskAnimationTimer.tolerance=.002;
    [NSRunLoop.mainRunLoop addTimer:taskAnimationTimer forMode:NSRunLoopCommonModes];
}
static void SetTasksExpanded(BOOL expanded, BOOL animated) { SetTaskMode(expanded ? FishTaskExpanded : FishTaskFolded,animated); }
static void CycleTaskCards(void) {
    if (tasksExpanded) SetTaskMode(FishTaskFolded,YES);
    else if (TaskCardsPresent()) SetTaskMode(FishTaskHidden,YES);
    else SetTaskMode(taskData.count ? FishTaskFolded : FishTaskExpanded,YES);
}
static void PreparePetDrag(void) {
    if (!taskAnimationActive) return;
    BOOL refresh=taskAnimationNeedsRefresh;
    CancelTaskAnimation();
    LayoutStatus();
    if (refresh) UpdateTaskRows();
}
static void ToggleTasks(void) {
    SetTasksExpanded(!tasksExpanded,YES);
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
    if (!statusPanel || !petWindow || layingOut || applyingPetOffset) return;
    layingOut = YES;
    NSView *content = petWindow.contentView;
    NSRect screen = PetDockScreen().visibleFrame;
    BOOL tracking = ((FishStatusDragButton *)overlay.taskButton).pressed || ((FishStatusDragButton *)overlay.disclosureButton).pressed;
    if (!tracking && petNeedsSideSelection) {
        statusAtTop = NSMidY(petWindow.frame) > NSMidY(screen);
        petNeedsSideSelection=NO;
    }
    BOOL directionChanged = tasksExpandUpward != !statusAtTop;
    tasksExpandUpward = !statusAtTop;
    if (directionChanged) CancelTaskAnimation();
    CGFloat offset=DesiredPetTaskOffset();
    if (directionChanged && petTaskOffset!=0 && offset!=0) {
        // A user drag changing sides keeps its exact avatar endpoint. Rebase
        // the dock to the new reservation direction instead of jumping the pet.
        petTaskOffset=offset;
    } else if (!taskAnimationActive && !preparingTaskAnimation) ApplyPetTaskOffset(offset);
    CGFloat textWidth = overlay.caption.cell.cellSize.width;
    CGFloat capsuleWidth = MIN(MAX(140,2*ceil((textWidth+60)/2)),MAX(140,2*floor((content.bounds.size.width-24)/2)));
    CGFloat panelWidth = capsuleWidth+24;
    // Transparent horizontal room preserves the native capsule edge rendering.
    // Move one capsule height toward the visible avatar, into the foot-level
    // gap marked by the user. Mirror that inset for an upper status anchor.
    CGFloat statusY = statusAtTop ? NSMaxY(content.bounds)-74 : 24;
    NSRect rect = [content convertRect:NSMakeRect(floor((content.bounds.size.width-capsuleWidth)/2)-12, statusY, panelWidth, 60) toView:nil];
    rect = [petWindow convertRectToScreen:rect];
    rect.origin.y-=petTaskOffset;
    // Clamp the painted capsule, retaining its transparent edge/shadow padding.
    rect.origin.x = MIN(MAX(rect.origin.x,NSMinX(screen)+8-12),NSMaxX(screen)-8-capsuleWidth-12);
    rect.origin.y = MIN(MAX(rect.origin.y,NSMinY(screen)+8-5),NSMaxY(screen)-8-45);
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
    if (directionChanged) UpdateTaskRows(); else LayoutTasks();
    UpdateTaskAccessibility();
    layingOut = NO;
}
static void InstallObservers(void) {
    observers = [NSMutableArray new];
    NSNotificationCenter *center = NSNotificationCenter.defaultCenter;
    void (^outsideClick)(NSEvent *) = ^(NSEvent *event) {
        NSPoint point = event.window ? [event.window convertPointToScreen:event.locationInWindow] : NSEvent.mouseLocation;
        if (tasksExpanded && !IsInsideStatus(point) && !NSPointInRect(point,taskPanel.frame)) {
            SetTasksExpanded(NO,YES);
        }
    };
    outsideClickMonitor = [NSEvent addGlobalMonitorForEventsMatchingMask:NSEventMaskLeftMouseDown handler:outsideClick];
    localClickMonitor = [NSEvent addLocalMonitorForEventsMatchingMask:NSEventMaskLeftMouseDown handler:^NSEvent *(NSEvent *event) { outsideClick(event); return event; }];
    for (NSNotificationName name in @[NSWindowDidMoveNotification, NSWindowDidResizeNotification,
        NSWindowDidChangeScreenNotification, NSWindowDidMiniaturizeNotification, NSWindowDidDeminiaturizeNotification]) {
        [observers addObject:[center addObserverForName:name object:nil queue:NSOperationQueue.mainQueue usingBlock:^(NSNotification *note) {
            if (note.object == petWindow && !applyingPetOffset) { petNeedsSideSelection=YES; PreparePetDrag(); LayoutStatus(); }
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
            CancelTaskAnimation();
            ApplyPetTaskOffset(0);
            petWindow.accessibilityChildren = originalAccessibilityChildren;
            [petWindow removeChildWindow:statusPanel];
            petWindow = window;
            petTaskOffset=0;
            petNeedsSideSelection=YES;
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
            NSLog(@"Desktop Fish status material: %@; shadow=.12/4/-1.5; height=40 radius=20; activePainting=1",
                overlay.liquidGlass ? @"NSGlassEffectView.regular tint=nil; HUD=none" : @"NSVisualEffectView.HUDWindow compatibility fallback");
            NSLog(@"Desktop Fish original glass initial focus: appActive=%d petKey=%d panelKey=%d actualPanelKey=%d; forceSelector=%d refreshSelector=%d",
                NSApp.active, petWindow.keyWindow, statusPanel.keyWindow, NSApp.keyWindow == statusPanel,
                [statusPanel respondsToSelector:NSSelectorFromString(@"_setForceActiveControls:")],
                [statusPanel respondsToSelector:NSSelectorFromString(@"_sendForcedWindowChangedKeyState")]);
        }
    });
    return atomic_load(&ready);
}
FISH_EXPORT void FishGlassStatusSetVisible(int visible) {
    OnMain(^{ wantsVisible = visible != 0; if (!wantsVisible) { CancelTaskAnimation(); tasksExpanded=NO; } LayoutStatus(); });
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
    OnMain(^{ SetTasksExpanded(YES,YES); RefreshActiveAppearance(); });
}
FISH_EXPORT void FishGlassStatusDestroy(void) {
    OnMain(^{
        CancelTaskAnimation();
        wantsVisible=NO;
        ApplyPetTaskOffset(0);
        if (outsideClickMonitor) [NSEvent removeMonitor:outsideClickMonitor];
        if (localClickMonitor) [NSEvent removeMonitor:localClickMonitor];
        outsideClickMonitor=nil; localClickMonitor=nil;
        [petWindow removeChildWindow:taskPanel]; [taskPanel orderOut:nil]; [taskPanel close];
        taskPanel=nil; taskScroll=nil; taskDocument=nil; taskListSurface=nil; taskRows=nil; taskStack=nil; taskData=nil; tasksExpanded=NO;
        statusAtTop=NO; tasksExpandUpward=YES; taskRowsExpandUpward=YES;
        taskCardsShown=YES; petTaskOffset=0;
        petNeedsSideSelection=YES;
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
