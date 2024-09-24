namespace RemoteControl.Config;

public record VlcConfiguration {

    public required string password { get; init; }
    public ushort port { get; init; } = 8080;
    public uint timeoutMs { get; init; } = 500;
    public uint jumpDurationMs { get; init; } = 5000;

}