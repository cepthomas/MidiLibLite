using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.ComponentModel.DataAnnotations;
using Ephemera.NBagOfTricks;


namespace Ephemera.MidiLibLite
{
    public class Defs
    {
        /// <summary>Default value.</summary>
        public const double DEFAULT_VOLUME = 0.8;

        /// <summary>Allow UI controls some more headroom.</summary>
        public const double MAX_VOLUME = 2.0;
    }

    /// <summary>Application level error. Above lua level.</summary>
    public class AppException(string message) : Exception(message) { }


//////////////////////////////////// from Nebulua /////////////////////////////////////
    /// <summary>Library error.</summary>
    public class MLLAppException(string message) : Exception(message) { }

    /// <summary>Channel playing.</summary>
    public enum PlayState { Normal, Solo, Mute }

    /// <summary>Channel direction.</summary>
    public enum Direction { None, Input, Output }

    /// <summary>References one channel. Supports translation to/from script unique int handle.</summary>
    /// <param name="DeviceId">Index in internal list</param>
    /// <param name="ChannelNumber">Midi channel 1-based</param>
    /// <param name="Output">T or F</param>
    public record struct ChannelHandle(int DeviceId, int ChannelNumber, Direction Direction)
    {
        const int OUTPUT_FLAG = 0x8000;

        /// <summary>Create from int handle.</summary>
        /// <param name="handle"></param>
        public ChannelHandle(int handle) : this(-1, -1, Direction.None)
        {
            Direction = (handle & OUTPUT_FLAG) > 0 ? Direction.Output : Direction.Input;
            DeviceId = ((handle & ~OUTPUT_FLAG) >> 8) & 0xFF;
            ChannelNumber = (handle & ~OUTPUT_FLAG) & 0xFF;
        }

        /// <summary>Operator to convert to int handle.</summary>
        /// <param name="ch"></param>
        public static implicit operator int(ChannelHandle ch)
        {
            return (ch.DeviceId << 8) | ch.ChannelNumber | (ch.Direction == Direction.Output ? OUTPUT_FLAG : OUTPUT_FLAG);
        }

        /// <summary>See me.</summary>
        public override readonly string ToString()
        {
            return $"{DeviceId}:{ChannelNumber}";
        }
    }

    #region Internal event defs
    public class BaseXXX //TODO1 better names
    {
        public int Channel { get; init; }

        /// <summary>Something to tell the client.</summary>
        public string ErrorInfo { get; set; } = "";
    }

    /// <summary>??? Notify host of user clicks.</summary>
    public class NoteOnXXX : BaseXXX
    {
        /// <summary>The note number to play.</summary>
        public int Note { get; init; }

        /// <summary>0 to 127.</summary>
        public int Velocity { get; init; }

        public NoteOnXXX(int channel, int note, int velocity)
        {
            Channel = channel;
            Note  = note;
            Velocity = velocity;
        }

        /// <summary>Read me.</summary>
        public override string ToString()
        {
            return $"NoteOn:{MusicDefinitions.NoteNumberToName(Note)}({Note}):{Velocity}";
        }
    }

    /// <summary>??? Notify host of user clicks.</summary>
    public class NoteOffXXX : BaseXXX
    {
        /// <summary>The note number to play.</summary>
        public int Note { get; init; }

        public NoteOffXXX(int channel, int note)
        {
            Channel = channel;
            Note  = note;
        }

        /// <summary>Read me.</summary>
        public override string ToString()
        {
            return $"NoteOff:{MusicDefinitions.NoteNumberToName(Note)}({Note})";
        }
    }

    /// <summary>??? Notify host of user clicks.</summary>
    public class ControllerXXX : BaseXXX
    {
        /// <summary>Specific controller id.</summary>
        public int ControllerId { get; init; }

        /// <summary>Payload.</summary>
        public int Value { get; init; }

        public ControllerXXX(int channel, int controllerId, int value)
        {
            Channel = channel;
            ControllerId = controllerId;
            Value = value;
        }

        /// <summary>Read me.</summary>
        public override string ToString()
        {
            return $"ControllerId:{MidiDefs.TheDefs.GetControllerName(ControllerId)}({ControllerId}):{Value}";
        }
    }

    /// <summary>??? Notify host of user clicks.</summary>
    public class PatchXXX : BaseXXX
    {
        /// <summary>Specific patch.</summary>
        [Required]
        public int Patch { get; init; }

        public PatchXXX(int channel, int patch)
        {
            Channel = channel;
            Patch  = patch;
        }

        /// <summary>Read me.</summary>
        public override string ToString()
        {
            return $"Patch:{Patch} TODO1 text get from channel";
        }
    }
    #endregion




    #region Events
    //public class MidiEventArgs : EventArgs
    //{
    //    [Required]
    //    public int Channel { get; set; }

    //    /// <summary>Something to tell the client.</summary>
    //    public string ErrorInfo { get; set; } = "";
    //}

    ///// <summary>??? Notify host of user clicks.</summary>
    //public class NoteEventArgs : MidiEventArgs
    //{
    //    /// <summary>The note number to play.</summary>
    //    [Required]
    //    public int Note { get; set; }

    //    /// <summary>0 to 127.</summary>
    //    [Required]
    //    public int Velocity { get; set; }

    //    /// <summary>Read me.</summary>
    //    public override string ToString()
    //    {
    //        return $"Note:{MusicDefinitions.NoteNumberToName(Note)}({Note}):{Velocity}";
    //    }
    //}

    ///// <summary>??? Notify host of user clicks.</summary>
    //public class ControllerEventArgs : MidiEventArgs
    //{
    //    /// <summary>Specific controller id.</summary>
    //    [Required]
    //    public int ControllerId { get; set; }

    //    /// <summary>Payload.</summary>
    //    [Required]
    //    public int Value { get; set; }

    //    /// <summary>Read me.</summary>
    //    public override string ToString()
    //    {
    //        return $"ControllerId:{MidiDefs.TheDefs.GetControllerName(ControllerId)}({ControllerId}):{Value}";
    //    }
    //}

    /// <summary>Notify host of UI changes.</summary>
    public class ChannelChangeEventArgs : EventArgs
    {
        public bool PatchChange { get; set; } = false;
        public bool ChannelNumberChange { get; set; } = false;
        public bool StateChange { get; set; } = false;
        public bool PresetFileChange { get; set; } = false;
    }
    #endregion


    // /// <summary>Notify host of changes.</summary>
    // public class ChannelControlEventArgs() : EventArgs;
//////////////////////////////////// from MidiLib /////////////////////////////////////
    // /// <summary>Notify host of asynchronous changes from user.</summary>
    // public class ChannelChangeEventArgs : EventArgs
    // {
    //     public bool PatchChange { get; set; } = false;
    //     public bool StateChange { get; set; } = false;
    //     public bool ChannelNumberChange { get; set; } = false;
    // }

    // /// <summary>
    // /// Midi (real or sim) has received something. It's up to the client to make sense of it.
    // /// Property value of -1 indicates invalid or not pertinent e.g a controller event doesn't have velocity.
    // /// </summary>
    // public class InputReceiveEventArgs : EventArgs
    // {
    //     /// <summary>Channel number 1-based. Required.</summary>
    //     public int Channel { get; set; } = 0;

    //     /// <summary>The note number to play. NoteOn/Off only.</summary>
    //     public int Note { get; set; } = -1;

    //     /// <summary>Specific controller id.</summary>
    //     public int Controller { get; set; } = -1;

    //     /// <summary>For Note = velocity. For controller = payload.</summary>
    //     public int Value { get; set; } = -1;

    //     /// <summary>Something to tell the client.</summary>
    //     public string ErrorInfo { get; set; } = "";

    //     /// <summary>Special controller id to carry pitch info.</summary>
    //     public const int PITCH_CONTROL = 1000;

    //     /// <summary>Read me.</summary>
    //     public override string ToString()
    //     {
    //         StringBuilder sb = new($"Channel:{Channel} ");

    //         if (ErrorInfo != "")
    //         {
    //             sb.Append($"Error:{ErrorInfo} ");
    //         }
    //         else
    //         {
    //             sb.Append($"Channel:{Channel} Note:{Note} Controller:{Controller} Value:{Value}");
    //         }

    //         return sb.ToString();
    //     }
    // }






//////////////////////////////////// from MidiLib /////////////////////////////////////
    public class Utils
    {
        // Load a standard midi def file.
        public static Dictionary<int, string> LoadDefs(string fn)
        {
            Dictionary<int, string> res = [];

            var ir = new IniReader(fn);

            var defs = ir.Contents["midi_defs"];

            defs.Values.ForEach(kv =>
            {
                int index = int.Parse(kv.Key); // can throw
                if (index < 0 || index > MidiDefs.MAX_MIDI) { throw new InvalidOperationException($"Invalid def file {fn}"); }
                res[index] = kv.Value.Length > 0 ? kv.Value : "";
            });

            return res;
        }
    }
}
