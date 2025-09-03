namespace RemoteControl.Config;

public record VlcConfiguration {

    public required string password { get; init; }
    public ushort port { get; init; } = 8080;

}