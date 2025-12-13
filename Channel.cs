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
    /// <summary>Some flavors of control need to be defeatured.</summary>
    [Flags]
    public enum ChannelControlOptions
    {
        Notes = 0x01,
        SoloMute = 0x02,
        Controller = 0x04,
        All = 0x0F, // of above
    }

    public class ChannelHandle
    {
        const int OUTPUT_FLAG = 0x0800;

        public static int Encode(int DeviceId, int ChannelNumber, bool Output)
        {
            return (int)(DeviceId << 4) | ChannelNumber | (Output ? OUTPUT_FLAG : OUTPUT_FLAG);
        }

        public static (int DeviceId, int ChannelNumber, bool Output) Decode(int Handle)
        {
            var output = (Handle & OUTPUT_FLAG) > 0;
            var deviceId = ((Handle & ~OUTPUT_FLAG) >> 4) & 0x0F;
            var channelNumber = (Handle & ~OUTPUT_FLAG) & 0xF0;
            return (deviceId, channelNumber, output);
        }

        public static string Format(int Handle)
        {
            var parts = Decode(Handle);
            return $"{(parts.Output ? "OUT" : "IN")}  {parts.DeviceId}:{parts.ChannelNumber}";
        }
    }

    //----------------------------------------------------------------
    ///// <summary>References one channel. Supports translation to/from script unique int handle.</summary>
    ///// <param name="DeviceId">Unique number</param>
    ///// <param name="ChannelNumber">Midi channel 1-based</param>
    ///// <param name="Output">T or F</param>
    //public record struct ChannelHandle(int DeviceId, int ChannelNumber, bool Output)
    //{
    //    const int OUTPUT_FLAG = 0x8000;

    //    /// <summary>Create from int handle.</summary>
    //    /// <param name="handle"></param>
    //    public ChannelHandle(int handle) : this(-1, -1, false)
    //    {
    //        Output = (handle & OUTPUT_FLAG) > 0;
    //        DeviceId = ((handle & ~OUTPUT_FLAG) >> 8) & 0xFF;
    //        ChannelNumber = (handle & ~OUTPUT_FLAG) & 0xFF;
    //    }

    //    ///// <summary>Operator to convert to int handle.</summary>
    //    //public int Raw()
    //    //{
    //    //    return (DeviceId << 8) | ChannelNumber | (Output ? OUTPUT_FLAG : OUTPUT_FLAG);
    //    //}

    //    /// <summary>Operator to convert to int handle.</summary>
    //    /// <param name="ch"></param>
    //    public static implicit operator int(ChannelHandle ch)
    //    {
    //        return (ch.DeviceId << 8) | ch.ChannelNumber | (ch.Output ? OUTPUT_FLAG : OUTPUT_FLAG);
    //    }

    //    /// <summary>See me.</summary>
    //    public override readonly string ToString()
    //    {
    //        return $"{(Output ? "OUT" : "IN")}  {DeviceId}:{ChannelNumber}";
    //    }
    //}

    //----------------------------------------------------------------
    /// <summary>Describes one midi output channel.</summary>
    public class OutputChannel
    {
        #region Properties
        /// <summary>Channel name - optional.</summary>
        public string ChannelName { get; set; } = "";

        /// <summary>Actual 1-based midi channel number.</summary>
        [Range(1, MidiDefs.NUM_CHANNELS)]
        public int ChannelNumber { get; private set; } = 1;

        /// <summary>Override default instrument presets.</summary>
        public string AliasFile
        {
            get { return _aliasFile; }
            set
            {
                _aliasFile = value;

                // Alternate instrument names?
                if (_aliasFile != "")
                {
                    try
                    {
                        Instruments.Clear();
                        var ir = new IniReader(_aliasFile);
                        var defs = ir.Contents["instruments"];

                        defs.Values.ForEach(kv =>
                        {
                            int i = int.Parse(kv.Key); // can throw
                            i = MathUtils.Constrain(i, 0, MidiDefs.MAX_MIDI);
                            Instruments.Add(i, kv.Value.Length > 0 ? kv.Value : "");
                        });
                    }
                    catch (Exception ex)
                    {
                        throw new MidiLibException($"Failed to load alias file {_aliasFile}: {ex.Message}");
                    }
                }
                else
                {
                    Instruments = MidiDefs.TheDefs.GetDefaultInstrumentDefs();
                }
            }
        }
        string _aliasFile = "";

        /// <summary>Current instrument/patch number.</summary>
        [Range(0, MidiDefs.MAX_MIDI)]
        public int Patch
        {
            get {  return _patch; }
            set
            {
                _patch = value;
                Device.Send(new Patch(ChannelNumber, _patch));
            }
        }
        int _patch = 0;

        /// <summary>Current volume.</summary>
        [Range(0.0, Defs.MAX_VOLUME)]
        public double Volume { get; set; } = Defs.DEFAULT_VOLUME;
    
        /// <summary>Edit current controller number.</summary>
        [Range(0, MidiDefs.MAX_MIDI)]
        public int ControllerId { get; set; } = 0;

        /// <summary>Controller payload.</summary>
        [Range(0, MidiDefs.MAX_MIDI)]
        public int ControllerValue { get; set; } = 50;

        /// <summary>ChannelControl options.</summary>
        public ChannelControlOptions DisplayOptions { get; set; } = ChannelControlOptions.All;

        /// <summary>Associated device.</summary>
        public IOutputDevice Device { get; init; }

        /// <summary>Handle for use by scripts.</summary>
        public int Handle { get; init; }

        /// <summary>True if channel is active.</summary>
        public bool Enable { get; set; } = true;

        /// <summary>Current list for this channel.</summary>
        public Dictionary<int, string> Instruments { get; private set; } = MidiDefs.TheDefs.GetDefaultInstrumentDefs();
        #endregion

        /// <summary>
        /// Constructor with required args.
        /// </summary>
        /// <param name="device"></param>
        /// <param name="channelNumber"></param>
        public OutputChannel(IOutputDevice device, int channelNumber)
        {
            Device = device;
            ChannelNumber = channelNumber;
            Volume = Defs.DEFAULT_VOLUME;
            Handle = ChannelHandle.Encode(device.Id, ChannelNumber, true);
        }

        /// <summary>
        /// Get patch name.
        /// </summary>
        /// <param name="which"></param>
        /// <returns>The name or a fabricated one if unknown.</returns>
        public string GetPatchName(int which)
        {
            return Instruments.TryGetValue(which, out string? value) ? value : $"PATCH_{which}";
        }
    }

    //----------------------------------------------------------------
    /// <summary>Describes one midi input channel.</summary>
    public class InputChannel
    {
        #region Properties
        /// <summary>Channel name - optional.</summary>
        public string ChannelName { get; set; } = "";

        /// <summary>Actual 1-based midi channel number.</summary>
        [Range(1, MidiDefs.NUM_CHANNELS)]
        public int ChannelNumber { get; set; } = 1;

        /// <summary>Associated device.</summary>
        public IInputDevice Device { get; init; }

        /// <summary>True if channel is active.</summary>
        public bool Enable { get; set; } = true;

        /// <summary>Handle for use by scripts.</summary>
        public int Handle { get; init; }
        #endregion

        /// <summary>
        /// Constructor with required args.
        /// </summary>
        /// <param name="device"></param>
        /// <param name="channelNumber"></param>
        public InputChannel(IInputDevice device, int channelNumber)
        {
            Device = device;
            ChannelNumber = channelNumber;
            Handle = ChannelHandle.Encode(device.Id, ChannelNumber, false);
        }
    }
}
