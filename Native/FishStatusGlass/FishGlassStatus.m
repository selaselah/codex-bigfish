#import <AppKit/AppKit.h>
#import <QuartzCore/QuartzCore.h>
#import <stdatomic.h>
#import <objc/message.h>
#import <objc/runtime.h>

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
static void CancelEdgeReturn(void);
static void FinishPetDrag(void);
static NSWindow *FindPetWindow(void);
static void BeginStatusDrag(NSPoint cursor);
static void MoveStatusDrag(NSWindow *window,NSPoint desired,NSPoint cursor);
static void EndStatusDrag(BOOL moved,NSPoint cursor);
static void CancelStatusDrag(void);
static NSScreen *CurrentStatusScreen(void);
static void DismissTaskNotification(NSString *sessionID, NSString *turnID);
static void PermanentlyHideTaskNotification(NSString *sessionID);
static void ApplyVisibleTasks(BOOL refresh);
static void LayoutTaskActions(void);

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
    BeginStatusDrag(self.downScreen);
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
    MoveStatusDrag(self.dragWindow,NSMakePoint(self.downOrigin.x+delta.x,self.downOrigin.y+delta.y),screen);
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
    EndStatusDrag(moved,screen);
    if (click) [self sendAction:self.action to:self.target];
}
- (void)viewWillMoveToWindow:(NSWindow *)window {
    if (!window) { self.pressed=NO; self.dragWindow=nil; [self highlight:NO]; CancelStatusDrag(); }
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
static NSView *taskActionOverlay;
static id taskActionScrollObserver;
static NSArray<NSDictionary *> *taskData;
// Full lifecycle snapshot stays intact; only card presentation is filtered.
static NSArray<NSDictionary *> *taskSnapshot;
static NSMutableDictionary<NSString *,NSString *> *dismissedTaskTurns;
static NSMutableSet<NSString *> *permanentlyHiddenSessions;
static NSUserDefaults *notificationPreferences;
static NSString *const HiddenSessionDefaultsKey=@"PermanentlyHiddenSessionIDs";
static NSString *const NotificationPreferencesDomain=@"local.desktop.rebuiltbigfish.tasknotifications";
static NSString *selectedTaskID;
static NSArray<NSString *> *stackOrder;
static NSString *stackInsertionPending;
static BOOL tasksExpanded = NO;
static BOOL taskCardsShown = YES;
static CGFloat petTaskOffset = 0;
static BOOL applyingPetOffset = NO;
static const BOOL tasksExpandUpward = YES;
static BOOL taskRowsExpandUpward = YES;
static id outsideClickMonitor;
static id localClickMonitor;
typedef struct { float x,y,screenX,screenY; int phase; } FishAvatarPointer;
static FishAvatarPointer avatarPointerQueue[128];
static NSUInteger avatarPointerHead,avatarPointerCount;
static void CaptureAvatarPointer(NSEvent *event) {
    if(event.window!=petWindow || !petWindow.contentView)return;
    int phase=event.type==NSEventTypeLeftMouseDown ? 1 : event.type==NSEventTypeLeftMouseDragged ? 2 : event.type==NSEventTypeLeftMouseUp ? 3 : 0;
    if(!phase)return;
    NSView *content=petWindow.contentView;NSPoint p=[content convertPoint:event.locationInWindow fromView:nil];
    NSPoint screen=[petWindow convertPointToScreen:event.locationInWindow];
    FishAvatarPointer value={(p.x-NSMinX(content.bounds))/MAX(1,NSWidth(content.bounds)),(p.y-NSMinY(content.bounds))/MAX(1,NSHeight(content.bounds)),screen.x,screen.y,phase};
    if(content.flipped)value.y=1-value.y;
    @synchronized(FishStatusOverlay.class) {
        if(avatarPointerCount==128) { avatarPointerHead=0;avatarPointerCount=1;avatarPointerQueue[0]=(FishAvatarPointer){0,0,0,0,4};return; }
        avatarPointerQueue[(avatarPointerHead+avatarPointerCount)%128]=value;avatarPointerCount++;
    }
}
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
static const CGFloat PetEdgeMargin = 16;
static NSTimer *edgeReturnTimer;
static __weak NSWindow *edgeReturnWindow;
static NSPoint edgeReturnFrom,edgeReturnTo;
static CFTimeInterval edgeReturnStart;
static NSUInteger edgeReturnGeneration;
static BOOL applyingEdgeReturn = NO;
static BOOL applyingStatusDrag = NO,statusDragActive = NO;
static __weak NSWindow *freeFrameWindow;
static Class freeFrameOriginalClass,freeFrameClass;
static IMP freeFrameOriginalIMP;
static IMP freeFrameOriginalLevelIMP;
static NSWindowLevel freeFrameOriginalLevel;
static NSPoint statusDockOffset;
static NSScreen *statusDragScreen,*edgeReturnScreen;
#ifdef FISH_STATUS_DRAG_TESTING
static NSArray<NSScreen *> *statusDragTestScreens;
#endif
static NSArray<NSScreen *> *StatusDragScreens(void) {
#ifdef FISH_STATUS_DRAG_TESTING
    if (statusDragTestScreens) return statusDragTestScreens;
#endif
    return NSScreen.screens;
}

// Keep the Unity window's class identity intact: AppKit accessibility caches
// that identity. Guard public frame/level setters only for this instance;
// other windows in this process retain their original rules.
static NSRect StatusAnchoredFrame(id window,SEL command,NSRect proposed,NSScreen *screen) {
    if (window==freeFrameWindow) return proposed;
    return ((NSRect(*)(id,SEL,NSRect,NSScreen *))freeFrameOriginalIMP)(window,command,proposed,screen);
}
static void StatusFloatingLevel(id window,SEL command,NSWindowLevel requested) {
    // UniWinC's topmost option requests popup-menu level101. Stay above normal
    // apps at the floating level instead, leaving system panels above the pet.
    if(window==freeFrameWindow)requested=NSFloatingWindowLevel;
    ((void(*)(id,SEL,NSWindowLevel))freeFrameOriginalLevelIMP)(window,command,requested);
}
static void RestoreAvatarFrameConstraint(void) {
    if (freeFrameClass && method_getImplementation(class_getInstanceMethod(freeFrameClass,@selector(constrainFrameRect:toScreen:)))==(IMP)StatusAnchoredFrame)
        class_replaceMethod(freeFrameClass,@selector(constrainFrameRect:toScreen:),freeFrameOriginalIMP,method_getTypeEncoding(class_getInstanceMethod(freeFrameClass,@selector(constrainFrameRect:toScreen:))));
    if (freeFrameClass && method_getImplementation(class_getInstanceMethod(freeFrameClass,@selector(setLevel:)))==(IMP)StatusFloatingLevel) {
        class_replaceMethod(freeFrameClass,@selector(setLevel:),freeFrameOriginalLevelIMP,method_getTypeEncoding(class_getInstanceMethod(freeFrameClass,@selector(setLevel:))));
        if(freeFrameWindow)freeFrameWindow.level=freeFrameOriginalLevel;
    }
    freeFrameWindow=nil;freeFrameClass=Nil;freeFrameOriginalClass=Nil;
}
static void InstallAvatarFrameConstraint(NSWindow *window) {
    if (freeFrameWindow==window) return;RestoreAvatarFrameConstraint();
    Class owner=object_getClass(window);Method method=class_getInstanceMethod(owner,@selector(constrainFrameRect:toScreen:));
    freeFrameOriginalIMP=method_getImplementation(method);freeFrameWindow=window;freeFrameOriginalClass=owner;freeFrameClass=owner;
    class_replaceMethod(owner,@selector(constrainFrameRect:toScreen:),(IMP)StatusAnchoredFrame,method_getTypeEncoding(method));
    Method level=class_getInstanceMethod(owner,@selector(setLevel:));
    freeFrameOriginalLevelIMP=method_getImplementation(level);freeFrameOriginalLevel=window.level;
    class_replaceMethod(owner,@selector(setLevel:),(IMP)StatusFloatingLevel,method_getTypeEncoding(level));
    window.level=NSFloatingWindowLevel;
}
static BOOL TaskCardsPresent(void) { return taskCardsShown && (taskData.count>0 || tasksExpanded); }
static NSDictionary *TaskWithID(NSString *sessionID) {
    for(NSDictionary *task in taskData)if([task[@"session_id"] isEqualToString:sessionID])return task;
    return nil;
}
static void LoadHiddenSessions(void) {
    if(permanentlyHiddenSessions)return;
    if(!notificationPreferences)notificationPreferences=[[NSUserDefaults alloc] initWithSuiteName:NotificationPreferencesDomain];
    permanentlyHiddenSessions=[NSMutableSet new];
    id saved=[notificationPreferences objectForKey:HiddenSessionDefaultsKey];
    if([saved isKindOfClass:NSArray.class])for(id sid in saved)
        if([sid isKindOfClass:NSString.class] && [[NSUUID alloc] initWithUUIDString:sid])
            [permanentlyHiddenSessions addObject:[sid lowercaseString]];
}
static BOOL SessionPermanentlyHidden(NSString *sessionID) {
    LoadHiddenSessions();return sessionID && [permanentlyHiddenSessions containsObject:sessionID.lowercaseString];
}
static NSArray<NSDictionary *> *DisplayedTaskItems(void) {
    if(taskData.count)return taskData;
    BOOL restorable=NO;for(NSDictionary *task in taskSnapshot)if(!SessionPermanentlyHidden(task[@"session_id"]))restorable=YES;
    return @[@{@"session_id":@"",@"title":!taskSnapshot.count ? @"暂无进行中的任务" : (restorable ? @"本轮通知已关闭" : @"任务通知已隐藏"),
        @"summary":restorable ? @"点击对应电饭锅可重新显示" : @"新的聊天开始后会显示在这里"}];
}
static NSArray<NSDictionary *> *StackTaskItems(void) {
    NSMutableArray *items=[NSMutableArray new];
    for(NSString *sid in stackOrder) { NSDictionary *task=TaskWithID(sid);if(task)[items addObject:task]; }
    return items;
}
static void PromoteStackTask(NSString *sid) {
    NSMutableArray *order=[stackOrder mutableCopy] ?: [NSMutableArray new];
    [order removeObject:sid];[order insertObject:sid atIndex:0];stackOrder=order;selectedTaskID=[sid copy];
}
static NSRect PetDockFrame(void) {
    NSRect frame=petWindow.frame;
    frame.origin.y-=petTaskOffset;
    return frame;
}
static NSScreen *PetDockScreen(void) { return CurrentStatusScreen(); }

static CGFloat DesiredPetTaskOffset(void) {
    if (!wantsVisible) return 0;
    // Keep the status dock fixed: add 8 pt without cards, and bring the avatar
    // 16 pt closer to the folded stack. Expanded retains the folded position.
    CGFloat reserve=TaskCardsPresent() ? TaskCardHeight+16+(MIN(MAX(taskData.count,1),3)-1)*6+8-16 : 8;
    return reserve;
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

// A standalone native Glass button bezel can disappear in the transparent task
// panel. Give each action its own Regular lens, with its glyph inside contentView.
// The panel overlay still avoids the card's top-left ancestor clipping.
@interface FishTaskActionButton : FishTaskButton
@property(nonatomic,strong) NSView *glass;
@property(nonatomic,strong) NSImageView *symbolView;
@end
@implementation FishTaskActionButton
- (instancetype)initWithFrame:(NSRect)frame {
    if (!(self=[super initWithFrame:frame])) return nil;
    self.title=@"";
    self.controlSize=NSControlSizeSmall;
    if (@available(macOS 26.0, *)) {
        self.bordered=NO;
        self.transparent=YES;
        self.imagePosition=NSNoImage;
        self.symbolView=[[NSImageView alloc] initWithFrame:self.bounds];
        self.symbolView.imageAlignment=NSImageAlignCenter;
        self.symbolView.imageScaling=NSImageScaleNone;
        self.symbolView.contentTintColor=NSColor.labelColor;
        self.symbolView.accessibilityElement=NO;
        NSGlassEffectView *glass=[[NSGlassEffectView alloc] initWithFrame:self.bounds];
        glass.style=NSGlassEffectViewStyleRegular;
        glass.contentView=self.symbolView;
        if (@available(macOS 27.0, *)) glass.effectIsInteractive=YES;
        self.glass=glass;
        [self addSubview:glass];
    } else {
        self.bordered=YES;
        self.bezelStyle=NSBezelStyleCircular;
        self.imagePosition=NSImageOnly;
    }
    return self;
}
- (void)setImage:(NSImage *)image {
    [super setImage:image];
    // NSButton may turn image drawing back on when its image changes.
    if(self.glass)self.imagePosition=NSNoImage;
    self.symbolView.image=image;
}
- (void)setContentTintColor:(NSColor *)color {
    [super setContentTintColor:color];
    self.symbolView.contentTintColor=color;
}
- (void)highlight:(BOOL)flag {
    [super highlight:flag];
    self.symbolView.alphaValue=flag ? .6 : 1;
}
- (void)viewDidMoveToWindow {
    [super viewDidMoveToWindow];self.needsLayout=YES;
}
- (void)viewDidChangeBackingProperties {
    [super viewDidChangeBackingProperties];self.needsLayout=YES;
}
- (void)layout {
    [super layout];
    if (@available(macOS 26.0, *)) {
        // Tiny circular lenses need a full backing-pixel raster gutter. The
        // half-pixel alignment used by large capsules weakens these side arcs.
        CGFloat edge=1/MAX(1,self.window.backingScaleFactor);
        self.glass.frame=NSInsetRect(self.bounds,edge,edge);
        ((NSGlassEffectView *)self.glass).cornerRadius=NSWidth(self.glass.bounds)/2;
        self.symbolView.frame=self.glass.bounds;
    }
}
@end

@interface FishTaskActionOverlay : NSView
@end
@implementation FishTaskActionOverlay
- (NSView *)hitTest:(NSPoint)point {
    if(self.hidden)return nil;
    NSPoint local=[self convertPoint:point fromView:self.superview];
    for(NSButton *button in self.subviews.reverseObjectEnumerator)
        if(!button.hidden && button.enabled && NSPointInRect(local,button.frame))return button;
    return nil;
}
@end

@interface FishTaskRow : NSView
@property(nonatomic, strong) NSVisualEffectView *backdrop;
@property(nonatomic, strong) NSView *glass;
@property(nonatomic, strong) NSView *content;
@property(nonatomic) BOOL liquidGlass;
@property(nonatomic, strong) NSTextField *titleLabel;
@property(nonatomic, strong) NSTextField *messageLabel;
@property(nonatomic, strong) NSButton *button;
@property(nonatomic, strong) NSButton *dismissButton;
@property(nonatomic, strong) NSButton *hideSessionButton;
@property(nonatomic, copy) NSString *sessionID;
@property(nonatomic, copy) NSString *turnID;
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
    self.dismissButton=[[FishTaskActionButton alloc] initWithFrame:NSMakeRect(0,TaskCardHeight-13,20,20)];
    self.dismissButton.image=[[NSImage imageWithSystemSymbolName:@"xmark" accessibilityDescription:nil]
        imageWithSymbolConfiguration:[NSImageSymbolConfiguration configurationWithPointSize:10 weight:NSFontWeightBold]];
    self.dismissButton.contentTintColor=NSColor.labelColor;
    self.dismissButton.wantsLayer=YES;
    self.dismissButton.target=self;
    self.dismissButton.action=@selector(dismissNotification:);
    self.dismissButton.accessibilityLabel=@"关闭本轮通知";
    self.dismissButton.toolTip=@"关闭本轮通知，任务继续运行；点击电饭锅可重新显示";
    self.hideSessionButton=[[FishTaskActionButton alloc] initWithFrame:NSMakeRect(28,TaskCardHeight-13,20,20)];
    self.hideSessionButton.image=[[NSImage imageWithSystemSymbolName:@"eye.slash" accessibilityDescription:nil]
        imageWithSymbolConfiguration:[NSImageSymbolConfiguration configurationWithPointSize:11 weight:NSFontWeightSemibold]];
    self.hideSessionButton.contentTintColor=NSColor.labelColor;
    self.hideSessionButton.wantsLayer=YES;
    self.hideSessionButton.target=self;
    self.hideSessionButton.action=@selector(hideSession:);
    self.hideSessionButton.accessibilityLabel=@"永久隐藏此聊天的通知";
    self.hideSessionButton.toolTip=@"永久隐藏此聊天的通知，新轮次和重启后也不再显示；任务继续运行";
    return self;
}
- (void)dismissNotification:(id)sender { DismissTaskNotification(self.sessionID,self.turnID); }
- (void)hideSession:(id)sender { PermanentlyHideTaskNotification(self.sessionID); }
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
    title.size.width = message.size.width = self.bounds.size.width-32;
    self.titleLabel.frame = title;
    self.messageLabel.frame = message;
    self.button.frame = self.bounds;
}
- (void)updateTask:(NSDictionary *)task {
    self.sessionID = task[@"session_id"];
    self.turnID = task[@"turn_id"] ?: @"";
    self.titleLabel.stringValue = task[@"title"] ?: @"未命名任务";
    self.messageLabel.stringValue = task[@"summary"] ?: @"正在思考";
    self.button.accessibilityLabel = self.titleLabel.stringValue;
    self.button.accessibilityValue = self.messageLabel.stringValue;
    NSString *message = [task[@"message"] length] ? task[@"message"] : self.messageLabel.stringValue;
    self.button.toolTip = [NSString stringWithFormat:@"%@\n%@", self.titleLabel.stringValue, message];
    self.button.enabled = [[NSUUID alloc] initWithUUIDString:self.sessionID] != nil;
    self.dismissButton.enabled=self.button.enabled;
    self.dismissButton.hidden=!self.button.enabled;
    self.dismissButton.accessibilityHelp=[NSString stringWithFormat:@"关闭%@的本轮通知，不停止任务",self.titleLabel.stringValue];
    self.hideSessionButton.enabled=self.button.enabled;
    self.hideSessionButton.hidden=!self.button.enabled;
    self.hideSessionButton.accessibilityHelp=[NSString stringWithFormat:@"永久隐藏%@的通知，不停止任务",self.titleLabel.stringValue];
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
@property(nonatomic, strong) NSArray<NSDictionary *> *items;
@property(nonatomic, strong) FishTaskRow *incoming;
@property(nonatomic, strong) NSTimer *insertionTimer;
@property(nonatomic) BOOL insertionActive,oldFrontVisible;
@property(nonatomic) CFTimeInterval insertionStart;
@property(nonatomic) NSUInteger insertionGeneration;
@property(nonatomic, strong) NSTimer *shakeTimer;
@property(nonatomic) BOOL shakeActive,pendingShake;
@property(nonatomic) CFTimeInterval shakeStart;
@property(nonatomic) NSUInteger shakeGeneration;
@property(nonatomic) CGFloat shakeOffset;
- (void)updateTasks:(NSArray<NSDictionary *> *)tasks;
- (void)insertTasks:(NSArray<NSDictionary *> *)tasks animated:(BOOL)animated;
- (void)cancelInsertion;
- (void)advanceInsertion:(NSTimeInterval)elapsed;
- (void)shake;
- (void)cancelShake;
- (void)advanceShake:(NSTimeInterval)elapsed;
@end
@implementation FishTaskStack
- (instancetype)initWithFrame:(NSRect)frame {
    if (!(self = [super initWithFrame:frame])) return nil;
    NSMutableArray *backs = [NSMutableArray new];
    // Farthest card first: the two native-glass edges peek below the front.
    for (NSUInteger i=0; i<2; i++) {
        FishTaskRow *row = [[FishTaskRow alloc] initWithFrame:NSMakeRect(0,0,TaskCardWidth,TaskCardHeight)];
        row.button.hidden = YES;
        row.titleLabel.hidden=YES;row.messageLabel.hidden=YES;
        row.dismissButton.hidden=YES;
        row.hideSessionButton.hidden=YES;
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
- (void)expandTasks:(id)sender {
    if(self.count==1)[self.front openTask:sender];
    else if (!tasksExpanded) ToggleTasks();
}
- (void)updateTasks:(NSArray<NSDictionary *> *)tasks {
    if(self.insertionActive && ![self.incoming.sessionID isEqualToString:tasks.firstObject[@"session_id"]])[self cancelInsertion];
    if((self.shakeActive || self.pendingShake) && ![self.items.firstObject[@"session_id"] isEqualToString:tasks.firstObject[@"session_id"]])[self cancelShake];
    self.items=[tasks copy];
    self.count = tasks.count;
    if(self.insertionActive)[self.incoming updateTask:tasks.firstObject ?: @{}];
    else [self.front updateTask:tasks.firstObject ?: @{}];
    for(NSUInteger i=0;i<self.backs.count;i++) {
        NSUInteger depth=2-i;
        [self.backs[i] updateTask:depth<tasks.count ? tasks[depth] : @{}];
        self.backs[i].dismissButton.hidden=YES;
        self.backs[i].hideSessionButton.hidden=YES;
    }
    self.front.dismissButton.hidden=self.insertionActive || !self.front.dismissButton.enabled;
    self.front.hideSessionButton.hidden=self.insertionActive || !self.front.hideSessionButton.enabled;
    self.incoming.dismissButton.hidden=YES;
    self.incoming.hideSessionButton.hidden=YES;
    self.button.accessibilityLabel = self.count==1 ? @"在 Codex 中打开任务，共 1 项" : [NSString stringWithFormat:@"展开任务列表，共 %lu 项",(unsigned long)self.count];
    FishTaskRow *top=self.insertionActive ? self.incoming : self.front;
    self.button.accessibilityValue = [NSString stringWithFormat:@"%@，%@",top.titleLabel.stringValue,top.messageLabel.stringValue];
    self.button.toolTip = self.count==1 ? [NSString stringWithFormat:@"点击在 Codex 中打开\n%@",top.button.toolTip] : [NSString stringWithFormat:@"点击展开 %lu 项任务\n%@",(unsigned long)self.count,top.button.toolTip];
    self.needsLayout = YES;
}
- (void)cancelInsertion {
    if(!self.insertionActive && !self.insertionTimer && !self.incoming)return;
    self.insertionGeneration++;[self.insertionTimer invalidate];self.insertionTimer=nil;
    [self.incoming removeFromSuperview];self.incoming=nil;self.insertionActive=NO;
    self.front.alphaValue=1;self.front.content.alphaValue=1;self.front.hidden=NO;self.button.enabled=YES;
    [self updateTasks:self.items ?: @[]];
}
- (void)insertTasks:(NSArray<NSDictionary *> *)tasks animated:(BOOL)animated {
    [self cancelShake];
    [self cancelInsertion];
    if(!animated || !tasks.count) { [self updateTasks:tasks];return; }
    self.oldFrontVisible=self.window.visible && !self.hidden && self.count>0;
    self.incoming=[[FishTaskRow alloc] initWithFrame:self.front.frame];
    [self.incoming updateTask:tasks.firstObject];
    self.incoming.button.hidden=YES;self.incoming.accessibilityHidden=YES;
    self.incoming.dismissButton.hidden=YES;
    self.incoming.hideSessionButton.hidden=YES;
    self.insertionActive=YES;self.button.enabled=NO;
    [self addSubview:self.incoming positioned:NSWindowAbove relativeTo:self.front];
    [self updateTasks:tasks];
    self.front.alphaValue=self.oldFrontVisible ? 1 : 0;
    self.insertionStart=CACurrentMediaTime();NSUInteger generation=++self.insertionGeneration;
    __weak FishTaskStack *weakSelf=self;
    self.insertionTimer=[NSTimer timerWithTimeInterval:1.0/60 repeats:YES block:^(NSTimer *timer) {
        FishTaskStack *stack=weakSelf;
        if(stack && stack.insertionGeneration==generation)[stack advanceInsertion:CACurrentMediaTime()-stack.insertionStart];
    }];self.insertionTimer.tolerance=.002;
    [NSRunLoop.mainRunLoop addTimer:self.insertionTimer forMode:NSRunLoopCommonModes];
}
- (void)advanceInsertion:(NSTimeInterval)elapsed {
    if(!self.insertionActive)return;
    CGFloat t=MIN(1,MAX(0,elapsed/.36)),ease=1-pow(1-t,3);
    NSUInteger layers=MIN(MAX(self.count,1),3);
    CGFloat y=self.expandsUpward ? 8 : 8+(layers-1)*6;
    NSRect front=NSMakeRect(10,y,TaskCardWidth,TaskCardHeight);
    CGFloat drop=MIN(34,MAX(0,NSHeight(self.bounds)-NSMaxY(front)-4));
    self.incoming.frame=NSMakeRect(10+10*(1-ease),y+drop*(1-ease),TaskCardWidth-10*(1-ease),TaskCardHeight);
    self.incoming.alphaValue=.22+.78*MIN(1,t/.55);
    self.front.frame=NSMakeRect(10+8*ease,y+(self.expandsUpward ? 6 : -6)*ease,TaskCardWidth-16*ease,TaskCardHeight);
    self.front.content.alphaValue=MAX(0,1-t/.22);
    self.incoming.needsLayout=YES;self.front.needsLayout=YES;
    [self.incoming layoutSubtreeIfNeeded];[self.front layoutSubtreeIfNeeded];
    if(t>=1 && !layingOutTasks) {
        // Keep the lens already on screen. Removing it and repainting the old
        // receded lens can expose stale native-glass clipping during resize.
        BOOL shake=self.pendingShake;self.pendingShake=NO;
        [self.front removeFromSuperview];self.front=self.incoming;self.incoming=nil;
        [self cancelInsertion];LayoutTasks();UpdateTaskAccessibility();
        if(shake)[self shake];
    }
}
- (void)cancelShake {
    self.pendingShake=NO;
    if(!self.shakeActive && !self.shakeTimer)return;
    self.shakeGeneration++;[self.shakeTimer invalidate];self.shakeTimer=nil;self.shakeActive=NO;
    self.front.frame=NSOffsetRect(self.front.frame,-self.shakeOffset,0);self.shakeOffset=0;
    self.needsLayout=YES;
}
- (void)shake {
    if(self.insertionActive || taskAnimationActive) { self.pendingShake=YES;return; }
    [self cancelShake];
    if(!self.window.visible || self.hidden || !self.count || NSWorkspace.sharedWorkspace.accessibilityDisplayShouldReduceMotion)return;
    self.shakeActive=YES;self.shakeStart=CACurrentMediaTime();NSUInteger generation=++self.shakeGeneration;
    __weak FishTaskStack *weakSelf=self;
    self.shakeTimer=[NSTimer timerWithTimeInterval:1.0/60 repeats:YES block:^(NSTimer *timer) {
        FishTaskStack *stack=weakSelf;
        if(stack && stack.shakeGeneration==generation)[stack advanceShake:CACurrentMediaTime()-stack.shakeStart];
    }];self.shakeTimer.tolerance=.002;
    [NSRunLoop.mainRunLoop addTimer:self.shakeTimer forMode:NSRunLoopCommonModes];
}
- (void)advanceShake:(NSTimeInterval)elapsed {
    if(!self.shakeActive)return;
    CGFloat t=MIN(1,MAX(0,elapsed/.28));
    if(t>=1)[self cancelShake];
    else self.shakeOffset=4*sin(6*M_PI*t)*(1-t);
    self.needsLayout=YES;[self layoutSubtreeIfNeeded];
    LayoutTaskActions();
}
- (void)layout {
    [super layout];
    NSUInteger layers = MIN(MAX(self.count,1),3);
    CGFloat frontY = self.expandsUpward ? 8 : 8 + (layers-1)*6;
    for (NSUInteger i=0; i<self.backs.count; i++) {
        NSUInteger depth = 2-i;
        FishTaskRow *back = self.backs[i];
        back.hidden = depth >= layers || (self.insertionActive && self.oldFrontVisible && depth==1);
        back.frame = NSMakeRect(10+depth*8,frontY+(self.expandsUpward ? 1.0 : -1.0)*depth*6,TaskCardWidth-depth*16,TaskCardHeight);
    }
    if(self.insertionActive)[self advanceInsertion:CACurrentMediaTime()-self.insertionStart];
    else self.front.frame = NSMakeRect(10+self.shakeOffset,frontY,TaskCardWidth,TaskCardHeight);
    self.button.frame = NSMakeRect(0,0,NSWidth(self.bounds),TaskCardHeight+16+(layers-1)*6);
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
    // A single task has one card, not a second expanded representation.
    if(tasksExpanded && taskData.count==1) { SetTaskMode(FishTaskFolded,YES);return; }
    CGFloat distanceFromAnchor = MAX(0, taskRowsExpandUpward ? NSMinY(taskScroll.contentView.bounds) : taskDocument.bounds.size.height-NSMaxY(taskScroll.contentView.bounds));
    NSArray *items = DisplayedTaskItems();
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
    NSArray *stackItems=StackTaskItems();
    if(stackInsertionPending && !tasksExpanded && !preparingTaskAnimation) {
        BOOL animate=wantsVisible && petWindow.visible && statusPanel.visible && taskCardsShown && !NSWorkspace.sharedWorkspace.accessibilityDisplayShouldReduceMotion;
        [taskStack insertTasks:stackItems animated:animate];stackInsertionPending=nil;
    } else [taskStack updateTasks:stackItems];
    taskRowsExpandUpward = tasksExpandUpward;
    if (!layingOut) LayoutStatus();
    LayoutTasks();
    CGFloat maxOffset = MAX(0,height-taskScroll.contentView.bounds.size.height);
    CGFloat offset = MIN(distanceFromAnchor,maxOffset);
    [taskScroll.contentView scrollToPoint:NSMakePoint(0,tasksExpandUpward ? offset : maxOffset-offset)];
    [taskScroll reflectScrolledClipView:taskScroll.contentView];
    LayoutTasks();
}
static CGFloat TaskPanelHeight(BOOL expanded) {
    // Keep the native-glass window at its settled size throughout placing.
    // Changing its backing canvas by40pt and shrinking it under a live lens
    // can leave a stale compositor clip even when AppKit view bounds agree.
    // advanceInsertion uses only the normal stack's available top padding.
    return expanded ? MIN(TaskListMaxHeight,MAX(TaskRowStep+12,DisplayedTaskItems().count*TaskRowStep+12)) : TaskCardHeight+16+(MIN(MAX(taskData.count,1),3)-1)*6;
}
static NSRect NaturalStatusRect(void) {
    NSView *content=petWindow.contentView;
    CGFloat textWidth=overlay.caption.cell.cellSize.width;
    CGFloat width=MIN(MAX(140,2*ceil((textWidth+60)/2)),MAX(140,2*floor((content.bounds.size.width-24)/2)));
    NSRect rect=[content convertRect:NSMakeRect(floor((content.bounds.size.width-width)/2)-12,24,width+24,60) toView:nil];
    rect=[petWindow convertRectToScreen:rect];rect.origin.y-=petTaskOffset;
    return rect;
}
static NSRect DockedStatusRect(void) { return NSOffsetRect(NaturalStatusRect(),statusDockOffset.x,statusDockOffset.y); }
static NSRect PaintedStatusRect(NSRect canvas) { return NSMakeRect(NSMinX(canvas)+12,NSMinY(canvas)+5,NSWidth(canvas)-24,40); }
static NSRect PetEdgeBounds(NSWindow *window) {
    // The dragged control is the anchor. The wider cards are allowed to slide
    // independently inside their screen; they must not recenter this control.
    return window==petWindow && statusPanel && overlay && wantsVisible ? PaintedStatusRect(DockedStatusRect()) : window.frame;
}
static NSPoint EdgeCorrection(NSRect bounds,NSRect visible) {
    NSRect safe=NSInsetRect(visible,PetEdgeMargin,PetEdgeMargin);NSPoint delta=NSZeroPoint;
    // An unusually small desktop cannot contain an oversized group. Keep the
    // status reachable at the bottom and center horizontally, without scaling.
    if (NSWidth(bounds)>NSWidth(safe)) delta.x=NSMidX(safe)-NSMidX(bounds);
    else if (NSMinX(bounds)<NSMinX(safe)) delta.x=NSMinX(safe)-NSMinX(bounds);
    else if (NSMaxX(bounds)>NSMaxX(safe)) delta.x=NSMaxX(safe)-NSMaxX(bounds);
    if (NSHeight(bounds)>NSHeight(safe)) delta.y=NSMinY(safe)-NSMinY(bounds);
    else if (NSMinY(bounds)<NSMinY(safe)) delta.y=NSMinY(safe)-NSMinY(bounds);
    else if (NSMaxY(bounds)>NSMaxY(safe)) delta.y=NSMaxY(safe)-NSMaxY(bounds);
    return delta;
}
static NSScreen *EdgeScreen(NSWindow *window,NSRect bounds) {
    NSScreen *best=window.screen ?: NSScreen.mainScreen;CGFloat bestArea=-1,bestDistance=CGFLOAT_MAX;
    NSPoint middle=NSMakePoint(NSMidX(bounds),NSMidY(bounds));
    for (NSScreen *screen in StatusDragScreens()) {
        NSRect overlap=NSIntersectionRect(bounds,screen.frame);CGFloat area=NSWidth(overlap)*NSHeight(overlap);
        CGFloat x=MIN(MAX(middle.x,NSMinX(screen.frame)),NSMaxX(screen.frame)),y=MIN(MAX(middle.y,NSMinY(screen.frame)),NSMaxY(screen.frame));
        CGFloat distance=(middle.x-x)*(middle.x-x)+(middle.y-y)*(middle.y-y);
        if (area>bestArea || (area==bestArea && distance<bestDistance)) { best=screen;bestArea=area;bestDistance=distance; }
    }
    return best;
}
static NSScreen *ScreenAtDragPoint(NSPoint point) {
    NSScreen *best=StatusDragScreens().firstObject;CGFloat nearest=CGFLOAT_MAX;
    for (NSScreen *screen in StatusDragScreens()) {
        if (NSPointInRect(point,screen.frame)) return screen;
        CGFloat x=MIN(MAX(point.x,NSMinX(screen.frame)),NSMaxX(screen.frame)),y=MIN(MAX(point.y,NSMinY(screen.frame)),NSMaxY(screen.frame));
        CGFloat d=(point.x-x)*(point.x-x)+(point.y-y)*(point.y-y);if(d<nearest){nearest=d;best=screen;}
    }
    return best;
}
static NSScreen *CurrentStatusScreen(void) {
    if (statusDragActive && statusDragScreen) return statusDragScreen;
    if (edgeReturnWindow && edgeReturnScreen) return edgeReturnScreen;
    return EdgeScreen(petWindow,petWindow && overlay ? PaintedStatusRect(DockedStatusRect()) : PetDockFrame());
}
static void BeginStatusDrag(NSPoint cursor) {
    CancelEdgeReturn();
    if (statusPanel && petWindow) {
        NSRect natural=NaturalStatusRect();
        statusDockOffset=NSMakePoint(NSMinX(statusPanel.frame)-NSMinX(natural),NSMinY(statusPanel.frame)-NSMinY(natural));
    }
    statusDragActive=YES;statusDragScreen=ScreenAtDragPoint(cursor);
}
static void MoveStatusDrag(NSWindow *window,NSPoint desired,NSPoint cursor) {
    statusDragScreen=ScreenAtDragPoint(cursor);
    if (StatusDragScreens().count==1) {
        NSRect status=NSOffsetRect(DockedStatusRect(),desired.x-window.frame.origin.x,desired.y-window.frame.origin.y);
        NSPoint correction=EdgeCorrection(PaintedStatusRect(status),StatusDragScreens().firstObject.visibleFrame);
        desired.x+=correction.x;desired.y+=correction.y;
    }
    // Solve before moving the parent: AppKit moves its child panels as part of
    // that operation. Moving past the limit then pulling a child back jitters.
    applyingStatusDrag=YES;[window setFrameOrigin:desired];LayoutStatus();applyingStatusDrag=NO;
}
static void CancelStatusDrag(void) { statusDragActive=NO;statusDragScreen=nil; }
static void CancelEdgeReturn(void) {
    edgeReturnGeneration++;[edgeReturnTimer invalidate];edgeReturnTimer=nil;edgeReturnWindow=nil;edgeReturnScreen=nil;
}
static void AdvanceEdgeReturn(CFTimeInterval elapsed) {
    NSWindow *window=edgeReturnWindow;
    if (!window || !window.visible) { CancelEdgeReturn();return; }
    CGFloat t=MIN(1,MAX(0,elapsed/.22)),eased=1-pow(1-t,3);
    NSPoint point=NSMakePoint(edgeReturnFrom.x+(edgeReturnTo.x-edgeReturnFrom.x)*eased,edgeReturnFrom.y+(edgeReturnTo.y-edgeReturnFrom.y)*eased);
    if (window==petWindow) point.y+=petTaskOffset;
    applyingEdgeReturn=YES;[window setFrameOrigin:point];LayoutStatus();
    if (t>=1) {
        // A task count/mode or display may change during these220ms. Refit the
        // current footprint once, rather than leaving its new top outside.
        NSRect bounds=PetEdgeBounds(window);NSScreen *screen=edgeReturnScreen ?: EdgeScreen(window,bounds);
        NSPoint correction=screen ? EdgeCorrection(bounds,screen.visibleFrame) : NSZeroPoint;
        if (!NSEqualPoints(correction,NSZeroPoint)) { [window setFrameOrigin:NSMakePoint(window.frame.origin.x+correction.x,window.frame.origin.y+correction.y)];LayoutStatus(); }
    }
    applyingEdgeReturn=NO;
    if (t>=1) CancelEdgeReturn();
}
static void FinishPetDragOnScreen(NSScreen *releaseScreen) {
    PreparePetDrag();LayoutStatus();
    NSWindow *window=petWindow ?: FindPetWindow();if (!window || !window.visible || window.miniaturized) return;
    NSRect bounds=PetEdgeBounds(window);NSScreen *screen=releaseScreen ?: EdgeScreen(window,bounds);if (!screen) return;
    NSPoint delta=EdgeCorrection(bounds,screen.visibleFrame);if (NSEqualPoints(delta,NSZeroPoint)) return;
    edgeReturnWindow=window;edgeReturnScreen=screen;edgeReturnFrom=window.frame.origin;
    if (window==petWindow) edgeReturnFrom.y-=petTaskOffset;
    edgeReturnTo=NSMakePoint(edgeReturnFrom.x+delta.x,edgeReturnFrom.y+delta.y);
    if (NSWorkspace.sharedWorkspace.accessibilityDisplayShouldReduceMotion) { AdvanceEdgeReturn(.22);return; }
    edgeReturnStart=CACurrentMediaTime();NSUInteger generation=edgeReturnGeneration;
    edgeReturnTimer=[NSTimer timerWithTimeInterval:1.0/60 repeats:YES block:^(NSTimer *timer) {
        if (generation==edgeReturnGeneration) AdvanceEdgeReturn(CACurrentMediaTime()-edgeReturnStart);
    }];edgeReturnTimer.tolerance=.002;
    [NSRunLoop.mainRunLoop addTimer:edgeReturnTimer forMode:NSRunLoopCommonModes];
}
static void FinishPetDrag(void) { FinishPetDragOnScreen(nil); }
static void EndStatusDrag(BOOL moved,NSPoint cursor) {
    statusDragScreen=ScreenAtDragPoint(cursor);
    if (moved) FinishPetDragOnScreen(statusDragScreen);
    CancelStatusDrag();LayoutStatus();
}
static NSRect TaskPanelRect(BOOL expanded) {
    NSRect screen = CurrentStatusScreen().visibleFrame;
    CGFloat height = TaskPanelHeight(expanded);
    NSRect pill = [statusPanel convertRectToScreen:[overlay convertRect:overlay.pill.frame toView:nil]];
    // Grow toward the desktop interior without moving or resizing the avatar.
    CGFloat available = tasksExpandUpward ? NSMaxY(screen)-8-NSMaxY(pill) : NSMinY(pill)-NSMinY(screen)-8;
    height = MIN(height,MAX(1,available));
    CGFloat x = MIN(MAX(NSMidX(pill)-TaskPanelWidth/2, NSMinX(screen)+8), NSMaxX(screen)-TaskPanelWidth-8);
    CGFloat y = tasksExpandUpward ? NSMaxY(pill) : NSMinY(pill)-height;
    return NSMakeRect(x,y,TaskPanelWidth,height);
}
static void LayoutTaskActions(void) {
    if(!taskActionOverlay || !taskPanel)return;
    taskActionOverlay.frame=taskPanel.contentView.bounds;
    BOOL active=wantsVisible && TaskCardsPresent() && !taskAnimationActive && !preparingTaskAnimation && !taskStack.insertionActive;
    NSArray<FishTaskRow *> *rows=tasksExpanded ? taskRows.allValues : (taskStack.front ? @[taskStack.front] : @[]);
    NSMutableSet *keep=[NSMutableSet new];
    NSRect viewport=tasksExpanded ? [taskActionOverlay convertRect:taskScroll.contentView.bounds fromView:taskScroll.contentView] : taskActionOverlay.bounds;
    if(active)for(FishTaskRow *row in rows) {
        if(row.hidden || !row.button.enabled || !TaskWithID(row.sessionID))continue;
        NSRect close=[taskActionOverlay convertRect:NSMakeRect(0,TaskCardHeight-13,20,20) fromView:row];
        NSRect hide=NSOffsetRect(close,28,0);
        // Hide the whole group when scrolling carries an action out of view;
        // never leave a partially cut circular button at a viewport edge.
        if(!NSContainsRect(viewport,close) || !NSContainsRect(viewport,hide) ||
           !NSContainsRect(taskActionOverlay.bounds,close) || !NSContainsRect(taskActionOverlay.bounds,hide))continue;
        NSArray *controls=@[row.dismissButton,row.hideSessionButton];
        for(NSUInteger i=0;i<controls.count;i++) {
            NSButton *control=controls[i];
            if(control.superview!=taskActionOverlay)[taskActionOverlay addSubview:control];
            control.frame=i==0 ? close : hide;control.hidden=NO;[keep addObject:control];
        }
    }
    for(NSButton *control in [taskActionOverlay.subviews copy])if(![keep containsObject:control]) {
        control.hidden=YES;[control removeFromSuperview];
    }
    taskActionOverlay.hidden=keep.count==0;
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
    BOOL resized=!NSEqualSizes(taskPanel.frame.size,frame.size);
    // The placing canvas has extra transparent headroom. Never display its new
    // size while the stack/glass hierarchy still has the old canvas bounds.
    [CATransaction begin];[CATransaction setDisableActions:YES];
    [taskPanel setFrame:frame display:NO];
    taskScroll.hidden = !tasksExpanded;
    taskStack.hidden = tasksExpanded;
    // Keep the hidden scroll viewport intact so collapsing/live updates do not
    // discard the user's distance from the top of the expanded list.
    if (tasksExpanded) taskScroll.frame = taskPanel.contentView.bounds;
    else { taskStack.expandsUpward=tasksExpandUpward; taskStack.frame = taskPanel.contentView.bounds; taskStack.needsLayout=YES; [taskStack layoutSubtreeIfNeeded]; }
    [taskPanel.contentView layoutSubtreeIfNeeded];
    LayoutTaskActions();
    if(resized)taskPanel.contentView.needsDisplay=YES;
    [CATransaction commit];
    [taskPanel displayIfNeeded];
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
    if (taskPanel.visible && !taskAnimationActive) {
        [children addObject:tasksExpanded ? (id)taskScroll : taskStack.button];
        if(!taskActionOverlay.hidden)for(NSButton *control in taskActionOverlay.subviews)
            if(!control.hidden && control.enabled)[children addObject:control];
    }
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
        taskActionOverlay=[[FishTaskActionOverlay alloc] initWithFrame:taskPanel.contentView.bounds];
        taskActionOverlay.wantsLayer=YES;taskActionOverlay.clipsToBounds=YES;
        [taskPanel.contentView addSubview:taskActionOverlay];
        taskScroll.contentView.postsBoundsChangedNotifications=YES;
        taskActionScrollObserver=[NSNotificationCenter.defaultCenter addObserverForName:NSViewBoundsDidChangeNotification object:taskScroll.contentView queue:NSOperationQueue.mainQueue usingBlock:^(NSNotification *note) {
            LayoutTaskActions();UpdateTaskAccessibility();
        }];
    }
}
static void CancelTaskAnimation(void) {
    [taskStack cancelShake];
    [taskStack cancelInsertion];
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
        BOOL shake=taskStack.pendingShake;
        CancelTaskAnimation();
        if (refresh) UpdateTaskRows(); else LayoutTasks();
        if(shake && !tasksExpanded && TaskCardsPresent())[taskStack shake];
    }
}
static void SetTaskMode(FishTaskMode mode, BOOL animated) {
    if(mode==FishTaskExpanded && taskData.count==1)mode=FishTaskFolded;
    EnsureTaskPanel();
    BOOL expanded=mode==FishTaskExpanded,shown=mode!=FishTaskHidden;
    if (tasksExpanded==expanded && taskCardsShown==shown) return;
    BOOL wasExpanded=tasksExpanded,wasShown=TaskCardsPresent(),interrupted=taskAnimationActive,wasVisible=taskPanel.visible;
    BOOL listTransition=wasExpanded || expanded || (interrupted && taskAnimationListTransition);
    NSRect oldCanvas=taskPanel.frame;
    CGFloat oldStackAlpha=taskStack.alphaValue;
    NSMutableDictionary *previous=[NSMutableDictionary new];
    if(wasExpanded && !interrupted)for(FishTaskRow *row in taskRows.allValues)previous[row.sessionID ?: @""]=@{
        @"frame":[NSValue valueWithRect:[taskPanel.contentView convertRect:row.bounds fromView:row]],@"alpha":@1};
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
    NSArray *items=expanded ? DisplayedTaskItems() : StackTaskItems();
    NSMutableArray *entries=[NSMutableArray new];
    // Farther cards are behind the front card as they leave or rejoin the stack.
    for (NSInteger i=(NSInteger)items.count-1;i>=0;i--) {
        NSDictionary *item=items[i];
        FishTaskRow *source=taskRows[item[@"session_id"] ?: @""];
        NSRect target=[taskPanel.contentView convertRect:source.bounds fromView:source];
        BOOL visible=NSIntersectsRect(target,taskAnimationHost.bounds);
        if (i!=0 && (!listTransition || !visible)) continue;
        NSUInteger stackIndex=[stackOrder indexOfObject:item[@"session_id"]];
        if(stackIndex==NSNotFound)stackIndex=i;
        CGFloat depth=MIN(stackIndex,2);
        NSRect folded=NSMakeRect(front.origin.x+depth*8,front.origin.y+(tasksExpandUpward ? 1.0 : -1.0)*depth*6,TaskCardWidth-depth*16,TaskCardHeight);
        FishTaskAnimationEntry *entry=[FishTaskAnimationEntry new];
        entry.row=[[FishTaskRow alloc] initWithFrame:NSMakeRect(0,0,TaskCardWidth,TaskCardHeight)];
        [entry.row updateTask:item]; entry.row.button.hidden=YES; entry.row.accessibilityHidden=YES;
        entry.row.dismissButton.hidden=YES;
        entry.row.hideSessionButton.hidden=YES;
        NSDictionary *prior=previous[item[@"session_id"] ?: @""];
        entry.fromFrame=prior ? NSOffsetRect([prior[@"frame"] rectValue],oldCanvas.origin.x-canvas.origin.x,oldCanvas.origin.y-canvas.origin.y) : (wasExpanded ? target : folded);
        entry.toFrame=expanded ? target : folded;
        entry.fromAlpha=prior ? [prior[@"alpha"] doubleValue] : (wasExpanded ? visible : (stackIndex==0 && wasShown && wasVisible ? 1 : 0));
        entry.toAlpha=!targetPresent ? 0 : (expanded ? visible : (stackIndex==0 ? 1 : 0));
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
    CancelEdgeReturn();
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
    NSRect screen = CurrentStatusScreen().visibleFrame;
    CGFloat offset=DesiredPetTaskOffset();
    if (!taskAnimationActive && !preparingTaskAnimation) ApplyPetTaskOffset(offset);
    NSRect rect=DockedStatusRect();CGFloat capsuleWidth=NSWidth(rect)-24;
    // Never fight a held drag or return animation with a second clamp. Keep
    // the legacy idle guard for external/window recreation placement only.
    if (!statusDragActive && !edgeReturnWindow) {
        rect.origin.x = MIN(MAX(rect.origin.x,NSMinX(screen)+8-12),NSMaxX(screen)-8-capsuleWidth-12);
        rect.origin.y = MIN(MAX(rect.origin.y,NSMinY(screen)+8-5),NSMaxY(screen)-8-45);
    }
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
    UpdateTaskAccessibility();
    layingOut = NO;
}
static void HandleTaskOutsideClick(NSEvent *event) {
        if (event.window==petWindow || event.window==statusPanel) CancelEdgeReturn();
        NSPoint point = event.window ? [event.window convertPointToScreen:event.locationInWindow] : NSEvent.mouseLocation;
        if (tasksExpanded && !IsInsideStatus(point) && !NSPointInRect(point,taskPanel.frame)) {
            SetTasksExpanded(NO,YES);
        }
}
static void InstallObservers(void) {
    observers = [NSMutableArray new];
    NSNotificationCenter *center = NSNotificationCenter.defaultCenter;
    void (^outsideClick)(NSEvent *) = ^(NSEvent *event) { HandleTaskOutsideClick(event); };
    outsideClickMonitor = [NSEvent addGlobalMonitorForEventsMatchingMask:NSEventMaskLeftMouseDown handler:outsideClick];
    localClickMonitor = [NSEvent addLocalMonitorForEventsMatchingMask:NSEventMaskLeftMouseDown|NSEventMaskLeftMouseDragged|NSEventMaskLeftMouseUp handler:^NSEvent *(NSEvent *event) {
        if(event.type==NSEventTypeLeftMouseDown)outsideClick(event);
        CaptureAvatarPointer(event);return event;
    }];
    for (NSNotificationName name in @[NSWindowDidMoveNotification, NSWindowDidResizeNotification,
        NSWindowDidChangeScreenNotification, NSWindowDidMiniaturizeNotification, NSWindowDidDeminiaturizeNotification]) {
        [observers addObject:[center addObserverForName:name object:nil queue:NSOperationQueue.mainQueue usingBlock:^(NSNotification *note) {
            if (note.object == petWindow && !applyingPetOffset && !applyingEdgeReturn && !applyingStatusDrag) { PreparePetDrag(); LayoutStatus(); }
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
            RestoreAvatarFrameConstraint();petWindow = window;InstallAvatarFrameConstraint(window);
            petTaskOffset=0;statusDockOffset=NSZeroPoint;CancelStatusDrag();
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
            NSLog(@"Desktop Fish status drag frame constraint: %@ ownWindowOnly=%d classUnchanged=%d",NSStringFromClass(object_getClass(window)),freeFrameWindow==window,object_getClass(window)==freeFrameOriginalClass);
            NSLog(@"Desktop Fish window ordering: floating=%ld original=%ld; avatar/status/tasks below system panels",(long)window.level,(long)freeFrameOriginalLevel);
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
    OnMain(^{ wantsVisible = visible != 0; if (!wantsVisible) { CancelStatusDrag(); CancelEdgeReturn(); CancelTaskAnimation(); tasksExpanded=NO; stackInsertionPending=nil; } LayoutStatus(); });
}
static void ApplyVisibleTasks(BOOL refresh) {
    NSMutableArray *tasks=[NSMutableArray new];NSMutableSet *seen=[NSMutableSet new];
    for(NSDictionary *task in taskSnapshot) {
        NSString *sid=task[@"session_id"];
        if(SessionPermanentlyHidden(sid))continue;
        if([dismissedTaskTurns[sid] isEqualToString:task[@"turn_id"] ?: @""])continue;
        [tasks addObject:task];[seen addObject:sid];
    }
    if([taskData isEqualToArray:tasks])return;
    BOOL hadSnapshot=taskData!=nil;
    NSMutableArray *order=[NSMutableArray new];
    for(NSDictionary *task in tasks)if(![stackOrder containsObject:task[@"session_id"]])[order addObject:task[@"session_id"]];
    NSString *newTop=order.firstObject;
    for(NSString *sid in stackOrder)if([seen containsObject:sid])[order addObject:sid];
    stackOrder=order;selectedTaskID=order.firstObject;
    taskData=[tasks copy];if(taskData.count)EnsureTaskPanel();
    if(hadSnapshot && newTop && taskCardsShown && !tasksExpanded)stackInsertionPending=newTop;
    if(refresh)UpdateTaskRows();
}
static void DismissTaskNotification(NSString *sessionID,NSString *turnID) {
    NSDictionary *task=TaskWithID(sessionID);
    // A queued click on an older row must never mute the next turn.
    if(!task || ![(task[@"turn_id"] ?: @"") isEqualToString:turnID ?: @""])return;
    CancelTaskAnimation();stackInsertionPending=nil;
    if(!dismissedTaskTurns)dismissedTaskTurns=[NSMutableDictionary new];
    dismissedTaskTurns[sessionID]=turnID ?: @"";
    ApplyVisibleTasks(NO);
    if(!taskData.count)tasksExpanded=NO;
    UpdateTaskRows();
}
static void PermanentlyHideTaskNotification(NSString *sessionID) {
    if(!TaskWithID(sessionID) || SessionPermanentlyHidden(sessionID))return;
    NSArray *previous=[permanentlyHiddenSessions.allObjects sortedArrayUsingSelector:@selector(compare:)];
    [permanentlyHiddenSessions addObject:sessionID.lowercaseString];
    [notificationPreferences setObject:[permanentlyHiddenSessions.allObjects sortedArrayUsingSelector:@selector(compare:)] forKey:HiddenSessionDefaultsKey];
    if(![notificationPreferences synchronize]) {
        [permanentlyHiddenSessions removeObject:sessionID.lowercaseString];
        [notificationPreferences setObject:previous forKey:HiddenSessionDefaultsKey];
        NSLog(@"Desktop Fish permanent notification preference could not be saved");return;
    }
    CancelTaskAnimation();stackInsertionPending=nil;
    [dismissedTaskTurns removeObjectForKey:sessionID];
    ApplyVisibleTasks(NO);if(!taskData.count)tasksExpanded=NO;UpdateTaskRows();
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
        NSString *turn=[item[@"turn_id"] isKindOfClass:NSString.class] ? item[@"turn_id"] : @"";
        safe[@"turn_id"]=turn.length<=256 ? turn : @"";
        NSString *state=[item[@"state"] isKindOfClass:NSString.class] ? item[@"state"] : @"";
        safe[@"state"]=[@[@"working",@"waiting",@"complete"] containsObject:state] ? state : @"";
        for (NSString *field in @[@"title",@"summary",@"message"]) {
            NSString *value=[item[field] isKindOfClass:NSString.class] ? item[field] : @"";
            NSUInteger limit=[field isEqualToString:@"title"] ? 160 : 600;
            if (value.length>limit) value=[value substringWithRange:[value rangeOfComposedCharacterSequencesForRange:NSMakeRange(0,limit)]];
            safe[field]=value;
        }
        [tasks addObject:safe]; [seen addObject:sid];
        if (tasks.count>=64) break;
    }
    OnMain(^{ if (![taskSnapshot isEqualToArray:tasks]) {
        for(NSString *sid in [dismissedTaskTurns.allKeys copy]) {
            NSDictionary *current=nil,*previous=nil;
            for(NSDictionary *task in tasks)if([task[@"session_id"] isEqualToString:sid])current=task;
            for(NSDictionary *task in taskSnapshot)if([task[@"session_id"] isEqualToString:sid])previous=task;
            BOOL legacyRestart=![current[@"turn_id"] length] && [previous[@"state"] isEqualToString:@"complete"] && [current[@"state"] isEqualToString:@"working"];
            if(!current || ![dismissedTaskTurns[sid] isEqualToString:current[@"turn_id"]] || legacyRestart)
                [dismissedTaskTurns removeObjectForKey:sid];
        }
        taskSnapshot=[tasks copy];ApplyVisibleTasks(YES);
    } });
}
FISH_EXPORT void FishGlassStatusFinishDrag(void) { OnMain(^{ FinishPetDrag(); }); }
FISH_EXPORT void FishGlassStatusShowTasks(void) {
    OnMain(^{ stackInsertionPending=nil; UpdateTaskRows(); SetTasksExpanded(YES,YES); RefreshActiveAppearance(); });
}
FISH_EXPORT void FishGlassStatusShowTask(const char *sessionUTF8) {
    if(!sessionUTF8 || strlen(sessionUTF8)>64)return;
    NSString *sessionID=[NSString stringWithUTF8String:sessionUTF8];
    if(!sessionID || ![[NSUUID alloc] initWithUUIDString:sessionID])return;
    OnMain(^{
        if(SessionPermanentlyHidden(sessionID))return;
        BOOL wasShown=TaskCardsPresent();
        BOOL wasDismissed=dismissedTaskTurns[sessionID]!=nil;
        if(wasDismissed) {
            [dismissedTaskTurns removeObjectForKey:sessionID];ApplyVisibleTasks(NO);
        }
        if(!TaskWithID(sessionID))return;
        BOOL same=!wasDismissed && [stackOrder.firstObject isEqualToString:sessionID];
        PromoteStackTask(sessionID);
        if(tasksExpanded) { stackInsertionPending=nil;SetTasksExpanded(NO,YES); }
        else {
            if(!same || !taskCardsShown)stackInsertionPending=[sessionID copy];
            taskCardsShown=YES;UpdateTaskRows();
        }
        if(same && wasShown)[taskStack shake];
        RefreshActiveAppearance();
    });
}
// The Unity input backend can sample the current cursor after a queued click.
// Preserve the actual window event's position and order, including quick taps.
FISH_EXPORT int FishGlassStatusPollAvatarPointer(float *x,float *y,float *screenX,float *screenY) {
    if(!x || !y || !screenX || !screenY)return 0;
    @synchronized(FishStatusOverlay.class) {
        if(!avatarPointerCount)return 0;
        FishAvatarPointer event=avatarPointerQueue[avatarPointerHead];
        avatarPointerHead=(avatarPointerHead+1)%128;avatarPointerCount--;
        *x=event.x;*y=event.y;*screenX=event.screenX;*screenY=event.screenY;return event.phase;
    }
}
FISH_EXPORT void FishGlassStatusDestroy(void) {
    OnMain(^{
        CancelStatusDrag();CancelEdgeReturn();
        @synchronized(FishStatusOverlay.class) { avatarPointerHead=avatarPointerCount=0; }
        CancelTaskAnimation();
        wantsVisible=NO;
        ApplyPetTaskOffset(0);
        if (outsideClickMonitor) [NSEvent removeMonitor:outsideClickMonitor];
        if (localClickMonitor) [NSEvent removeMonitor:localClickMonitor];
        if (taskActionScrollObserver) [NSNotificationCenter.defaultCenter removeObserver:taskActionScrollObserver];
        taskActionScrollObserver=nil;
        outsideClickMonitor=nil; localClickMonitor=nil;
        [petWindow removeChildWindow:taskPanel]; [taskPanel orderOut:nil]; [taskPanel close];
        taskPanel=nil; taskScroll=nil; taskDocument=nil; taskListSurface=nil; taskRows=nil;taskActionOverlay=nil; taskStack=nil; taskData=nil;taskSnapshot=nil;dismissedTaskTurns=nil;permanentlyHiddenSessions=nil;notificationPreferences=nil; selectedTaskID=nil; stackOrder=nil;stackInsertionPending=nil;tasksExpanded=NO;
        taskRowsExpandUpward=YES;
        taskCardsShown=YES; petTaskOffset=0;statusDockOffset=NSZeroPoint;
        for (id token in observers) [NSNotificationCenter.defaultCenter removeObserver:token];
        observers = nil;
        [petWindow removeChildWindow:statusPanel];
        [statusPanel orderOut:nil];
        [statusPanel close];
        petWindow.accessibilityChildren=originalAccessibilityChildren;
        RestoreAvatarFrameConstraint();originalAccessibilityChildren=nil;
        overlay = nil; statusPanel = nil; petWindow = nil;
        wantsVisible = YES;
        atomic_store(&ready, 0);
    });
}
