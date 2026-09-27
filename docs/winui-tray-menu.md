# Native WinUI tray menu

The service retains its tray icon and command handlers. Right-click sends the
current labels, checked states and cursor position to a dedicated WinUI settings
executable instance using a current-user named pipe. Its native `MenuFlyout` uses
the standard WinUI animations, icons, separators and light/dark resources.

The host does not take the settings-window mutex or open the main settings page.
It exits when its owning service exits. If startup or IPC fails, the service logs
the error and falls back to the existing menu.

The anchor HWND is layered with zero opacity before activation: a transparent
XAML brush alone leaves a black native-window surface, and Windows can enforce
a minimum window size even when requesting 1 x 1. The unconstrained flyout uses
its own native window. Both anchor and popup are topmost while the menu is open.
The host takes foreground focus for keyboard navigation; moving focus outside
the dedicated host dismisses the menu. A short timer backs up the activation
notification for clicks on the desktop and other windows. Closing the flyout
stops that timer and hides the anchor; no selected action returns cancellation.

Manual regression checks: right-click from the visible tray and overflow panel;
verify no extra rectangle, correct layering and native animation; dismiss with
Escape, a desktop click and another app; reopen several times; select an action;
check light/dark mode, display scaling and monitor edges. Compilation alone does
not verify these desktop interactions.

Native opacity reference:
[SetLayeredWindowAttributes](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setlayeredwindowattributes).
