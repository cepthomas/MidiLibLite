using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using System.IO;
using System.ComponentModel;
using System.Drawing.Design;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;
using Ephemera.NBagOfTricks;


namespace Ephemera.MidiLibLite
{
    /// <summary>One channel config. Host can persist these.</summary>
    [Serializable]
    public class OutputChannelConfig
    {
        /// <summary>Device name.</summary>
        [Editor(typeof(DeviceTypeEditor), typeof(UITypeEditor))]
        public string DeviceName { get; set; } = "";

        /// <summary>Channel name - optional.</summary>
        public string ChannelName { get; set; } = "";

        /// <summary>Actual 1-based midi channel number.</summary>
        [Editor(typeof(MidiValueTypeEditor), typeof(UITypeEditor))]
        [Range(1, MidiDefs.NUM_CHANNELS)]
        public int ChannelNumber { get; set; } = 1;

        /// <summary>Override default instrument presets.</summary>
        [Editor(typeof(FileNameEditor), typeof(UITypeEditor))]
        public string PresetFile { get; set; } = "";

        /// <summary>Current instrument/patch number.</summary>
        [Editor(typeof(PatchTypeEditor), typeof(UITypeEditor))]
        [TypeConverter(typeof(PatchConverter))]
        [Range(0, MidiDefs.MAX_MIDI)]
        public int Patch { get; set; } = 0;

        /// <summary>Current volume.</summary>
        // [Browsable(true)]
        [Range(0.0, Defs.MAX_VOLUME)]
        public double Volume { get; set; } = Defs.DEFAULT_VOLUME;
    }

    /// <summary>One channel config. Host can persist these.</summary>
    [Serializable]
    public class InputChannelConfig
    {
        /// <summary>Device name.</summary>
        [Editor(typeof(DeviceTypeEditor), typeof(UITypeEditor))]
        public string DeviceName { get; set; } = "";

        /// <summary>Channel name - optional.</summary>
        public string ChannelName { get; set; } = "";

        /// <summary>Actual 1-based midi channel number.</summary>
        [Editor(typeof(MidiValueTypeEditor), typeof(UITypeEditor))]
        [Range(1, MidiDefs.NUM_CHANNELS)]
        public int ChannelNumber { get; set; } = 1;
    }


    /// <summary>Describes one midi output channel. Some properties are optional.</summary>
    // [Serializable]
    public class OutputChannel
    {
        // #region Persisted Properties
        // /// <summary>Channel name - optional.</summary>
        // public string ChannelName { get; set; } = "";

        // /// <summary>Actual 1-based midi channel number.</summary>
        // [Browsable(true)]
        // [Editor(typeof(MidiValueTypeEditor), typeof(UITypeEditor))]
        // [Range(1, MidiDefs.NUM_CHANNELS)]
        // public int ChannelNumber { get; set; } = 1;

        // /// <summary>Override default instrument presets.</summary>
        // [Browsable(true)]
        // [Editor(typeof(FileNameEditor), typeof(UITypeEditor))]
        // public string PresetFile { get; set; } = "";

        // /// <summary>Current instrument/patch number.</summary>
        // [Browsable(true)]
        // [Editor(typeof(PatchTypeEditor), typeof(UITypeEditor))]
        // [TypeConverter(typeof(PatchConverter))]
        // [Range(0, MidiDefs.MAX_MIDI)]
        // public int Patch { get; set; } = 0;

        // /// <summary>Current volume.</summary>
        // [Browsable(true)]
        // [Range(0.0, Defs.MAX_VOLUME)]
        // public double Volume { get; set; } = Defs.DEFAULT_VOLUME;
        // #endregion




        /// <summary>My config.</summary>
        public OutputChannelConfig Config { get; init; }
        //{
        //    get {  return _config; }
        //    set {  _config = value; UpdateUi(); }
        //}
        //OutputChannelConfig _config = new();

        #region Properties
        /// <summary>Associated device.</summary>
        // [Browsable(false)]
        // [JsonIgnore]
        public IOutputDevice Device { get; init; }

        /// <summary>Handle for use by scripts.</summary>
        // [Browsable(false)]
        // [JsonIgnore]
        public ChannelHandle Handle { get; init; } // from Nebulua

        /// <summary>True if channel is active.</summary>
        // [Browsable(false)]
        // [JsonIgnore]
        public bool Enable { get; set; } = true;

        /// <summary>Convenience property.</summary>
        // [Browsable(false)]
        // [JsonIgnore]
        public Dictionary<int, string> Instruments { get { return _instruments; } }
        Dictionary<int, string> _instruments = MidiDefs.TheDefs.GetDefaultInstrumentDefs();
        #endregion

        /// <summary>
        /// Constructor with required args.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="device"></param>
        // / <param name="channel"></param>
        // / <param name="name"></param>
        public OutputChannel(OutputChannelConfig config, IOutputDevice device)//, int channel, string name)
        {
            // if (channel is < 1 or > MidiDefs.NUM_CHANNELS) { throw new ArgumentOutOfRangeException(nameof(channel)); }
            // if (string.IsNullOrEmpty(name)) { throw new ArgumentException(nameof(name)); }

            Device = device;
            Config = config;
            // ChannelNumber = channel;
            // ChannelName = name;
            Handle = new(device.Id, Config.ChannelNumber, true);
        }

        /// <summary>Use default or custom presets.</summary>
        public void UpdatePresets()
        {
            try
            {
                _instruments = Config.PresetFile != "" ?
                    Utils.LoadDefs(Config.PresetFile) :
                    MidiDefs.TheDefs.GetDefaultInstrumentDefs();
            }
            catch (Exception ex)
            {
                throw new MidiLibException($"Failed to load defs file {Config.PresetFile}: {ex.Message}");
            }
        }

        /// <summary>
        /// Get patch name.
        /// </summary>
        /// <param name="which"></param>
        /// <returns>The name or a fabricated one if unknown.</returns>
        public string GetPatchName(int which)
        {
            return _instruments.TryGetValue(which, out string? value) ? value : $"PATCH_{which}";
        }
    }

    /// <summary>Describes one midi input channel. Some properties are optional.</summary>
    [Serializable]
    public class InputChannel
    {
        // #region Fields
        // #endregion

        // #region Persisted Properties
        // /// <summary>Actual 1-based midi channel number.</summary>
        // [Browsable(true)]
        // [Editor(typeof(MidiValueTypeEditor), typeof(UITypeEditor))]
        // [Range(1, MidiDefs.NUM_CHANNELS)]
        // public int ChannelNumber { get; set; } = 1;

        // /// <summary>Channel name - optional.</summary>
        // [Browsable(true)]
        // public string ChannelName { get; set; } = "";
        // #endregion

        #region Properties

        /// <summary>My config.</summary>
        public InputChannelConfig Config { get; init; }
        //{
        //    get {  return _config; }
        //    set {  _config = value; UpdateUi(); }
        //}
        //InputChannelConfig _config = new();


        /// <summary>Associated device.</summary>
        // [Browsable(false)]
        // [JsonIgnore]
        public IInputDevice Device { get; init; }

        /// <summary>True if channel is active.</summary>
        // [Browsable(false)]
        // [JsonIgnore]
        public bool Enable { get; set; } = true;

        /// <summary>Handle for use by scripts.</summary>
        // [Browsable(false)]
        // [JsonIgnore]
        public ChannelHandle Handle { get; init; }
        #endregion

        /// <summary>
        /// Constructor with required args.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="device"></param>
        ///// <param name="channel"></param>
        ///// <param name="name"></param>
        public InputChannel(InputChannelConfig config, IInputDevice device)// int channel, string name)
        {
            // if (channel is < 1 or > MidiDefs.NUM_CHANNELS) { throw new ArgumentOutOfRangeException(nameof(channel)); }
            // if (string.IsNullOrEmpty(name)) { throw new ArgumentException(nameof(name)); }

            Device = device;
            Config = config;
            // ChannelNumber = channel;
            // ChannelName = name;
            Handle = new(device.Id, Config.ChannelNumber, false);
        }
    }

    /// <summary>References one channel. Supports translation to/from script unique int handle.</summary>
    /// <param name="DeviceId">Index in internal list</param>
    /// <param name="ChannelNumber">Midi channel 1-based</param>
    /// <param name="Output">T or F</param>
    public record struct ChannelHandle(int DeviceId, int ChannelNumber, bool Output) // TODO1 still pertinent?
    {
        const int OUTPUT_FLAG = 0x8000;

        /// <summary>Create from int handle.</summary>
        /// <param name="handle"></param>
        public ChannelHandle(int handle) : this(-1, -1, false)
        {
            Output = (handle & OUTPUT_FLAG) > 0;
            DeviceId = ((handle & ~OUTPUT_FLAG) >> 8) & 0xFF;
            ChannelNumber = (handle & ~OUTPUT_FLAG) & 0xFF;
        }

        /// <summary>Operator to convert to int handle.</summary>
        /// <param name="ch"></param>
        public static implicit operator int(ChannelHandle ch)
        {
            return (ch.DeviceId << 8) | ch.ChannelNumber | (ch.Output ? OUTPUT_FLAG : OUTPUT_FLAG);
        }

        /// <summary>See me.</summary>
        public override readonly string ToString()
        {
            return $"{(Output ? "OUT" : "IN")}  {DeviceId}:{ChannelNumber}";
        }
    }
}
