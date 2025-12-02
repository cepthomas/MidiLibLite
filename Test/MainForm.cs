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
    public partial class MainForm : Form
    {
        #region Fields - app
        /// <summary>All the channel controls.</summary>
        readonly List<ChannelControl> _channelControls = [];

        /// <summary>Where to put things.</summary>
        readonly string _outPath = @"..\..\out";

        /// <summary>Cosmetics.</summary>
        readonly Color _drawColor = Color.Aquamarine;

        /// <summary>Cosmetics.</summary>
        readonly Color _selectedColor = Color.Yellow;



        Manager mgr = new();

        #endregion

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

            mgr.InputReceive += Mgr_InputReceive;
        }

        /// <summary>
        /// Window is set up now.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e) // -> or from script
        {
            new ChannelControl(new OutputChannel(new NullOutputDevice(), 1, "nada"));
            DemoScriptApp();

            base.OnLoad(e);
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            mgr.DestroyDevices();

            if (disposing && (components is not null))
            {
                components.Dispose();
            }

            base.Dispose(disposing);
        }
        #endregion


        /// <summary>
        /// A standard app where controls/channels are defined in VS designer.
        /// </summary>
        void DemoStandardApp()
        {
            ///// 1 - create all devices
            bool ok = mgr.CreateDevices();

            ///// 2 - create all channels (explicit or from script api calls)
            // // script: local hnd_ccin  = api.open_midi_input("loopMIDI Port 1", 1, "my input")
            // var ch1 = mgr.CreateInputChannel("loopMIDI Port 1", 1, "my input");
            // // script: local hnd_keys  = api.open_midi_output("Microsoft GS Wavetable Synth", 1,  "keys", inst.AcousticGrandPiano)
            // var ch2 = mgr.CreateOutputChannel("Microsoft GS Wavetable Synth", 1, "keys", 0); //AcousticGrandPiano);
            // // script: local hnd_drums = api.open_midi_output("Microsoft GS Wavetable Synth", 10, "drums", kit.Jazz)
            // var ch3 = mgr.CreateOutputChannel("Microsoft GS Wavetable Synth", 10, "drums", 32); // kit.Jazz);

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
        }


        /// <summary>
        /// App driven by a script e.g. Nebulu/Nebulator.
        /// </summary>
        void DemoScriptApp()
        {
            ///// 1 - create all devices
            bool ok = mgr.CreateDevices();

            ///// 2 - create all channels (explicit or from script api calls)
            // script: local hnd_ccin  = api.open_midi_input("loopMIDI Port 1", 1, "my input")
            var ch1 = mgr.CreateInputChannel("loopMIDI Port 1", 1, "my input");
            // script: local hnd_keys  = api.open_midi_output("Microsoft GS Wavetable Synth", 1,  "keys", inst.AcousticGrandPiano)
            var ch2 = mgr.CreateOutputChannel("Microsoft GS Wavetable Synth", 1, "keys", 0); //AcousticGrandPiano);
            // script: local hnd_drums = api.open_midi_output("Microsoft GS Wavetable Synth", 10, "drums", kit.Jazz)
            var ch3 = mgr.CreateOutputChannel("Microsoft GS Wavetable Synth", 10, "drums", 32); // kit.Jazz);


            // public InputChannel CreateInputChannel(string deviceName, int channelNumber, string channelName)
            // {
            //     // Check args.
            //     if (string.IsNullOrEmpty(deviceName))
            //     {
            //         throw new ArgumentException($"Invalid input midi device {deviceName ?? "null"}");
            //     }

            //     if (channelNumber < 1 || channelNumber > MidiDefs.NUM_CHANNELS)
            //     {
            //         throw new ArgumentException($"Invalid input midi channel {channelNumber}");
            //     }


            // public OutputChannel CreateOutputChannel(string deviceName, int channelNumber, string channelName, int patch)
            // {
            //     // Check args.
            //     if (string.IsNullOrEmpty(deviceName))
            //     {
            //         throw new ArgumentException($"Invalid output midi device {deviceName ?? "null"}");
            //     }

            //     if (channelNumber < 1 || channelNumber > MidiDefs.NUM_CHANNELS)
            //     {
            //         throw new ArgumentException($"Invalid output midi channel {channelNumber}");
            //     }





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
        }



        void Mgr_InputReceive(object? sender, BaseEvent e)
        {
            throw new NotImplementedException();
        }


        //var chan = cc!.BoundChannel!;
        //if (chan.Enable)
        //{
        //    //SendNote(cc!.BoundChannel.ChannelNumber, e.Note, e.Velocity);
        //    //SendController(cc!.BoundChannel.ChannelNumber, (MidiController)e.ControllerId, e.Value);
        //    //SendEvent(chnd.DeviceId, evt);
        //}

        // Determine channel.
        //ChannelHandle chnd = new(_inputDevices.IndexOf(indev), e.Channel, false);

        // Do the work.
        //_interop.ReceiveMidiNote(chnd, evt.NoteNumber, (double)evt.Velocity / MidiDefs.MAX_MIDI);
        //_interop.ReceiveMidiNote(chnd, evt.NoteNumber, 0);
        //_interop.ReceiveMidiController(chnd, (int)evt.Controller, evt.ControllerValue);




        ///// <summary>
        ///// UI clicked something. Send some midi.
        ///// </summary>
        ///// <param name="sender"></param>
        ///// <param name="e"></param>
        void Cc_MidiSend(object? sender, BaseEvent e)
        {
            var cc = sender as ChannelControl;
            var chan = cc!.BoundChannel!;

            if (chan.Enable)
            {
                // get device


                //SendNote(cc!.BoundChannel.ChannelNumber, e.Note, e.Velocity);
                //SendController(cc!.BoundChannel.ChannelNumber, (MidiController)e.ControllerId, e.Value);
                //SendEvent(chnd.DeviceId, evt);
            }
        }

        /// <summary>
        /// UI clicked something in one of the channel controls.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Cc_ChannelChange(object? sender, ChannelChangeEventArgs e) //TODO1 all these
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

                    var ch = cit.BoundChannel.Handle;


                    //if (ch.DeviceId >= _outputDevices.Count)
                    //{
                    //    throw new AppException($"Invalid device id [{ch.DeviceId}]");
                    //}
                    //var output = _outputDevices[ch.DeviceId];


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
        //void SendEvent(int devid, BaseEvent evt)
        //{
        //    var mout = _outputDevices[devid];
        //    mout?.Send(evt);
        //}



        #region Controls


        void CreateControls()
        {
            DestroyControls();

//foreach (var chout in _outputChannels) TODOX
//{
//    var cc = CreateControl(chout);
//}

        }


        /// <summary>
        /// Create control.
        /// </summary>
        ChannelControl CreateControl(OutputChannel channel)
        {
            ChannelControl cc = new(channel)
            {
                DrawColor = _drawColor,
                SelectedColor = _selectedColor,
                Volume = 0.56
            };

            //    // Adjust positioning for next iteration.
            //    x += control.Width + 5;
            //    Location = new(x, y),

            //cc.UpdatePresets();

            cc.ChannelChange += Cc_ChannelChange;
            cc.SendMidi += Cc_MidiSend;

            Controls.Add(cc);

            return cc;
        }

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
        #endregion


        #region Misc internals
        /// <summary>
        /// Tell me something good.
        /// </summary>
        /// <param name="s"></param>
        void Info(string s)
        {
            txtViewer.AppendLine($"{s}");
        }

        void Warn(string s)
        {
            txtViewer.AppendLine($"WWRN {s}");
        }

        void Error(string s)
        {
            txtViewer.AppendLine($"ERR {s}");
        }
        #endregion


        void KillAll() //TODO1
        {

        }
    }
}
