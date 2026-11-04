// Uses a unique preferences suite and example tasks only. Real user preferences,
// lifecycle snapshots, running chats and cooker state are never changed.
#define main OriginalTaskStackMain
#import "test_task_stack.m"
#undef main

static NSString *ID(NSUInteger i) { return [NSString stringWithFormat:@"00000000-0000-4000-8000-%012lu",(unsigned long)i]; }
static NSArray *Items(NSUInteger count,NSString *turn) {
    NSMutableArray *items=[NSMutableArray new];
    for(NSUInteger i=0;i<count;i++)[items addObject:@{@"session_id":ID(i),@"turn_id":turn,@"state":@"working",@"title":[NSString stringWithFormat:@"任务%lu",(unsigned long)i],@"summary":@"最新消息",@"expires_at":@(NSDate.date.timeIntervalSince1970+3600)}];
    return items;
}
static void Publish(NSArray *items) {
    NSData *json=[NSJSONSerialization dataWithJSONObject:@{@"tasks":items} options:0 error:nil];
    FishGlassStatusUpdateTasks([[NSString alloc] initWithData:json encoding:NSUTF8StringEncoding].UTF8String);SettleTaskAnimation();
}
static NSWindow *Create(NSString *suite) {
    notificationPreferences=[[NSUserDefaults alloc] initWithSuiteName:suite];
    NSWindow *window=[[NSWindow alloc] initWithContentRect:NSMakeRect(600,400,312,416) styleMask:NSWindowStyleMaskBorderless backing:NSBackingStoreBuffered defer:NO];
    window.title=@"BigFatBlueFish";[window orderFront:nil];FishGlassStatusUpdate("工作中 · 3 项","working");return window;
}
static void CheckControls(FishTaskRow *row) {
    Require(!row.dismissButton.hidden && !row.hideSessionButton.hidden && row.hideSessionButton.enabled,@"Live cards expose both independent controls");
    Require(NSMinX(row.hideSessionButton.frame)-NSMaxX(row.dismissButton.frame)==8 && NSMinY(row.hideSessionButton.frame)==NSMinY(row.dismissButton.frame),@"Hide sits to the right of close with8pt clear spacing and aligned centers");
    for(NSButton *control in @[row.dismissButton,row.hideSessionButton]) {
        NSRect frame=[taskPanel.contentView convertRect:control.bounds fromView:control];
        Require(NSContainsRect(taskPanel.contentView.bounds,frame),@"Both complete circular controls fit the original canvas");
        Require([taskPanel.contentView hitTest:NSMakePoint(NSMidX(frame),NSMidY(frame))]==control,@"Each control wins its own hit target over card navigation");
    }
    Require(row.hideSessionButton.image && [row.hideSessionButton.accessibilityLabel isEqualToString:@"永久隐藏此聊天的通知"],@"Hide has its own icon and accessible meaning");
}
static void Capture(NSString *name) {
    const char *directory=getenv("FISH_HIDE_CAPTURE_DIR");if(!directory)return;
    NSView *view=taskPanel.contentView;NSBitmapImageRep *bitmap=[view bitmapImageRepForCachingDisplayInRect:view.bounds];[view cacheDisplayInRect:view.bounds toBitmapImageRep:bitmap];
    NSString *path=[[NSString stringWithUTF8String:directory] stringByAppendingPathComponent:[name stringByAppendingString:@".png"]];
    Require([[bitmap representationUsingType:NSBitmapImageFileTypePNG properties:@{}] writeToFile:path atomically:YES],@"Save full native control layout");
}
int main(int argc,const char **argv) { @autoreleasepool {
    [NSApplication.sharedApplication setActivationPolicy:NSApplicationActivationPolicyProhibited];
    BOOL child=argc==3 && strcmp(argv[1],"--read-persisted")==0;
    NSString *suite=child ? [NSString stringWithUTF8String:argv[2]] : [@"local.desktop.rebuiltbigfish.test.taskhide." stringByAppendingString:NSUUID.UUID.UUIDString];
    NSWindow *window=Create(suite);Publish(Items(3,@"turn-a"));
    if(child) {
        Require(taskSnapshot.count==3 && taskData.count==2 && !TaskWithID(ID(0)),@"A fresh process reads and filters the saved session before its first card");
        Publish(Items(3,@"later-turn"));FishGlassStatusShowTask(ID(0).UTF8String);SettleTaskAnimation();
        Require(taskData.count==2 && !TaskWithID(ID(0)),@"A fresh process cannot reveal the permanent session via a new turn or cooker");
        FishGlassStatusDestroy();[window orderOut:nil];return 0;
    }
    NSUserDefaults *store=notificationPreferences;
    NSRect dock=statusPanel.frame;NSString *caption=overlay.caption.stringValue;
    CheckControls(taskStack.front);Capture(@"folded-hide");
    for(FishTaskRow *back in taskStack.backs)Require(back.dismissButton.hidden && back.hideSessionButton.hidden,@"Decorative rear cards have neither action");
    [taskStack.front.hideSessionButton performClick:nil];SettleTaskAnimation();
    Require(taskSnapshot.count==3 && taskData.count==2 && SessionPermanentlyHidden(ID(0)),@"Permanent hide filters only card presentation while all tasks remain");
    Require([overlay.caption.stringValue isEqualToString:caption] && NSEqualRects(dock,statusPanel.frame),@"Hide keeps work count and status dock unchanged");
    Require([[store arrayForKey:HiddenSessionDefaultsKey] isEqualToArray:@[ID(0)]],@"Only the session UUID is persisted, without turn/message content");
    Publish(Items(3,@"new-turn"));FishGlassStatusShowTask(ID(0).UTF8String);SettleTaskAnimation();
    Require(taskData.count==2 && !TaskWithID(ID(0)),@"New turns and explicit cooker clicks never revive a permanently hidden session");
    NSTask *reader=[NSTask new];reader.executableURL=[NSURL fileURLWithPath:[NSString stringWithUTF8String:argv[0]]];reader.arguments=@[@"--read-persisted",suite];
    NSError *error=nil;Require([reader launchAndReturnError:&error],@"Launch isolated preference reader");[reader waitUntilExit];Require(reader.terminationStatus==0,@"Fresh-process persistence and cooker suppression pass");
    FishGlassStatusDestroy();[window orderOut:nil];window=Create(suite);Publish(Items(3,@"after-recreate"));
    Require(!TaskWithID(ID(0)) && permanentlyHiddenSessions.count==1,@"Plugin teardown/recreation retains the stored session exclusion");
    Publish(@[]);Publish(Items(3,@"after-absence"));Require(!TaskWithID(ID(0)),@"Session disappearance and return do not erase permanent hiding");
    FishGlassStatusShowTask(ID(1).UTF8String);SettleTaskAnimation();
    [taskStack.front.dismissButton performClick:nil];SettleTaskAnimation();FishGlassStatusShowTask(ID(1).UTF8String);SettleTaskAnimation();
    Require(TaskWithID(ID(1)) && !TaskWithID(ID(0)),@"Per-turn close remains separately restorable without overriding permanent hiding");
    FishGlassStatusShowTasks();SettleTaskAnimation();CheckControls(taskRows[ID(2)]);Capture(@"expanded-hide");
    [((FishTaskRow *)taskRows[ID(2)]).hideSessionButton performClick:nil];SettleTaskAnimation();
    Require(taskData.count==1 && !tasksExpanded && SessionPermanentlyHidden(ID(2)),@"Expanded hide preserves the existing one-card transition");
    CheckControls(taskStack.front);[taskStack.front.hideSessionButton performClick:nil];SettleTaskAnimation();
    Require(!taskPanel.visible && taskSnapshot.count==3 && permanentlyHiddenSessions.count==3,@"Hiding every session removes all cards while retaining lifecycle data");
    FishGlassStatusShowTasks();SettleTaskAnimation();
    FishTaskRow *empty=taskRows[@""];Require([empty.titleLabel.stringValue isEqualToString:@"任务通知已隐藏"] && empty.hideSessionButton.hidden && empty.dismissButton.hidden,@"All-permanent placeholder does not suggest cooker restoration");
    Publish(Items(4,@"new-chat"));Require(taskData.count==1 && TaskWithID(ID(3)),@"New unrelated sessions still appear normally");
    FishGlassStatusDestroy();[window orderOut:nil];
    [store setObject:@[@1,@"invalid",@"ABCDEFAB-CDEF-4ABC-8DEF-000000000001"] forKey:HiddenSessionDefaultsKey];Require([store synchronize],@"Persist isolated malformed-input fixture");
    notificationPreferences=[[NSUserDefaults alloc] initWithSuiteName:suite];LoadHiddenSessions();
    Require(permanentlyHiddenSessions.count==1 && SessionPermanentlyHidden(@"abcdefab-cdef-4abc-8def-000000000001"),@"Preference loading accepts only valid UUIDs and normalizes case");
    permanentlyHiddenSessions=nil;notificationPreferences=nil;
    [store removePersistentDomainForName:suite];Require([store synchronize],@"Clean only the unique test suite");
    NSLog(@"PASS: hide/right8pt spacing/hit bounds/AX; session-only persistence across fresh process/recreation/absence/newturn; cooker never revives; per-turn close unchanged; raw lifecycle/count/dock intact; expanded/all-hidden/new-chat/UUID validation; isolated preference suite cleaned");return 0;
} }
