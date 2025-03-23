#if DISCORD_RPC
using DiscordRPC;
using DiscordRPC.Logging;
#endif
using System.ComponentModel;
using System.Drawing.Drawing2D;
using NAudio.Wave;
using System.Diagnostics;
using System.Net;
using Newtonsoft.Json;

namespace WuffeRadioApp
{
    public partial class Form1 : Form
    {
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        private SynchronizationContext _syncContext;
        #if DISCORD_RPC
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public DiscordRpcClient? DiscordClient;
        #endif
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        private bool FirstMount = false;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public static DateTime CheckRadioTime { get; private set; }

        // START - NAUDIO
        enum StreamingPlaybackState
        {
            Stopped,
            Playing,
            Buffering,
            Paused
        }
        // STOP - NAUDIO

        // START - ALLOW DRAG WITHOUT TITLE BAR
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool ReleaseCapture();
        // END - ALLOW DRAG WITHOUT TITLE BAR

        #if DISCORD_RPC
        void InitializeDiscord()
        {
            DiscordClient = new DiscordRpcClient( DiscordRPC.ClientID );
            DiscordClient.Initialize();
        }

        private void ClearRichPresence()
        {
            if (DiscordClient is null) return;
            DiscordClient.ClearPresence();
        }
        #endif

        public Form1()
        {
            InitializeComponent();
            #if DISCORD_RPC
            InitializeDiscord();
            #endif
            CheckRadioTime = DateTime.Now + TimeSpan.FromSeconds(1.5);
            volumeSlider1.VolumeChanged += OnVolumeSliderChanged;
            Disposed += Form1_Disposing;
        }

        // START - NAUDIO
        void OnVolumeSliderChanged(object sender, EventArgs e)
        {
            if (volumeProvider != null)
            {
                volumeProvider.Volume = volumeSlider1.Volume;
            }
        }

        private BufferedWaveProvider bufferedWaveProvider;
        private IWavePlayer waveOut;
        private volatile StreamingPlaybackState playbackState;
        private volatile bool fullyDownloaded;
        private static HttpClient httpClient;
        private VolumeWaveProvider16 volumeProvider;

        delegate void ShowErrorDelegate(string message);

        private void ShowError(string message)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new ShowErrorDelegate(ShowError), message);
            }
            else
            {
                MessageBox.Show(message);
            }
        }

        private void StreamMp3(object state)
        {
            fullyDownloaded = false;
            var url = (string)state;
            if (httpClient == null) httpClient = new HttpClient();
            Stream stream;
            try
            {
                stream = httpClient.GetStreamAsync(url).Result;
            }
            catch (Exception e)
            {
                ShowError(e.Message);
                return;
            }
            var buffer = new byte[16384 * 4]; // needs to be big enough to hold a decompressed frame

            IMp3FrameDecompressor decompressor = null;
            try
            {
                using (stream)
                {
                    var readFullyStream = new ReadFullyStream(stream);
                    do
                    {
                        if (IsBufferNearlyFull)
                        {
                            Debug.WriteLine("Buffer getting full, taking a break");
                            Thread.Sleep(500);
                        }
                        else
                        {
                            Mp3Frame frame;
                            try
                            {
                                frame = Mp3Frame.LoadFromStream(readFullyStream);
                            }
                            catch (EndOfStreamException)
                            {
                                fullyDownloaded = true;
                                // reached the end of the MP3 file / stream
                                break;
                            }
                            catch (WebException)
                            {
                                // probably we have aborted download from the GUI thread
                                break;
                            }
                            if (frame == null) break;
                            if (decompressor == null)
                            {
                                // don't think these details matter too much - just help ACM select the right codec
                                // however, the buffered provider doesn't know what sample rate it is working at
                                // until we have a frame
                                decompressor = CreateFrameDecompressor(frame);
                                bufferedWaveProvider = new BufferedWaveProvider(decompressor.OutputFormat);
                                bufferedWaveProvider.BufferDuration =
                                    TimeSpan.FromSeconds(20); // allow us to get well ahead of ourselves
                                //this.bufferedWaveProvider.BufferedDuration = 250;
                            }
                            int decompressed = decompressor.DecompressFrame(frame, buffer, 0);
                            //Debug.WriteLine(String.Format("Decompressed a frame {0}", decompressed));
                            bufferedWaveProvider.AddSamples(buffer, 0, decompressed);
                        }

                    } while (playbackState != StreamingPlaybackState.Stopped);
                    // was doing this in a finally block, but for some reason
                    // we are hanging on response stream .Dispose so never get there
                    decompressor.Dispose();
                }
            }
            finally
            {
                if (decompressor != null)
                {
                    decompressor.Dispose();
                }
            }
        }

        private static IMp3FrameDecompressor CreateFrameDecompressor(Mp3Frame frame)
        {
            WaveFormat waveFormat = new Mp3WaveFormat(frame.SampleRate, frame.ChannelMode == ChannelMode.Mono ? 1 : 2,
                frame.FrameLength, frame.BitRate);
            return new AcmMp3FrameDecompressor(waveFormat);
        }

        private bool IsBufferNearlyFull
        {
            get
            {
                return bufferedWaveProvider != null &&
                       bufferedWaveProvider.BufferLength - bufferedWaveProvider.BufferedBytes
                       < bufferedWaveProvider.WaveFormat.AverageBytesPerSecond / 4;
            }
        }

        private IWavePlayer CreateWaveOut()
        {
            return new WaveOut();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (playbackState != StreamingPlaybackState.Stopped)
            {
                if (waveOut == null && bufferedWaveProvider != null)
                {
                    Debug.WriteLine("Creating WaveOut Device");
                    waveOut = CreateWaveOut();
                    waveOut.PlaybackStopped += OnPlaybackStopped;
                    volumeProvider = new VolumeWaveProvider16(bufferedWaveProvider);
                    volumeProvider.Volume = volumeSlider1.Volume;
                    waveOut.Init(volumeProvider);
                }
                else if (bufferedWaveProvider != null)
                {
                    var bufferedSeconds = bufferedWaveProvider.BufferedDuration.TotalSeconds;
                    // make it stutter less if we buffer up a decent amount before playing
                    if (bufferedSeconds < 0.5 && playbackState == StreamingPlaybackState.Playing && !fullyDownloaded)
                    {
                        Pause();
                    }
                    else if (bufferedSeconds > 4 && playbackState == StreamingPlaybackState.Buffering)
                    {
                        Play();
                    }
                    else if (fullyDownloaded && bufferedSeconds == 0)
                    {
                        Debug.WriteLine("Reached end of stream");
                        StopPlayback();
                    }
                }

            }
        }

        private void Play()
        {
            waveOut.Play();
            Debug.WriteLine(String.Format("Started playing, waveOut.PlaybackState={0}", waveOut.PlaybackState));
            playbackState = StreamingPlaybackState.Playing;
        }

        private void StopPlayback()
        {
            if (playbackState != StreamingPlaybackState.Stopped)
            {
                if (!fullyDownloaded)
                {
                    //webRequest.Abort();
                }

                playbackState = StreamingPlaybackState.Stopped;
                if (waveOut != null)
                {
                    waveOut.Stop();
                    waveOut.Dispose();
                    waveOut = null;
                }
                timer1.Enabled = false;
                // n.b. streaming thread may not yet have exited
                Thread.Sleep(500);
            }
        }

        private void OnPlaybackStopped(object sender, StoppedEventArgs e)
        {
            Debug.WriteLine("Playback Stopped");
            if (e.Exception != null)
            {
                MessageBox.Show(String.Format("Playback Error {0}", e.Exception.Message));
            }
        }

        private void Pause()
        {
            playbackState = StreamingPlaybackState.Buffering;
            waveOut.Pause();
            Debug.WriteLine(String.Format("Paused to buffer, waveOut.PlaybackState={0}", waveOut.PlaybackState));
        }

        private void Form1_Disposing(object sender, EventArgs e)
        {
            StopPlayback();
        }
        // STOP - NAUDIO

        private void Form1_Load(object sender, EventArgs e)
        {
            song_name.Location = song_cover_art.PointToClient(song_name.Parent.PointToScreen(song_name.Location));
            artist.Location = song_cover_art.PointToClient(artist.Parent.PointToScreen(artist.Location));

            song_name.Parent = song_cover_art;
            artist.Parent = song_cover_art;
            song_name.BackColor = Color.Transparent;
            artist.BackColor = Color.Transparent;
            _syncContext = SynchronizationContext.Current;

            playbackState = StreamingPlaybackState.Buffering;

            var timer = new System.Timers.Timer(150);
            timer.Elapsed += (sender, args) => { OnTimerUpdated(); };
            timer.Start();
        }

        private void OnTimerUpdated()
        {
            #if DISCORD_RPC
            if (DiscordClient != null)
                DiscordClient.Invoke();
            #endif
            TimeSpan span = DateTime.Now - CheckRadioTime;
            if (span.TotalSeconds > 1.0)
                OnUpdateRichPresence();
        }

        private void OnUpdateRichPresence()
        {
            HttpClientHandler handler = new HttpClientHandler();
            handler.UseDefaultCredentials = true;
            HttpClient client = new HttpClient(handler);
            double ExtraDelay = 5.0;
            string SongName, SongArtist, SongArt;
            SongName = SongArt = SongArtist = "";

            // Always wait for a bit.
            try
            {
                var response = client.GetAsync("https://etsr.wuffesan.com/api/nowplaying");
                var responseBody = response.Result.Content.ReadAsStringAsync();
                List<Services.Radio.RadioInfo> RadioStations = JsonConvert.DeserializeObject<List<Services.Radio.RadioInfo>>(responseBody.Result);
                Services.Radio.RadioInfo RadioStation = RadioStations[0];
                if (RadioStation != null)
                {
                    Services.Radio.StationMounts waffle_mounts = RadioStation.station.mounts[0];
                    Services.Radio.NowPlaying nowPlaying = RadioStation.now_playing;
                    ExtraDelay = nowPlaying.duration - nowPlaying.elapsed;
                    if ( ExtraDelay < 0)
                        ExtraDelay = 1.0;
                    #if DISCORD_RPC
                    if (DiscordClient != null)
                    {
                        DiscordClient.SetPresence(new RichPresence()
                        {
                            Details = $"Listening to {nowPlaying.song.title}",
                            State = $"By {nowPlaying.song.artist}",
                            Timestamps = new Timestamps()
                            {
                                Start = DateTime.Now + TimeSpan.FromSeconds(nowPlaying.elapsed),
                                End = DateTime.Now + TimeSpan.FromSeconds(nowPlaying.duration)
                            }
                        });
                    }
                    #endif
                    SongName = nowPlaying.song.title;
                    SongArtist = nowPlaying.song.artist;
                    SongArt = nowPlaying.song.art;
                    _syncContext.Post(_ =>
                    {
                        if (!FirstMount)
                        {
                            bufferedWaveProvider = null;
                            ThreadPool.QueueUserWorkItem(StreamMp3, waffle_mounts.url);
                            FirstMount = true;
                            timer1.Enabled = true;
                        }
                    }, null);
                }
                else
                {
                    #if DISCORD_RPC
                    ClearRichPresence();
                    #endif
                }
            }
            catch (Exception)
            {
                #if DISCORD_RPC
                ClearRichPresence();
                #endif
            }

            handler.Dispose();
            client.Dispose();

            _syncContext.Post(_ =>
            {
                song_name.Text = SongName;
                artist.Text = SongArtist;
                try
                {
                    if (!string.IsNullOrEmpty(SongArt))
                        song_cover_art.LoadAsync(SongArt);
                    else
                        song_cover_art.Image = null;
                }
                catch (Exception)
                {
                    song_cover_art.Image = null;
                }
            }, null);

            CheckRadioTime = DateTime.Now + TimeSpan.FromSeconds(ExtraDelay);
        }

        private void button2_MouseEnter(object sender, EventArgs e)
        {
            button2.BackColor = Color.FromArgb(200, 0, 0);
        }

        private void button2_MouseLeave(object sender, EventArgs e)
        {
            button2.BackColor = Color.DarkRed;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void song_cover_art_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        protected void RenderDropshadowText(Graphics graphics, string text, Font font, Color foreground, Color shadow, int shadowAlpha, PointF location)
        {
            const int DISTANCE = 2;
            for (int offset = 1; 0 <= offset; offset--)
            {
                Color color = ((offset < 1) ?
                    foreground : Color.FromArgb(shadowAlpha, shadow));
                using (var brush = new SolidBrush(color))
                {
                    var point = new PointF()
                    {
                        X = location.X + (offset * DISTANCE),
                        Y = location.Y + (offset * DISTANCE)
                    };
                    graphics.DrawString(text, font, brush, point);
                    if (offset > 0)
                    {
                        using (var blurBrush = new SolidBrush(Color.FromArgb((shadowAlpha / 2), color)))
                        {
                            graphics.DrawString(text, font, blurBrush, (point.X + 1), point.Y);
                            graphics.DrawString(text, font, blurBrush, (point.X - 1), point.Y);
                        }
                    }
                }
            }
        }

        private void song_cover_art_Paint(object sender, PaintEventArgs e)
        {
            if (song_cover_art.Image != null)
                e.Graphics.DrawImage(song_cover_art.Image, 0, 0, song_cover_art.Width, song_cover_art.Height);

            Color left = Color.Transparent;
            Color right = Color.FromArgb(64, 64, 64);
            LinearGradientMode direction = LinearGradientMode.Horizontal;
            LinearGradientBrush brush = new LinearGradientBrush(song_cover_art.ClientRectangle, left, right, direction);
            e.Graphics.FillRectangle(brush, song_cover_art.ClientRectangle);
        }

        private void song_name_Paint(object sender, PaintEventArgs e)
        {
            RenderDropshadowText(e.Graphics, song_name.Text, song_name.Font, Color.White, Color.DimGray, 64, new PointF(0, 0));
        }

        private void artist_Paint(object sender, PaintEventArgs e)
        {
            RenderDropshadowText(e.Graphics, artist.Text, artist.Font, Color.White, Color.DimGray, 64, new PointF(0, 0));
        }
    }
}
