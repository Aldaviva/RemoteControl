namespace RemoteControl.Applications;

public readonly struct PlaybackState(bool isPlaying, bool canPlay) {

    public readonly bool isPlaying = isPlaying;
    public readonly bool canPlay   = canPlay;

}