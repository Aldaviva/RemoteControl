using RemoteControl.Remote;

namespace RemoteControl.Applications.Vivaldi;

public abstract class BrowserCommand<RESPONSE> where RESPONSE: BrowserResponse {

    internal ulong requestId { get; set; }
    public abstract string name { get; }

}

public interface BrowserResponse {

    internal ulong requestId { get; init; }

}

public class BaseResponse: BrowserResponse {

    public ulong requestId { get; init; }

}

public class PressButton(RemoteControlButton button): BrowserCommand<ButtonPressed> {

    public override string name => nameof(PressButton);

    public RemoteControlButton button { get; } = button;

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