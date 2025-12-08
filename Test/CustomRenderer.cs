using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.ComponentModel.DataAnnotations;
using Ephemera.NBagOfTricks;
using Ephemera.MidiLibLite;


namespace Ephemera.MidiLib.Test
{
    public class CustomRenderer : UserControl
    {
        /// <summary>Required designer variable.</summary>
        IContainer components;

        ToolTip toolTip;

        /// <summary>Tracking for note off.</summary>
        int _lastNote = -1;

        /// <summary>Context.</summary>
        public ChannelHandle ChannelHandle { get; init; }

        /// <summary>UI midi send.</summary>
        public event EventHandler<BaseMidiEvent>? SendMidi;

        /// <summary>Constructor.</summary>
        public CustomRenderer()
        {
            InitializeComponent();
        }

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            toolTip = new System.Windows.Forms.ToolTip(components);
            SuspendLayout();

            // CustomRenderer
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 19F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Name = "CustomRenderer";
            Size = new System.Drawing.Size(231, 174);
            ResumeLayout(false);
            PerformLayout();
        }

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        /// <summary>Paint the surface.</summary>
        /// <param name="e"></param>
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            g.FillRectangle(Brushes.LightCoral, ClientRectangle);

            // Border.
            g.DrawLine(Pens.Black, Left, Top, Right, Top);
            g.DrawLine(Pens.Black, Left, Bottom, Right, Bottom);
            g.DrawLine(Pens.Black, Left, Top, Left, Bottom);
            g.DrawLine(Pens.Black, Right, Top, Right, Bottom);

            // Grid.
            for (int x = Left; x < Right; x += 25)
            {
                g.DrawLine(Pens.Black, x, Top, x, Bottom);
            }

            for (int y = Bottom; y > Top; y -= 25)
            {
                g.DrawLine(Pens.Black, Left, y, Right, y);
            }

            base.OnPaint(e);
        }

        /// <summary>
        /// Show the pixel info.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            var res = MouseToUser();

            if (res is not null)
            {
                toolTip.SetToolTip(this, $"X:{res.Value.ux} Y:{res.Value.uy}");

                // Also gen click?
                if (e.Button == MouseButtons.Left)
                {
                    // Dragging. Did it change?
                    if (_lastNote != res.Value.ux)
                    {
                        if (_lastNote != -1)
                        {
                            // Turn off last note.
                            SendMidi?.Invoke(this, new NoteOff(ChannelHandle.ChannelNumber, _lastNote));
                        }

                        // Start the new note.
                        _lastNote = res.Value.ux;
                        SendMidi?.Invoke(this, new NoteOn(ChannelHandle.ChannelNumber, res.Value.ux, res.Value.uy));
                    }
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
            var res = MouseToUser();
            if (res is not null)
            {
                _lastNote = res.Value.ux;
                SendMidi?.Invoke(this, new NoteOn(ChannelHandle.ChannelNumber, res.Value.ux, res.Value.uy));
            }

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
                SendMidi?.Invoke(this, new NoteOff(ChannelHandle.ChannelNumber, _lastNote));
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
                SendMidi?.Invoke(this, new NoteOff(ChannelHandle.ChannelNumber, _lastNote));
            }

            // Reset and tell client.
            _lastNote = -1;

            base.OnMouseLeave(e);
        }

        /// <summary>
        /// Get mouse x and y mapped to user coordinates.
        /// </summary>
        /// <returns>Tuple of x and y.</returns>
        (int ux, int uy)? MouseToUser()
        {
            var mp = PointToClient(MousePosition);
            // Map and check.
            int x = MathUtils.Map(mp.X, Left, Right, 0, MidiDefs.MAX_MIDI);
            int y = MathUtils.Map(mp.Y, Top, Bottom, 0, MidiDefs.MAX_MIDI);
            return (x, y);
        }
    }
}
