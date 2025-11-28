using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using NAudio.Midi;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfUis;

using Ephemera.MidiLibLite;


// 1 - create all devices
// 2 - create all channels (explicit or from script api calls)
// 3 - create a channel control for each channel and bind object
//
// script api calls:
// local midi_device_in  = "loopMIDI Port 1"
// local hnd_ccin  = api.open_midi_input(midi_device_in, 1, midi_device_in)
// local midi_device_out    = "Microsoft GS Wavetable Synth" 
// local hnd_keys    = api.open_midi_output(midi_device_out, 1,  "keys",       inst.AcousticGrandPiano)
// local hnd_drums   = api.open_midi_output(midi_device_out, 10, "drums",      kit.Jazz)


namespace Ephemera.MidiLibLite.Test
{
    public partial class MainForm : Form
    {
        #region Persisted non-editable properties


//////////////////////////////////// from Nebulua /////////////////////////////////////
        /// <summary>All midi devices to use for send.</summary>
        readonly List<IOutputDevice> _outputs = [];

        /// <summary>All midi devices to use for receive.</summary>
        readonly List<IInputDevice> _inputs = [];



        // /// <summary>All the channel play controls.</summary>
        // readonly List<ChannelControl> _channelControls = [];

        // /// <summary>Midi output.</summary>
        // IOutputDevice _outputDevice;// = new NullOutputDevice();

        // /// <summary>Midi input.</summary>
        // IInputDevice? _inputDevice;



        #region Fields - internal
        /// <summary>All the channels - key is user assigned name.</summary>
        readonly Dictionary<string, Channel> _channels = new();

        /// <summary>All the channel play controls.</summary>
        readonly List<ChannelControl> _channelControls = new();

        /// <summary>My logging.</summary>
        readonly Logger _logger = LogManager.CreateLogger("MainForm");

        /// <summary>Test stuff.</summary>
        readonly TestSettings _settings = new();
        #endregion

        #region Fields - adjust to taste
        /// <summary>Cosmetics.</summary>
        readonly Color _controlColor = Color.Aquamarine;

        /// <summary>Where to put things.</summary>
        readonly string _outPath = @"..\..\out";
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor. No logging allowed yet!
        /// </summary>
        public MainForm()
        {
            // Must do this first before initializing.
            _settings = (TestSettings)SettingsCore.Load(".", typeof(TestSettings));
            InitializeComponent();

            // Make sure out path exists.
            DirectoryInfo di = new(_outPath);
            di.Create();

            // Logger. Note: you can create this here but don't call any _logger functions until loaded.
            LogManager.MinLevelFile = LogLevel.Trace;
            LogManager.MinLevelNotif = LogLevel.Trace;
            LogManager.LogMessage += LogManager_LogMessage;
            LogManager.Run(Path.Join(_outPath, "log.txt"), 100000);

            // The text output.
            txtViewer.Font = Font;
            txtViewer.WordWrap = true;
            txtViewer.MatchText.Add("ERR", Color.LightPink);
            txtViewer.MatchText.Add("WRN", Color.Plum);

            // Hook up some simple UI handlers.
//            btnKillMidi.Click += (_, __) => { _channels.Values.ForEach(ch => ch.Kill()); };
//            btnLogMidi.CheckedChanged += (_, __) => _outputDevice.LogEnable = btnLogMidi.Checked;
        }

        /// <summary>
        /// Window is set up now. OK to log!!
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            // UI configs.
            sldVolume.DrawColor = _controlColor;
            sldVolume.Minimum = 0.0;
            sldVolume.Maximum = Defs.MAX_VOLUME;
            sldVolume.Resolution = Defs.MAX_VOLUME / 50;
            sldVolume.Value = Defs.DEFAULT_VOLUME;
            sldVolume.Label = "volume";

            bool ok = CreateDevices();



//////////////////////// TODO1 from MidiGenerator ////////////////////////
            // /// Init the channels and their corresponding controls. /////
            // _settings.ClClChannel.UpdatePresets();
            // ClClChannel1.BoundChannel = _settings.ClClChannel;
            // ClClChannel1.ControlColor = _settings.ControlColor;
            // ClClChannel1.Enabled = true;
            // ClClChannel1.ChannelChange += User_ChannelChange;
            // ClClChannel1.NoteSend += User_NoteSend;
            // ClClChannel1.ControllerSend += User_ControllerSend;

            // ///// Finish up. /////
            // SendPatch(VkeyControl.BoundChannel.ChannelNumber, VkeyControl.BoundChannel.Patch);
            // SendPatch(ClClControl.BoundChannel.ChannelNumber, ClClControl.BoundChannel.Patch);


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

        #region Devices
        /// <summary>
        /// Create all I/O devices from user settings.
        /// </summary>
        /// <returns>Success</returns>
        bool CreateDevices() //TODO1
        {
            bool ok = true;

            // First...
            DestroyDevices();

            // Set up input device.
            foreach (var devname in _settings.MidiSettings.InputDevices)
            {
                var indev = new MidiInputDevice(devname);

                if (!indev.Valid)
                {
                    _logger.Error($"Something wrong with your input device:{devname}");
                    ok = false;
                }
                else
                {
                    indev.CaptureEnable = true;
                    indev.InputReceive += Listener_InputReceive;
                    _inputs.Add(indev);
                }
            }

            // Set up output device.
            foreach (var devname in _settings.MidiSettings.OutputDevices)
            {
                // Try midi.
                var outdev = new MidiOutputDevice(devname);
                if (!outdev.Valid)
                {
                    _logger.Error($"Something wrong with your output device:{devname}");
                    ok = false;
                }
                else
                {
                    outdev.LogEnable = btnLogMidi.Checked;
                    _outputs.Add(outdev);
                }
            }

            return ok;
        }

        /// <summary>
        /// Clean up.
        /// </summary>
        void DestroyDevices()
        {
            _inputs.ForEach(d => d.Dispose());
            _inputs.Clear();
            _outputs.ForEach(d => d.Dispose());
            _outputs.Clear();
        }
        #endregion



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

        /// <summary>
        /// User edits the channel params.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void User_ChannelChange(object? sender, ChannelEventArgs e)
        {
            var cc = sender as ChannelControl;
            if (e.PatchChange || e.ChannelNumberChange)
            {
                SendPatch(cc!.BoundChannel.ChannelNumber, cc.BoundChannel.Patch);
            }

            if (e.PresetFileChange)
            {
                // Update channel presets.
                cc!.BoundChannel.UpdatePresets();
            }
        }



        #region Utilities
        /// <summary>
        /// Show log events.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void LogManager_LogMessage(object? sender, LogMessageEventArgs e)
        {
            // Usually come from a different thread.
            if (IsHandleCreated)
            {
                this.InvokeIfRequired(_ => { Tell($"{e.Message}"); });
            }
        }

        #endregion

        #region Debug stuff
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Settings_Click(object sender, EventArgs e)
        {
            SettingsEditor.Edit(_settings, "howdy!!!", 400);
            _settings.Save();
            _logger.Warn("You better restart!");
        }
        #endregion



//////////////////////////////////// from Nebulua /////////////////////////////////////
        /// <summary>
        /// Midi input arrived. This is on a system thread.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Listener_InputReceive(object? sender, InputReceiveEventArgs e)
        {
           _logger.Trace($"Listener:{sender} Note:{e.Note} Controller:{e.Controller} Value:{e.Value}");

           // Translate and pass to output.
           var channel = _channels["chan16"];
           NoteEvent nevt = e.Value > 0 ?
              new NoteOnEvent(0, channel.ChannelNumber, e.Note % MidiDefs.MAX_MIDI, e.Value % MidiDefs.MAX_MIDI, 0) :
              new NoteEvent(0, channel.ChannelNumber, MidiCommandCode.NoteOff, e.Note, 0);
           channel.SendEvent(nevt);
        }
        void Midi_ReceiveEvent(object? sender, MidiEvent e)
        {
            lock (_interopLock)
            {
                try
                {
                    if (string.IsNullOrEmpty(Thread.CurrentThread.Name))
                    {
                        Thread.CurrentThread.Name = "MIDI_RCV";
                    }

                    var input = (MidiInputDevice)sender!;
                    ChannelHandle ch = new(_inputs.IndexOf(input), e.Channel, Direction.Input);
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
        void ChannelControlEvent(object? sender, ChannelControlEventArgs e)
        {
            ChannelControl control = (ChannelControl)sender!;

            // Update all channel enables.
            bool anySolo = _channelControls.Where(c => c.State == PlayState.Solo).Any();

            foreach (var c in _channelControls)
            {
                bool enable = anySolo ? c.State == PlayState.Solo : c.State != PlayState.Mute;

                var ch = c.ChHandle;

                if (ch.DeviceId >= _outputs.Count)
                {
                    throw new AppException($"Invalid device id [{ch.DeviceId}]");
                }

                var output = _outputs[ch.DeviceId];
                if (!output.Channels.TryGetValue(ch.ChannelNumber, out Channel? value))
                {
                    throw new AppException($"Invalid channel [{ch.ChannelNumber}]");
                }

                value.Enable = enable;
                if (!enable)
                {
                    // Kill just in case.
                    _outputs[ch.DeviceId].Send(new ControlChangeEvent(0, ch.ChannelNumber, MidiController.AllNotesOff, 0));
                }
            };
        }



//////////////////////////////////// from Nebulua /////////////////////////////////////
        #region Script ==> Host Functions
        /// <summary>
        /// Script creates an input channel.
        /// </summary>
        /// <param name="_"></param>
        /// <param name="e"></param>
        /// <exception cref="AppException">From called functions</exception>
        void Interop_OpenMidiInput(object? _, OpenMidiInputArgs e)
        {
            e.ret = -1; // default is invalid

            // Check args.
            if (string.IsNullOrEmpty(e.dev_name))
            {
                _loggerScr.Warn($"Invalid input midi device {e.dev_name}");
                return;
            }

            if (e.chan_num < 1 || e.chan_num > MidiDefs.NUM_CHANNELS)
            {
                _loggerScr.Warn($"Invalid input midi channel {e.chan_num}");
                return;
            }

            try
            {
                // Locate or create the device.
                var input = _inputs.FirstOrDefault(o => o.DeviceName == e.dev_name);
                if (input is null)
                {
                    input = new(e.dev_name); // throws if invalid
                    input.ReceiveEvent += Midi_ReceiveEvent;
                    _inputs.Add(input);
                }

                Channel ch = new() { ChannelName = e.chan_name, Enable = true };
                input.Channels.Add(e.chan_num, ch);

                ChannelHandle chHnd = new(_inputs.Count - 1, e.chan_num, Direction.Input);
                e.ret = chHnd;
            }
            catch (AppException ex)
            {
                ProcessException(ex);
            }
        }

        /// <summary>
        /// Script creates an output channel.
        /// </summary>
        /// <param name="_"></param>
        /// <param name="e"></param>
        /// <exception cref="AppException">From called functions</exception>
        void Interop_OpenMidiOutput(object? _, OpenMidiOutputArgs e)
        {
            e.ret = -1; // default is invalid

            // Check args.
            if (string.IsNullOrEmpty(e.dev_name))
            {
                _loggerScr.Warn($"Invalid output midi device {e.dev_name ?? "null"}");
                return;
            }

            if (e.chan_num < 1 || e.chan_num > MidiDefs.NUM_CHANNELS)
            {
                _loggerScr.Warn($"Invalid output midi channel {e.chan_num}");
                return;
            }

            try
            {
                // Locate or create the device.
                var output = _outputs.FirstOrDefault(o => o.DeviceName == e.dev_name);
                if (output is null)
                {
                    output = new(e.dev_name); // throws if invalid
                    _outputs.Add(output);
                }

                // Add specific channel.
                Channel ch = new() { ChannelName = e.chan_name, Enable = true, Patch = e.patch };
                output.Channels.Add(e.chan_num, ch);

                ChannelHandle chHnd = new(_outputs.Count - 1, e.chan_num, Direction.Output);
                e.ret = chHnd;

                if (e.patch >= 0)
                {
                    // Send the patch now.
                    PatchChangeEvent pevt = new(0, e.chan_num, e.patch);
                    output.Send(pevt);
                    output.Channels[e.chan_num].Patch = e.patch;
                }
            }
            catch (AppException ex)
            {
                ProcessException(ex);
            }
        }

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

            if (ch.DeviceId >= _outputs.Count ||
                ch.ChannelNumber < 1 ||
                ch.ChannelNumber > MidiDefs.NUM_CHANNELS)
            {
                _loggerScr.Warn($"Invalid channel {e.chan_hnd}");
                return;
            }

//            Trace($"+++ Interop_SendMidiNote() [{Thread.CurrentThread.Name}] ({Environment.CurrentManagedThreadId})");

            // Sound or quiet?
            var output = _outputs[ch.DeviceId];
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
                    _loggerMidi.Trace($">>> {FormatMidiEvent(evt, CurrentState == ExecState.Run ? State.Instance.CurrentTick : 0, e.chan_hnd)}");
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

            if (ch.DeviceId >= _outputs.Count ||
                ch.ChannelNumber < 1 ||
                ch.ChannelNumber > MidiDefs.NUM_CHANNELS)
            {
                _loggerScr.Warn($"Invalid channel {e.chan_hnd}");
                return;
            }

            int controller = MathUtils.Constrain(e.controller, MidiDefs.MIN_MIDI, MidiDefs.MAX_MIDI);
            int value = MathUtils.Constrain(e.value, MidiDefs.MIN_MIDI, MidiDefs.MAX_MIDI);

            var output = _outputs[ch.DeviceId];
            MidiEvent evt;

            evt = new ControlChangeEvent(0, ch.ChannelNumber, (MidiController)controller, value);

            output.Send(evt);

            if (UserSettings.Current.MonitorSnd)
            {
                _loggerMidi.Trace($">>> {FormatMidiEvent(evt, CurrentState == ExecState.Run ? State.Instance.CurrentTick : 0, e.chan_hnd)}");
            }
        }

        /// <summary>
        /// Script wants to log something. Doesn't throw.
        /// </summary>
        /// <param name="_"></param>
        /// <param name="e"></param>
        void Interop_Log(object? _, LogArgs e)
        {
            e.ret = 0; // not used

            if (e.level < (int)LogLevel.Trace || e.level > (int)LogLevel.Error)
            {
                e.ret = -1;
                _loggerScr.Warn($"Invalid log level {e.level}");
                e.level = (int)LogLevel.Warn;
            }

            string s = $"{e.msg ?? "null"}";
            switch ((LogLevel)e.level)
            {
                case LogLevel.Trace: _loggerScr.Trace(s); break;
                case LogLevel.Debug: _loggerScr.Debug(s); break;
                case LogLevel.Info: _loggerScr.Info(s); break;
                case LogLevel.Warn: _loggerScr.Warn(s); break;
                case LogLevel.Error: _loggerScr.Error(s); break;
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

//////////////////////////////////// from Nebulua /////////////////////////////////////
        /// <summary>
        /// Create controls.
        /// </summary>
        void CreateControls()
        {
            DestroyControls();

            // Create channels and controls.

            List<ChannelHandle> valchs = [];
            for (int devNum = 0; devNum < _outputs.Count; devNum++)
            {
                var output = _outputs[devNum];
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
                    if (ch.DeviceId < _outputs.Count)
                    {
                        var dev = _outputs[ch.DeviceId];
                        devName = dev.DeviceName;
                        chanName = dev.Channels[ch.ChannelNumber].ChannelName;
                        patchNum = dev.Channels[ch.ChannelNumber].Patch;
                    }
                }
                else
                {
                    if (ch.DeviceId < _inputs.Count)
                    {
                        var dev = _inputs[ch.DeviceId];
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
        /// User clicked something. Send some midi.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void User_NoteSend(object? sender, NoteEventArgs e)
        {
            var cc = sender as ChannelControl;
            SendNote(cc!.BoundChannel.ChannelNumber, e.Note, e.Velocity);
        }

//////////////////////// from MidiGenerator ////////////////////////
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


//////////////////////// from MidiGenerator ////////////////////////
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void LogMidi_Click(object? sender, EventArgs e)
        {
            _settings.LogMidi = btnLogMidi.Checked;
        }



//////////////////////// from MidiGenerator ////////////////////////
        /// <summary>
        /// 
        /// </summary>
        /// <param name="chan"></param>
        /// <param name="note"></param>
        /// <param name="velocity"></param>
        void SendNote(int chanNum, int note, int velocity)
        {
            _logger.Trace($"Note Ch:{chanNum} N:{note} V:{velocity}");
            NoteEvent evt = velocity > 0 ?
               new NoteOnEvent(0, chanNum, note, velocity, 0) :
               new NoteEvent(0, chanNum, MidiCommandCode.NoteOff, note, 0);
           SendEvent(evt);
        }

//////////////////////// from MidiGenerator ////////////////////////
        /// <summary>
        /// Patch sender.
        /// </summary>
        void SendPatch(int chanNum, int patch)
        {
            _logger.Trace($"Patch Ch:{chanNum} P:{patch}");
            PatchChangeEvent evt = new(0, chanNum, patch);
           SendEvent(evt);
        }

//////////////////////// from MidiGenerator ////////////////////////
        /// <summary>
        /// Send a controller.
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="val"></param>
        void SendController(int chanNum, MidiController controller, int val)
        {
            _logger.Trace($"Controller Ch:{chanNum} C:{controller} V:{val}");
            ControlChangeEvent evt = new(0, chanNum, controller, val);
           SendEvent(evt);
        }

//////////////////////// from MidiGenerator ////////////////////////
        /// <summary>
        /// Send midi all notes off.
        /// </summary>
        void Kill(int chanNum)
        {
            ControlChangeEvent evt = new(0, chanNum, MidiController.AllNotesOff, 0);
            SendEvent(evt);
        }

//////////////////////// from MidiGenerator ////////////////////////
        /// <summary>
        /// Send the event.
        /// </summary>
        /// <param name="evt"></param>
        void SendEvent(MidiEvent evt)
        {
            _midiOut?.Send(evt.GetAsShortMessage());
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



    public class TestSettings : SettingsCore
    {
        ///// <summary>The current settings.</summary>
        //public static TestSettings Current { get; set; } = new();

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
