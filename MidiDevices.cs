using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NAudio.Midi;
using Ephemera.NBagOfTricks;


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
        /// <summary>Device name as defined by the system.</summary>
        public string DeviceName { get; }

        public bool CaptureEnable { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public bool Valid => throw new NotImplementedException();

        public bool LogEnable { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        /// <summary>Info about device channels. Key is channel number, 1-based.</summary>
//        public Dictionary<int, Channel> Channels = []; //TODO1
        #endregion

        #region Events
        /// <summary>Client needs to deal with this.</summary>
        //public event EventHandler<MidiEvent>? ReceiveEvent;
        // public event EventHandler<MidiEventArgs>? InputReceive;
        //public event EventHandler<MidiEvent>? InputReceive;
        public event EventHandler<BaseXXX>? InputReceive;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Normal constructor.
        /// </summary>
        /// <param name="deviceName">Client must supply name of device.</param>
        public MidiInputDevice(string deviceName)
        {
            bool realInput = false;
            DeviceName = deviceName;

            // Figure out which midi input device.
            for (int i = 0; i < MidiIn.NumberOfDevices; i++)
            {
                if (deviceName == MidiIn.DeviceInfo(i).ProductName)
                {
                    _midiIn = new MidiIn(i);
                    _midiIn.MessageReceived += MidiIn_MessageReceived;
                    _midiIn.ErrorReceived += MidiIn_ErrorReceived;
                    _midiIn.Start();
                    realInput = true;
                    break;
                }
            }

            if (_midiIn is null)
            {
                // if (deviceName == "ccMidiGen") // Assume internal type.
                // {
                //     _midiIn = null;
                //     realInput = false;
                // }
                // else
                {
                    throw new MLLAppException($"Invalid input midi device name [{deviceName}]");
                }
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

        #region Traffic
        /// <summary>
        /// Process real midi input event. ???TODO2 Don't throw in this thread!
        /// </summary>
        void MidiIn_MessageReceived(object? sender, MidiInMessageEventArgs e)
        {
            // Decode the message. We only care about a few.
            var mevt = MidiEvent.FromRawMessage(e.RawMessage);

            BaseXXX evt = mevt switch
            {
                NoteOnEvent onevt => new NoteOnXXX
                {
                    Channel = onevt.Channel,
                    Note = onevt.NoteNumber,
                    Velocity = onevt.Velocity
                },
                NoteEvent offevt => offevt.Velocity == 0 ?
                    new NoteOffXXX
                    {
                        Channel = offevt.Channel,
                        Note = offevt.NoteNumber,
                    } :
                    new NoteOnXXX
                    {
                        Channel = offevt.Channel,
                        Note = offevt.NoteNumber,
                        Velocity = offevt.Velocity
                    },
                ControlChangeEvent ctlevt => new ControllerXXX
                {
                    Channel = ctlevt.Channel,
                    ControllerId = (int)ctlevt.Controller,
                    Value = ctlevt.ControllerValue
                },
                _ => new BaseXXX()
                {
                    // TODO2 just ignore?
                    //ErrorInfo = $"Invalid message: {m}"
                }
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
        #endregion
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
        /// <summary>Device name as defined by the system.</summary>
        public string DeviceName { get; }

        public bool Valid => throw new NotImplementedException();

        public bool LogEnable { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        /// <summary>Info about device channels. Key is channel number, 1-based.</summary>
//        public Dictionary<int, Channel> Channels = []; //TODO1
        #endregion

        #region Lifecycle
        /// <summary>
        /// Normal constructor. OK to throw in here.
        /// </summary>
        /// <param name="deviceName">Client must supply name of device.</param>
        /// <exception cref="LuaException"></exception>
        public MidiOutputDevice(string deviceName)
        {
            DeviceName = deviceName;

            // Figure out which midi output device.
            for (int i = 0; i < MidiOut.NumberOfDevices; i++)
            {
                if (deviceName == MidiOut.DeviceInfo(i).ProductName)
                {
                    _midiOut = new MidiOut(i);
                    break;
                }
            }

            if (_midiOut is null)
            {
                 throw new MLLAppException($"Invalid output midi device name [{deviceName}]");
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

        #region Traffic
        /// <summary>
        /// Send midi event. TODO2 OK to throw in here.
        /// </summary>
        public void Send(BaseXXX evt)
        {
            MidiEvent mevt = evt switch
            {
                NoteOnXXX onevt => new NoteOnEvent(0, onevt.Channel, onevt.Note, onevt.Velocity, 0),
                NoteOffXXX onevt => new NoteEvent(0, onevt.Channel,  MidiCommandCode.NoteOff, onevt.Note, 0),
                ControllerXXX ctlevt => new ControlChangeEvent(0, ctlevt.Channel, (MidiController)ctlevt.ControllerId, ctlevt.Value),
                _ => throw new MLLAppException($"Invalid event: {evt}")
            };

            _midiOut?.Send(mevt.GetAsShortMessage());
        }
        #endregion
    }
    
}
