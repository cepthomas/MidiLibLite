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
using Ephemera.NBagOfTricks;


namespace Ephemera.MidiLibLite
{
    /// <summary>Describes one midi output channel. Some properties are optional.</summary>
    [Serializable] // TODO1 host should handle persistence?!
    public class OutputChannel
    {
        #region Persisted Editable Properties
        /// <summary>Channel name - optional.</summary>
        public string ChannelName { get; set; } = "";

        /// <summary>Actual 1-based midi channel number.</summary>
        [Browsable(true)]
        [Editor(typeof(MidiValueTypeEditor), typeof(UITypeEditor))]
        public int ChannelNumber
        {
            get { return _channelNumber; }
            set { _channelNumber = MathUtils.Constrain(value, 1, MidiDefs.NUM_CHANNELS); }
        }
        int _channelNumber = 1;

        /// <summary>Override default instrument presets.</summary>
        [Browsable(true)]
        [Editor(typeof(FileNameEditor), typeof(UITypeEditor))]
        public string PresetFile
        {
            get { return _presetFile; }
            set { _presetFile = value; }
        }
        string _presetFile = "";

        /// <summary>Edit current instrument/patch number.</summary>
        [Browsable(true)]
        [Editor(typeof(PatchTypeEditor), typeof(UITypeEditor))]
        [TypeConverter(typeof(PatchConverter))]
        public int Patch
        {
            get { return _patch; }
            set { _patch = MathUtils.Constrain(value, 0, MidiDefs.MAX_MIDI); }
        }
        int _patch = 0;

         /// <summary>Edit current controller number.</summary>
         [Browsable(true)]
         [Editor(typeof(MidiValueTypeEditor), typeof(UITypeEditor))]
         public int ControllerId
         {
             get { return _controllerId; }
             set { _controllerId = MathUtils.Constrain(value, 0, MidiDefs.MAX_MIDI); }
         }
         int _controllerId = 0;
        #endregion

        #region Persisted Non-editable Properties
        /// <summary>Current volume.</summary>
        [Browsable(false)]
        public double Volume
        {
            get { return _volume; }
            set { _volume = MathUtils.Constrain(value, 0.0, Defs.MAX_VOLUME); }
        }
        double _volume = Defs.DEFAULT_VOLUME;

        /// <summary>Controller payload.</summary>
        [Browsable(false)]
        public int ControllerValue
        {
            get { return _controllerValue; }
            set { _controllerValue = MathUtils.Constrain(value, 0, MidiDefs.MAX_MIDI); }
        }
        int _controllerValue = 0;
        #endregion

        #region Non-persisted Properties
        /// <summary>Associated device.</summary>
        [Browsable(false)]
        [JsonIgnore]
        public IOutputDevice Device { get; init; }

        /// <summary>Handle for use by scripts.</summary>
        [Browsable(false)]
        [JsonIgnore]
        public ChannelHandle Handle { get; init; } // from Nebulua

        /// <summary>True if channel is active.</summary>
        [Browsable(false)]
        [JsonIgnore]
        public bool Enable { get; set; } = true;

        /// <summary>Convenience property.</summary>
        [Browsable(false)]
        [JsonIgnore]
        public Dictionary<int, string> Instruments { get { return _instruments; } }
        Dictionary<int, string> _instruments = MidiDefs.TheDefs.GetDefaultInstrumentDefs();
        #endregion

        /// <summary>
        /// Constructor with required args.
        /// </summary>
        /// <param name="device"></param>
        /// <param name="channelNumber"></param>
        /// <param name="channelName"></param>
        public OutputChannel(IOutputDevice device, int channelNumber, string channelName)
        {
            Device = device;
            ChannelNumber = channelNumber;
            ChannelName = channelName;
            Handle = new(device.Id, channelNumber, true);
        }

        /// <summary>Use default or custom presets.</summary>
        /// <exception cref="FileNotFoundException"></exception>
        public void UpdatePresets()
        {
            if (PresetFile != "")
            {
                if (!File.Exists(PresetFile))
                {
                    throw new FileNotFoundException(PresetFile);
                }
                _instruments = Utils.LoadDefs(PresetFile);
            }
            else // use defaults
            {
                _instruments = MidiDefs.TheDefs.GetDefaultInstrumentDefs();
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
        #region Fields
        #endregion

        #region Persisted Editable Properties
        /// <summary>Actual 1-based midi channel number.</summary>
        [Browsable(true)]
        [Editor(typeof(MidiValueTypeEditor), typeof(UITypeEditor))]
        public int ChannelNumber
        {
            get { return _channelNumber; }
            set { _channelNumber = MathUtils.Constrain(value, 1, MidiDefs.NUM_CHANNELS); }
        }
        int _channelNumber = 1;

        /// <summary>Channel name - optional.</summary>
        public string ChannelName { get; set; } = "";
        #endregion

        #region Persisted Non-editable Properties
        #endregion

        #region Non-persisted Properties
        /// <summary>Associated device.</summary>
        [Browsable(false)]
        [JsonIgnore]
        public IInputDevice Device { get; init; }

        /// <summary>True if channel is active.</summary>
        [Browsable(false)]
        [JsonIgnore]
        public bool Enable { get; set; } = true;

        /// <summary>Handle for use by scripts.</summary>
        [Browsable(false)]
        [JsonIgnore]
        public ChannelHandle Handle { get; init; }
        #endregion

        /// <summary>
        /// Constructor with required args.
        /// </summary>
        /// <param name="device"></param>
        /// <param name="channelNumber"></param>
        /// <param name="channelName"></param>
        public InputChannel(IInputDevice device, int channelNumber, string channelName)
        {
            Device = device;
            ChannelNumber = channelNumber;
            ChannelName = channelName;
            Handle = new(device.Id, channelNumber, false);
        }
    }

    /// <summary>References one channel. Supports translation to/from script unique int handle.</summary>
    /// <param name="DeviceId">Index in internal list</param>
    /// <param name="ChannelNumber">Midi channel 1-based</param>
    /// <param name="Output">T or F</param>
    public record struct ChannelHandle(int DeviceId, int ChannelNumber, bool Output)
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
