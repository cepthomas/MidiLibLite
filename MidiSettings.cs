using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Design;
using System.Text.Json.Serialization;
using System.Windows.Forms.Design;
using System.IO;
using Ephemera.NBagOfTricks;


namespace Ephemera.MidiLibLite
{
    [Serializable]
    public class MidiSettings
    {
        /// <summary>The current settings.</summary>
        //public static MidiSettings Current { get; set; } = new();

        #region Persisted editable properties
        [DisplayName("Control Color")]
        [Description("The color used for active control surfaces.")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonColorConverter))]
        public Color ControlColor { get; set; } = Color.Red; // from MidiGen

        [DisplayName("Active Color")]
        [Description("Active control surfaces.")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonColorConverter))]
        public Color ActiveColor { get; set; } = Color.DodgerBlue; // from Nebulua

        [DisplayName("Selected Color")]
        [Description("The color used for selected controls.")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonColorConverter))]
        public Color SelectedColor { get; set; } = Color.Moccasin; // from Nebulua

        [DisplayName("Background Color")]
        [Description("The color used for overall background.")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonColorConverter))]
        public Color BackColor { get; set; } = Color.AliceBlue; // from Nebulua
        #endregion

        #region Properties - persisted editable
        [DisplayName("Input Devices")]
        [Description("Valid devices if handling input.")]
        [Browsable(true)]
        [Editor(typeof(DeviceTypeEditor), typeof(UITypeEditor))]
        public List<string> InputDevices { get; set; } = new(); // from MidiLib
        //public List<DeviceSpec> InputDevices { get; set; } = new();

        [DisplayName("Output Devices")]
        [Description("Valid devices if sending output.")]
        [Browsable(true)]
        [Editor(typeof(DeviceTypeEditor), typeof(UITypeEditor))]
        public List<string> OutputDevices { get; set; } = new();// from MidiLib
        //public List<DeviceSpec> OutputDevices { get; set; } = new();
        #endregion
    }
}
