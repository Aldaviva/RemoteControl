using System.Xml.Serialization;

namespace RemoteControl.Applications.Vlc;

/*
 * Source: https://code.videolan.org/videolan/vlc/-/blob/7df26860bbae7a6e2c41a3d8aacd0fff346e5123/include/vlc_player.h#L191
 */
public enum VlcPlaybackState {

    [XmlEnum(Name = "unknown")]
    UNKNOWN,

    [XmlEnum(Name = "stopped")]
    STOPPED,

    /// <summary>
    /// Buffering, attempting to start. Not actually playing, but transitioning to <see cref="PLAYING"/>.
    /// </summary>
    [XmlEnum(Name = "started")]
    STARTED,

    /// <summary>
    /// Media is actually playing
    /// </summary>
    [XmlEnum(Name = "playing")]
    PLAYING,

    [XmlEnum(Name = "paused")]
    PAUSED,

    /// <summary>
    /// Transitioning to <see cref="STOPPED"/>
    /// </summary>
    [XmlEnum(Name = "stopping")]
    STOPPING

}