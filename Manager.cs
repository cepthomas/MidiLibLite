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
// using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfUis;


namespace Ephemera.MidiLibLite
{
    public class Manager
    {
        #region Fields
        /// <summary>All midi devices to use for send.</summary>
        readonly List<IOutputDevice> _outputDevices = [];

        /// <summary>All midi devices to use for receive.</summary>
        readonly List<IInputDevice> _inputDevices = [];

        /// <summary>All the output channels.</summary>
        readonly List<OutputChannel> _outputChannels = new();

        /// <summary>All the input channels.</summary>
        readonly List<InputChannel> _inputChannels = new();
        #endregion

        #region Events
        /// <summary>Handler for message arrived.</summary>
        public event EventHandler<BaseEvent>? InputReceive;
        #endregion

        #region Script => Host API
        /// <summary>
        /// 
        /// </summary>
        /// <param name="deviceName"></param>
        /// <param name="channelNumber"></param>
        /// <param name="channelName"></param>
        /// <returns></returns>
        /// <exception cref="MidiLibException"></exception>
        public InputChannel CreateInputChannel(string deviceName, int channelNumber, string channelName)
        {
            // Locate the device.
            var indev = _inputDevices.Where(o => o.DeviceName == deviceName);
            if (!indev.Any())
            {
                throw new MidiLibException($"Invalid input device: {deviceName}");
            }
            var dev = indev.ElementAt(0);

            // Add the channel.
            InputChannel ch = new(dev, channelNumber, channelName)
            {
                Enable = true,
            };

            _inputChannels.Add(ch);
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
        /// <exception cref="MidiLibException"></exception>
        public OutputChannel CreateOutputChannel(string deviceName, int channelNumber, string channelName, int patch)
        {
            // Locate the device.
            var outdev = _outputDevices.Where(o => o.DeviceName == deviceName);
            if (!outdev.Any())
            {
                throw new MidiLibException($"Invalid output device: {deviceName}");
            }
            var dev = outdev.ElementAt(0);

            // Add the channel.
            OutputChannel ch = new(dev, channelNumber, channelName)
            {
                Enable = true,
                Patch = patch
            };

            _outputChannels.Add(ch);

            // Send the patch now.
            if (patch >= 0)
            {
                dev.Send(new Patch(channelNumber, patch));
            }
            return ch;

            //return chnd;
        }
        #endregion

        #region Devices
        /// <summary>
        /// Create all I/O devices from user settings.
        /// </summary>
        /// <returns>Success</returns>
        public bool CreateDevices() // TODO1 also OSC, null, etc => IInputDevice...
        {
            bool ok = true;

            // First...
            DestroyDevices();

            // Set up input devices.
            foreach (var devname in MidiInputDevice.AvailableDevices())
            {
                var indev = new MidiInputDevice(devname);//TODO1 support retry

                if (!indev.Valid)
                {
                    throw new MidiLibException($"Something wrong with your input device:{devname}");
                }
                else
                {
                    indev.CaptureEnable = true;
                    indev.InputReceive += Midi_ReceiveEvent;
                    _inputDevices.Add(indev);
                }
            }

            // Set up output devices.
            foreach (var devname in MidiOutputDevice.AvailableDevices())
            {
                // Try midi.
                var outdev = new MidiOutputDevice(devname);//TODO1 support retry
                if (!outdev.Valid)
                {
                    throw new MidiLibException($"Something wrong with your output device:{devname}");
                }
                else
                {
                    _outputDevices.Add(outdev);
                }
            }

            return ok;
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

        /// <summary>
        /// Midi input arrived from device. This is on a system thread.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Midi_ReceiveEvent(object? sender, BaseEvent e)
        {
            var indev = (MidiInputDevice)sender!;

            // Just pass up.
            InputReceive?.Invoke(indev, e);
        }
        #endregion
    }
}
