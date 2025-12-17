using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Ephemera.NBagOfTricks;


// ? zoom, drag, shift

namespace Ephemera.MidiLibLite
{
    /// <summary>User selection options.</summary>
    public enum SnapType { Bar, Beat, Sub }

    /// <summary>The control.</summary>
    public class TimeBar : UserControl // TODO1 implement - used by TimeBar MusicTime
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
        public Font FontLarge { get; set; } = new("Microsoft Sans Serif", 20, FontStyle.Regular, GraphicsUnit.Point, 0);

        /// <summary>Baby font.</summary>
        public Font FontSmall { get; set; } = new("Microsoft Sans Serif", 10, FontStyle.Regular, GraphicsUnit.Point, 0);

        /// <summary>For drawing.</summary>
        public Color ControlColor { get; set; } = Color.Red;

        /// <summary>How to select times.</summary>
        public SnapType Snap { get; set; }
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
                _brush.Dispose();
            }
            base.Dispose(disposing);
        }
        #endregion

        #region Drawing
        /// <summary>
        /// Draw the control.
        /// </summary>
        protected override void OnPaint(PaintEventArgs pe)
        {
            // Setup.
            pe.Graphics.Clear(BackColor);

            if (RunState.Instance.IsComposition)
            {
                var vpos = Height / 2;

                // Loop area.
                int lstart = GetClientFromTick(RunState.Instance.LoopStart);
                int lend = GetClientFromTick(RunState.Instance.LoopEnd);
                pe.Graphics.DrawLine(_penMarker, lstart, 0, lstart, Height);
                pe.Graphics.DrawLine(_penMarker, lend, 0, lend, Height);
                pe.Graphics.FillPolygon(_penMarker.Brush, new PointF[] { new(lstart, vpos - 5), new(lstart, vpos + 5), new(lstart + 10, vpos) });
                pe.Graphics.FillPolygon(_penMarker.Brush, new PointF[] { new(lend, vpos - 5), new(lend, vpos + 5), new(lend - 10, vpos) });

                // Bars? vert line per bar or ?

                // Sections.
                var fsize = pe.Graphics.MeasureString("X", FontSmall).Height;

                foreach (var (tick, name) in RunState.Instance.SectionInfo)
                {
                    int sect = GetClientFromTick(tick);
                    pe.Graphics.DrawLine(_penMarker, sect, 0, sect, Height);
                    _format.Alignment = StringAlignment.Center;
                    _format.LineAlignment = StringAlignment.Center;
                    pe.Graphics.DrawString(name, FontSmall, Brushes.Black, sect + 2, Height - fsize - 2);
                }

                // Current pos.
                int cpos = GetClientFromTick(RunState.Instance.CurrentTick);
                pe.Graphics.DrawLine(_penMarker, cpos, 0, cpos, Height);
                pe.Graphics.FillPolygon(_penMarker.Brush, new PointF[] { new(cpos - 5, 0), new(cpos + 5, 0), new(cpos, 10) });
            }
            // else free-running

            // Time text.
            _format.Alignment = StringAlignment.Center;
            _format.LineAlignment = StringAlignment.Center;
            pe.Graphics.DrawString(MusicTime.Format(RunState.Instance.CurrentTick), FontLarge, Brushes.Black, ClientRectangle, _format);
            
            _format.Alignment = StringAlignment.Near;
            _format.LineAlignment = StringAlignment.Near;
            pe.Graphics.DrawString(MusicTime.Format(RunState.Instance.LoopStart), FontSmall, Brushes.Black, ClientRectangle, _format);
            
            _format.Alignment = StringAlignment.Far;
            _format.LineAlignment = StringAlignment.Near;
            pe.Graphics.DrawString(MusicTime.Format(RunState.Instance.LoopEnd), FontSmall, Brushes.Black, ClientRectangle, _format);
        }
        protected void OnPaint_BarBar(PaintEventArgs pe) //BarBar
        {
            // Setup.
            pe.Graphics.Clear(BackColor);

            // Validate times.
            MusicTime zero = new();
            _start.Constrain(zero, _length);
            _start.Constrain(zero, _end);
            _end.Constrain(zero, _length);
            _end.Constrain(_start, _length);
            _current.Constrain(_start, _end);

            // Draw the bar.
            if (_current < _length)
            {
                int dstart = Scale(_start);
                int dend = _current > _end ? Scale(_end) : Scale(_current);
                pe.Graphics.FillRectangle(_brush, dstart, 0, dend - dstart, Height);
            }

            // Draw start/end markers.
            if (_start != zero || _end != _length)
            {
                int mstart = Scale(_start);
                int mend = Scale(_end);
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
                pe.Graphics.DrawString(_start.Format(), FontSmall, Brushes.Black, ClientRectangle, _format);
                _format.Alignment = StringAlignment.Far;
                pe.Graphics.DrawString(_end.Format(), FontSmall, Brushes.Black, ClientRectangle, _format);
            }
        }

        #endregion

        #region UI handlers
        /// <summary>
        /// Handle selection operations.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (RunState.Instance.IsComposition && e.KeyData == Keys.Escape)
            {
                // Reset.
                RunState.Instance.LoopStart = -1;
                RunState.Instance.LoopEnd = -1;
                Invalidate();
            }
            base.OnKeyDown(e);
        }
        protected void OnKeyDown_BarBar(KeyEventArgs e) // BarBar
        {
            if (e.KeyData == Keys.Escape)
            {
                // Reset.
                _start.Reset();
                _end.Reset();
                Invalidate();
            }
            base.OnKeyDown(e);
        }

        /// <summary>
        /// Handle mouse position changes.
        /// </summary>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (RunState.Instance.IsComposition)
            {
                var sub = GetTickFromClient(e.X);
                var bs = GetRounded(sub, Snap);
                var sdef = GetTimeDefString(bs);

                _toolTip.SetToolTip(this, $"{MusicTime.Format(bs)} {sdef}");

                _lastXPos = e.X;

                Invalidate();
            }
            base.OnMouseMove(e);
        }
        protected void OnMouseMove_BarBar(MouseEventArgs e) // BarBar
        {
            if (e.Button == MouseButtons.Left)
            {
                _current.SetRounded(GetSubFromMouse(e.X), MusicTime.Snap);
                CurrentTimeChanged?.Invoke(this, new EventArgs());
            }
            else if (e.X != _lastXPos)
            {
                MusicTime bs = new();
                // var vv = MidiSettings.LibSettings;

                var sub = GetSubFromMouse(e.X);
                bs.SetRounded(sub, MusicTime.Snap);
                string sdef = GetTimeDefString(bs.TotalBeats);
                _toolTip.SetToolTip(this, $"{bs.Format()} {sdef}");
                _lastXPos = e.X;
            }

            Invalidate();
            base.OnMouseMove(e);
        }

        /// <summary>
        /// Selection of time and loop points.
        /// </summary>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (RunState.Instance.IsComposition)
            {
                int lstart = RunState.Instance.LoopStart;
                int lend = RunState.Instance.LoopEnd;
                int newval = GetRounded(GetTickFromClient(e.X), Snap);

                if (ModifierKeys.HasFlag(Keys.Control))
                {
                    if (newval < lend)
                    {
                        RunState.Instance.LoopStart = newval;
                    }
                    // else beeeeeep?
                }
                else if (ModifierKeys.HasFlag(Keys.Alt))
                {
                    if (newval > lstart)
                    {
                        RunState.Instance.LoopEnd = newval;
                    }
                    // else beeeeeep?
                }
                else
                {
                    RunState.Instance.CurrentTick = newval;
                }

                Invalidate();
            }
            base.OnMouseDown(e);
        }
        protected void OnMouseDown_BarBar(MouseEventArgs e) // BarBar
        {
            if (ModifierKeys.HasFlag(Keys.Control))
            {
                _start.SetRounded(GetSubFromMouse(e.X), MusicTime.Snap);
            }
            else if (ModifierKeys.HasFlag(Keys.Alt))
            {
                _end.SetRounded(GetSubFromMouse(e.X), MusicTime.Snap);
            }
            else
            {
                _current.SetRounded(GetSubFromMouse(e.X), MusicTime.Snap);
            }

            CurrentTimeChanged?.Invoke(this, new EventArgs());
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

            RunState.Instance.SectionInfo.TakeWhile(si => si.tick <= val).ForEach(si => s = si.name);

            return s;
        }
        string GetTimeDefString_BarBar(int val)// BarBar
        {
            string s = "";

            foreach (KeyValuePair<int, string> kv in TimeDefs)
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

            return s;
        }

        /// <summary>
        /// Convert x pos to tick.
        /// </summary>
        /// <param name="x"></param>
        int GetTickFromClient(int x)
        {
            int tick = 0;

            if (RunState.Instance.CurrentTick < RunState.Instance.Length)
            {
                tick = x * RunState.Instance.Length / Width;
                tick = MathUtils.Constrain(tick, 0, RunState.Instance.Length);
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
            return RunState.Instance.Length > 0 ? tick * Width / RunState.Instance.Length : 0;
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
    //}



    /////////////////////////////// MidiLib /////////////////////////////////////
    /////////////////////////////// TODO1 MidiLib stuff /////////////////////////
    /////////////////////////////// MidiLib /////////////////////////////////////

    ///// <summary>The control.</summary>
    //public class BarBar : UserControl
    //{
        //#region Fields
        ///// <summary>For tracking mouse moves.</summary>
        //int _lastXPos = 0;

        ///// <summary>Tooltip for mousing.</summary>
        //readonly ToolTip _toolTip = new();

        ///// <summary>For drawing text.</summary>
        //readonly StringFormat _format = new() { LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Center };
        //#endregion

        #region Backing fields
        readonly SolidBrush _brush = new(Color.White);
        //readonly Pen _penMarker = new(Color.Black, 1);
        MusicTime _length = new();
        MusicTime _start = new();
        MusicTime _end = new();
        MusicTime _current = new();
        #endregion

        #region Properties
        /// <summary>Total length of the bar.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]
        public MusicTime Length { get { return _length; } set { _length = value; Invalidate(); } }

        /// <summary>Start of marked region.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]
        public MusicTime Start { get { return _start; } set { _start = value; Invalidate(); } }

        /// <summary>End of marked region.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]
        public MusicTime End { get { return _end; } set { _end = value; Invalidate(); } }

        /// <summary>Where we be now.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]
        public MusicTime Current { get { return _current; } set { _current = value; Invalidate(); } }

        /// <summary>For styling.</summary>
        public Color ProgressColor { get { return _brush.Color; } set { _brush.Color = value; } }

        /// <summary>For styling.</summary>
        public Color MarkerColor { get { return _penMarker.Color; } set { _penMarker.Color = value; } }

        ///// <summary>Big font.</summary>
        //public Font FontLarge { get; set; } = new("Microsoft Sans Serif", 20, FontStyle.Regular, GraphicsUnit.Point, 0);

        ///// <summary>Baby font.</summary>
        //public Font FontSmall { get; set; } = new("Microsoft Sans Serif", 10, FontStyle.Regular, GraphicsUnit.Point, 0);

        /// <summary>All the important beat points with their names. Used also by tooltip.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]
        public Dictionary<int, string> TimeDefs { get; set; } = [];
        #endregion

        #region Events
        /// <summary>Value changed by user.</summary>
        public event EventHandler? CurrentTimeChanged;
        #endregion

        //#region Lifecycle
        ///// <summary>
        ///// Normal constructor.
        ///// </summary>
        //public BarBar()
        //{
        //    SetStyle(ControlStyles.DoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
        //}

        ///// <summary> 
        ///// Clean up any resources being used.
        ///// </summary>
        ///// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        //protected override void Dispose(bool disposing)
        //{
        //    if (disposing)
        //    {
        //        _toolTip.Dispose();
        //        _brush.Dispose();
        //        _penMarker.Dispose();
        //        _format.Dispose();
        //    }
        //    base.Dispose(disposing);
        //}
        //#endregion

        #region Drawing
        /// <summary>
        /// Draw the control.
        /// </summary>
        #endregion

        #region UI handlers
        /// <summary>
        /// Handle selection operations.
        /// </summary>
        /// <param name="e"></param>

        /// <summary>
        /// Handle mouse position changes.
        /// </summary>

        /// <summary>
        /// Handle dragging.
        /// </summary>
        #endregion




        #region Public functions
        /// <summary>
        /// Change current time. 
        /// </summary>
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
            else if (_current < _start)
            {
                _current.SetRounded(_start.TotalSubs, SnapType.Sub);
                done = true;
            }
            else if (_current > _end)
            {
                _current.SetRounded(_end.TotalSubs, SnapType.Sub);
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
            _start.Reset();
            _end.Reset();

            Invalidate();
        }
        /// <summary>
        /// Gets the time def string associated with val.
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        #endregion

        #region Private functions
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
    }
    
}
