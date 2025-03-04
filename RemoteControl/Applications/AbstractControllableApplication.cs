using ManagedWinapi.Windows;
using Microsoft.Win32;
using RemoteControl.Caching;
using RemoteControl.Remote;
using System.ComponentModel;
using System.Diagnostics;
using Unfucked;

namespace RemoteControl.Applications;

public abstract class AbstractControllableApplication: ControllableApplication {

    private const string APP_PATHS = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths";

    protected static readonly TimeSpan CACHE_DURATION = TimeSpan.FromSeconds(2);

    private readonly SingletonCache<SystemWindow>  foregroundWindow = SingletonCache<SystemWindow>.create(() => SystemWindow.ForegroundWindow, CACHE_DURATION);
    private readonly SingletonCache<SystemWindow?> windowCache;
    protected SystemWindow? appWindow => windowCache.value;

    protected AbstractControllableApplication() {
        windowCache = SingletonCache<SystemWindow?>.create(() => SystemWindow.FilterToplevelWindows(isApplicationWindow).FirstOrDefault(), CACHE_DURATION);
    }

    protected virtual bool isApplicationWindow(SystemWindow window) => window.ClassName == windowClassName
        && (executableFilename is not { } exeName || Path.GetFileNameWithoutExtension(exeName).Equals(window.GetProcessExecutableBasename(), StringComparison.OrdinalIgnoreCase));

    protected abstract string windowClassName { get; }

    protected virtual string? executableFilename { get; } = null;

    public abstract ApplicationPriority priority { get; }

    public abstract string name { get; }

    public bool isRunning => appWindow?.ClassName != null;

    public bool isFocused => appWindow == foregroundWindow.value;

    public abstract Task<PlaybackState> fetchPlaybackState();

    public abstract Task sendButtonPress(RemoteControlButton button);

    public bool launch() {
        if (executableFilename != null && Registry.GetValue(Path.Combine(APP_PATHS, executableFilename), string.Empty, null) is string executableAbsoluteFilename) {
            try {
                using Process process = Process.Start(executableAbsoluteFilename);
                return true;
            } catch (Win32Exception) { }
        }
        return false;
    }

    protected void unminimize() {
        if (appWindow?.WindowState == FormWindowState.Minimized) {
            appWindow.WindowState = FormWindowState.Maximized;
        }
    }

    protected virtual void Dispose(bool disposing) {
        if (disposing) {
            windowCache.Dispose();
        }
    }

    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

}