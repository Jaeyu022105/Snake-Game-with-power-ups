using Class5;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class Form5 : Form
    {
        private Label lblTitle;
        private Button btnHome;
        private ComboBox cmbFilter;
        private Panel tablePanel;

        private readonly LeaderboardManager leaderboardManager;

        public Form5()
        {
            InitializeComponent();

            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            UpdateStyles();
            BackColor = Color.Black;

            string saveFilePath = Path.Combine(Application.StartupPath, "player_scores.txt");
            leaderboardManager = new LeaderboardManager(saveFilePath);

            SetupUI();
            Load += Form5_Load;
            Resize += Form5_Resize;
        }

        private void Form5_Load(object sender, EventArgs e)
        {
            leaderboardManager.LoadScores();
            PopulateFilter();
            AdjustLayout();
            DisplayLeaderboard(cmbFilter.SelectedItem?.ToString() ?? "Overall");
        }

        private void Form5_Resize(object sender, EventArgs e)
        {
            AdjustLayout();
            DisplayLeaderboard(cmbFilter.SelectedItem?.ToString() ?? "Overall");
        }

        private void BtnHome_Click(object sender, EventArgs e)
        {
            AppNavigator.Navigate(() => new Form2());
        }

        private void AdjustLayout()
        {
            if (lblTitle != null)
            {
                lblTitle.Left = (ClientSize.Width - lblTitle.Width) / 2;
                lblTitle.Top = 100;
            }

            if (cmbFilter != null)
            {
                cmbFilter.Left = ClientSize.Width - cmbFilter.Width - 60;
                cmbFilter.Top = 60;
            }

            if (btnHome != null)
            {
                btnHome.Left = 40;
                btnHome.Top = 40;
            }

            if (tablePanel != null)
            {
                int left = 120;
                int top = 220;
                int rightMargin = 120;
                int bottomMargin = 80;
                tablePanel.Bounds = new Rectangle(
                    left,
                    top,
                    Math.Max(400, ClientSize.Width - left - rightMargin),
                    Math.Max(300, ClientSize.Height - top - bottomMargin));
            }
        }

        private void SetupUI()
        {
            btnHome = new Button
            {
                Text = "HOME",
                Font = new Font("Consolas", 28, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Width = 240,
                Height = 90,
                Top = 40,
                Left = 40
            };
            btnHome.FlatAppearance.BorderColor = Color.White;
            btnHome.FlatAppearance.BorderSize = 3;
            btnHome.FlatAppearance.MouseOverBackColor = Color.FromArgb(60, Color.White);
            btnHome.FlatAppearance.MouseDownBackColor = Color.FromArgb(100, Color.White);
            btnHome.Click += BtnHome_Click;
            Controls.Add(btnHome);

            lblTitle = new Label
            {
                Text = "LEADERBOARDS",
                Font = new Font("Consolas", 56, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true
            };
            Controls.Add(lblTitle);

            cmbFilter = new ComboBox
            {
                Font = new Font("Consolas", 20, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Black,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 320,
                Height = 60,
                FlatStyle = FlatStyle.Flat,
                DrawMode = DrawMode.OwnerDrawFixed
            };
            cmbFilter.DrawItem += (s, e) =>
            {
                if (e.Index < 0)
                    return;

                e.DrawBackground();
                using (Brush brush = new SolidBrush(Color.White))
                {
                    string text = cmbFilter.Items[e.Index].ToString();
                    e.Graphics.DrawString(text, cmbFilter.Font, brush, e.Bounds.Location);
                }
                e.DrawFocusRectangle();
            };
            cmbFilter.SelectedIndexChanged += (s, e) =>
            {
                string selected = cmbFilter.SelectedItem?.ToString() ?? "Overall";
                DisplayLeaderboard(selected);
            };
            Controls.Add(cmbFilter);

            tablePanel = new Panel
            {
                AutoScroll = false,
                BackColor = Color.Transparent,
                BorderStyle = BorderStyle.None
            };
            Controls.Add(tablePanel);
        }

        private void PopulateFilter()
        {
            cmbFilter.Items.Clear();
            List<string> filters = leaderboardManager.GetAvailableFilters();
            cmbFilter.Items.AddRange(filters.ToArray());
            cmbFilter.SelectedIndex = 0;
        }

        private void DisplayLeaderboard(string filter)
        {
            tablePanel.Controls.Clear();

            List<PlayerRecord> filteredRecords = leaderboardManager.GetLeaderboard(filter);

            TableLayoutPanel table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 6,
                BackColor = Color.Transparent,
                Margin = new Padding(0),
                Padding = new Padding(10)
            };

            table.ColumnStyles.Clear();
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45f));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 10f));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45f));

            table.RowStyles.Clear();
            for (int i = 0; i < 6; i++)
                table.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / 6f));

            AddHeaderToTable(table, "NAME", 0);
            AddHeaderToTable(table, "LEVEL", 1);
            AddHeaderToTable(table, "SCORE", 2);

            int row = 1;
            foreach (PlayerRecord record in filteredRecords)
            {
                AddCellToTable(table, record.Name, 0, row);
                AddCellToTable(table, record.Level.ToString(), 1, row);
                AddCellToTable(table, record.Score.ToString(), 2, row);
                row++;
            }

            while (row <= 5)
            {
                AddCellToTable(table, "-", 0, row);
                AddCellToTable(table, "-", 1, row);
                AddCellToTable(table, "-", 2, row);
                row++;
            }

            tablePanel.Controls.Add(table);
        }

        private void AddHeaderToTable(TableLayoutPanel table, string text, int col)
        {
            Label lbl = new Label
            {
                Text = text,
                Font = new Font("Consolas", 36, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            table.Controls.Add(lbl, col, 0);
        }

        private void AddCellToTable(TableLayoutPanel table, string text, int col, int row)
        {
            Label lbl = new Label
            {
                Text = text,
                Font = new Font("Consolas", 28, FontStyle.Regular),
                ForeColor = Color.White,
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };

            if (row >= 0 && row < table.RowCount)
                table.Controls.Add(lbl, col, row);
        }
    }
}
