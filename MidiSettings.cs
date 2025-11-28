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
    public class MidiSettings //: SettingsCore
    {
        #region Persisted editable properties
        [DisplayName("Control Color")]
        [Description("The color used for active control surfaces.")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonColorConverter))]
        public Color ControlColor { get; set; } = Color.Red;

        // [DisplayName("Output Device")]
        // [Description("Valid output device.")]
        // [Browsable(true)]
        // [Editor(typeof(DeviceTypeEditor), typeof(UITypeEditor))]
        // public string OutputDevice { get; set; } = "???";
        #endregion

        //#region Persisted non-editable properties
        //[Browsable(false)]
        //public Channel VkeyChannel { get; set; } = new();

        //[Browsable(false)]
        //public Channel ClClChannel { get; set; } = new();

        //[Browsable(false)]
        //public bool LogMidi { get; set; } = false;
        //#endregion

//////////////////////// from Nebulua ////////////////////////
        /// <summary>The current settings.</summary>
        //public static UserSettings_EX Current { get; set; } = new();

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

        [DisplayName("Background Color")]
        [Description("The color used for overall background.")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonColorConverter))]
        public Color BackColor { get; set; } = Color.AliceBlue;



//////////////////////// from MidiLib ////////////////////////


        #region Properties - persisted editable
        [DisplayName("Input Devices")]
        [Description("Valid devices if handling input.")]
        [Browsable(true)]
        [Editor(typeof(DeviceTypeEditor), typeof(UITypeEditor))]
        public List<string> InputDevices { get; set; } = new();
        //public List<DeviceSpec> InputDevices { get; set; } = new();

        [DisplayName("Output Devices")]
        [Description("Valid devices if sending output.")]
        [Browsable(true)]
        [Editor(typeof(DeviceTypeEditor), typeof(UITypeEditor))]
        public List<string> OutputDevices { get; set; } = new();
        //public List<DeviceSpec> OutputDevices { get; set; } = new();

        #endregion

    }

    // [Serializable]
    // public class DeviceSpec
    // {
    //     [DisplayName("Device Id")]
    //     [Description("User supplied id for use in client.")]
    //     [Browsable(true)]
    //     public string DeviceId { get; set; } = "";

    //     [DisplayName("Device Name")]
    //     [Description("System device name.")]
    //     [Browsable(true)]
    //     public string DeviceName { get; set; } = "";
    // }

}
