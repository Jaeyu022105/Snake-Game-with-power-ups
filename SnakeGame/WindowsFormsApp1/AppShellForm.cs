using System;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public sealed class AppShellForm : Form
    {
        private const int FadeIntervalMs = 15;
        private const int FadeStep = 32;

        private readonly Panel contentHost;
        private readonly FadeOverlay overlay;
        private readonly Timer transitionTimer;

        private Form currentScreen;
        private Form pendingScreen;
        private Func<Form> queuedNavigation;
        private bool isTransitioning;
        private bool isFadingOut;

        public AppShellForm()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
            UpdateStyles();
            DoubleBuffered = true;
            KeyPreview = true;
            BackColor = Color.Black;
            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Maximized;
            StartPosition = FormStartPosition.CenterScreen;

            contentHost = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black
            };

            overlay = new FadeOverlay
            {
                Dock = DockStyle.Fill,
                Visible = false
            };

            transitionTimer = new Timer { Interval = FadeIntervalMs };
            transitionTimer.Tick += TransitionTimer_Tick;

            Controls.Add(contentHost);
            Controls.Add(overlay);

            Shown += AppShellForm_Shown;
            FormClosed += AppShellForm_FormClosed;
        }

        public void NavigateTo(Func<Form> screenFactory, bool animated = true)
        {
            if (screenFactory == null)
                return;

            if (isTransitioning)
            {
                queuedNavigation = screenFactory;
                return;
            }

            Form nextScreen = screenFactory();
            PrepareScreen(nextScreen);

            if (currentScreen == null || !animated)
            {
                SwapScreen(nextScreen);
                overlay.Alpha = 0;
                overlay.Visible = false;
                return;
            }

            pendingScreen = nextScreen;
            isTransitioning = true;
            isFadingOut = true;
            overlay.Alpha = 0;
            overlay.Visible = true;
            overlay.BringToFront();
            transitionTimer.Start();
        }

        private void AppShellForm_Shown(object sender, EventArgs e)
        {
            AppNavigator.Initialize(this);
            NavigateTo(() => new Form1(), false);
        }

        private void AppShellForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            transitionTimer.Stop();
            transitionTimer.Dispose();
            AssetCache.DisposeAll();
        }

        private void TransitionTimer_Tick(object sender, EventArgs e)
        {
            if (isFadingOut)
            {
                overlay.Alpha = Math.Min(255, overlay.Alpha + FadeStep);
                if (overlay.Alpha >= 255)
                {
                    SwapScreen(pendingScreen);
                    pendingScreen = null;
                    isFadingOut = false;
                }

                return;
            }

            overlay.Alpha = Math.Max(0, overlay.Alpha - FadeStep);
            if (overlay.Alpha > 0)
                return;

            transitionTimer.Stop();
            overlay.Visible = false;
            isTransitioning = false;

            if (queuedNavigation == null)
                return;

            Func<Form> nextNavigation = queuedNavigation;
            queuedNavigation = null;
            BeginInvoke(new Action(() => NavigateTo(nextNavigation)));
        }

        private void PrepareScreen(Form screen)
        {
            screen.TopLevel = false;
            screen.TopMost = false;
            screen.ShowInTaskbar = false;
            screen.FormBorderStyle = FormBorderStyle.None;
            screen.Dock = DockStyle.Fill;
            screen.BackColor = Color.Black;
        }

        private void SwapScreen(Form nextScreen)
        {
            SuspendLayout();
            try
            {
                if (currentScreen != null)
                {
                    contentHost.Controls.Remove(currentScreen);
                    currentScreen.Hide();
                    currentScreen.Dispose();
                }

                currentScreen = nextScreen;
                contentHost.Controls.Add(nextScreen);
                nextScreen.Show();
                nextScreen.BringToFront();
                overlay.BringToFront();
                BeginInvoke(new Action(FocusCurrentScreen));
            }
            finally
            {
                ResumeLayout(true);
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (currentScreen is IInputScreen inputScreen && inputScreen.HandleKeyInput(keyData))
                return true;

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void FocusCurrentScreen()
        {
            if (currentScreen == null || currentScreen.IsDisposed)
                return;

            currentScreen.Select();
            currentScreen.Focus();
            ActiveControl = currentScreen;
        }

        private sealed class FadeOverlay : Control
        {
            private int alpha;

            public int Alpha
            {
                get => alpha;
                set
                {
                    int bounded = Math.Max(0, Math.Min(255, value));
                    if (alpha == bounded)
                        return;

                    alpha = bounded;
                    Invalidate();
                }
            }

            public FadeOverlay()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
                UpdateStyles();
                BackColor = Color.Black;
                TabStop = false;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);

                if (alpha <= 0)
                    return;

                using (Brush overlayBrush = new SolidBrush(Color.FromArgb(alpha, Color.Black)))
                {
                    e.Graphics.FillRectangle(overlayBrush, ClientRectangle);
                }
            }
        }
    }
}
