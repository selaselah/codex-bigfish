import AppKit
import SwiftUI
import QuartzCore

// The status is a desktop accessory, so it keeps its active glass appearance
// while the person works in another app. It never takes keyboard focus.
private final class StatusPanel: NSPanel {
    override var canBecomeKey: Bool { false }
    override var canBecomeMain: Bool { false }
}
private final class StatusModel: ObservableObject {
    @Published var caption = ""
    @Published var tone = "idle"
    @Published var width: CGFloat = 140
}
@available(macOS 26.0, *)
private struct StatusCapsule: View {
    @ObservedObject var model: StatusModel
    private var colour: Color {
        switch model.tone {
        case "complete": return Color(red: 0.18, green: 0.66, blue: 0.50)
        case "waiting": return Color(red: 0.91, green: 0.64, blue: 0.22)
        case "error": return Color(red: 0.91, green: 0.33, blue: 0.39)
        case "idle": return Color(red: 0.48, green: 0.56, blue: 0.67)
        default: return Color(red: 0.29, green: 0.55, blue: 0.95)
        }
    }
    var body: some View {
        HStack(spacing: 10) {
            Circle().fill(colour).frame(width: 6, height: 6)
            Text(model.caption).font(.system(size: 13, weight: .medium))
                .lineLimit(1).truncationMode(.tail)
        }
        .padding(.horizontal, 16)
        .frame(width: model.width, height: 40)
        .glassEffect(.clear.tint(.white.opacity(0.35)), in: Capsule())
        .environment(\.appearsActive, true)
        .accessibilityElement(children: .ignore)
        .accessibilityLabel("桌宠状态")
        .accessibilityValue(model.caption)
        .padding(10) // Let the system glass render its own edge and shadow.
    }
}
private final class StatusController {
    let model = StatusModel()
    let panel: StatusPanel
    weak var parent: NSWindow?
    var observations: [NSObjectProtocol] = []
    var wantsVisible = true
    var loggedInitialFocus = false
    var desktopBackdrop: NSVisualEffectView?
    var backdropWidth: CGFloat = 0
    var legacy: NSVisualEffectView?
    var legacyCaption: NSTextField?
    var backend: Int32 = 1
    init() {
        panel = StatusPanel(contentRect: NSRect(x: 0, y: 0, width: 160, height: 60),
            styleMask: [.borderless, .nonactivatingPanel], backing: .buffered, defer: false)
        panel.title = "桌宠状态"
        panel.isOpaque = false
        panel.backgroundColor = .clear
        panel.hasShadow = true
        panel.hidesOnDeactivate = false
        panel.isFloatingPanel = true
        panel.becomesKeyOnlyIfNeeded = true
        panel.ignoresMouseEvents = true
        panel.isReleasedWhenClosed = false
        panel.isExcludedFromWindowsMenu = true
        panel.collectionBehavior = [.fullScreenAuxiliary, .ignoresCycle]
        if #available(macOS 26.0, *) {
            let hosting = NSHostingView(rootView: StatusCapsule(model: model))
            hosting.sizingOptions = []
            hosting.safeAreaRegions = []
            let root = NSView()
            let backdrop = NSVisualEffectView()
            // Glass samples its own window. Bridge the desktop into this
            // transparent accessory using the public behind-window material.
            // Its explicit active state preserves real background blur on blur.
            backdrop.blendingMode = .behindWindow
            backdrop.material = .underWindowBackground
            backdrop.state = .active
            root.addSubview(backdrop)
            hosting.autoresizingMask = [.width, .height]
            root.addSubview(hosting)
            panel.contentView = root
            desktopBackdrop = backdrop
            backend = 2
        } else {
            let root = NSView()
            let effect = NSVisualEffectView()
            effect.blendingMode = .behindWindow
            effect.material = .hudWindow
            effect.state = .active
            effect.wantsLayer = true
            effect.layer?.cornerRadius = 20
            effect.layer?.masksToBounds = true
            root.addSubview(effect)
            let caption = NSTextField(labelWithString: "")
            caption.font = .systemFont(ofSize: 13, weight: .medium)
            caption.alignment = .center
            caption.lineBreakMode = .byTruncatingTail
            caption.setAccessibilityLabel("桌宠状态")
            effect.addSubview(caption)
            panel.contentView = root
            legacy = effect; legacyCaption = caption
        }
        let centre = NotificationCenter.default
        for name in [NSWindow.didMoveNotification, NSWindow.didResizeNotification,
                     NSWindow.didMiniaturizeNotification, NSWindow.didDeminiaturizeNotification,
                     NSWindow.didChangeScreenNotification] {
            observations.append(centre.addObserver(forName: name, object: nil, queue: .main) { [weak self] note in
                guard let self, note.object as? NSWindow === self.parent else { return }
                self.layout()
            })
        }
        for name in [NSApplication.didBecomeActiveNotification, NSApplication.didResignActiveNotification] {
            observations.append(centre.addObserver(forName: name, object: nil, queue: .main) { [weak self] _ in
                guard let self else { return }
                self.layout()
                NSLog("Desktop Fish glass focus: appActive=%d petKey=%d panelKey=%d activeAppearance=1",
                    NSApp.isActive, self.parent?.isKeyWindow ?? false, self.panel.isKeyWindow)
            })
        }
        NSLog("Desktop Fish status: native SwiftUI Clear Liquid Glass + active behind-window desktop sampling; height=40 radius=20; appearsActive=1; nonactivating accessory")
        NSLog("Desktop Fish glass accessibility: reduceTransparency=%d reduceMotion=%d",
            NSWorkspace.shared.accessibilityDisplayShouldReduceTransparency,
            NSWorkspace.shared.accessibilityDisplayShouldReduceMotion)
    }
    func update(caption: String, tone: String, window: NSWindow) {
        if parent !== window {
            parent?.removeChildWindow(panel)
            parent = window
            panel.collectionBehavior = window.collectionBehavior.union([.fullScreenAuxiliary, .ignoresCycle])
            window.addChildWindow(panel, ordered: .above)
        }
        let clean = caption.replacingOccurrences(of: "\n", with: " ")
        let textWidth = (clean as NSString).size(withAttributes: [.font: NSFont.systemFont(ofSize: 13, weight: .medium)]).width
        let width = min(max(140, ceil(textWidth) + 48), max(140, window.contentView!.bounds.width - 24))
        if model.caption != clean { model.caption = clean }
        if model.tone != tone { model.tone = tone }
        if model.width != width { model.width = width }
        legacyCaption?.stringValue = clean
        wantsVisible = true
        layout()
        if !loggedInitialFocus {
            loggedInitialFocus = true
            NSLog("Desktop Fish glass initial focus: appActive=%d petKey=%d panelKey=%d backdropActive=1 height=40",
                  NSApp.isActive, window.isKeyWindow, panel.isKeyWindow)
        }
    }
    func layout() {
        guard let parent, let content = parent.contentView else { return }
        // Keep the original top edge below the feet; the taller capsule extends
        // downwards in its own transparent accessory window instead of covering them.
        let viewRect = NSRect(x: floor((content.bounds.width - model.width) / 2) - 10,
                              y: -21, width: model.width + 20, height: 60)
        let screenRect = parent.convertToScreen(content.convert(viewRect, to: nil))
        if panel.frame != screenRect { panel.setFrame(screenRect, display: true) }
        panel.level = parent.level
        if let desktopBackdrop {
            desktopBackdrop.frame = NSRect(x: 10, y: 10, width: model.width, height: 40)
            if backdropWidth != model.width {
                backdropWidth = model.width
                let size = NSSize(width: model.width, height: 40)
                desktopBackdrop.maskImage = NSImage(size: size, flipped: false) { rect in
                    NSColor.white.setFill()
                    NSBezierPath(roundedRect: rect, xRadius: 20, yRadius: 20).fill()
                    return true
                }
            }
            // Keep the native hosting surface at the panel's full size.
            panel.contentView?.subviews.last?.frame = NSRect(origin: .zero, size: screenRect.size)
        }
        legacy?.frame = NSRect(x: 10, y: 10, width: model.width, height: 40)
        legacyCaption?.frame = NSRect(x: 16, y: 10, width: model.width - 32, height: 20)
        if wantsVisible && parent.isVisible && !parent.isMiniaturized {
            if panel.parent !== parent { parent.addChildWindow(panel, ordered: .above) }
            if !panel.isVisible { panel.orderFront(nil) }
        } else { panel.orderOut(nil) }
    }
    func dispose() {
        for token in observations { NotificationCenter.default.removeObserver(token) }
        observations.removeAll()
        parent?.removeChildWindow(panel)
        panel.orderOut(nil)
        panel.close()
    }
}
private var controller: StatusController?
private let readinessLock = NSLock()
private var readiness: Int32 = 0
private func setReadiness(_ value: Int32) {
    readinessLock.lock(); readiness = value; readinessLock.unlock()
}
private func onMain(_ block: @escaping () -> Void) {
    if Thread.isMainThread { block() } else { DispatchQueue.main.async(execute: block) }
}
@_cdecl("FishGlassStatusIsReady")
public func fishGlassStatusIsReady() -> Int32 {
    readinessLock.lock(); defer { readinessLock.unlock() }
    return readiness
}
@_cdecl("FishGlassStatusUpdate")
public func fishGlassStatusUpdate(_ captionUTF8: UnsafePointer<CChar>?, _ toneUTF8: UnsafePointer<CChar>?) -> Int32 {
    let caption = captionUTF8.map { String(cString: $0) } ?? ""
    let tone = toneUTF8.map { String(cString: $0) } ?? "idle"
    onMain {
        guard let window = NSApp.windows.first(where: { $0.title == "BigFatBlueFish" && $0.isVisible && !($0 is StatusPanel) }) else { setReadiness(0); return }
        if controller == nil { controller = StatusController() }
        controller?.update(caption: caption, tone: tone, window: window)
        setReadiness(controller?.backend ?? 0)
    }
    return fishGlassStatusIsReady()
}
@_cdecl("FishGlassStatusSetVisible")
public func fishGlassStatusSetVisible(_ visible: Int32) {
    onMain { controller?.wantsVisible = visible != 0; controller?.layout() }
}
@_cdecl("FishGlassStatusDestroy")
public func fishGlassStatusDestroy() {
    onMain { controller?.dispose(); controller = nil; setReadiness(0) }
}
