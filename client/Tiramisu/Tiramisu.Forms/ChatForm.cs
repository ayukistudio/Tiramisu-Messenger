using MaterialSkin.Controls;
using MaterialSkin;
using Microsoft.Web.WebView2.Core;
using System;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;

namespace TiramisuClient
{
    public partial class ChatForm : MaterialForm
    {
        private readonly int _contactId;
        private readonly string _contactUsername;
        private readonly int _userId;
        private readonly string _password;
        private readonly ECCEncryption _encryption;
        private readonly WebView2 _webView;

        public ChatForm(int contactId, string contactUsername, int userId, string password, ECCEncryption encryption)
        {
            InitializeComponent();
            _contactId = contactId;
            _contactUsername = contactUsername;
            _userId = userId;
            _password = password;
            _encryption = encryption;

            var materialSkinManager = MaterialSkinManager.Instance;
            materialSkinManager.AddFormToManage(this);
            materialSkinManager.Theme = MaterialSkinManager.Themes.DARK;
            materialSkinManager.ColorScheme = new ColorScheme(
                Primary.Grey900, Primary.Grey800,
                Primary.Grey700, Accent.Red100, TextShade.WHITE);
            this.Text = $"Чат с {contactUsername}";
            this.FormBorderStyle = FormBorderStyle.Sizable;

            var webViewPanel = new Panel { Dock = DockStyle.Fill };
            this.Controls.Add(webViewPanel);

            _webView = new WebView2 { Dock = DockStyle.Fill };
            webViewPanel.Controls.Add(_webView);

            InitializeWebView();
        }

        private async void InitializeWebView()
        {
            try
            {
                await _webView.EnsureCoreWebView2Async(null);

                MainForm mainForm = null;
                foreach (Form form in Application.OpenForms)
                {
                    if (form is MainForm)
                    {
                        mainForm = (MainForm)form;
                        break;
                    }
                }
                if (mainForm == null)
                {
                    this.Close();
                    return;
                }

                _webView.CoreWebView2.AddHostObjectToScript("backend", new MainForm.BackendHost(mainForm));

                string escapedUsername = _contactUsername.Replace("'", "\\'");
                string initScript = $"window.currentContactId = {_contactId}; window.currentContactUsername = '{escapedUsername}'; console.log('Injected contactId: {_contactId}, username: {escapedUsername}');";
                await _webView.CoreWebView2.ExecuteScriptAsync(initScript);

                _webView.Source = new Uri($"http://localhost:8080/chat.html?t={DateTime.Now.Ticks}");
                Console.WriteLine("WebView initialized and navigated to chat.html");
            }
            catch (Exception ex)
            {
                this.Close();
            }
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.ClientSize = new System.Drawing.Size(800, 600);
            this.Name = "ChatForm";
            this.ShowIcon = false;
            this.ResumeLayout(false);
        }
    }
}