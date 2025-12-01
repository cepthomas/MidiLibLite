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
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfUis;
using Ephemera.MidiLibLite;


namespace Ephemera.MidiLibLite.Test
{
    /// <summary>Application level error.</summary>
    public class AppException(string message) : Exception(message) { }

    public partial class MainForm : Form
    {
        #region Fields
        /// <summary>All midi devices to use for send.</summary>
        readonly List<IOutputDevice> _outputDevices = [];
        //readonly Dictionary<int, IOutputDevice> _outputDevices = [];

        /// <summary>All midi devices to use for receive.</summary>
        readonly List<IInputDevice> _inputDevices = [];
        //readonly Dictionary<int, IInputDevice> _inputDevices = [];

        /// <summary>All the output channels.</summary>
        readonly List<OutputChannel> _outputChannels = new();

        /// <summary>All the output channels - key is handle.</summary>
        //readonly Dictionary<ChannelHandle, OutputChannel> _outputChannels = new();
        /// <summary>All the output channels - key is user assigned name.</summary>
        // readonly Dictionary<string, Channel> _outputChannels = new();

        /// <summary>All the input channels.</summary>
        readonly List<InputChannel> _inputChannels = new();

        /// <summary>All the input channels - key is handle.</summary>
        //readonly Dictionary<ChannelHandle, InputChannel> _inputChannels = new();
        /// <summary>All the intput channels - key is user assigned name.</summary>
        // readonly Dictionary<string, InputChannel> _inputChannels = new();

        /// <summary>All the output channel controls.</summary>
        readonly List<ChannelControl> _channelControls = new();

        ///// <summary>Test settings.</summary>
        //readonly UserSettings _settings = new();

        /// <summary>Interop serializing access.</summary>
        readonly object _lock = new();


        /// <summary>Where to put things.</summary>
        readonly string _outPath = @"..\..\out";
        #endregion

        /// <summary>Cosmetics.</summary>
        readonly Color _drawColor = Color.Aquamarine;

        /// <summary>Cosmetics.</summary>
        readonly Color _selectedColor = Color.Yellow;

        //public class MidiSettings
        //{
        //    public Color ControlColor { get; set; } = Color.Red; // from MidiGen
        //    public Color ActiveColor { get; set; } = Color.DodgerBlue; // from Nebulua
        //    public Color SelectedColor { get; set; } = Color.Moccasin; // from Nebulua
        //    public Color BackColor { get; set; } = Color.AliceBlue; // from Nebulua
        //    public List<string> InputDevices { get; set; } = new(); // from MidiLib
        //    public List<string> OutputDevices { get; set; } = new();// from MidiLib
        //}





        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// </summary>
        public MainForm()
        {
            InitializeComponent();

            // Make sure out path exists.
            DirectoryInfo di = new(_outPath);
            di.Create();

            // The text output.
            txtViewer.Font = Font;
            txtViewer.WordWrap = true;
            txtViewer.MatchText.Add("ERR", Color.LightPink);
            txtViewer.MatchText.Add("WRN", Color.Plum);

            // Master volume.
            sldVolume.DrawColor = _drawColor;
            sldVolume.Minimum = 0.0;
            sldVolume.Maximum = Defs.MAX_VOLUME;
            sldVolume.Resolution = Defs.MAX_VOLUME / 50;
            sldVolume.Value = Defs.DEFAULT_VOLUME;
            sldVolume.Label = "volume";

            // Hook up some simple UI handlers.
            btnKillMidi.Click += (_, __) => { };
            btnLogMidi.CheckedChanged += (_, __) => { };
        }

        /// <summary>
        /// Window is set up now.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            ///// 1 - create all devices
            bool ok = CreateDevices();

            ///// 2 - create all channels (explicit or from script api calls)
            // script: local hnd_ccin  = api.open_midi_input("loopMIDI Port 1", 1, "my input")
            var ch1 = CreateInputChannel("loopMIDI Port 1", 1, "my input");
            // script: local hnd_keys  = api.open_midi_output("Microsoft GS Wavetable Synth", 1,  "keys", inst.AcousticGrandPiano)
            var ch2 = CreateOutputChannel("Microsoft GS Wavetable Synth", 1, "keys", 0); //AcousticGrandPiano);
            // script: local hnd_drums = api.open_midi_output("Microsoft GS Wavetable Synth", 10, "drums", kit.Jazz)
            var ch3 = CreateOutputChannel("Microsoft GS Wavetable Synth", 10, "drums", 32); // kit.Jazz);

            ///// 3 - create a channel control for each output channel and bind object
            CreateControls();

            ///// 4 - do work
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
            DestroyDevices();

            if (disposing && (components is not null))
            {
                components.Dispose();
            }

            base.Dispose(disposing);
        }
        #endregion

        #region Controls


        void CreateControls()
        {
            DestroyControls();

            foreach (var chout in _outputChannels)
            {
                var cc = CreateControl(chout);
            }
        }


        /// <summary>
        /// Create control.
        /// </summary>
        ChannelControl CreateControl(OutputChannel channel)
        {
            ChannelControl cc = new(channel);
            cc.DrawColor = _drawColor;
            cc.SelectedColor = _selectedColor;
            cc.Volume = 0.56;

            //cc.UpdatePresets();

            cc.ChannelChange += Cc_ChannelChange;
            cc.SendMidi += Cc_MidiSend;

            Controls.Add(cc);

            return cc;

            //////////////// from Nebulua TODO1 pick over ////////////////
            // Create channels and controls.
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
        #endregion



        ///// <summary>
        ///// User clicked something. Send some midi.
        ///// </summary>
        ///// <param name="sender"></param>
        ///// <param name="e"></param>
        void Cc_MidiSend(object? sender, BaseEvent e)
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
                // Update all channel enables.
                bool anySolo = _channelControls.Where(c => c.State == ChannelState.Solo).Any();

                foreach (var cit in _channelControls)
                {
                    bool enable = anySolo ? cit.State == ChannelState.Solo : cit.State != ChannelState.Mute;

                    var ch = cit.BoundChannel.ChHandle;

                    if (ch.DeviceId >= _outputDevices.Count)
                    {
                        throw new AppException($"Invalid device id [{ch.DeviceId}]");
                    }

                    var output = _outputDevices[ch.DeviceId];

                    //if (!output.Channels.TryGetValue(ch.ChannelNumber, out OutputChannel? value))
                    //{
                    //    throw new AppException($"Invalid channel [{ch.ChannelNumber}]");
                    //}
                    //value.Enable = enable;
                    //if (!enable)
                    //{
                    //    // Kill just in case.
                    //    _outputDevices[ch.DeviceId].Send(new ControlChangeEvent(0, ch.ChannelNumber, MidiController.AllNotesOff, 0));
                    //}
                }

                switch (cc.State)
                {
                    case ChannelState.Normal:
                        break;

                    case ChannelState.Solo:
                        // Mute any other non-solo channels.
                        //_channels.Values.ForEach(ch =>
                        //{
                        //    if (channel.ChannelNumber != ch.ChannelNumber && channel.State != ChannelState.Solo)
                        //    {
                        //        channel.Kill();
                        //    }
                        //});
                        break;

                    case ChannelState.Mute:
                        //channel.Kill();
                        break;
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
        void SendEvent(int devid, BaseEvent evt)
        {
            var mout = _outputDevices[devid];
            mout?.Send(evt);
        }

        // Midi input arrived from device. This is on a system thread.
        void Midi_ReceiveEvent(object? sender, BaseEvent e)
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
            ChannelHandle chnd = new(_inputDevices.IndexOf(indev), e.Channel, false);

            // Do the work.
            //_interop.ReceiveMidiNote(chnd, evt.NoteNumber, (double)evt.Velocity / MidiDefs.MAX_MIDI);
            //_interop.ReceiveMidiNote(chnd, evt.NoteNumber, 0);
            //_interop.ReceiveMidiController(chnd, (int)evt.Controller, evt.ControllerValue);
        }


        #region Script => Host API

        // Script => Host API
        InputChannel CreateInputChannel(string deviceName, int channelNumber, string channelName)
        {
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
                var indev = _inputDevices.Where(o => o.DeviceName == deviceName);
                if (indev is null)
                {
                    throw new AppException("throws if invalid");
                }

                // Add the channel.
                InputChannel ch = new(_inputDevices.IndexOf(indev), channelNumber, channelName)
                {
                    Enable = true,
                };

                _inputChannels.Add(ch);
                return ch;
            }
            catch (AppException ex)
            {
                throw new AppException(ex.Message);
            }
        }

        // Script => Host API
        OutputChannel CreateOutputChannel(string deviceName, int channelNumber, string channelName, int patch)
        {
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
                var outdev = _outputDevices.Where(o => o.DeviceName == deviceName);
                // var outdev = _outputDevices.FirstOrDefault(o => o.DeviceName == deviceName);
                if (outdev is null)
                {
                    throw new AppException("throws if invalid");
                }

                // Add the channel.
                OutputChannel ch = new(_outputDevices.IndexOf(outdev), channelNumber, channelName)
                {
                    Enable = true,
                    Patch = patch
                };

                _outputChannels.Add( ch);

                // Send the patch now.
                if (patch >= 0)
                {
////                    SendPatch(chnd, patch);
                }
                return ch;
            }
            catch (AppException ex)
            {
                throw new AppException(ex.Message);
            }

            //return chnd;
        }
        #endregion

        #region Devices => MLL?
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
            foreach (var devname in MidiInputDevice.AvailableDevices())
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
            foreach (var devname in MidiOutputDevice.AvailableDevices())
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

        #region Misc

        /// <summary>
        /// Tell me something good.
        /// </summary>
        /// <param name="s"></param>
        void Tell(string s)
        {
            txtViewer.AppendLine($"{s}");
        }
        #endregion
    }
}
