using System.Collections.Generic;

namespace WuffeRadioApp.Services.Radio
{
    public class RadioInfo
    {
        public Station station { get; set; }
        public Listeners listeners { get; set; }
        public Live live { get; set; }
        public NowPlaying now_playing { get; set; }
        public PlayingNext playing_next { get; set; }
        public List<SongHistory> song_history { get; set; }
        public bool is_online { get; set; }
        public string cache { get; set; }
    }
}
