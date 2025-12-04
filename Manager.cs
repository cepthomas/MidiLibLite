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
        readonly Dictionary<int, OutputChannel> _outputChannels = new();

        /// <summary>All the input channels. Key is handle.</summary>
        readonly Dictionary<int, InputChannel> _inputChannels = new();
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
            if (string.IsNullOrEmpty(deviceName)) { throw new ArgumentException(nameof(deviceName)); }
            if (channelNumber is < 1 or > MidiDefs.NUM_CHANNELS) { throw new ArgumentOutOfRangeException(nameof(channelNumber)); }

            // Locate the device.
            var indev = _inputDevices.Where(o => o.DeviceName == deviceName);
            if (!indev.Any())
            {
                throw new MidiLibException($"Invalid input device name [{deviceName}]");
            }
            var dev = indev.ElementAt(0);

            // Add the channel.
            InputChannel ch = new(dev, channelNumber, channelName)
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
            if (string.IsNullOrEmpty(deviceName)) { throw new ArgumentException(nameof(deviceName)); }
            if (channelNumber is < 1 or > MidiDefs.NUM_CHANNELS) { throw new ArgumentOutOfRangeException(nameof(channelNumber)); }

            // Locate the device.
            var outdev = _outputDevices.Where(o => o.DeviceName == deviceName);
            if (!outdev.Any())
            {
                throw new MidiLibException($"Invalid output device name [{deviceName}]");
            }
            var dev = outdev.ElementAt(0);

            // Add the channel.
            OutputChannel ch = new(dev, channelNumber, channelName)
            {
                Enable = true,
                Patch = patch
            };

            _outputChannels.Add(ch.Handle, ch);

            // Send the patch now.
            if (patch >= 0)
            {
                dev.Send(new Patch(channelNumber, patch));
            }
            return ch;
        }
        #endregion







        #region Devices
        /// <summary>
        /// Create all I/O devices from user settings.
        /// </summary>
        /// <returns>Success</returns>
        public void CreateDevices() // TODO1 also OSC, null - from script or api
        {
            // First...
            DestroyDevices();

            // Set up input devices.
            foreach (var devname in MidiInputDevice.AvailableDevices())
            {
                var indev = new MidiInputDevice(devname);

                if (!indev.Valid)
                {
                    throw new MidiLibException($"Something wrong with your input device [{devname}]");
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
                var outdev = new MidiOutputDevice(devname);
                if (!outdev.Valid)
                {
                    throw new MidiLibException($"Something wrong with your output device [{devname}]");
                }
                else
                {
                    _outputDevices.Add(outdev);
                }
            }
        }

        //TODO1 support retry x2
        // ///// Determine midi output device. /////
        // Text = "Midi Generator - no output device";
        // timer1.Interval = 1000;
        // timer1.Tick += (sender, e) => ConnectDevice();
        // timer1.Start();
        // /// <summary>
        // /// Figure out which midi output device.
        // /// </summary>
        // void ConnectDevice()
        // {
        //     if (_midiOut == null)
        //     {
        //         // Retry.
        //         string deviceName = _settings.OutputDevice;
        //         for (int i = 0; i < MidiOut.NumberOfDevices; i++)
        //         {
        //             if (deviceName == MidiOut.DeviceInfo(i).ProductName)
        //             {
        //                 _midiOut = new MidiOut(i);
        //                 Text = $"Midi Generator - {deviceName}";
        //                 Tell($"Connect to {deviceName}");
        //                 timer1.Stop();
        //                 break;
        //             }
        //         }
        //     }
        // }

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
        void Midi_ReceiveEvent(object? sender, BaseMidiEvent e)
        {
            var indev = (MidiInputDevice)sender!;

            // Just pass up.
            InputReceive?.Invoke(indev, e);
        }
        #endregion




        public IOutputDevice GetOutputDevice(int id) // TODO1 bit klunky
        {
            return _outputDevices[id];
        }


        public OutputChannel GetOutputChannel(ChannelHandle chnd) // TODO1 bit klunky
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
                _outputChannels.ForEach(ch => ch.Value.Device.Send(new Controller(ch.Value.ChannelNumber, cc, 0)));
            }
            else
            {
                channel.Device.Send(new Controller(channel.ChannelNumber, cc, 0));
            }
        }
    }
}
