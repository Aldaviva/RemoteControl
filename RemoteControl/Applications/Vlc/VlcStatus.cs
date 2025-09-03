using System.Xml.Serialization;

namespace RemoteControl.Applications.Vlc;

[XmlRoot(ElementName = "root")]
public readonly struct VlcStatus {

    [XmlElement(ElementName = "fullscreen")]
    public bool fullscreen { get; init; }

    [XmlElement(ElementName = "apiversion")]
    public int apiVersion { get; init; }

    /// <summary>
    /// <para>Current playback time, in seconds, in the range [0, <see cref="lengthSec"/>].</para>
    /// <para>For the percentage-based equivalent that is more precise than 1 second, see <see cref="positionPercent"/>.</para>
    /// </summary>
    [XmlElement(ElementName = "time")]
    public int positionSec { get; init; }

    /// <summary>
    /// Total duration of the media, in seconds
    /// </summary>
    [XmlElement(ElementName = "length")]
    public int lengthSec { get; init; }

    /// <summary>
    /// Whether the media is playing, paused, or stopped
    /// </summary>
    [XmlElement(ElementName = "state")]
    public VlcPlaybackState playbackState { get; init; }

    [XmlElement(ElementName = "version")]
    public required string vlcVersion { get; init; }

    /// <summary>
    /// <para>Current playback time as a percentage of the total <see cref="lengthSec"/>, in the range [0.0, 1.0].</para>
    /// <para>For the number of whole seconds, see <see cref="positionSec"/>.</para>
    /// </summary>
    [XmlElement(ElementName = "position")]
    public double positionPercent { get; init; }

}