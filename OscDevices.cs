using System;
using System.Collections.Generic;
using Ephemera.NBagOfTricks;


namespace Ephemera.MidiLibLite
{
    /// <summary>Provides midi over OSC. Server side.</summary>
    public sealed class OscInput : IInputDevice
    {
        #region Fields
        /// <summary>OSC input device.</summary>
        NebOsc.Input? _oscInput = null;
        #endregion

        #region Events
        /// <inheritdoc />
        public event EventHandler<BaseEvent>? InputReceive;
        #endregion

        #region Properties
        /// <inheritdoc />
        public string DeviceName { get; private set; } = "Invalid";

        /// <inheritdoc />
        public bool CaptureEnable { get; set; } = true; // default

        /// <inheritdoc />
        public bool Valid { get { return _oscInput is not null; } }

        /// <inheritdoc />
        public int Id => throw new NotImplementedException();
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor. Throws for invalid args etc.
        /// <param name="deviceName">Client must supply name of device.</param>
        /// </summary>
        public OscInput(string deviceName)
        {
            // Check for properly formed port.
            List<string> parts = deviceName.SplitByToken(":");
            if (parts.Count == 2)
            {
                if (int.TryParse(parts[1], out int port))
                {
                    _oscInput = new NebOsc.Input(port);
                    DeviceName = _oscInput.DeviceName;
                    _oscInput.InputReceived += OscInput_InputReceived;
                    _oscInput.Notification += OscInput_Notification;
                }
            }
        }

        /// <summary>
        /// Resource clean up.
        /// </summary>
        public void Dispose()
        {
            _oscInput?.Dispose();
            _oscInput = null;
        }
        #endregion

        #region Private functions
        /// <summary>
        /// OSC has something to say. Errors arrive here.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void OscInput_Notification(object? sender, NebOsc.NotificationEventArgs e)
        {
            if (e.IsError)
            {
                throw new MidiLibException($"OSC receive error: {e.Message}");
            }
            // else??? always error?
        }

        /// <summary>
        /// Handle a received message.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void OscInput_InputReceived(object? sender, NebOsc.InputReceiveEventArgs e)
        {
            if (!CaptureEnable) return;

            // message could be:
            // /noteon/ channel notenum vel
            // /noteoff/ channel notenum
            // /controller/ channel ctlnum val

            e.Messages.ForEach(m =>
            {
                BaseEvent evt = (m.Address, m.Data.Count) switch
                {
                    ("/noteon/", 3) => new NoteOn((int)m.Data[0], (int)m.Data[1], (int)m.Data[2]),
                    ("/noteoff/", 2) => new NoteOff((int)m.Data[0], (int)m.Data[1]),
                    ("/controller/", 3) => new Controller((int)m.Data[0], (int)m.Data[1], (int)m.Data[2]),
                    _ => new BaseEvent() // TODO2 just ignore? or throw new MidiLibException or  ErrorInfo = $"Invalid message: {m}"
                };

                InputReceive?.Invoke(this, evt);
             });
        }
        #endregion
    }

    /// <summary>Provides midi over OSC. Client side.</summary>
    public sealed class OscOutput : IOutputDevice
    {
        #region Fields
        /// <summary>OSC output device.</summary>
        NebOsc.Output? _oscOutput;

        /// <summary>Access synchronizer.</summary>
        readonly object _lock = new();
        #endregion

        #region Properties
        /// <inheritdoc />
        public string DeviceName { get; private set; } = "Invalid";

        /// <inheritdoc />
        public bool Valid { get { return _oscOutput is not null; } }

        /// <inheritdoc />
        public int Id => throw new NotImplementedException();
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor. Throws for invalid args etc.
        /// </summary>
        /// <param name="deviceName">Client must supply name of device.</param>
        public OscOutput(string deviceName)
        {
            // Check for properly formed url:port.
            List<string> parts = deviceName.SplitByToken(":");
            if (parts.Count == 3)
            {
                if (int.TryParse(parts[2], out int port))
                {
                    string ip = parts[1];
                    _oscOutput = new NebOsc.Output(ip, port);
                    DeviceName = _oscOutput.DeviceName;
                    _oscOutput.Notification += OscOutput_Notification;
                }
            }
        }

        /// <summary>
        /// Resource clean up.
        /// </summary>
        public void Dispose()
        {
            _oscOutput?.Dispose();
            _oscOutput = null;
        }
        #endregion

        #region Public functions
        /// <inheritdoc />
        public void Send(BaseEvent mevt)
        {
            // Critical code section.
            if (_oscOutput is not null)
            {
                lock (_lock)
                {
                    NebOsc.Message msg = null;

                    switch (mevt)
                    {
                        case NoteOn evt:
                            // /noteon/ channel notenum
                            msg = new NebOsc.Message() { Address = "/noteon" };
                            msg.Data.Add(evt.Channel);
                            msg.Data.Add(evt.Note);
                            msg.Data.Add(evt.Velocity);
                            break;

                        case NoteOff evt:// when evt.Velocity == 0: // aka NoteOff
                            // /noteoff/ channel notenum
                            msg = new NebOsc.Message() { Address = "/noteoff" };
                            msg.Data.Add(evt.Channel);
                            msg.Data.Add(evt.Note);
                            break;

                        case Controller evt:
                            // /controller/ channel ctlnum val
                            msg = new NebOsc.Message() { Address = "/controller" };
                            msg.Data.Add(evt.Channel);
                            msg.Data.Add(evt.ControllerId);
                            msg.Data.Add(evt.Value);
                            break;

                        case Patch evt:
                            // /patch/ channel patchnum
                            msg = new NebOsc.Message() { Address = "/patch" };
                            msg.Data.Add(evt.Channel);
                            msg.Data.Add(evt.Value);
                            break;

                        default:
                            // Unknown!
                            throw new MidiLibException($"Unknown event: {mevt}");
                    }

                    _oscOutput.Send(msg);
                }
            }
        }
        #endregion

        #region Private functions
        /// <summary>
        /// OSC has something to say. Errors arrive here.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void OscOutput_Notification(object? sender, NebOsc.NotificationEventArgs e)
        {
            if (e.IsError)
            {
                throw new MidiLibException($"OSC send error: {e.Message}");
            }
            // else??? always error?
        }
        #endregion
    }
}
