using System.Windows.Forms;

namespace WuffeRadioApp
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            song_cover_art = new PictureBox();
            song_name = new Label();
            artist = new Label();
            button2 = new Button();
            volumeSlider1 = new NAudio.Gui.VolumeSlider();
            timer1 = new System.Windows.Forms.Timer(components);
            ((System.ComponentModel.ISupportInitialize)song_cover_art).BeginInit();
            SuspendLayout();
            // 
            // song_cover_art
            // 
            song_cover_art.BackgroundImageLayout = ImageLayout.Stretch;
            song_cover_art.Location = new Point(-3, -95);
            song_cover_art.Name = "song_cover_art";
            song_cover_art.Size = new Size(330, 330);
            song_cover_art.SizeMode = PictureBoxSizeMode.StretchImage;
            song_cover_art.TabIndex = 4;
            song_cover_art.TabStop = false;
            song_cover_art.Paint += song_cover_art_Paint;
            song_cover_art.MouseDown += song_cover_art_MouseDown;
            // 
            // song_name
            // 
            song_name.AutoSize = true;
            song_name.BackColor = Color.Transparent;
            song_name.Font = new Font("Segoe UI", 15F);
            song_name.ForeColor = Color.Black;
            song_name.Location = new Point(12, 9);
            song_name.Name = "song_name";
            song_name.Size = new Size(95, 28);
            song_name.TabIndex = 0;
            song_name.Text = "Loading...";
            song_name.Paint += song_name_Paint;
            // 
            // artist
            // 
            artist.AutoSize = true;
            artist.BackColor = Color.Transparent;
            artist.ForeColor = Color.Black;
            artist.Location = new Point(12, 37);
            artist.Name = "artist";
            artist.Size = new Size(58, 15);
            artist.TabIndex = 1;
            artist.Text = "Unknown";
            artist.Paint += artist_Paint;
            // 
            // button2
            // 
            button2.BackColor = Color.DarkRed;
            button2.FlatStyle = FlatStyle.Flat;
            button2.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            button2.ForeColor = Color.Black;
            button2.Location = new Point(376, -6);
            button2.Name = "button2";
            button2.Size = new Size(50, 26);
            button2.TabIndex = 3;
            button2.Text = "X";
            button2.UseVisualStyleBackColor = false;
            button2.Click += button2_Click;
            button2.MouseEnter += button2_MouseEnter;
            button2.MouseLeave += button2_MouseLeave;
            // 
            // volumeSlider1
            // 
            volumeSlider1.Location = new Point(326, 94);
            volumeSlider1.Name = "volumeSlider1";
            volumeSlider1.Size = new Size(96, 16);
            volumeSlider1.TabIndex = 5;
            volumeSlider1.Volume = 0.05621F;
            // 
            // timer1
            // 
            timer1.Interval = 250;
            timer1.Tick += timer1_Tick;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(64, 64, 64);
            ClientSize = new Size(423, 114);
            Controls.Add(volumeSlider1);
            Controls.Add(button2);
            Controls.Add(artist);
            Controls.Add(song_name);
            Controls.Add(song_cover_art);
            ForeColor = Color.White;
            FormBorderStyle = FormBorderStyle.None;
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "WaffleRadio App";
            Load += Form1_Load;
            MouseDown += Form1_MouseDown;
            ((System.ComponentModel.ISupportInitialize)song_cover_art).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label song_name;
        private Label artist;
        private Button button2;
        private PictureBox song_cover_art;
        private NAudio.Gui.VolumeSlider volumeSlider1;
        private System.Windows.Forms.Timer timer1;
    }
}
