using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfUis;


// TODO2 support device create retry for midi devices like MidiGenerator.


namespace Ephemera.MidiLibLite
{
    public class Manager
    {
        #region Fields
        /// <summary>All midi devices to use for send. Index is the id.</summary>
        readonly List<IOutputDevice> _outputDevices = [];

        /// <summary>All midi devices to use for receive. Index is the id.</summary>
        readonly List<IInputDevice> _inputDevices = [];

        /// <summary>All the output channels. Key is handle.</summary>
        readonly Dictionary<int, OutputChannel> _outputChannels = [];

        /// <summary>All the input channels. Key is handle.</summary>
        readonly Dictionary<int, InputChannel> _inputChannels = [];
        #endregion

        #region Events
        /// <summary>Handler for message arrived.</summary>
        public event EventHandler<BaseMidiEvent>? InputReceive;
        #endregion

        #region Script => Host API
        /// <summary>
        /// 
        /// </summary>
        /// <param name="deviceName"></param>
        /// <param name="channelNumber"></param>
        /// <param name="channelName"></param>
        /// <returns></returns>
        public InputChannel OpenMidiInput(string deviceName, int channelNumber, string channelName)
        {
            // Check args.
            if (string.IsNullOrEmpty(deviceName)) { throw new ArgumentException("Invalid deviceName"); }
            if (channelNumber is < 1 or > MidiDefs.NUM_CHANNELS) { throw new ArgumentOutOfRangeException(nameof(channelNumber)); }

            var indev = GetInputDevice(deviceName);

            if (indev is null)
            {
                throw new MidiLibException($"Invalid input device [{deviceName}]");
            }

            var config = new InputChannelConfig()
            {
                DeviceName = deviceName,
                ChannelName = channelName,
                ChannelNumber = channelNumber
            };

            // Add the channel.
            InputChannel ch = new(config, indev)
            {
                Enable = true,
            };

            _inputChannels.Add(ch.Handle, ch);
            return ch;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="deviceName"></param>
        /// <param name="channelNumber"></param>
        /// <param name="channelName"></param>
        /// <param name="patch"></param>
        /// <returns></returns>
        public OutputChannel OpenMidiOutput(string deviceName, int channelNumber, string channelName, int patch)
        {
            // Check args.
            if (string.IsNullOrEmpty(deviceName)) { throw new ArgumentException("Invalid deviceName"); }
            if (channelNumber is < 1 or > MidiDefs.NUM_CHANNELS) { throw new ArgumentOutOfRangeException(nameof(channelNumber)); }

            var outdev = GetOutputDevice(deviceName);

            if (outdev is null)
            {
                throw new MidiLibException($"Invalid output device [{deviceName}]");
            }

            var config = new OutputChannelConfig()
            {
                DeviceName = deviceName,
                ChannelName = channelName,
                ChannelNumber = channelNumber,
                PresetFile = "",
                Patch = patch,
                Volume = Defs.DEFAULT_VOLUME
            };

            // Add the channel.
            OutputChannel ch = new(config, outdev)
            {
                Enable = true,
            };

            _outputChannels.Add(ch.Handle, ch);

            // Send the patch now.
            outdev.Send(new Patch(channelNumber, patch));

            return ch;
        }
        #endregion

        #region Devices
        /// <summary>
        /// Get I/O device. Lazy creation.
        /// </summary>
        /// <param name="deviceName"></param>
        /// <returns>The device or null if invalid.</returns>
        IInputDevice? GetInputDevice(string deviceName)
        {
            IInputDevice? dev = null;

            // Check for known.
            var indevs = _inputDevices.Where(o => o.DeviceName == deviceName);

            if (!indevs.Any())
            {
                // Is it a new device? Try to create it.

                // Midi input device?
                if (MidiInputDevice.AvailableDevices().Contains(deviceName))
                {
                    dev = new MidiInputDevice(deviceName) { Id = _inputDevices.Count + 1 };
                }

                // Others?
                var parts = deviceName.SplitByToken(":");
                switch (parts[0].ToLower(), parts.Count)
                {
                    case ("oscin", 2):
                        dev = new OscInputDevice(parts[1]) { Id = _inputDevices.Count + 1 };
                        break;

                    case ("nullin", 2):
                        dev = new NullInputDevice(deviceName) { Id = _inputDevices.Count + 1 };
                        break;
                }

                if (dev is not null)
                {
                    _inputDevices.Add(dev);
                    dev.CaptureEnable = true;
                    // Just pass inputs up.
                    dev.InputReceive += (object? sender, BaseMidiEvent e) => InputReceive?.Invoke((MidiInputDevice)sender!, e);
                }
            }
            else
            {
                dev = indevs.ElementAt(0);
            }

            return dev;
        }

        /// <summary>
        /// Get I/O device. Lazy creation.
        /// </summary>
        /// <param name="deviceName"></param>
        /// <returns>The device or null if invalid.</returns>
        IOutputDevice? GetOutputDevice(string deviceName)
        {
            IOutputDevice? dev = null;

            // Check for known.
            var outdevs = _outputDevices.Where(o => o.DeviceName == deviceName);

            if (!outdevs.Any())
            {
                // Is it a new device? Try to create it.

                // Midi output device?
                if (MidiOutputDevice.AvailableDevices().Contains(deviceName))
                {
                    dev = new MidiOutputDevice(deviceName) { Id = _outputDevices.Count + 1 };
                }

                // Others?
                var parts = deviceName.SplitByToken(":");
                switch (parts[0].ToLower(), parts.Count)
                {
                    case ("oscout", 3):
                        dev = new OscOutputDevice(parts[1], parts[2]) { Id = _outputDevices.Count + 1 };
                        break;

                    case ("nullout", 2):
                        dev = new NullOutputDevice(deviceName) { Id = _outputDevices.Count + 1 };
                        break;
                }

                if (dev is not null)
                {
                    _outputDevices.Add(dev);
                }
            }
            else
            {
                dev = outdevs.ElementAt(0);
            }

            return dev;
        }

        /// <summary>
        /// Clean up.
        /// </summary>
        public void DestroyDevices()
        {
            _inputDevices.ForEach(d => d.Dispose());
            _inputDevices.Clear();
            _outputDevices.ForEach(d => d.Dispose());
            _outputDevices.Clear();
        }
        #endregion

        #region Misc
        /// <summary>
        /// Helper. TODO2 bit klunky.
        /// </summary>
        /// <param name="id"></param>
        /// <returns>The device.</returns>
        public IOutputDevice GetOutputDevice(int id)
        {
            return _outputDevices[id];
        }

        /// <summary>
        /// Helper. TODO2 bit klunky.
        /// </summary>
        /// <param name="chnd"></param>
        /// <returns>The channel.</returns>

        public OutputChannel GetOutputChannel(ChannelHandle chnd)
        {
            return _outputChannels[chnd];
        }

        /// <summary>
        /// Stop all midi. Doesn't throw.
        /// </summary>
        /// <param name="channel"></param>
        public void Kill(OutputChannel? channel = null)
        {
            int cc = MidiDefs.GetControllerNumber("AllNotesOff");

            if (channel is null)
            {
                _outputChannels.ForEach(ch => ch.Value.Device.Send(new Controller(ch.Value.Config.ChannelNumber, cc, 0)));
            }
            else
            {
                channel.Device.Send(new Controller(channel.Config.ChannelNumber, cc, 0));
            }
        }
        #endregion
    }
}
