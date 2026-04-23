using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class Form4 : Form
    {
        private readonly string assetsFolder;
        private readonly Button btnBack;

        public Form4()
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
            BackgroundImage = AssetCache.GetOrLoad(assetsFolder, "image11.png");

            btnBack = new Button
            {
                Text = "BACK",
                Font = new Font("Arial Rounded MT Bold", 24, FontStyle.Bold),
                ForeColor = Color.White,
                Width = 160,
                Height = 70,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                TabStop = false
            };
            btnBack.FlatAppearance.BorderSize = 3;
            btnBack.FlatAppearance.BorderColor = Color.White;
            btnBack.FlatAppearance.MouseOverBackColor = Color.FromArgb(80, Color.White);
            btnBack.Click += BtnBack_Click;

            Controls.Add(btnBack);

            Load += (s, e) => LayoutControls();
            Resize += (s, e) => LayoutControls();
        }

        private void LayoutControls()
        {
            btnBack.Location = new Point(40, 40);
        }

        private void BtnBack_Click(object sender, EventArgs e)
        {
            AppNavigator.Navigate(() => new Form2());
        }
    }
}
