using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NAudio.Midi;


namespace Ephemera.MidiLibLite
{
    /// <summary>Abstraction layer to support all midi-like devices.</summary>
    public interface IDevice : IDisposable
    {
        #region Properties
        /// <summary>Device name as defined by the system.</summary>
        string DeviceName { get; }

        /// <summary>Are we ok?</summary>
        bool Valid { get; }

        /// <summary>Log traffic at Trace level.</summary>
        bool LogEnable { get; set; }
        #endregion
    }

    /// <summary>Abstraction layer to support input devices.</summary>
    public interface IInputDevice : IDevice
    {
        #region Properties
        /// <summary>Capture on/off.</summary>
        bool CaptureEnable { get; set; }
        #endregion

        #region Events
        /// <summary>Handler for message arrived.</summary>
        event EventHandler<MidiEventArgs>? InputReceive;
        #endregion
    }

    /// <summary>Abstraction layer to support output devices.</summary>
    public interface IOutputDevice : IDevice
    {
        #region Properties

        #endregion

        #region Functions
        /// <summary>Send midi event.</summary>
        /// <param name="evt"></param>
        void Send(MidiEventArgs evt);

        /// <summary>Send midi event.</summary>
        /// <param name="evt"></param>
        void Send(MidiEvent evt);
        #endregion
    }
}
