using NAudio.Wave;
using WhisperTray.Core.Audio;

namespace WhisperTray.App.Adapters;

/// <summary>
/// Enumerates legacy-MME input devices via NAudio. Device identity is the
/// ProductName so that settings survive reboots and device re-enumeration
/// (WaveInEvent indices are not stable).
/// </summary>
public sealed class WaveInDeviceEnumerator : IAudioDeviceEnumerator
{
    public IReadOnlyList<AudioDeviceInfo> List()
    {
        var count = WaveInEvent.DeviceCount;
        if (count <= 0)
        {
            return Array.Empty<AudioDeviceInfo>();
        }

        var devices = new List<AudioDeviceInfo>(count);
        for (var i = 0; i < count; i++)
        {
            var caps = WaveInEvent.GetCapabilities(i);
            devices.Add(new AudioDeviceInfo(
                Id: caps.ProductName,
                DisplayName: caps.ProductName,
                IsDefault: i == 0));
        }
        return devices;
    }

    public static int ResolveDeviceIndex(string? deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            return -1;
        }

        var count = WaveInEvent.DeviceCount;
        for (var i = 0; i < count; i++)
        {
            var caps = WaveInEvent.GetCapabilities(i);
            if (string.Equals(caps.ProductName, deviceId, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }
        return -1;
    }
}
