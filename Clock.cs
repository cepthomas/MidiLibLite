using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Ephemera.NBagOfTricks;


namespace Ephemera.MidiLibLite
{
    /// <summary>
    /// Master clock for dynamic applications - used by TimeBar.
    /// Can be fixed duration or free-running.
    /// Some property changes (like from UI) can notify clients.
    /// </summary>
    public class Clock
    {

        #region Properties

        /// <summary>Total length of the sequence.</summary>
        public MusicTime Length
        {
            get { return _length; }
            set { _length = value; }
        }
        MusicTime _length = new();

        /// <summary>Start of marked/loop region.</summary>
        public MusicTime Start
        {
            // get { return _start < 0 ? 0 : _start; }
            get { return _start; }
            set { _start = value; ValidateTimes(); }
        }
        MusicTime _start = new();

        /// <summary>End of marked/loop region.</summary>
        public MusicTime End
        {
            // get { return _end < 0 ? _length : _end; }
            get { return _end; }
            set { _end = value; ValidateTimes(); }
        }
        MusicTime _end = new();

        /// <summary>Where we be now.</summary>
        public MusicTime Current
        {
            get { return _current; }
            set { _current = value; ValidateTimes(); NotifyStateChanged(); }
        }
        MusicTime _current = new();

        /// <summary>All the important beat points with their names. Used also by tooltip.</summary>
        public Dictionary<int, string> TimeDefs { get; set; } = [];

        /// <summary>Metadata.</summary>
        public List<(int tick, string name)> SectionInfo
        {
            get { return _sectionInfo; }
            set { _sectionInfo = value; _length = _sectionInfo.Last().tick; ValidateTimes(); }
        }
        List<(int tick, string name)> _sectionInfo = [];



        /// <summary>Current tempo in bpm. Notifies client.</summary>
        public int Tempo
        {
            get { return _tempo; }
            set { if (value != _tempo) { _tempo = value; NotifyStateChanged(); } }
        }
        int _tempo = 100;

        /// <summary>Keep going at end of loop.</summary> 
        public bool DoLoop { get; set; } = false;

        /// <summary>Master volume.</summary>
        public double Volume { get; set; } = 0.8;

        /// <summary>Convenience for readability.</summary>
        public bool IsFreeRunning { get { return _length.TotalBeats == 0; } }


///////////////////////////// nebulua flavor ////////////////////////////////////

        // /// <summary>Where are we in fixed sequence. Notifies client.</summary>
        // public int CurrentTick
        // {
        //     get { return _currentTick; }
        //     set { _currentTick = value; ValidateTimes(); NotifyStateChanged(); }
        // }
        // int _currentTick = 0;

        // /// <summary>Metadata.</summary>
        // public List<(int tick, string name)> SectionInfo
        // {
        //     get { return _sectionInfo; }
        //     set { _sectionInfo = value; _length = _sectionInfo.Last().tick; ValidateTimes(); }
        // }
        // List<(int tick, string name)> _sectionInfo = [];

        // /// <summary>Total length of the sequence.</summary>
        // public int Length
        // {
        //     get { return _length; }
        // }
        // int _length = 0;

        // /// <summary>Start of loop region.</summary>
        // public int LoopStart
        // {
        //     get { return _loopStart < 0 ? 0 : _loopStart; }
        //     set { _loopStart = value; ValidateTimes(); }
        // }
        // int _loopStart = -1; // unknown

        // /// <summary>End of loop region.</summary>
        // public int LoopEnd
        // {
        //     get { return _loopEnd < 0 ? _length : _loopEnd; }
        //     set { _loopEnd = value; ValidateTimes(); }
        // }
        // int _loopEnd = -1; // unknown
        #endregion

        #region Events
        public event EventHandler<string>? ValueChangeEvent;
        public void NotifyStateChanged([CallerMemberName] string name = "")
        {
            ValueChangeEvent?.Invoke(this, name);
        }
        #endregion

        #region Lifecycle
        /// <summary>Prevent client multiple instantiation.</summary>
        Clock() { }

        /// <summary>The singleton instance.</summary>
        public static Clock Instance
        {
            get
            {
                _instance ??= new Clock();
                return _instance;
            }
        }

        /// <summary>The singleton instance.</summary>
        static Clock? _instance;
        #endregion

        #region Public functions
        /// <summary>
        /// Convert script version into internal format.
        /// </summary>
        /// <param name="sectInfo"></param>
        public void InitSectionInfo(Dictionary<int, string> sectInfo)
        {
            _sectionInfo.Clear();
            _length = 0;//.Reset();
            _start = -1;
            _end = -1;
            _current = 0;

            if (sectInfo.Count > 0)
            {
                List<(int tick, string name)> sinfo = [];
                var spos = sectInfo.Keys.OrderBy(k => k).ToList();
                spos.ForEach(sp => _sectionInfo.Add((sp, sectInfo[sp])));

                // Also reset position stuff.
                _length = _sectionInfo.Last().tick;
                ValidateTimes();
            }
        }
        #endregion

        #region Private functions
        /// <summary>
        /// Validate and correct all times. 0 -> loop-start -> loop-end -> length
        /// </summary>
        void ValidateTimes()
        {
            if (_length > 0)
            {
                // Fix loop points.
                int lstart = _start.Sub < 0 ? 0 : _start.Sub;
                int lend = _end.Sub < 0 ? _length.Sub : _end.Sub;
                _start = Math.Min(lstart, lend);
                _end = Math.Min(lend, _length.Sub);
                _current = MathUtils.Constrain(_current.Sub, lstart, lend);
            }
            else // free-running
            {
                _start = 0;
                _end = 0;
                // _current = 0;
            }
        }
        #endregion
    }
}
