﻿using System.Runtime.CompilerServices;
using System.Text.Json;

namespace RemoteControl;

public abstract class RemoteException(string message, Exception? cause = null): ApplicationException(message, cause);

public class NoBrowserConnected(string message): RemoteException(message);

public abstract class BrowserExtensionException(string message): RemoteException(message) {

    protected BrowserExtensionException(): this("Browser extension returned an exception response") { }

}

public sealed class UnmappedBrowserExtensionException(string name, JsonDocument responseBody, [CallerFilePath] string exceptionsSourceFile = $"{nameof(RemoteControl)}\\Exceptions.cs")
    : BrowserExtensionException($"Unmapped exception {name} from browser extension, please add 'public class {name}: {nameof(BrowserExtensionException)};' in {exceptionsSourceFile}.") {

    public JsonDocument responseBody { get; } = responseBody;

}

public class UnsupportedWebsite: BrowserExtensionException {

    public Uri url { get; init; } = null!;

}

public class UnsupportedCommand: BrowserExtensionException {

    public string name { get; init; } = null!;

}