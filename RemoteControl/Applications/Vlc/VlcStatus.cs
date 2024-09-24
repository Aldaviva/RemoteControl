using System.Xml.Serialization;

namespace RemoteControl.Applications.Vlc;

/*[XmlRoot(ElementName = "videoeffects")]
public class VideoEffects {

    [XmlElement(ElementName = "hue")]
    public int hue { get; set; }

    [XmlElement(ElementName = "saturation")]
    public double saturation { get; set; }

    [XmlElement(ElementName = "contrast")]
    public double contrast { get; set; }

    [XmlElement(ElementName = "brightness")]
    public double brightness { get; set; }

    [XmlElement(ElementName = "gamma")]
    public double gamma { get; set; }

}

[XmlRoot(ElementName = "info")]
public class StreamInformation {

    [XmlAttribute(AttributeName = "name")]
    public required string key { get; set; }

    [XmlText]
    public required string value { get; set; }

}

[XmlRoot(ElementName = "category")]
public class StreamInformationCategory {

    [XmlElement(ElementName = "info")]
    public required List<StreamInformation> information { get; set; }

    [XmlAttribute(AttributeName = "name")]
    public required string categoryName { get; set; }
    //
    // [XmlText]
    // public string text { get; set; }

}

[XmlRoot(ElementName = "information")]
public class Information {

    [XmlElement(ElementName = "category")]
    public required List<StreamInformationCategory> categories { get; set; }

}

[XmlRoot(ElementName = "stats")]
public class Statistics {

    [XmlElement(ElementName = "lostabuffers")]
    public long lostABuffers { get; set; }

    [XmlElement(ElementName = "readpackets")]
    public long readPackets { get; set; }

    [XmlElement(ElementName = "lostpictures")]
    public long lostPictures { get; set; }

    [XmlElement(ElementName = "demuxreadbytes")]
    public long demuxReadBytes { get; set; }

    [XmlElement(ElementName = "demuxbitrate")]
    public double demuxBitrate { get; set; }

    [XmlElement(ElementName = "playedabuffers")]
    public long playedABuffers { get; set; }

    [XmlElement(ElementName = "demuxcorrupted")]
    public int demuxCorrupted { get; set; }

    [XmlElement(ElementName = "sendbitrate")]
    public double sendBitrate { get; set; }

    [XmlElement(ElementName = "sentbytes")]
    public long sentBytes { get; set; }

    [XmlElement(ElementName = "displayedpictures")]
    public long displayedPictures { get; set; }

    [XmlElement(ElementName = "demuxreadpackets")]
    public long demuxReadPackets { get; set; }

    [XmlElement(ElementName = "sentpackets")]
    public long sentPackets { get; set; }

    [XmlElement(ElementName = "inputbitrate")]
    public double inputBitrate { get; set; }

    [XmlElement(ElementName = "demuxdiscontinuity")]
    public int demuxDiscontinuity { get; set; }

    [XmlElement(ElementName = "averagedemuxbitrate")]
    public double averageDemuxBitrate { get; set; }

    [XmlElement(ElementName = "decodedvideo")]
    public long decodedVideo { get; set; }

    [XmlElement(ElementName = "averageinputbitrate")]
    public double averageInputBitrate { get; set; }

    [XmlElement(ElementName = "readbytes")]
    public long readBytes { get; set; }

    [XmlElement(ElementName = "decodedaudio")]
    public long decodedaudio { get; set; }

}*/

[XmlRoot(ElementName = "root")]
public readonly struct VlcStatus {

    [XmlElement(ElementName = "fullscreen")]
    public bool fullscreen { get; init; }

    /*/// <summary>
    /// One of <c>default</c>, <c>16:9</c>, <c>4:3</c>, <c>1:1</c>, <c>16:10</c>, <c>221:100</c>, <c>235:100</c>, <c>239:100</c>, and <c>5:4</c>.
    /// </summary>
    [XmlElement(ElementName = "aspectratio")]
    public required string aspectRatio { get; set; }*/

    /*/// <summary>
    /// The duration of the "short jump length" in the VLC preferences, in seconds.
    /// </summary>
    [XmlElement(ElementName = "seek_sec")]
    public int seekDurationSec { get; set; }*/

    [XmlElement(ElementName = "apiversion")]
    public int apiVersion { get; init; }

    /*[XmlElement(ElementName = "currentplid")]
    public int currentPlaylistId { get; set; }*/

    /// <summary>
    /// <para>Current playback time, in seconds, in the range [0, <see cref="lengthSec"/>].</para>
    /// <para>For the percentage-based equivalent that is more precise than 1 second, see <see cref="positionPercent"/>.</para>
    /// </summary>
    [XmlElement(ElementName = "time")]
    public int positionSec { get; init; }

    /*[XmlElement(ElementName = "volume")]
    public byte volume { get; set; }*/

    /// <summary>
    /// Total duration of the media, in seconds
    /// </summary>
    [XmlElement(ElementName = "length")]
    public int lengthSec { get; init; }

    /*[XmlElement(ElementName = "random")]
    public bool shuffle { get; set; }*/

    // [XmlElement(ElementName = "audiofilters")]
    // public audiofilters audiofilters { get; set; }

    /*[XmlElement(ElementName = "rate")]
    public double rate { get; set; }

    [XmlElement(ElementName = "videoeffects")]
    public required VideoEffects videoEffects { get; set; }*/

    /// <summary>
    /// Whether the media is playing, paused, or stopped
    /// </summary>
    [XmlElement(ElementName = "state")]
    public VlcPlaybackState playbackState { get; init; }

    /*[XmlElement(ElementName = "loop")]
    public bool loop { get; set; }*/

    [XmlElement(ElementName = "version")]
    public required string vlcVersion { get; init; }

    /// <summary>
    /// <para>Current playback time as a percentage of the total <see cref="lengthSec"/>, in the range [0.0, 1.0].</para>
    /// <para>For the number of whole seconds, see <see cref="positionSec"/>.</para>
    /// </summary>
    [XmlElement(ElementName = "position")]
    public double positionPercent { get; init; }

    /*[XmlElement(ElementName = "audiodelay")]
    public double audioDelay { get; set; }

    [XmlElement(ElementName = "repeat")]
    public bool repeat { get; set; }

    [XmlElement(ElementName = "subtitledelay")]
    public double subtitleDelay { get; set; }*/

    // [XmlElement(ElementName = "equalizer")]
    // public object equalizer { get; set; }

    /*[XmlElement(ElementName = "information")]
    public required Information information { get; set; }

    [XmlElement(ElementName = "stats")]
    public required Statistics statistics { get; set; }*/

}

/*
 * Source: https://code.videolan.org/videolan/vlc/-/blob/7df26860bbae7a6e2c41a3d8aacd0fff346e5123/include/vlc_player.h#L191
 */
public enum VlcPlaybackState {

    UNKNOWN,
    STOPPED,

    /// <summary>
    /// Buffering, attempting to start. Not actually playing, but transitioning to <see cref="PLAYING"/>.
    /// </summary>
    STARTED,

    /// <summary>
    /// Media is actually playing
    /// </summary>
    PLAYING,
    PAUSED,

    /// <summary>
    /// Transitioning to <see cref="STOPPED"/>
    /// </summary>
    STOPPING

}