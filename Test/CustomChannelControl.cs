using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Ephemera.NBagOfTricks;


namespace Ephemera.MidiLibLite.Test
{
    public class CustomChannelControl : ChannelControl
    {
        #region Fields
        /// <summary>Background image data.</summary>
        PixelBitmap? _bmp;

        /// <summary>The pen.</summary>
        readonly Pen _pen = new(Color.Red, 2);

        int _lastNote = 0;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Normal constructor.
        /// </summary>
        public CustomChannelControl()
        {
            SetStyle(ControlStyles.DoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
            Name = nameof(CustomChannelControl);
        }

        /// <summary>
        /// Init after properties set.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            //DrawBitmap();

            base.OnLoad(e);
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            _bmp?.Dispose();
            _pen?.Dispose();

            base.Dispose(disposing);
        }
        #endregion

        #region Event handlers
        /// <summary>
        /// Paint the surface.
        /// </summary>
        /// <param name="pe"></param>
        protected override void OnPaint(PaintEventArgs pe)
        {
            Graphics g = pe.Graphics;
            var r = DrawRect;

            // Draw background.
            for (var y = r.Top; y < r.Bottom; y++)
            {
                for (var x = r.Left; x < r.Right; x++)
                {
                    _bmp!.SetPixel(x, y, Color.FromArgb(255, x * 256 / r.Width, y * 256 / r.Height, 150));
                }
            }

            // Border.
            g.DrawLine(_pen, r.Left, r.Top, r.Right, r.Top);
            g.DrawLine(_pen, r.Left, r.Bottom, r.Right, r.Bottom);
            g.DrawLine(_pen, r.Left, r.Top, r.Left, r.Bottom);
            g.DrawLine(_pen, r.Right, r.Top, r.Right, r.Bottom);

            // Grid.
            for (int x = r.Left; x < r.Right; x += 10)
            {
                g.DrawLine(_pen, x, r.Top, x, r.Bottom);
            }

            base.OnPaint(pe);
        }

        /// <summary>
        /// Show the pixel info.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            var (ux, uy) = MouseToUser();

            // Also gen click?
            if (e.Button == MouseButtons.Left)
            {
                // Dragging. Did it change?
                if (_lastNote != ux)
                {
                    if (_lastNote != -1)
                    {
                        // Turn off last note.
                        OnSendMidi(new NoteOff(BoundChannel.ChannelNumber, _lastNote));
                    }

                    // Start the new note.
                    _lastNote = ux;
                    OnSendMidi(new NoteOn(BoundChannel.ChannelNumber, ux, uy));
                }
            }

            base.OnMouseMove(e);
        }

        /// <summary>
        /// Send info to client.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            var (ux, uy) = MouseToUser();
            _lastNote = ux;
            OnSendMidi(new NoteOn(BoundChannel.ChannelNumber, ux, uy));

            base.OnMouseDown(e);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (_lastNote != -1)
            {
                OnSendMidi(new NoteOff(BoundChannel.ChannelNumber, _lastNote));
                _lastNote = -1;
            }

            base.OnMouseUp(e);
        }

        /// <summary>
        /// Disable control
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseLeave(EventArgs e)
        {
            // Turn off last click.
            if (_lastNote != -1)
            {
                OnSendMidi(new NoteOff(BoundChannel.ChannelNumber, _lastNote));
            }

            // Reset and tell client.
            _lastNote = -1;

            base.OnMouseLeave(e);
        }
        #endregion

        #region Private functions
        /// <summary>
        /// Get mouse x and y mapped to user coordinates.
        /// </summary>
        /// <returns>Tuple of x and y.</returns>
        (int ux, int uy) MouseToUser()
        {
            var mp = PointToClient(MousePosition);

            return (mp.X, mp.Y);
        }
        #endregion
    }
}
