using RemoteControl.Remote;

namespace RemoteControl.Applications.Vivaldi;

public abstract class BrowserCommand<RESPONSE> where RESPONSE: BrowserResponse {

    public ulong requestId { get; internal set; }
    public abstract string name { get; }

}

public interface BrowserResponse {

    public ulong requestId { get; init; }
    internal string? exception { get; init; }

}

public class BaseResponse: BrowserResponse {

    public ulong requestId { get; init; }
    public string? exception { get; init; }

}

public class PressButton(RemoteControlButton button): BrowserCommand<ButtonPressed> {

    public override string name => nameof(PressButton);

    public RemoteControlButton button { get; } = button;

}

public class PressSeekButton(bool isForwards): PressButton(isForwards ? RemoteControlButton.NEXT_TRACK : RemoteControlButton.PREVIOUS_TRACK) {

    public uint jumpDurationSec { get; init; }

}

public class ButtonPressed: BaseResponse {

    public Website website { get; init; }

}

public class FetchPlaybackState: BrowserCommand<PlaybackStateFetched> {

    public override string name => nameof(FetchPlaybackState);

}

public class PlaybackStateFetched: BaseResponse {

    public PlaybackState playbackState { get; init; }

}