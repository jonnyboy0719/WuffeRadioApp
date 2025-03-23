namespace WuffeRadioApp.Services.Radio
{
    public class PlayingNext
    {
        public int cued_at { get; set; }
        public int played_at { get; set; }
        public double duration { get; set; }
        public string playlist { get; set; }
        public string streamer { get; set; }
        public bool is_request { get; set; }
        public Song song { get; set; }
    }
}
