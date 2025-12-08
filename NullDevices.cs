using System;
using System.Collections.Generic;
using Ephemera.NBagOfTricks;


// TODO2 add scaffolding.


namespace Ephemera.MidiLibLite
{
    public class NullInputDevice : IInputDevice
    {
        /// <inheritdoc />
        public string DeviceName { get; }

        /// <inheritdoc />
        public bool Valid { get; set; }

        /// <inheritdoc />
        public bool CaptureEnable { get; set; }

        /// <inheritdoc />
        public int Id { get; init; }

        /// <inheritdoc />
        public event EventHandler<BaseMidiEvent>? InputReceive;

        #region Lifecycle
        /// <summary>
        /// Normal constructor.
        /// </summary>
        /// <param name="deviceName">Client must supply name of device.</param>
        public NullInputDevice(string deviceName)
        {
            if (string.IsNullOrEmpty(deviceName)) { throw new ArgumentException("Invalid deviceName"); }

            DeviceName = deviceName;
        }

        /// <summary>
        /// Resource clean up.
        /// </summary>
        public void Dispose()
        {
        }
        #endregion
    }

    public class NullOutputDevice : IOutputDevice
    {
        /// <inheritdoc />
        public string DeviceName { get; }

        /// <inheritdoc />
        public bool Valid { get; set; }

        /// <inheritdoc />
        public int Id { get; init; }

        #region Lifecycle
        /// <summary>
        /// Normal constructor. OK to throw in here.
        /// </summary>
        /// <param name="deviceName">Client must supply name of device.</param>
        public NullOutputDevice(string deviceName)
        {
            if (string.IsNullOrEmpty(deviceName)) { throw new ArgumentException("Invalid deviceName"); }
            DeviceName = deviceName;
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        public void Dispose()
        {
            // Resources.
        }
        #endregion

        /// <inheritdoc />
        public void Send(BaseMidiEvent evt)
        {
        
        }
    }
}
