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
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using NAudio.Midi;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfUis;
using Ephemera.MidiLibLite;


namespace Ephemera.MidiLibLite.Test
{
    public partial class MainForm : Form
    {
        #region Fields
        /// <summary>All midi devices to use for send.</summary>
        //readonly Dictionary<int, IOutputDevice> _outputDevices = [];
        readonly List<IOutputDevice> _outputDevices = [];

        /// <summary>All midi devices to use for receive.</summary>
        //readonly Dictionary<int, IInputDevice> _inputDevices = [];
        readonly List<IInputDevice> _inputDevices = [];

        // /// <summary>All the channel play controls.</summary>
        // readonly List<ChannelControl> _channelControls = [];

        // /// <summary>Midi output.</summary>
        // IOutputDevice _outputDevice;// = new NullOutputDevice();

        // /// <summary>Midi input.</summary>
        // IInputDevice? _inputDevice;

        /// <summary>All the output channels - key is handle.</summary>
        readonly Dictionary<ChannelHandle, Channel> _outputChannels = new();
        /// <summary>All the output channels - key is user assigned name.</summary>
        // readonly Dictionary<string, Channel> _outputChannels = new();

        /// <summary>All the input channels - key is handle.</summary>
        readonly Dictionary<ChannelHandle, InputChannel> _inputChannels = new();
        /// <summary>All the intput channels - key is user assigned name.</summary>
        // readonly Dictionary<string, InputChannel> _inputChannels = new();

        /// <summary>All the output channel controls.</summary>
        readonly List<ChannelControl> _channelControls = new();

        /// <summary>Test settings.</summary>
        readonly UserSettings _settings = new();

        /// <summary>Interop serializing access.</summary>
        readonly object _lock = new();

        /// <summary>Cosmetics.</summary>
        readonly Color _controlColor = Color.Aquamarine;

        /// <summary>Where to put things.</summary>
        readonly string _outPath = @"..\..\out";
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// </summary>
        public MainForm()
        {
            InitializeComponent();

            // TODO1 init by code.
            _settings = (UserSettings)SettingsCore.Load(".", typeof(UserSettings));

            // Make sure out path exists.
            DirectoryInfo di = new(_outPath);
            di.Create();

            // The text output.
            txtViewer.Font = Font;
            txtViewer.WordWrap = true;
            txtViewer.MatchText.Add("ERR", Color.LightPink);
            txtViewer.MatchText.Add("WRN", Color.Plum);

            // UI configs.
            sldVolume.DrawColor = _controlColor;
            sldVolume.Minimum = 0.0;
            sldVolume.Maximum = Defs.MAX_VOLUME;
            sldVolume.Resolution = Defs.MAX_VOLUME / 50;
            sldVolume.Value = Defs.DEFAULT_VOLUME;
            sldVolume.Label = "volume";

            // Hook up some simple UI handlers.
            // btnKillMidi.Click += (_, __) => { _channels.Values.ForEach(ch => ch.Kill()); };
            // btnLogMidi.CheckedChanged += (_, __) => _outputDevice.LogEnable = btnLogMidi.Checked;
        }

        /// <summary>
        /// Window is set up now. OK to log!!
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            ///// 1 - create all devices
            bool ok = CreateDevices();

            ///// 2 - create all channels (explicit or from script api calls)
            // script api calls:
            // local hnd_ccin  = api.open_midi_input("loopMIDI Port 1", 1, "my input")
            var chnd1 = CreateInput("loopMIDI Port 1", 1, "my input");
            // local hnd_keys  = api.open_midi_output("Microsoft GS Wavetable Synth", 1,  "keys", inst.AcousticGrandPiano)
            var chnd2 = CreateOutput("Microsoft GS Wavetable Synth", 1,  "keys", 0); //AcousticGrandPiano);
            // local hnd_drums = api.open_midi_output("Microsoft GS Wavetable Synth", 10, "drums", kit.Jazz)
            var chnd3 = CreateOutput("Microsoft GS Wavetable Synth", 10, "drums", 32); // kit.Jazz);

            ///// 3 - create a channel control for each channel and bind object
            foreach (var chout in _outputChannels)
            {
                ChannelControl cc = new();
                // ClClChannel1.BoundChannel = _settings.ClClChannel;
                // ClClChannel1.ControlColor = _settings.ControlColor;
                // ClClChannel1.Enabled = true;

                // ClClChannel1.ChannelChange += User_ChannelChange;
                // ClClChannel1.NoteSend += User_NoteSend;
                // ClClChannel1.ControllerSend += User_ControllerSend;

                // _settings.ClClChannel.UpdatePresets();

            }

        /// <summary>
        /// The user clicked something in one of the channel controls.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        // void Control_ChannelChange(object? sender, ChannelChangeEventArgs e)
        // {
        //     ChannelControl cc = (ChannelControl)sender!;
        //     Channel channel = cc.BoundChannel;

        //     if (e.StateChange)
        //     {
        //         switch (cc.State)
        //         {
        //             case ChannelState.Normal:
        //                 break;

        //             case ChannelState.Solo:
        //                 // Mute any other non-solo channels.
        //                 _channels.Values.ForEach(ch =>
        //                 {
        //                     if (channel.ChannelNumber != ch.ChannelNumber && channel.State != ChannelState.Solo)
        //                     {
        //                         channel.Kill();
        //                     }
        //                 });
        //                 break;

        //             case ChannelState.Mute:
        //                 channel.Kill();
        //                 break;
        //         }
        //     }

        //     if (e.PatchChange && channel.Patch >= 0)
        //     {
        //         channel.SendPatch();
        //     }
        // }

        // /// <summary>
        // /// User edits the channel params.
        // /// </summary>
        // /// <param name="sender"></param>
        // /// <param name="e"></param>
        // void User_ChannelChange(object? sender, ChannelEventArgs e)
        // {
        //     var cc = sender as ChannelControl;
        //     if (e.PatchChange || e.ChannelNumberChange)
        //     {
        //         SendPatch(cc!.BoundChannel.ChannelNumber, cc.BoundChannel.Patch);
        //     }

        //     if (e.PresetFileChange)
        //     {
        //         // Update channel presets.
        //         cc!.BoundChannel.UpdatePresets();
        //     }
        // }



            // 4 - do work
            // api.set_volume(hnd_keys, 0.7)
            // api.send_midi_controller(hnd_synth, ctrl.Pan, 90)
            // api.send_midi_note(hnd_strings, note_num, volume)--, 0)
            // -- Handler for input note events. Optional.
            // function receive_midi_note(chan_hnd, note_num, volume)
            // -- Handlers for input controller events. Optional.
            // function receive_midi_controller(chan_hnd, controller, value)


            base.OnLoad(e);
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            LogManager.Stop();

            // Wait a bit in case there are some lingering events.
            System.Threading.Thread.Sleep(100);

            DestroyDevices();

            if (disposing && (components is not null))
            {
                components.Dispose();
            }

            base.Dispose(disposing);
        }
        #endregion


        // ddddddddddddddddddddddddd
        ChannelHandle CreateInput(string deviceName, int channelNumber, string channelName)
        {
            ChannelHandle chnd;

            // Check args.
            if (string.IsNullOrEmpty(deviceName))
            {
                throw new ArgumentException($"Invalid input midi device {deviceName ?? "null"}");
            }

            if (channelNumber < 1 || channelNumber > MidiDefs.NUM_CHANNELS)
            {
                throw new ArgumentException($"Invalid input midi channel {channelNumber}");
            }

            try
            {
                // Locate the device.
                var indev = _inputDevices.First(o => o.DeviceName == deviceName);
                // var indev = _inputDevices.FirstOrDefault(o => o.DeviceName == deviceName);
                if (indev is null)
                {
                    throw new AppException("throws if invalid");
                }

                // Add the channel.
                InputChannel ch = new()
                {
                    ChannelNumber = channelNumber,
                    ChannelName = channelName,
                    Enable = true,
                };

                //indev.Channels.Add(channelNumber, ch);
                chnd = new(_inputDevices.IndexOf(indev), channelNumber, Direction.Output);
                _inputChannels.Add(chnd, ch);
            }
            catch (AppException ex)
            {
                // was ProcessException(ex);
                throw new AppException("");
            }

            return chnd;
        }


        // ddddddddddddddddddddddddd
        ChannelHandle CreateOutput(string deviceName, int channelNumber, string channelName, int patch)
        {
            ChannelHandle chnd;

            // Check args.
            if (string.IsNullOrEmpty(deviceName))
            {
                throw new ArgumentException($"Invalid output midi device {deviceName ?? "null"}");
            }

            if (channelNumber < 1 || channelNumber > MidiDefs.NUM_CHANNELS)
            {
                throw new ArgumentException($"Invalid output midi channel {channelNumber}");
            }

            try
            {
                // Locate the device.
                var outdev = _outputDevices.First(o => o.DeviceName == deviceName);
                // var outdev = _outputDevices.FirstOrDefault(o => o.DeviceName == deviceName);
                if (outdev is null)
                {
                    throw new AppException("throws if invalid");
                }

                // Add the channel.
                Channel ch = new()
                {
                    ChannelNumber = channelNumber,
                    ChannelName = channelName,
                    Enable = true,
                    Patch = patch
                };

                //outdev.Channels.Add(channelNumber, ch);
                chnd = new(_outputDevices.IndexOf(outdev), channelNumber, Direction.Output);
                _outputChannels.Add(chnd, ch);

                // Send the patch now.
                if (patch >= 0)
                {
                    SendPatch(chnd, patch);
                }
            }
            catch (AppException ex)
            {
                // was ProcessException(ex);
                throw new AppException("");
            }

            return chnd;
        }



        #region Devices
        /// <summary>
        /// Create all I/O devices from user settings.
        /// </summary>
        /// <returns>Success</returns>
        bool CreateDevices()
        {
            bool ok = true;

            // First...
            DestroyDevices();

            // Set up input devices.
            foreach (var devname in _settings.MidiSettings.InputDevices)
            {
                var indev = new MidiInputDevice(devname);//TODO1 retry

                if (!indev.Valid)
                {
                    throw new AppException($"Something wrong with your input device:{devname}");
                    ok = false;
                }
                else
                {
                    indev.CaptureEnable = true;
                    indev.InputReceive += Midi_ReceiveEvent;
                    _inputDevices.Add(indev);
                }
            }

            // Set up output devices.
            foreach (var devname in _settings.MidiSettings.OutputDevices)
            {
                // Try midi.
                var outdev = new MidiOutputDevice(devname);//TODO1 retry
                if (!outdev.Valid)
                {
                    // _logger.Error($"Something wrong with your output device:{devname}");
                    ok = false;
                }
                else
                {
                    outdev.LogEnable = btnLogMidi.Checked;
                    _outputDevices.Add(outdev);
                }
            }

            return ok;
        }

        /// <summary>
        /// Clean up.
        /// </summary>
        void DestroyDevices()
        {
            _inputDevices.ForEach(d => d.Dispose());
            _inputDevices.Clear();
            _outputDevices.ForEach(d => d.Dispose());
            _outputDevices.Clear();
        }
        #endregion




        #region Midi Send
        /// <summary>
        /// 
        /// </summary>
        /// <param name="chan"></param>
        /// <param name="note"></param>
        /// <param name="velocity"></param>
        void SendNote(ChannelHandle chnd, int note, int velocity)
        {
           // _logger.Trace($"Note Ch:{chanNum} N:{note} V:{velocity}");
            NoteEvent evt = velocity > 0 ?
               new NoteOnEvent(0, chnd.ChannelNumber, note, velocity, 0) :
               new NoteEvent(0, chnd.ChannelNumber, MidiCommandCode.NoteOff, note, 0);
           SendEvent(chnd.DeviceId, evt);
        }

        /// <summary>
        /// Patch sender.
        /// </summary>
        void SendPatch(ChannelHandle chnd, int patch)
        {
           // _logger.Trace($"Patch Ch:{chanNum} P:{patch}");
            PatchChangeEvent evt = new(0, chnd.ChannelNumber, patch);
           SendEvent(chnd.DeviceId, evt);
        }

        /// <summary>
        /// Send a controller.
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="val"></param>
        void SendController(ChannelHandle chnd, MidiController controller, int val)
        {
           // _logger.Trace($"Controller Ch:{chanNum} C:{controller} V:{val}");
            ControlChangeEvent evt = new(0, chnd.ChannelNumber, controller, val);
           SendEvent(chnd.DeviceId, evt);
        }

        /// <summary>
        /// Send midi all notes off.
        /// </summary>
        void Kill(ChannelHandle chnd)
        {
            ControlChangeEvent evt = new(0, chnd.ChannelNumber, MidiController.AllNotesOff, 0);
            SendEvent(chnd.DeviceId, evt);
        }

        /// <summary>
        /// Send the event.
        /// </summary>
        /// <param name="evt"></param>
        void SendEvent(int devid, MidiEvent evt)
        {
            var mout = _outputDevices[devid];
            mout?.Send(evt);
            // mout?.Send(evt.GetAsShortMessage());
        }
        #endregion





////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////
//////////////////////////     LIB OK TO HERE    ///////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////




//////////////////////// from MidiGenerator ////////////////////////
        /// <summary>
        /// User clicked something. Send some midi.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void User_NoteSend(object? sender, NoteEventArgs e)
        {
            var cc = sender as ChannelControl;
            SendNote(cc!.BoundChannel.ChannelNumber, e.Note, e.Velocity);
        }

        /// <summary>
        /// User clicked something. Send some midi.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void User_ControllerSend(object? sender, ControllerEventArgs e)
        {
            var cc = sender as ChannelControl;
            SendController(cc!.BoundChannel.ChannelNumber, (MidiController)e.ControllerId, e.Value);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void LogMidi_Click(object? sender, EventArgs e)
        {
            _settings.LogMidi = btnLogMidi.Checked;
        }







        #region Utilities
        // /// <summary>
        // /// Show log events.
        // /// </summary>
        // /// <param name="sender"></param>
        // /// <param name="e"></param>
        // void LogManager_LogMessage(object? sender, LogMessageEventArgs e)
        // {
        //     // Usually come from a different thread.
        //     if (IsHandleCreated)
        //     {
        //         this.InvokeIfRequired(_ => { Tell($"{e.Message}"); });
        //     }
        // }

        #endregion

        #region Debug stuff
        // /// <summary>
        // /// 
        // /// </summary>
        // /// <param name="sender"></param>
        // /// <param name="e"></param>
        // void Settings_Click(object sender, EventArgs e)
        // {
        //     SettingsEditor.Edit(_settings, "howdy!!!", 400);
        //     _settings.Save();
        //     _logger.Warn("You better restart!");
        // }
        #endregion



//////////////////////////////////// from Nebulua /////////////////////////////////////
        /// <summary>
        /// Midi input arrived. This is on a system thread.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Midi_ReceiveEvent(object? sender, MidiEvent e)
        {
           // // _logger.Trace($"Listener:{sender} Note:{e.Note} Controller:{e.Controller} Value:{e.Value}");
           // // Translate and pass to output.
           // var channel = _channels["chan16"];
           // NoteEvent nevt = e.Value > 0 ?
           //    new NoteOnEvent(0, channel.ChannelNumber, e.Note % MidiDefs.MAX_MIDI, e.Value % MidiDefs.MAX_MIDI, 0) :
           //    new NoteEvent(0, channel.ChannelNumber, MidiCommandCode.NoteOff, e.Note, 0);
           // channel.SendEvent(nevt);



            lock (_lock)
            {
                try
                {
                    if (string.IsNullOrEmpty(Thread.CurrentThread.Name))
                    {
                        Thread.CurrentThread.Name = "MIDI_RCV";
                    }

                    var input = (MidiInputDevice)sender!;
                    ChannelHandle ch = new(_inputDevices.IndexOf(input), e.Channel, Direction.Input);
                    int chanHnd = ch;
                    bool logit = true;

                    // Do the work.
                    switch (e)
                    {
                        case NoteOnEvent evt:
                            _interop.ReceiveMidiNote(chanHnd, evt.NoteNumber, (double)evt.Velocity / MidiDefs.MAX_MIDI);
                            break;

                        case NoteEvent evt:
                            _interop.ReceiveMidiNote(chanHnd, evt.NoteNumber, 0);
                            break;

                        case ControlChangeEvent evt:
                            _interop.ReceiveMidiController(chanHnd, (int)evt.Controller, evt.ControllerValue);
                            break;

                        default: // Ignore others for now.
                            logit = false;
                            break;
                    }

                    if (logit && UserSettings.Current.MonitorRcv)
                    {
                        // _loggerMidi.Trace($"<<< {FormatMidiEvent(e, CurrentState == ExecState.Run ? State.Instance.CurrentTick : 0, chanHnd)}");
                    }
                }
                catch (Exception ex)
                {
                    // ProcessException(ex);
                }
            }
        }

        /// <summary>
        /// Process UI change.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ChannelControlEvent(object? sender, ChannelEventArgs e)
        {
            ChannelControl control = (ChannelControl)sender!;

            // Update all channel enables.
            bool anySolo = _channelControls.Where(c => c.State == PlayState.Solo).Any();

            foreach (var c in _channelControls)
            {
                bool enable = anySolo ? c.State == PlayState.Solo : c.State != PlayState.Mute;

                var ch = c.ChHandle;

                if (ch.DeviceId >= _outputDevices.Count)
                {
                    throw new AppException($"Invalid device id [{ch.DeviceId}]");
                }

                var output = _outputDevices[ch.DeviceId];
                if (!output.Channels.TryGetValue(ch.ChannelNumber, out Channel? value))
                {
                    throw new AppException($"Invalid channel [{ch.ChannelNumber}]");
                }

                value.Enable = enable;
                if (!enable)
                {
                    // Kill just in case.
                    _outputDevices[ch.DeviceId].Send(new ControlChangeEvent(0, ch.ChannelNumber, MidiController.AllNotesOff, 0));
                }
            };
        }



////////////////////////// interop functions from Nebulua /////////////////////////////////////
        /// <summary>
        /// Script wants to send a midi note. Doesn't throw.
        /// </summary>
        /// <param name="_"></param>
        /// <param name="e"></param>
        void Interop_SendMidiNote(object? _, SendMidiNoteArgs e)
        {
            e.ret = 0; // not used

            // Check args for valid device and channel.
            ChannelHandle ch = new(e.chan_hnd);

            if (ch.DeviceId >= _outputDevices.Count ||
                ch.ChannelNumber < 1 ||
                ch.ChannelNumber > MidiDefs.NUM_CHANNELS)
            {
                // _loggerScr.Warn($"Invalid channel {e.chan_hnd}");
                return;
            }

//            Trace($"+++ Interop_SendMidiNote() [{Thread.CurrentThread.Name}] ({Environment.CurrentManagedThreadId})");

            // Sound or quiet?
            var output = _outputDevices[ch.DeviceId];
            if (output.Channels[ch.ChannelNumber].Enable)
            {
                int note_num = MathUtils.Constrain(e.note_num, MidiDefs.MIN_MIDI, MidiDefs.MAX_MIDI);

                // Check for note off.
                var vol = e.volume * State.Instance.Volume;
                int vel = vol == 0.0 ? 0 : MathUtils.Constrain((int)(vol * MidiDefs.MAX_MIDI), MidiDefs.MIN_MIDI, MidiDefs.MAX_MIDI);
                MidiEvent evt = vel == 0?
                    new NoteEvent(0, ch.ChannelNumber, MidiCommandCode.NoteOff, note_num, 0) :
                    new NoteEvent(0, ch.ChannelNumber, MidiCommandCode.NoteOn, note_num, vel);

                output.Send(evt);

                if (UserSettings.Current.MonitorSnd)
                {
                    // _loggerMidi.Trace($">>> {FormatMidiEvent(evt, CurrentState == ExecState.Run ? State.Instance.CurrentTick : 0, e.chan_hnd)}");
                }
            }
        }

        /// <summary>
        /// Script wants to send a midi controller. Doesn't throw.
        /// </summary>
        /// <param name="_"></param>
        /// <param name="e"></param>
        void Interop_SendMidiController(object? _, SendMidiControllerArgs e)
        {
            e.ret = 0; // not used

            // Check args.
            ChannelHandle ch = new(e.chan_hnd);

            if (ch.DeviceId >= _outputDevices.Count ||
                ch.ChannelNumber < 1 ||
                ch.ChannelNumber > MidiDefs.NUM_CHANNELS)
            {
                _loggerScr.Warn($"Invalid channel {e.chan_hnd}");
                // return;
            }

            int controller = MathUtils.Constrain(e.controller, MidiDefs.MIN_MIDI, MidiDefs.MAX_MIDI);
            int value = MathUtils.Constrain(e.value, MidiDefs.MIN_MIDI, MidiDefs.MAX_MIDI);

            var output = _outputDevices[ch.DeviceId];
            MidiEvent evt;

            evt = new ControlChangeEvent(0, ch.ChannelNumber, (MidiController)controller, value);

            output.Send(evt);

            if (UserSettings.Current.MonitorSnd)
            {
                // _loggerMidi.Trace($">>> {FormatMidiEvent(evt, CurrentState == ExecState.Run ? State.Instance.CurrentTick : 0, e.chan_hnd)}");
            }
        }
        #endregion



        /// <summary>
        /// Tell me something good.
        /// </summary>
        /// <param name="s"></param>
        void Tell(string s)
        {
            txtViewer.AppendLine($"{s}");
        }



//////////////////////////////////// from Nebulua /////////////////////////////////////
        /// <summary>
        /// Destroy controls.
        /// </summary>
        void DestroyControls()
        {
            KillAll();

            // Clean out our current elements.
            _channelControls.ForEach(c =>
            {
                Controls.Remove(c);
                c.Dispose();
            });
            _channelControls.Clear();
        }

        /// <summary>
        /// Create controls.
        /// </summary>
        void CreateControls()
        {
            DestroyControls();

            // Create channels and controls.

            List<ChannelHandle> valchs = [];
            for (int devNum = 0; devNum < _outputDevices.Count; devNum++)
            {
                var output = _outputDevices[devNum];
                output.Channels.ForEach(ch => { valchs.Add(new(devNum, ch.Key, Direction.Output)); });
            }

            valchs.ForEach(ch =>
            {
                ChannelControl control = new(ch)
                {
                    Location = new(x, y),
                    Info = GetInfo(ch)
                };

                control.ChannelControlEvent += ChannelControlEvent;
                Controls.Add(control);
                _channelControls.Add(control);

                // Adjust positioning for next iteration.
                x += control.Width + 5;
            });


            // local func
            List<string> GetInfo(ChannelHandle ch)
            {
                string devName = "unknown";
                string chanName = "unknown";
                int patchNum = -1;

                if (ch.Direction == Direction.Output)
                {
                    if (ch.DeviceId < _outputDevices.Count)
                    {
                        var dev = _outputDevices[ch.DeviceId];
                        devName = dev.DeviceName;
                        chanName = dev.Channels[ch.ChannelNumber].ChannelName;
                        patchNum = dev.Channels[ch.ChannelNumber].Patch;
                    }
                }
                else
                {
                    if (ch.DeviceId < _inputDevices.Count)
                    {
                        var dev = _inputDevices[ch.DeviceId];
                        devName = dev.DeviceName;
                        chanName = dev.Channels[ch.ChannelNumber].ChannelName;
                    }
                }

                List<string> ret = [];
                ret.Add($"{(ch.Direction == Direction.Output ? "output: " : "input: ")}:{chanName}");
                ret.Add($"device: {devName}");

                if (patchNum != -1)
                {
                    // Determine patch name.
                    string sname;
                    if (ch.ChannelNumber == MidiDefs.DEFAULT_DRUM_CHANNEL)
                    {
                        sname = $"kit: {patchNum}";
                        if (MidiDefs.DrumKits.TryGetValue(patchNum, out string? kitName))
                        {
                            sname += ($" {kitName}");
                        }
                    }
                    else
                    {
                        sname = $"patch: {patchNum}";
                        if (MidiDefs.Instruments.TryGetValue(patchNum, out string? patchName))
                        {
                            sname += ($" {patchName}");
                        }
                    }

                    ret.Add(sname);
                }

                return ret;
            }
        }







//////////////////////// from MidiGenerator ////////////////////////
        /// <summary>
        /// Figure out which midi output device.
        /// </summary>
        void ConnectDevice()
        {
            if (_midiOut == null)
            {
                // Retry.
                string deviceName = _settings.OutputDevice;
                for (int i = 0; i < MidiOut.NumberOfDevices; i++)
                {
                    if (deviceName == MidiOut.DeviceInfo(i).ProductName)
                    {
                        _midiOut = new MidiOut(i);
                        Text = $"Midi Generator - {deviceName}";
                        Tell($"Connect to {deviceName}");
                        timer1.Stop();
                        break;
                    }
                }
            }
        }
        #endregion
    }



    public class UserSettings : SettingsCore
    {
        ///// <summary>The current settings.</summary>
        //public static UserSettings Current { get; set; } = new();

        [Browsable(false)]
        public Channel ClClChannel1 { get; set; } = new();
        [Browsable(false)]
        public Channel ClClChannel2 { get; set; } = new();

        [DisplayName("Ignore Me")]
        [Description("I do nothing.")]
        [Browsable(true)]
        public bool IgnoreMe { get; set; } = true;

        [DisplayName("Midi Settings")]
        [Description("Edit midi settings.")]
        [Browsable(true)]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public MidiSettings MidiSettings { get; set; } = new();
    }
}
