using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class Form2 : Form
    {
        private readonly string assetsFolder;
        private readonly Button btnPlay;
        private readonly Button btnAbout;
        private readonly Button btnExit;
        private readonly PictureBox trophy;

        public Form2()
        {
            InitializeComponent();

            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            UpdateStyles();

            assetsFolder = Path.Combine(Application.StartupPath, "Assets");
            if (!Directory.Exists(assetsFolder))
                Directory.CreateDirectory(assetsFolder);

            BackColor = Color.Black;
            BackgroundImageLayout = ImageLayout.Stretch;
            BackgroundImage = AssetCache.GetOrLoad(assetsFolder, "image10.png");

            btnPlay = CreateMenuButton("PLAY", 36, 320, 100);
            btnPlay.Click += BtnPlay_Click;

            btnAbout = CreateMenuButton("ABOUT", 36, 320, 100);
            btnAbout.Click += BtnAbout_Click;

            btnExit = CreateMenuButton("EXIT GAME", 18, 200, 60);
            btnExit.FlatAppearance.BorderSize = 2;
            btnExit.Click += (s, e) => AppNavigator.ExitGame();

            trophy = new PictureBox
            {
                SizeMode = PictureBoxSizeMode.Zoom,
                Cursor = Cursors.Hand,
                Width = 180,
                Height = 180,
                BackColor = Color.Transparent,
                Image = AssetCache.GetOrLoad(assetsFolder, "trophy.png")
            };
            trophy.Click += Trophy_Click;

            Controls.Add(btnPlay);
            Controls.Add(btnAbout);
            Controls.Add(btnExit);
            Controls.Add(trophy);

            Load += (s, e) => LayoutMenuControls();
            Resize += (s, e) => LayoutMenuControls();
        }

        private Button CreateMenuButton(string text, float fontSize, int width, int height)
        {
            Button button = new Button
            {
                Text = text,
                Font = new Font("Arial Rounded MT Bold", fontSize, FontStyle.Bold),
                ForeColor = Color.White,
                Width = width,
                Height = height,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                TabStop = false
            };
            button.FlatAppearance.BorderSize = 3;
            button.FlatAppearance.BorderColor = Color.White;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(80, Color.White);
            return button;
        }

        private void LayoutMenuControls()
        {
            int centerX = ClientSize.Width / 2;
            int centerY = ClientSize.Height / 2;

            btnPlay.Location = new Point(centerX - btnPlay.Width / 2, centerY - btnPlay.Height - 30);
            btnAbout.Location = new Point(centerX - btnAbout.Width / 2, centerY + 30);
            trophy.Location = new Point(ClientSize.Width - trophy.Width - 40, ClientSize.Height - trophy.Height - 40);
            btnExit.Location = new Point(30, 30);
        }

        private void BtnPlay_Click(object sender, EventArgs e)
        {
            AppNavigator.Navigate(() => new Form3());
        }

        private void BtnAbout_Click(object sender, EventArgs e)
        {
            AppNavigator.Navigate(() => new Form4());
        }

        private void Trophy_Click(object sender, EventArgs e)
        {
            AppNavigator.Navigate(() => new Form5());
        }
    }
}
