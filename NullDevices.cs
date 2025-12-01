using System;
using System.Collections.Generic;
using Ephemera.NBagOfTricks;


// TODO2 add scaffolding.


namespace Ephemera.MidiLibLite
{
    public class NullInputDevice : IInputDevice
    {
        public string DeviceName => nameof(NullOutputDevice);
        public bool Valid { get { return false; } }
        public bool CaptureEnable { get; set; }
        public event EventHandler<BaseEvent>? InputReceive;
        public void Dispose() { }
    }

    public class NullOutputDevice : IOutputDevice
    {
        public string DeviceName => nameof(NullOutputDevice);
        public bool Valid { get { return false; } }
        public void Dispose() { }
        public void Send(BaseEvent evt) { }
    }
}
