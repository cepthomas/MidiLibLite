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
    public class UserSettings : SettingsCore
    {
        #region Persisted editable properties
        [DisplayName("Control Color")]
        [Description("The color used for active control surfaces.")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonColorConverter))]
        public Color ControlColor { get; set; } = Color.Red;

        [DisplayName("Output Device")]
        [Description("Valid output device.")]
        [Browsable(true)]
//        [Editor(typeof(DeviceTypeEditor), typeof(UITypeEditor))]
        public string OutputDevice { get; set; } = "???";
        #endregion

        #region Persisted non-editable properties
        [Browsable(false)]
        public Channel VkeyChannel { get; set; } = new();

        [Browsable(false)]
        public Channel ClClChannel { get; set; } = new();

        [Browsable(false)]
        public bool LogMidi { get; set; } = false;
        #endregion

        //////////////////////// from Nebulua
        /// <summary>The current settings.</summary>
        public static UserSettings Current { get; set; } = new();

        [DisplayName("Active Color")]
        [Description("Active control surfaces.")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonColorConverter))]
        public Color ActiveColor { get; set; } = Color.DodgerBlue;

        [DisplayName("Selected Color")]
        [Description("The color used for selected controls.")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonColorConverter))]
        public Color SelectedColor { get; set; } = Color.Moccasin;

    }
}
