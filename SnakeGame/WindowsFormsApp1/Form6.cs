using Class5;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class Form6 : Form
    {
        private readonly int score;
        private readonly int level;

        private TextBox txtName;
        private Button btnConfirm;
        private Label lblError;
        private Panel borderPanel;

        private readonly LeaderboardManager leaderboardManager;
        private readonly string placeholder = "ENTER YOUR NAME";

        public Form6(int score, int level)
        {
            InitializeComponent();
            this.score = score;
            this.level = level;

            DoubleBuffered = true;
            BackColor = Color.Black;
            BackgroundImageLayout = ImageLayout.Stretch;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
            UpdateStyles();

            string saveFilePath = Path.Combine(Application.StartupPath, "player_scores.txt");
            leaderboardManager = new LeaderboardManager(saveFilePath);
            leaderboardManager.LoadScores();

            SetupUI();
        }

        private void SetupUI()
        {
            BackgroundImage = AssetCache.GetOrLoad(Path.Combine(Application.StartupPath, "Assets", "image12.png"));

            txtName = new TextBox
            {
                Font = new Font("Consolas", 28, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Black,
                BorderStyle = BorderStyle.None,
                TextAlign = HorizontalAlignment.Center,
                Width = 500,
                Height = 60,
                Multiline = false
            };

            txtName.Text = placeholder;
            txtName.ForeColor = Color.FromArgb(180, 180, 180);
            txtName.Enter += TxtName_Enter;
            txtName.Leave += TxtName_Leave;
            txtName.TextChanged += TxtName_TextChanged;
            txtName.TabIndex = 0;

            borderPanel = new Panel
            {
                Width = txtName.Width + 10,
                Height = txtName.Height + 10,
                BackColor = Color.Transparent,
                BorderStyle = BorderStyle.FixedSingle
            };
            Controls.Add(borderPanel);
            txtName.Parent = borderPanel;
            txtName.Location = new Point(5, 5);
            txtName.BringToFront();

            borderPanel.Paint += (s, e) =>
            {
                using (Pen pen = new Pen(Color.White, 2))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, borderPanel.Width - 1, borderPanel.Height - 1);
                }
            };

            lblError = new Label
            {
                Text = "That name is already taken. Please use another name",
                Font = new Font("Consolas", 14, FontStyle.Regular),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                AutoSize = true,
                Visible = false
            };
            Controls.Add(lblError);

            btnConfirm = new Button
            {
                Text = "CONFIRM",
                Font = new Font("Consolas", 20, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Width = 220,
                Height = 70,
                FlatStyle = FlatStyle.Flat,
                Enabled = false
            };
            btnConfirm.FlatAppearance.BorderSize = 3;
            btnConfirm.FlatAppearance.BorderColor = Color.White;
            btnConfirm.FlatAppearance.MouseOverBackColor = Color.FromArgb(60, Color.White);
            btnConfirm.FlatAppearance.MouseDownBackColor = Color.FromArgb(100, Color.White);
            btnConfirm.Click += BtnConfirm_Click;
            Controls.Add(btnConfirm);

            Load += (s, e) => LayoutEntryControls();
            Resize += (s, e) => LayoutEntryControls();
            VisibleChanged += (s, e) =>
            {
                if (!Visible)
                    return;

                LayoutEntryControls();
                BeginInvoke(new Action(FocusNameInput));
            };
            Enter += (s, e) => BeginInvoke(new Action(FocusNameInput));
        }

        private void TxtName_Enter(object sender, EventArgs e)
        {
            if (txtName.Text == placeholder)
            {
                txtName.Text = string.Empty;
                txtName.ForeColor = Color.White;
            }
        }

        private void TxtName_Leave(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                txtName.Text = placeholder;
                txtName.ForeColor = Color.FromArgb(180, 180, 180);
            }
        }

        private void TxtName_TextChanged(object sender, EventArgs e)
        {
            string currentText = txtName.Text.Trim();
            bool isPlaceholder = currentText == placeholder;
            bool isValidLength = !string.IsNullOrWhiteSpace(currentText) && !isPlaceholder;
            bool isTaken = isValidLength && leaderboardManager.IsNameTaken(currentText);

            if (isTaken)
            {
                lblError.Visible = true;
                btnConfirm.Enabled = false;
                LayoutEntryControls();
                return;
            }

            lblError.Visible = false;
            btnConfirm.Enabled = isValidLength;
        }

        private void BtnConfirm_Click(object sender, EventArgs e)
        {
            string playerName = txtName.Text.Trim();
            if (string.IsNullOrEmpty(playerName) || playerName == placeholder)
                return;

            try
            {
                leaderboardManager.SaveNewScore(playerName, score, level);
                FadeToForm5();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving score: " + ex.Message, "Score Save Failed");
            }
        }

        private void FadeToForm5()
        {
            AppNavigator.Navigate(() => new Form5());
        }

        private void LayoutEntryControls()
        {
            if (borderPanel == null || btnConfirm == null || lblError == null)
                return;

            int centerX = ClientSize.Width / 2;
            int contentTop = (int)(ClientSize.Height * 0.42);

            borderPanel.Location = new Point(centerX - (borderPanel.Width / 2), contentTop - borderPanel.Height);
            btnConfirm.Location = new Point(centerX - (btnConfirm.Width / 2), borderPanel.Bottom + 40);
            lblError.Location = new Point(centerX - (lblError.Width / 2), btnConfirm.Bottom + 16);

            borderPanel.BringToFront();
            btnConfirm.BringToFront();
            lblError.BringToFront();
        }

        private void FocusNameInput()
        {
            if (txtName == null || txtName.IsDisposed || !Visible)
                return;

            txtName.Focus();
            if (txtName.Text == placeholder)
                txtName.SelectAll();
            else
                txtName.SelectionStart = txtName.TextLength;
        }
    }
}
