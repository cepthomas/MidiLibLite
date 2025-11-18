using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using System.Windows.Forms.Design;
using System.IO;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Design;
using System.Windows.Forms;
using NAudio.Midi;
using Ephemera.NBagOfTricks;


// from MidiGenerator/MidiLibNew
namespace Ephemera.MidiLibLite
{
    /// <summary>Describes one midi output channel. Some properties are optional.</summary>
    [Serializable]
    public class Channel
    {
        #region Fields

        /// <summary>All possible instrument/patch.</summary>
        //[Browsable(false)]
        //[JsonIgnore]
        //public Dictionary<int, string> Instruments { get; set; } = [];
        Dictionary<int, string> _instruments = MidiDefs.TheDefs.GetDefaultInstrumentDefs();

        #endregion

        #region Persisted Non-editable Properties
        /// <summary>Actual 1-based midi channel number.</summary>
        [Browsable(true)]
//X        [Editor(typeof(ChannelSelectorTypeEditor), typeof(UITypeEditor))]
        public int ChannelNumber
        {
            get { return _channelNumber; }
            set { _channelNumber = MathUtils.Constrain(value, 1, MidiDefs.NUM_CHANNELS); }
        }
        int _channelNumber = 1;

        /// <summary>Current volume.</summary>
        [Browsable(false)]
        public double Volume
        {
            get { return _volume; }
            set { _volume = MathUtils.Constrain(value, 0.0, MidiLibDefs.MAX_VOLUME); }
        }
        double _volume = MidiLibDefs.DEFAULT_VOLUME;



        /// <summary>Override default instrument presets.</summary>
        [Browsable(true)]
        [Editor(typeof(FileNameEditor), typeof(UITypeEditor))]
        public string PresetFile
        {
            get { return _presetFile; }
            set
            {
                //if (value == "") // use defaults
                //{
                //    Instruments = MidiDefs.GetDefaultInstrumentDefs();
                //}
                //else // load override
                //{
                //    if (!File.Exists(value)) throw new FileNotFoundException(x);
                //    Instruments = LibUtils.LoadDefs(value);
                //}
                _presetFile = value;
            }
        }
        string _presetFile = "";

        /// <summary>Current instrument/patch number.</summary>
        [Browsable(true)]
//X        [Editor(typeof(PatchTypeEditor), typeof(UITypeEditor))]
//X        [TypeConverter(typeof(PatchConverter))]
        //public int Patch { get; set; } = 0;
        public int Patch
        {
            get { return _patch; }
            set { _patch = MathUtils.Constrain(value, 0, MidiDefs.MAX_MIDI); }
        }
        int _patch = 0;
        #endregion


        #region Properties - not persisted
        /// <summary>Associated midi device.</summary>
        [Browsable(false)]
        [JsonIgnore]
        public IOutputDevice? Device { get; set; } = null;

        /// <summary>Handle.</summary>
        public ChannelHandle ChHandle { get; init; }

        #endregion

        /// <summary>
        /// Get patch name.
        /// </summary>
        /// <param name="which"></param>
        /// <returns>The name.</returns>
        public string GetPatchName()
        {
            if (_instruments.Count == 0)
            {
                _instruments = MidiDefs.TheDefs.GetDefaultInstrumentDefs();

                //if (value == "") // use defaults
                //{
                //    Instruments = MidiDefs.GetDefaultInstrumentDefs();
                //}
                //else // load override
                //{
                //    if (!File.Exists(value)) throw new FileNotFoundException(x);
                //    Instruments = LibUtils.LoadDefs(value);
                //}
            }
            string ret = _instruments[Patch];
            return ret;
        }

        #region Functions


        /// <summary>
        /// General patch sender.
        /// </summary>
        public void SendPatch()
        {
            if(Patch >= MidiDefs.MIN_MIDI && Patch <= MidiDefs.MAX_MIDI)
            {
                PatchChangeEvent evt = new(0, ChannelNumber, Patch);
                SendEvent(evt);
            }
        }

        /// <summary>
        /// Send a controller now.
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="val"></param>
        public void SendController(MidiController controller, int val)
        {
            ControlChangeEvent evt = new(0, ChannelNumber, controller, val);
            SendEvent(evt);
        }

        /// <summary>
        /// Send midi all notes off.
        /// </summary>
        public void Kill()
        {
            SendController(MidiController.AllNotesOff, 0);
        }

        /// <summary>
        /// Generic event sender.
        /// </summary>
        /// <param name="evt"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public void SendEvent(MidiEvent evt)
        {
            if (Device is null)
            {
                throw new InvalidOperationException("Device not set");
            }

            // Now send it.
            Device.SendEvent(evt);
        }
#endregion
    }


    /// <summary>Helper extension methods.</summary>
    //public static class ChannelUtils
    //{
    //    /// <summary>
    //    /// Any solo in collection.
    //    /// </summary>
    //    /// <typeparam name="T"></typeparam>
    //    /// <param name="channels"></param>
    //    /// <returns></returns>
    //    public static bool AnySolo<T>(this Dictionary<string, T> channels) where T : Channel
    //    {
    //        var solo = channels.Values.Where(c => c.State == ChannelState.Solo).Any();
    //        return solo;
    //    }

    //    /// <summary>
    //    /// Get subs for the collection, rounded to beat.
    //    /// </summary>
    //    /// <typeparam name="T"></typeparam>
    //    /// <param name="channels"></param>
    //    /// <returns></returns>
    //    public static int TotalSubs<T>(this Dictionary<string, T> channels) where T : Channel
    //    {
    //        var chmax = channels.Values.Max(ch => ch.MaxSub);
    //        // Round total up to next beat.
    //        BarTime bs = new();
    //        bs.SetRounded(chmax, SnapType.Beat, true);
    //        return bs.TotalSubs;
    //    }
    //}
}
