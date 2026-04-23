using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using ImageSequenceManager = SnakeGameLibrary.ImageSequenceManager;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        private readonly string assetsFolder;
        private readonly ImageSequenceManager imageManager;
        private readonly Timer imageTimer;
        private readonly Timer fadeTimer;

        private Image nextImage;
        private float fadeOpacity;
        private bool isFading;
        private bool sequenceComplete;
        private bool bufferingComplete;
        private bool navigationRequested;

        public Form1()
        {
            InitializeComponent();

            assetsFolder = Path.Combine(Application.StartupPath, "Assets");
            imageManager = new ImageSequenceManager(assetsFolder);

            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
            UpdateStyles();
            BackColor = Color.Black;
            BackgroundImageLayout = ImageLayout.Stretch;

            imageTimer = new Timer { Interval = 800 };
            imageTimer.Tick += ImageTimer_Tick;

            fadeTimer = new Timer { Interval = 30 };
            fadeTimer.Tick += FadeTimer_Tick;

            Load += Form1_Load;
            Disposed += Form1_Disposed;

            ShowImage(AssetCache.GetOrLoad(imageManager.GetCurrentImagePath()));
            imageTimer.Start();
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            await AssetCache.PreloadAllAsync(assetsFolder);
            bufferingComplete = true;
            TryContinue();
        }

        private void ImageTimer_Tick(object sender, EventArgs e)
        {
            string nextPath = imageManager.GetNextImagePath();
            if (nextPath != null)
            {
                StartFadeToNextImage(nextPath);
                return;
            }

            imageTimer.Stop();
            sequenceComplete = true;
            TryContinue();
        }

        private void StartFadeToNextImage(string imagePath)
        {
            nextImage = AssetCache.GetOrLoad(imagePath);
            if (nextImage == null)
                return;

            fadeOpacity = 0f;
            isFading = true;
            fadeTimer.Start();
        }

        private void FadeTimer_Tick(object sender, EventArgs e)
        {
            fadeOpacity += 0.2f;
            if (fadeOpacity >= 1f)
            {
                fadeTimer.Stop();
                isFading = false;
                BackgroundImage = nextImage;
                nextImage = null;
            }

            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (!isFading || nextImage == null)
                return;

            if (BackgroundImage != null)
                e.Graphics.DrawImage(BackgroundImage, ClientRectangle);

            using (Brush fadeBrush = new SolidBrush(Color.FromArgb((int)(fadeOpacity * 255), Color.Black)))
            {
                e.Graphics.FillRectangle(fadeBrush, ClientRectangle);
            }

            e.Graphics.DrawImage(nextImage, ClientRectangle);
        }

        private void ShowImage(Image image)
        {
            if (image != null)
                BackgroundImage = image;
            else
                BackColor = Color.Black;
        }

        private void TryContinue()
        {
            if (navigationRequested || !sequenceComplete || !bufferingComplete)
                return;

            navigationRequested = true;
            AppNavigator.Navigate(() => new Form2());
        }

        private void Form1_Disposed(object sender, EventArgs e)
        {
            imageTimer?.Stop();
            imageTimer?.Dispose();
            fadeTimer?.Stop();
            fadeTimer?.Dispose();
        }
    }
}
