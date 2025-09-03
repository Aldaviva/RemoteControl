namespace RemoteControl.Config;

public record GeneralConfiguration {

    public uint httpClientTimeoutMs { get; init; } = 500;
    public uint jumpDurationSec { get; init; } = 4;

}