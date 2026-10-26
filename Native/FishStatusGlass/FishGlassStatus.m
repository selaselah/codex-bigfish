#import <AppKit/AppKit.h>
#import <QuartzCore/QuartzCore.h>
#import <stdatomic.h>

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
    self.backdrop.layer.cornerRadius = 12;
    self.backdrop.layer.masksToBounds = YES;
    [self.pill addSubview:self.backdrop];
    self.content = [[NSView alloc] initWithFrame:NSZeroRect];
    if (@available(macOS 26.0, *)) {
        NSGlassEffectView *glass = [[NSGlassEffectView alloc] initWithFrame:NSZeroRect];
        glass.style = NSGlassEffectViewStyleClear;
        glass.cornerRadius = 12;
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
    self.caption.font = [NSFont systemFontOfSize:11 weight:NSFontWeightMedium];
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
    CGFloat width = MIN(MAX(116, ceil(text) + 39), MAX(116, self.bounds.size.width - 24));
    self.pill.frame = NSMakeRect(floor((self.bounds.size.width-width)/2), 5, width, 24);
    self.backdrop.frame = self.pill.bounds;
    if (self.liquidGlass) self.glass.frame = self.pill.bounds;
    self.content.frame = self.pill.bounds;
    self.dot.frame = NSMakeRect(12, 9, 6, 6);
    self.caption.frame = NSMakeRect(25, 3, width - 37, 18);
    CGPathRef shadow = CGPathCreateWithRoundedRect(self.pill.bounds, 12, 12, NULL);
    self.pill.layer.shadowPath = shadow;
    CGPathRelease(shadow);
}
@end

static FishStatusOverlay *overlay;
static NSWindow *FindPetWindow(void) {
    for (NSWindow *window in NSApp.windows) {
        if ([window.title isEqualToString:@"BigFatBlueFish"] && window.contentView && window.visible) return window;
    }
    return nil;
}
static void OnMain(dispatch_block_t block) {
    if (NSThread.isMainThread) block();
    else dispatch_async(dispatch_get_main_queue(), block);
}
FISH_EXPORT int FishGlassStatusIsReady(void) { return atomic_load(&ready); }
FISH_EXPORT int FishGlassStatusUpdate(const char *captionUTF8, const char *toneUTF8) {
    NSString *caption = captionUTF8 ? [NSString stringWithUTF8String:captionUTF8] : @"";
    NSString *tone = toneUTF8 ? [NSString stringWithUTF8String:toneUTF8] : @"idle";
    OnMain(^{
        NSWindow *window = FindPetWindow();
        if (!window) { atomic_store(&ready, 0); return; }
        if (!overlay) {
            overlay = [[FishStatusOverlay alloc] initWithFrame:window.contentView.bounds];
            NSLog(@"Desktop Fish status: %@; capsule height=24 radius=12", overlay.liquidGlass ? @"native Liquid Glass" : @"native backdrop vibrancy");
        }
        if (overlay.superview != window.contentView) {
            [overlay removeFromSuperview];
            overlay.frame = window.contentView.bounds;
            [window.contentView addSubview:overlay positioned:NSWindowAbove relativeTo:nil];
        }
        [overlay updateCaption:caption tone:tone];
        overlay.hidden = NO;
        atomic_store(&ready, overlay.liquidGlass ? 2 : 1);
    });
    return atomic_load(&ready);
}
FISH_EXPORT void FishGlassStatusSetVisible(int visible) {
    OnMain(^{ overlay.hidden = !visible; });
}
FISH_EXPORT void FishGlassStatusDestroy(void) {
    OnMain(^{ [overlay removeFromSuperview]; overlay = nil; atomic_store(&ready, 0); });
}
