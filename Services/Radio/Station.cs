using System.Collections.Generic;

namespace WuffeRadioApp.Services.Radio
{
    public class Station
    {
        public int id { get; set; }
        public string name { get; set; }
        public string shortcode { get; set; }
        public string description { get; set; }
        public string frontend { get; set; }
        public string backend { get; set; }
        public string timezone { get; set; }
        public string listen_url { get; set; }
        public string url { get; set; }
        public string public_player_url { get; set; }
        public string playlist_pls_url { get; set; }
        public string playlist_m3u_url { get; set; }
        public bool is_public { get; set; }
        public List<StationMounts> mounts { get; set; }
        public List<string> remotes { get; set; }
        public bool hls_enabled { get; set; }
        public bool hls_is_default { get; set; }
        public string hls_url { get; set; }
        public int hls_listeners { get; set; }
    }
}
