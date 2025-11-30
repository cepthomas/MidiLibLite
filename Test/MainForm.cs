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
//using NAudio.Midi;
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
        readonly Dictionary<ChannelHandle, OutputChannel> _outputChannels = new();
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
            var chnd2 = CreateOutput("Microsoft GS Wavetable Synth", 1, "keys", 0); //AcousticGrandPiano);
            // local hnd_drums = api.open_midi_output("Microsoft GS Wavetable Synth", 10, "drums", kit.Jazz)
            var chnd3 = CreateOutput("Microsoft GS Wavetable Synth", 10, "drums", 32); // kit.Jazz);

            ///// 3 - create a channel control for each output channel and bind object
            CreateControls();

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
        /// Create controls.
        /// </summary>
        void CreateControls()
        {
            DestroyControls();

            foreach (var chout in _outputChannels)
            {
                ChannelControl cc = new();
                cc.DrawColor = Color.Red;
                cc.SelectedColor = Color.Moccasin;
                cc.Volume = 0.56;
                //cc.BoundChannel = chout;
                // cc.UpdatePresets();

                cc.ChannelChange += Cc_ChannelChange;
                cc.MidiSend += Cc_MidiSend;
            }

            //////////////// from Nebulua ////////////////

            // Create channels and controls. TODO1 pick over
            //List<ChannelHandle> valchs = [];
            //for (int devNum = 0; devNum < _outputDevices.Count; devNum++)
            //{
            //    var output = _outputDevices[devNum];
            //    output.Channels.ForEach(ch => { valchs.Add(new(devNum, ch.Key, Direction.Output)); });
            //}
            //valchs.ForEach(ch =>
            //{
            //    ChannelControl control = new(ch)
            //    {
            //        Location = new(x, y),
            //        Info = GetInfo(ch)
            //    };
            //    control.ChannelControlEvent += ChannelControlEvent;
            //    Controls.Add(control);
            //    _channelControls.Add(control);
            //    // Adjust positioning for next iteration.
            //    x += control.Width + 5;
            //});

            //// local func
            //List<string> GetInfo(ChannelHandle ch)
            //{
            //    string devName = "unknown";
            //    string chanName = "unknown";
            //    int patchNum = -1;
            //    if (ch.Direction == Direction.Output)
            //    {
            //        if (ch.DeviceId < _outputDevices.Count)
            //        {
            //            var dev = _outputDevices[ch.DeviceId];
            //            devName = dev.DeviceName;
            //            chanName = dev.Channels[ch.ChannelNumber].ChannelName;
            //            patchNum = dev.Channels[ch.ChannelNumber].Patch;
            //        }
            //    }
            //    else
            //    {
            //        if (ch.DeviceId < _inputDevices.Count)
            //        {
            //            var dev = _inputDevices[ch.DeviceId];
            //            devName = dev.DeviceName;
            //            chanName = dev.Channels[ch.ChannelNumber].ChannelName;
            //        }
            //    }
            //    List<string> ret = [];
            //    ret.Add($"{(ch.Direction == Direction.Output ? "output: " : "input: ")}:{chanName}");
            //    ret.Add($"device: {devName}");
            //    if (patchNum != -1)
            //    {
            //        // Determine patch name.
            //        string sname;
            //        if (ch.ChannelNumber == MidiDefs.DEFAULT_DRUM_CHANNEL)
            //        {
            //            sname = $"kit: {patchNum}";
            //            if (MidiDefs.DrumKits.TryGetValue(patchNum, out string? kitName))
            //            {
            //                sname += ($" {kitName}");
            //            }
            //        }
            //        else
            //        {
            //            sname = $"patch: {patchNum}";
            //            if (MidiDefs.Instruments.TryGetValue(patchNum, out string? patchName))
            //            {
            //                sname += ($" {patchName}");
            //            }
            //        }
            //        ret.Add(sname);
            //    }
            //    return ret;
            //}
        }


        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            DestroyDevices();

            if (disposing && (components is not null))
            {
                components.Dispose();
            }

            base.Dispose(disposing);
        }
        #endregion

        ///// <summary>
        ///// User clicked something. Send some midi.
        ///// </summary>
        ///// <param name="sender"></param>
        ///// <param name="e"></param>
        void Cc_MidiSend(object? sender, BaseXXX e)
        {
            var cc = sender as ChannelControl;
            var chan = cc!.BoundChannel!;

            if (chan.Enable)
            {
                //SendNote(cc!.BoundChannel.ChannelNumber, e.Note, e.Velocity);
                //SendController(cc!.BoundChannel.ChannelNumber, (MidiController)e.ControllerId, e.Value);
                //SendEvent(chnd.DeviceId, evt);
            }
        }

        /// <summary>
        /// The user clicked something in one of the channel controls.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Cc_ChannelChange(object? sender, ChannelChangeEventArgs e)
        {
            ChannelControl cc = (ChannelControl)sender!;
            OutputChannel channel = cc.BoundChannel;

            if (e.StateChange)
            {
                //// Update all channel enables.
                //bool anySolo = _channelControls.Where(c => c.State == PlayState.Solo).Any();

                //foreach (var c in _channelControls)
                //{
                //    bool enable = anySolo ? c.State == PlayState.Solo : c.State != PlayState.Mute;

                //    var ch = c.ChHandle;

                //    if (ch.DeviceId >= _outputDevices.Count)
                //    {
                //        throw new AppException($"Invalid device id [{ch.DeviceId}]");
                //    }

                //    var output = _outputDevices[ch.DeviceId];
                //    if (!output.Channels.TryGetValue(ch.ChannelNumber, out OutputChannel? value))
                //    {
                //        throw new AppException($"Invalid channel [{ch.ChannelNumber}]");
                //    }

                //    value.Enable = enable;
                //    if (!enable)
                //    {
                //        // Kill just in case.
                //        _outputDevices[ch.DeviceId].Send(new ControlChangeEvent(0, ch.ChannelNumber, MidiController.AllNotesOff, 0));
                //    }
                //}

                switch (cc.State)
                {
                    //case ChannelState.Normal:
                    //    break;

                    //case ChannelState.Solo:
                    //    // Mute any other non-solo channels.
                    //    _channels.Values.ForEach(ch =>
                    //    {
                    //        if (channel.ChannelNumber != ch.ChannelNumber && channel.State != ChannelState.Solo)
                    //        {
                    //            channel.Kill();
                    //        }
                    //    });
                    //    break;

                    //case ChannelState.Mute:
                    //    channel.Kill();
                    //    break;
                }
            }

            if (e.PatchChange && channel.Patch >= 0)
            {
                //SendPatch();
            }


            if (e.PresetFileChange)
            {
                // Update channel presets.
                cc!.BoundChannel.UpdatePresets();
            }
        }





        /// <summary>
        /// Send the event.
        /// </summary>
        /// <param name="evt"></param>
        void SendEvent(int devid, BaseXXX evt)
        {
            var mout = _outputDevices[devid];
            mout?.Send(evt);
            // mout?.Send(evt.GetAsShortMessage());
        }





        #region Script API requirements TODO1
        // Midi input arrived. This is on a system thread. Host => script API.
        void Midi_ReceiveEvent(object? sender, BaseXXX e)
        {
            var indev = (MidiInputDevice)sender!;

            //var chan = cc!.BoundChannel!;
            //if (chan.Enable)
            //{
            //    //SendNote(cc!.BoundChannel.ChannelNumber, e.Note, e.Velocity);
            //    //SendController(cc!.BoundChannel.ChannelNumber, (MidiController)e.ControllerId, e.Value);
            //    //SendEvent(chnd.DeviceId, evt);
            //}

            // Determine channel.
            ChannelHandle chnd = new(_inputDevices.IndexOf(indev), e.Channel, Direction.Input);

            // Do the work.
            //_interop.ReceiveMidiNote(chnd, evt.NoteNumber, (double)evt.Velocity / MidiDefs.MAX_MIDI);
            //_interop.ReceiveMidiNote(chnd, evt.NoteNumber, 0);
            //_interop.ReceiveMidiController(chnd, (int)evt.Controller, evt.ControllerValue);
        }
        
        ChannelHandle CreateInput(string deviceName, int channelNumber, string channelName) // Script => Host API
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

        ChannelHandle CreateOutput(string deviceName, int channelNumber, string channelName, int patch) // Script => Host API
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
                OutputChannel ch = new()
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
////                    SendPatch(chnd, patch);
                }
            }
            catch (AppException ex)
            {
                // was ProcessException(ex);
                throw new AppException("");
            }

            return chnd;
        }
        #endregion

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

        /// <summary>
        /// Destroy controls.
        /// </summary>
        void DestroyControls()
        {
            //KillAll();

            // Clean out our current elements.
            _channelControls.ForEach(c =>
            {
                Controls.Remove(c);
                c.Dispose();
            });
            _channelControls.Clear();
        }


        /// <summary>
        /// Tell me something good.
        /// </summary>
        /// <param name="s"></param>
        void Tell(string s)
        {
            txtViewer.AppendLine($"{s}");
        }
    }

    public class UserSettings : SettingsCore
    {
        ///// <summary>The current settings.</summary>
        //public static UserSettings Current { get; set; } = new();

        [Browsable(false)]
        public OutputChannel ClClChannel1 { get; set; } = new();
        [Browsable(false)]
        public OutputChannel ClClChannel2 { get; set; } = new();

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

    /// <summary>Sink that doesn't do anything.</summary>
    public class NullOutputDevice : IOutputDevice
    {
        public string DeviceName => nameof(NullOutputDevice);
        public bool Valid { get { return false; } }
        public bool LogEnable { get; set; }
        public void Dispose() { }
        public void Send(BaseXXX evt) { }
    }

    public class NullInputDevice : IInputDevice
    {
        public string DeviceName => nameof(NullOutputDevice);
        public bool Valid { get { return false; } }
        public bool LogEnable { get; set; }
        public void Dispose() { }

        //public void Send(BaseXXX evt) { }
        public bool CaptureEnable { get; set; }
        public event EventHandler<BaseXXX>? InputReceive;
    }
}
