#import <AppKit/AppKit.h>
#import <QuartzCore/QuartzCore.h>
#import <stdatomic.h>
#import <objc/message.h>

#define FISH_EXPORT __attribute__((visibility("default")))
static atomic_int ready = 0;

@interface FishStatusOverlay : NSView
@property(nonatomic, strong) NSView *pill;
@property(nonatomic, strong) NSVisualEffectView *backdrop;
@property(nonatomic, strong) NSView *glass;
@property(nonatomic, strong) NSView *content;
@property(nonatomic, strong) NSTextField *caption;
@property(nonatomic, strong) NSView *dot;
@property(nonatomic, copy) NSString *tone;
@property(nonatomic) BOOL liquidGlass;
- (void)updateCaption:(NSString *)caption tone:(NSString *)tone;
@end

@implementation FishStatusOverlay
- (BOOL)isFlipped { return NO; }
- (NSView *)hitTest:(NSPoint)point { return nil; }
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
    return self;
}
- (void)updateCaption:(NSString *)caption tone:(NSString *)tone {
    BOOL changed = ![tone isEqualToString:self.tone];
    self.caption.stringValue = [caption stringByReplacingOccurrencesOfString:@"\n" withString:@" "];
    self.caption.accessibilityValue = caption;
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
        if ([statusPanel respondsToSelector:force]) ((void(*)(id,SEL,BOOL))objc_msgSend)(statusPanel, force, YES);
        if ([statusPanel respondsToSelector:glass]) ((void(*)(id,SEL,BOOL))objc_msgSend)(statusPanel, glass, YES);
        if ([statusPanel respondsToSelector:refresh]) ((void(*)(id,SEL))objc_msgSend)(statusPanel, refresh);
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
    NSRect rect = [content convertRect:NSMakeRect(0, -16, content.bounds.size.width, 60) toView:nil];
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
    layingOut = NO;
}
static void InstallObservers(void) {
    observers = [NSMutableArray new];
    NSNotificationCenter *center = NSNotificationCenter.defaultCenter;
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
            statusPanel.ignoresMouseEvents = YES;
            statusPanel.releasedWhenClosed = NO;
            statusPanel.excludedFromWindowsMenu = YES;
            overlay = [[FishStatusOverlay alloc] initWithFrame:statusPanel.contentView.bounds];
            statusPanel.contentView = overlay;
            InstallObservers();
            created = YES;
        }
        if (petWindow != window) {
            [petWindow removeChildWindow:statusPanel];
            petWindow = window;
            statusPanel.collectionBehavior = window.collectionBehavior | NSWindowCollectionBehaviorFullScreenAuxiliary | NSWindowCollectionBehaviorIgnoresCycle;
        }
        [overlay updateCaption:caption tone:tone];
        wantsVisible = YES;
        LayoutStatus();
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
    OnMain(^{ wantsVisible = visible != 0; LayoutStatus(); });
}
FISH_EXPORT void FishGlassStatusDestroy(void) {
    OnMain(^{
        for (id token in observers) [NSNotificationCenter.defaultCenter removeObserver:token];
        observers = nil;
        [petWindow removeChildWindow:statusPanel];
        [statusPanel orderOut:nil];
        [statusPanel close];
        overlay = nil; statusPanel = nil; petWindow = nil;
        wantsVisible = YES;
        atomic_store(&ready, 0);
    });
}
