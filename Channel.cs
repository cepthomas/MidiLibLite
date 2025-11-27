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
using NAudio.Midi;
using Ephemera.NBagOfTricks;


namespace Ephemera.MidiLibLite // from MidiGenerator
{
    /// <summary>Describes one midi output channel. Some properties are optional.</summary>
    [Serializable]
    public class Channel
    {
        #region Fields
        /// <summary>All possible instrument/patch.</summary>
        Dictionary<int, string> _instruments = MidiDefs.TheDefs.GetDefaultInstrumentDefs();
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
        /// <summary>Convenience property.</summary>
        [Browsable(false)]
        [JsonIgnore]
        public Dictionary<int, string> Instruments { get { return _instruments; } }
        #endregion

        #region Misc functions
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
        #endregion
    }
}
