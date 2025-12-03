using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NAudio.Midi;
using Ephemera.NBagOfTricks;
using NAudio.CoreAudioApi;


namespace Ephemera.MidiLibLite
{
    /// <summary>
    /// A midi input device.
    /// </summary>
    public class MidiInputDevice : IInputDevice
    {
        #region Fields
        /// <summary>NAudio midi input device.</summary>
        readonly MidiIn? _midiIn = null;
        #endregion

        #region Properties
        /// <inheritdoc />
        public string DeviceName { get; }

        /// <inheritdoc />
        public int Id { get; }

        /// <inheritdoc />
        public bool CaptureEnable { get; set; }

        /// <inheritdoc />
        public bool Valid { get { return _midiIn is not null; } }
        #endregion

        #region Events
        /// <summary>Client needs to deal with this.</summary>
        public event EventHandler<BaseMidiEvent>? InputReceive;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Normal constructor.
        /// </summary>
        /// <param name="deviceName">Client must supply name of device.</param>
        public MidiInputDevice(string deviceName)
        {
            // Figure out which midi output device.
            var devs = AvailableDevices();
            var ind = devs.IndexOf(deviceName);
            if (ind >= 0)
            {
                DeviceName = deviceName;
                Id = ind;
                _midiIn = new MidiIn(ind);
                _midiIn.MessageReceived += MidiIn_MessageReceived;
                _midiIn.ErrorReceived += MidiIn_ErrorReceived;
                _midiIn.Start();
            }
            else
            {
                DeviceName = "";
                throw new MidiLibException($"Invalid input midi device name [{deviceName}]");
            }
        }

        /// <summary>
        /// Resource clean up.
        /// </summary>
        public void Dispose()
        {
            _midiIn?.Stop();
            _midiIn?.Dispose();
        }
        #endregion

        /// <summary>
        /// Process driver level midi input event. ???TODO2 Don't throw in this thread!
        /// </summary>
        void MidiIn_MessageReceived(object? sender, MidiInMessageEventArgs e)
        {
            if (!CaptureEnable) return;

            // Decode the message. We only care about a few.
            var mevt = MidiEvent.FromRawMessage(e.RawMessage);

            BaseMidiEvent evt = mevt switch
            {
                NoteOnEvent onevt => new NoteOn(onevt.Channel, onevt.NoteNumber, onevt.Velocity),
                NoteEvent offevt => offevt.Velocity == 0 ?
                    new NoteOff(offevt.Channel, offevt.NoteNumber) :
                    new NoteOn(offevt.Channel, offevt.NoteNumber, offevt.Velocity),
                ControlChangeEvent ctlevt => new Controller(ctlevt.Channel, (int)ctlevt.Controller, ctlevt.ControllerValue),
                _ => new BaseMidiEvent() // TODO2 just ignore? or ErrorInfo = $"Invalid message: {m}"
            };

            InputReceive?.Invoke(this, evt);
        }

        /// <summary>
        /// Process error midi event - parameter 1 is invalid. Do I care?
        /// </summary>
        void MidiIn_ErrorReceived(object? sender, MidiInMessageEventArgs e)
        {
            // TODO2 just ignore?
            // string ErrorInfo = $"Message:0x{e.RawMessage:X8}";
        }

        /// <summary>
        /// Get a list of available device names.
        /// </summary>
        /// <returns></returns>
        public static List<string> AvailableDevices()
        {
            List<string> devs = [];

            for (int i = 0; i < MidiIn.NumberOfDevices; i++)
            {
                devs.Add(MidiIn.DeviceInfo(i).ProductName);
            }

            return devs;
        }
    }

    /// <summary>
    /// A midi output device.
    /// </summary>
    public class MidiOutputDevice : IOutputDevice
    {
        #region Fields
        /// <summary>NAudio midi output device.</summary>
        readonly MidiOut? _midiOut = null;
        #endregion

        #region Properties
        /// <inheritdoc />
        public string DeviceName { get; }

        /// <inheritdoc />
        public int Id { get; }

        /// <inheritdoc />
        public bool Valid { get { return _midiOut is not null;} }
        #endregion

        #region Lifecycle
        /// <summary>
        /// Normal constructor. OK to throw in here.
        /// </summary>
        /// <param name="deviceName">Client must supply name of device.</param>
        public MidiOutputDevice(string deviceName)
        {
            // Figure out which midi output device.
            var devs = AvailableDevices();
            var ind = devs.IndexOf(deviceName);
            if (ind >= 0)
            {
                DeviceName = deviceName;
                Id = ind;
                _midiOut = new MidiOut(ind);
            }
            else
            {
                DeviceName = "";
                throw new MidiLibException($"Invalid output midi device name [{deviceName}]");
            }
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        public void Dispose()
        {
            // Resources.
            _midiOut?.Dispose();
        }
        #endregion

        /// <summary>
        /// Send midi event. TODO2 OK to throw in here.
        /// </summary>
        public void Send(BaseMidiEvent evt)
        {
            MidiEvent mevt = evt switch
            {
                NoteOn onevt => new NoteOnEvent(0, onevt.Channel, onevt.Note, onevt.Velocity, 0),
                NoteOff onevt => new NoteEvent(0, onevt.Channel, MidiCommandCode.NoteOff, onevt.Note, 0),
                Controller ctlevt => new ControlChangeEvent(0, ctlevt.Channel, (MidiController)ctlevt.ControllerId, ctlevt.Value),
                Patch pevt => new PatchChangeEvent(0, pevt.Channel, pevt.Value),
                _ => throw new MidiLibException($"Invalid event: {evt}")
            };

            _midiOut?.Send(mevt.GetAsShortMessage());
        }

        /// <summary>
        /// Get a list of available device names.
        /// </summary>
        /// <returns></returns>
        public static List<string> AvailableDevices()
        {
            List<string> devs = [];

            for (int i = 0; i < MidiOut.NumberOfDevices; i++)
            {
                devs.Add(MidiOut.DeviceInfo(i).ProductName);
            }

            return devs;
        }
    }
}
