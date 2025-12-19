#define _NEW_ML

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Runtime.CompilerServices;
using Ephemera.NBagOfTricks;


// TODO2 ? zoom, drag, shift

// Basic version is from Nebulua, simpler. MidiLib version knows about MusicTime.
// TODO1 Look at how client uses API. Nebulator, Midifrier
// - TimeDefs => use SectionInfo
// - public event EventHandler? CurrentTimeChanged;
// - IncrementCurrent(1)
// - MidiSettings.LibSettings


namespace Ephemera.MidiLibLite
{
    /// <summary>The control.</summary>
    public class TimeBar : UserControl
    {
        #region Fields
        /// <summary>For tracking mouse moves.</summary>
        int _lastXPos = 0;

        /// <summary>Tooltip for mousing.</summary>
        readonly ToolTip _toolTip = new();

        /// <summary>For drawing text.</summary>
        readonly StringFormat _format = new() { LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Center };

        /// <summary>For drawing lines etc.</summary>
        readonly Pen _penMarker = new(Color.Red, 1);
        #endregion

        #region Properties
        /// <summary>Big font.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]
        public Font FontLarge { get; set; } = new("Microsoft Sans Serif", 20, FontStyle.Regular, GraphicsUnit.Point, 0);

        /// <summary>Baby font.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]
        public Font FontSmall { get; set; } = new("Microsoft Sans Serif", 10, FontStyle.Regular, GraphicsUnit.Point, 0);

        /// <summary>Drawing the active elements of a control.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]
        public Color ControlColor { get; set; } = Color.Red;

        /// <summary>Drawing the control when selected.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]
        public Color SelectedColor { get; set; } = Color.Blue;

        /// <summary>How to select times.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]
        public SnapType Snap { get; set; }
        #endregion



        #region Properties => MidiLib
        /// <summary>Total length of the sequence.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]
        public MusicTime Length { get { return _length; } set { _length = value; Invalidate(); } }
        MusicTime _length = new();

        /// <summary>Start of marked region.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]
        public MusicTime SelStart { get { return _selStart; } set { _selStart = value; ValidateTimes(); Invalidate(); } }
        MusicTime _selStart = new();

        /// <summary>End of marked region.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]
        public MusicTime SelEnd { get { return _selEnd; } set { _selEnd = value; ValidateTimes(); Invalidate(); } }
        MusicTime _selEnd = new();

        /// <summary>Where we be now.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]
        public MusicTime Current { get { return _current; } set { _current = value; ValidateTimes(); NotifyStateChanged(); Invalidate(); } }
        MusicTime _current = new();

// /// <summary>All the important beat points with their names. Used also by tooltip.</summary>
// [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]
// public Dictionary<int, string> TimeDefs { get; set; } = [];

        #endregion




//////////////////////////// added ///////////////////////////////////
//////////////////////////// added ///////////////////////////////////
//////////////////////////// added ///////////////////////////////////
        /// <summary>Metadata.</summary>
        public List<(int tick, string name)> SectionInfo //TODO1 doesn't really belong here
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


        #region Events
        public event EventHandler<string>? ValueChangeEvent;
        public void NotifyStateChanged([CallerMemberName] string name = "")
        {
            ValueChangeEvent?.Invoke(this, name);
        }
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
            _selStart = 0;
            _selEnd = 0;
            _current = 0;

            if (sectInfo.Count > 0)
            {
                List<(int tick, string name)> sinfo = [];
                var spos = sectInfo.Keys.OrderBy(k => k).ToList();
                spos.ForEach(sp => _sectionInfo.Add((sp, sectInfo[sp])));

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
                int lstart = _selStart.Sub < 0 ? 0 : _selStart.Sub;
                int lend = _selEnd.Sub < 0 ? _length.Sub : _selEnd.Sub;
                _selStart = Math.Min(lstart, lend);
                _selEnd = Math.Min(lend, _length.Sub);
                _current = MathUtils.Constrain(_current.Sub, lstart, lend);
            }
            else // free-running
            {
                _selStart = 0;
                _selEnd = 0;
                // _current = 0;
            }
        }
        #endregion
//////////////////////////// added ///////////////////////////////////
//////////////////////////// added ///////////////////////////////////
//////////////////////////// added ///////////////////////////////////












        #region Events => MidiLib OK
        /// <summary>Value changed by user.</summary>
        public event EventHandler? CurrentTimeChanged; //TODO1
        #endregion

        #region Public functions => MidiLib OK
        /// <summary>Change current time programmatically.</summary>
        /// <param name="num">Subs/ticks.</param>
        /// <returns>True if at the end of the sequence.</returns>
        public bool IncrementCurrent(int num)
        {
            bool done = false;

            _current.Increment(num);

            if (_current < new MusicTime(0))
            {
                _current.Reset();
            }
            else if (_current < _selStart)
            {
                _current.SetRounded(_selStart.TotalSubs, SnapType.Sub);
                done = true;
            }
            else if (_current > _selEnd)
            {
                _current.SetRounded(_selEnd.TotalSubs, SnapType.Sub);
                done = true;
            }

            Invalidate();

            return done;
        }

        /// <summary>
        /// Clear everything.
        /// </summary>
        public void Reset()
        {
            _lastXPos = 0;
            _length.Reset();
            _current.Reset();
            _selStart.Reset();
            _selEnd.Reset();

            Invalidate();
        }
        #endregion

        #region Private functions => MidiLib OK
        /// <summary>
        /// Convert x pos to sub.
        /// </summary>
        /// <param name="x"></param>
        int GetSubFromMouse(int x)
        {
            int sub = 0;

            if(_current < _length)
            {
                sub = x * _length.TotalSubs / Width;
                sub = MathUtils.Constrain(sub, 0, _length.TotalSubs);
            }

            return sub;
        }

        /// <summary>
        /// Map from time to UI pixels.
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public int Scale(MusicTime val)
        {
            return val.TotalSubs * Width / _length.TotalSubs;
        }
        #endregion






        #region Lifecycle
        /// <summary>
        /// Normal constructor.
        /// </summary>
        public TimeBar()
        {
            SetStyle(ControlStyles.DoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
        }

        /// <summary>
        /// Later init.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            _penMarker.Color = ControlColor;
            base.OnLoad(e);
        }

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _toolTip.Dispose();
                _penMarker.Dispose();
                _format.Dispose();
//                _brush.Dispose();
            }
            base.Dispose(disposing);
        }
        #endregion

        #region Drawing
        /// <summary>
        /// Draw the control.
        /// </summary>
        protected override void OnPaint(PaintEventArgs pe) // Nebulua simple
        {
            // Setup.
            pe.Graphics.Clear(BackColor);

#if _NEW_ML

            if (!Clock.Instance.IsFreeRunning)
            {
                var vpos = Height / 2;

                // Loop area.
                int lstart = GetClientFromTick(Clock.Instance.SelStart.Sub);
                int lend = GetClientFromTick(Clock.Instance.SelEnd.Sub);
                pe.Graphics.DrawLine(_penMarker, lstart, 0, lstart, Height);
                pe.Graphics.DrawLine(_penMarker, lend, 0, lend, Height);
                pe.Graphics.FillPolygon(_penMarker.Brush, new PointF[] { new(lstart, vpos - 5), new(lstart, vpos + 5), new(lstart + 10, vpos) });
                pe.Graphics.FillPolygon(_penMarker.Brush, new PointF[] { new(lend, vpos - 5), new(lend, vpos + 5), new(lend - 10, vpos) });

                // Bars? vert line per bar or ?

                // Sections.
                var fsize = pe.Graphics.MeasureString("X", FontSmall).Height;

                foreach (var (tick, name) in Clock.Instance.SectionInfo)
                {
                    int sect = GetClientFromTick(tick);
                    pe.Graphics.DrawLine(_penMarker, sect, 0, sect, Height);
                    _format.Alignment = StringAlignment.Center;
                    _format.LineAlignment = StringAlignment.Center;
                    pe.Graphics.DrawString(name, FontSmall, Brushes.Black, sect + 2, Height - fsize - 2);
                }

                // Current pos.
                int cpos = GetClientFromTick(Clock.Instance.Current.Sub);
                pe.Graphics.DrawLine(_penMarker, cpos, 0, cpos, Height);
                pe.Graphics.FillPolygon(_penMarker.Brush, new PointF[] { new(cpos - 5, 0), new(cpos + 5, 0), new(cpos, 10) });
            }
            // else free-running

            // Time text.
            _format.Alignment = StringAlignment.Center;
            _format.LineAlignment = StringAlignment.Center;
            pe.Graphics.DrawString(Clock.Instance.Current.ToString(), FontLarge, Brushes.Black, ClientRectangle, _format);
            //pe.Graphics.DrawString(MusicTime.Format(Clock.Instance.Current.Sub), FontLarge, Brushes.Black, ClientRectangle, _format);

            _format.Alignment = StringAlignment.Near;
            _format.LineAlignment = StringAlignment.Near;
            pe.Graphics.DrawString(Clock.Instance.SelStart.ToString(), FontSmall, Brushes.Black, ClientRectangle, _format);
            //pe.Graphics.DrawString(MusicTime.Format(Clock.Instance.SelStart.Sub), FontSmall, Brushes.Black, ClientRectangle, _format);

            _format.Alignment = StringAlignment.Far;
            _format.LineAlignment = StringAlignment.Near;
            pe.Graphics.DrawString(Clock.Instance.SelEnd.ToString(), FontSmall, Brushes.Black, ClientRectangle, _format);
            //pe.Graphics.DrawString(MusicTime.Format(Clock.Instance.SelEnd.Sub), FontSmall, Brushes.Black, ClientRectangle, _format);

#else // !_NEW_ML TODO1
            // Setup.
            pe.Graphics.Clear(BackColor);

            // Validate times.
            MusicTime zero = new();
            _selStart.Constrain(zero, _length);
            _selStart.Constrain(zero, _selEnd);
            _selEnd.Constrain(zero, _length);
            _selEnd.Constrain(_selStart, _length);
            _current.Constrain(_selStart, _selEnd);

            // Draw the bar.
            if (_current < _length)
            {
                int dstart = Scale(_selStart);
                int dend = _current > _selEnd ? Scale(_selEnd) : Scale(_current);
                pe.Graphics.FillRectangle(_penMarker.Brush, dstart, 0, dend - dstart, Height); // was _brush
            }

            // Draw start/end markers.
            if (_selStart != zero || _selEnd != _length)
            {
                int mstart = Scale(_selStart);
                int mend = Scale(_selEnd);
                pe.Graphics.DrawLine(_penMarker, mstart, 0, mstart, Height);
                pe.Graphics.DrawLine(_penMarker, mend, 0, mend, Height);
            }

            // Text.
            if (DesignMode) // Can't access LibSettings yet.
            {
                _format.Alignment = StringAlignment.Center;
                pe.Graphics.DrawString("CENTER", FontLarge, Brushes.Black, ClientRectangle, _format);
                _format.Alignment = StringAlignment.Near;
                pe.Graphics.DrawString("NEAR", FontSmall, Brushes.Black, ClientRectangle, _format);
                _format.Alignment = StringAlignment.Far;
                pe.Graphics.DrawString("FAR", FontSmall, Brushes.Black, ClientRectangle, _format);
            }
            else
            {
                _format.Alignment = StringAlignment.Center;
                pe.Graphics.DrawString(_current.Format(), FontLarge, Brushes.Black, ClientRectangle, _format);
                _format.Alignment = StringAlignment.Near;
                pe.Graphics.DrawString(_selStart.Format(), FontSmall, Brushes.Black, ClientRectangle, _format);
                _format.Alignment = StringAlignment.Far;
                pe.Graphics.DrawString(_selEnd.Format(), FontSmall, Brushes.Black, ClientRectangle, _format);
            }
#endif

        }
        #endregion

        #region UI handlers
        /// <summary>
        /// Handle selection operations.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnKeyDown(KeyEventArgs e) // Nebulua simple
        {
#if _NEW_ML
            if (!Clock.Instance.IsFreeRunning && e.KeyData == Keys.Escape)
            {
                // Reset.
                Clock.Instance.SelStart = -1;
                Clock.Instance.SelEnd = -1;
                Invalidate();
            }
#else // !_NEW_ML TODO1
            if (e.KeyData == Keys.Escape)
            {
                // Reset.
                _selStart.Reset();
                _selEnd.Reset();
                Invalidate();
            }
#endif
            base.OnKeyDown(e);
        }

        /// <summary>
        /// Handle mouse position changes.
        /// </summary>
        protected override void OnMouseMove(MouseEventArgs e) // Nebulua simple
        {
#if _NEW_ML
         if (!Clock.Instance.IsFreeRunning)
            {
                var sub = GetTickFromClient(e.X);
                var bs = GetRounded(sub, Snap);
                var sdef = GetTimeDefString(bs);

                _toolTip.SetToolTip(this, $"TODO1 {bs} {sdef}");

                _lastXPos = e.X;
            }
#else // !_NEW_ML TODO1
            if (e.Button == MouseButtons.Left)
            {
                _current.SetRounded(GetSubFromMouse(e.X), Snap);
                CurrentTimeChanged?.Invoke(this, new EventArgs());
            }
            else if (e.X != _lastXPos)
            {
                MusicTime bs = new();
                // var vv = MidiSettings.LibSettings;

                var sub = GetSubFromMouse(e.X);
                bs.SetRounded(sub, Snap);
                string sdef = GetTimeDefString(bs.TotalBeats);
                _toolTip.SetToolTip(this, $"{bs.Format()} {sdef}");
                _lastXPos = e.X;
            }
#endif
            Invalidate();

            base.OnMouseMove(e);
        }

        /// <summary>
        /// Selection of time and loop points.
        /// </summary>
        protected override void OnMouseDown(MouseEventArgs e) // Nebulua simple
        {
#if _NEW_ML
            if (!Clock.Instance.IsFreeRunning)
            {
                int lstart = Clock.Instance.SelStart.Sub;
                int lend = Clock.Instance.SelEnd.Sub;
                int newval = GetRounded(GetTickFromClient(e.X), Snap);

                if (ModifierKeys.HasFlag(Keys.Control))
                {
                    if (newval < lend)
                    {
                        Clock.Instance.SelStart = newval;
                    }
                    // else beeeeeep?
                }
                else if (ModifierKeys.HasFlag(Keys.Alt))
                {
                    if (newval > lstart)
                    {
                        Clock.Instance.SelEnd = newval;
                    }
                    // else beeeeeep?
                }
                else
                {
                    Clock.Instance.Current = newval;
                }
            }
#else // !_NEW_ML TODO1
            if (ModifierKeys.HasFlag(Keys.Control))
            {
                _selStart.SetRounded(GetSubFromMouse(e.X), Snap);
            }
            else if (ModifierKeys.HasFlag(Keys.Alt))
            {
                _selEnd.SetRounded(GetSubFromMouse(e.X), Snap);
            }
            else
            {
                _current.SetRounded(GetSubFromMouse(e.X), Snap);
            }

            CurrentTimeChanged?.Invoke(this, new EventArgs());
#endif
            Invalidate();
            base.OnMouseDown(e);
        }
#endregion

        #region Private functions
        /// <summary>
        /// Gets the time def string associated with val.
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        static string GetTimeDefString(int val)
        {
            string s = "";
#if _NEW_ML
            Clock.Instance.SectionInfo.TakeWhile(si => si.tick <= val).ForEach(si => s = si.name);

#else // !_NEW_ML TODO1

            foreach (KeyValuePair<int, string> kv in TimeDefs) // => Clock.Instance.SectionInfo
            {
                if (kv.Key > val)
                {
                    break;
                }
                else
                {
                    s = kv.Value;
                }
            }

#endif

            return s;
        }

        /// <summary>
        /// Convert x pos to tick.
        /// </summary>
        /// <param name="x"></param>
        int GetTickFromClient(int x)
        {
            int tick = 0;

            if (Clock.Instance.Current < Clock.Instance.Length)
            {
                tick = x * Clock.Instance.Length.Sub / Width;
                tick = MathUtils.Constrain(tick, 0, Clock.Instance.Length.Sub);
            }

            return tick;
        }

        /// <summary>
        /// Map from time to UI pixels.
        /// </summary>
        /// <param name="tick"></param>
        /// <returns></returns>
        int GetClientFromTick(int tick)
        {
            return Clock.Instance.Length > 0 ? tick * Width / Clock.Instance.Length.Sub : 0;
        }

        /// <summary>
        /// Set to sub using specified rounding.
        /// </summary>
        /// <param name="tick"></param>
        /// <param name="snapType"></param>
        /// <param name="up">To ceiling otherwise closest.</param>
        static int GetRounded(int tick, SnapType snapType, bool up = false)
        {
            if (tick > 0 && snapType != SnapType.Sub)
            {
                int res = snapType == SnapType.Bar ? MusicTime.SubsPerBar : MusicTime.SubsPerBeat;

                double dtick = Math.Floor((double)tick);
                int floor = (int)(dtick / res) * res;
                int ceiling = floor + res;

                tick = (up || (ceiling - tick) < res / 2) ? ceiling : floor;
            }

            return tick;
        }
        #endregion
    }
}
