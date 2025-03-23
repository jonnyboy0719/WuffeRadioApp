using System.Collections.Generic;

namespace WuffeRadioApp.Services.Radio
{
    public class StationMounts
    {
        public int id { get; set; }
        public string name { get; set; }
        public string url { get; set; }
        public int bitrate { get; set; }
        public string format { get; set; }
        public Listeners listeners { get; set; }
        public string path { get; set; }
        public bool is_default { get; set; }
    }
}
