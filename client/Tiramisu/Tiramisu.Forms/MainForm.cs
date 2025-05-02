using MaterialSkin.Controls;
using MaterialSkin;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;
using System.Linq;
using Microsoft.Web.WebView2.WinForms;
using System.Drawing;

namespace TiramisuClient
{
    public partial class MainForm : MaterialForm
    {
        private readonly WebView2 webView;
        private readonly HttpClient httpClient = new HttpClient();
        private readonly string apiBaseUrl = "http://localhost:5000";
        private readonly ECCEncryption encryption = new ECCEncryption();
        private readonly HttpListener httpListener;
        private readonly NotifyIcon notifyIcon;
        private int userId;
        private string username;
        private string password;
        private const int WebServerPort = 8080;
        private CancellationTokenSource pollCts;
        private readonly Dictionary<int, string> keyCache = new Dictionary<int, string>();

        public MainForm()
        {
            InitializeComponent();
            var materialSkinManager = MaterialSkinManager.Instance;
            materialSkinManager.AddFormToManage(this);
            materialSkinManager.Theme = MaterialSkinManager.Themes.DARK;
            materialSkinManager.ColorScheme = new ColorScheme(
                Primary.Grey900, Primary.Grey800,
                Primary.Grey700, Accent.Red100, TextShade.WHITE);
            Text = "Tiramisu";
            FormBorderStyle = FormBorderStyle.Sizable;

            notifyIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Text = "Tiramisu",
                Visible = true
            };
            notifyIcon.DoubleClick += (s, e) => Show();

            var webViewPanel = new Panel { Dock = DockStyle.Fill };
            Controls.Add(webViewPanel);
            Controls.SetChildIndex(webViewPanel, 0);

            webView = new WebView2 { Dock = DockStyle.Fill };
            webViewPanel.Controls.Add(webView);

            httpListener = new HttpListener();
            httpListener.Prefixes.Add($"http://localhost:{WebServerPort}/");
            StartWebServer();

            InitializeWebView();
            InitializeDatabase();
        }

        private void StartWebServer()
        {
            httpListener.Start();
            ThreadPool.QueueUserWorkItem(_ =>
            {
                while (httpListener.IsListening)
                {
                    try
                    {
                        var context = httpListener.GetContext();
                        var request = context.Request;
                        var response = context.Response;

                        string relativePath = request.Url.AbsolutePath.TrimStart('/');
                        string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "gui", relativePath);

                        if (string.IsNullOrEmpty(Path.GetExtension(filePath)) || relativePath == "")
                            filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "gui", "index.html");

                        if (File.Exists(filePath))
                        {
                            byte[] buffer = File.ReadAllBytes(filePath);
                            response.ContentLength64 = buffer.Length;
                            response.ContentType = GetContentType(filePath);
                            response.OutputStream.Write(buffer, 0, buffer.Length);
                        }
                        else
                        {
                            response.StatusCode = 404;
                            byte[] buffer = Encoding.UTF8.GetBytes($"File not found: {filePath}");
                            response.ContentLength64 = buffer.Length;
                            response.ContentType = "text/plain";
                            response.OutputStream.Write(buffer, 0, buffer.Length);
                        }

                        response.Close();
                    }
                    catch (Exception ex)
                    {
                    }
                }
            });
        }

        private string GetContentType(string filePath)
        {
            switch (Path.GetExtension(filePath).ToLower())
            {
                case ".html": return "text/html";
                case ".js": return "application/javascript";
                case ".css": return "text/css";
                case ".png": return "image/png";
                case ".jpg": return "image/jpeg";
                default: return "application/octet-stream";
            }
        }

        private async void InitializeWebView()
        {
            try
            {
                await webView.EnsureCoreWebView2Async(null);
                if (webView.CoreWebView2 != null)
                {
                    webView.CoreWebView2.AddHostObjectToScript("backend", new BackendHost(this));
                    webView.Source = new Uri($"http://localhost:{WebServerPort}/index.html");
                }
            }
            catch (Exception ex)
            {
            }
        }

        private void InitializeDatabase()
        {
            if (!File.Exists("database.db"))
                SQLiteConnection.CreateFile("database.db");

            using (var conn = new SQLiteConnection("Data Source=database.db;Version=3;"))
            {
                conn.Open();
                string sql = @"
                    CREATE TABLE IF NOT EXISTS messages (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        contact_id INTEGER,
                        message TEXT,
                        encrypted_key TEXT,
                        is_sent BOOLEAN,
                        timestamp DATETIME
                    );
                    CREATE TABLE IF NOT EXISTS contacts (
                        id INTEGER PRIMARY KEY,
                        username TEXT,
                        shared_key TEXT
                    );";
                using (var cmd = new SQLiteCommand(sql, conn))
                    cmd.ExecuteNonQuery();
            }
        }

        public async Task<string> Login(string username, string password)
        {
            var data = new { username, password };
            var content = new StringContent(
                Newtonsoft.Json.JsonConvert.SerializeObject(data),
                Encoding.UTF8, "application/json");

            try
            {
                var response = await httpClient.PostAsync($"{apiBaseUrl}/login", content);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var user = JObject.Parse(json);
                    userId = user["id"]?.Value<int>() ?? throw new Exception("User ID not found in response");
                    this.username = user["username"]?.Value<string>() ?? throw new Exception("Username not found in response");
                    this.password = password;
                    Text = $"Tiramisu - {this.username}";
                    StartPollingForDialogs();
                    return json;
                }
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                return Newtonsoft.Json.JsonConvert.SerializeObject(new { error = ex.Message });
            }
        }

        public async Task<string> Register(string username, string password)
        {
            var data = new { username, password };
            var content = new StringContent(
                Newtonsoft.Json.JsonConvert.SerializeObject(data),
                Encoding.UTF8, "application/json");

            try
            {
                var response = await httpClient.PostAsync($"{apiBaseUrl}/register", content);
                var json = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    var user = JObject.Parse(json);
                    userId = user["id"]?.Value<int>() ?? throw new Exception("User ID not found in response");
                    this.username = username;
                    this.password = password;
                    return json;
                }
                return json;
            }
            catch (Exception ex)
            {
                return Newtonsoft.Json.JsonConvert.SerializeObject(new { error = ex.Message });
            }
        }

        public async Task<string> UpdateUsername(string newUsername)
        {
            var data = new { id = userId, username = newUsername };
            var content = new StringContent(
                Newtonsoft.Json.JsonConvert.SerializeObject(data),
                Encoding.UTF8, "application/json");

            try
            {
                var response = await httpClient.PostAsync($"{apiBaseUrl}/update", content);
                if (response.IsSuccessStatusCode)
                {
                    username = newUsername;
                    return Newtonsoft.Json.JsonConvert.SerializeObject(new { success = true });
                }
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                return Newtonsoft.Json.JsonConvert.SerializeObject(new { error = ex.Message });
            }
        }

        public async Task<string> SearchUser(int userId)
        {
            try
            {
                var response = await httpClient.GetAsync($"{apiBaseUrl}/search/{userId}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var user = JObject.Parse(json);
                    int id = user["id"]?.Value<int>() ?? throw new Exception("User ID not found in response");
                    string username = user["username"]?.Value<string>() ?? throw new Exception("Username not found in response");

                    using (var conn = new SQLiteConnection("Data Source=database.db;Version=3;"))
                    {
                        conn.Open();
                        string sql = "INSERT OR IGNORE INTO contacts (id, username, shared_key) VALUES (?, ?, ?)";
                        using (var cmd = new SQLiteCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("id", id);
                            cmd.Parameters.AddWithValue("username", username);
                            cmd.Parameters.AddWithValue("shared_key", "");
                            cmd.ExecuteNonQuery();
                        }
                    }
                    await NotifyContactsUpdate();
                    return json;
                }
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                return Newtonsoft.Json.JsonConvert.SerializeObject(new { error = ex.Message });
            }
        }

        public string GetContacts()
        {
            try
            {
                using (var conn = new SQLiteConnection("Data Source=database.db;Version=3;"))
                {
                    conn.Open();
                    string sql = "SELECT id, username, shared_key FROM contacts";
                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            var contacts = new List<object>();
                            while (reader.Read())
                            {
                                var contact = new
                                {
                                    id = reader.GetInt32(0),
                                    username = reader.GetString(1),
                                    shared_key = reader.IsDBNull(2) ? "" : reader.GetString(2)
                                };
                                contacts.Add(contact);
                            }
                            return Newtonsoft.Json.JsonConvert.SerializeObject(contacts);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Newtonsoft.Json.JsonConvert.SerializeObject(new { error = ex.Message });
            }
        }

        public async Task<string> GetMessages(int contactId)
        {
            try
            {
                var response = await httpClient.GetAsync($"{apiBaseUrl}/dialogs/{userId}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var dialogs = JArray.Parse(json);
                    var dialog = dialogs.FirstOrDefault(d => d["initiator_id"].Value<int>() == contactId || d["receiver_id"].Value<int>() == contactId);
                    if (dialog == null)
                        return Newtonsoft.Json.JsonConvert.SerializeObject(new object[] { });

                    int dialogId = dialog["id"]?.Value<int>() ?? throw new Exception("Dialog ID not found in response");
                    response = await httpClient.GetAsync($"{apiBaseUrl}/messages/{dialogId}");
                    if (response.IsSuccessStatusCode)
                    {
                        var messagesJson = await response.Content.ReadAsStringAsync();
                        var messages = JArray.Parse(messagesJson);
                        var result = new List<object>();

                        string sharedKey;
                        if (!keyCache.TryGetValue(dialogId, out sharedKey))
                        {
                            sharedKey = encryption.LoadEncryptedKey(dialogId);
                            keyCache[dialogId] = sharedKey;
                        }

                        foreach (var msg in messages)
                        {
                            string encryptedMessage = msg["message"]?.Value<string>() ?? "";
                            string encryptedKey = sharedKey;
                            string decryptedMessage = encryptedMessage;
                            if (!string.IsNullOrEmpty(sharedKey) && !string.IsNullOrEmpty(encryptedMessage))
                            {
                                try
                                {
                                    decryptedMessage = encryption.DecryptMessage(encryptedMessage, encryptedKey, null);
                                }
                                catch
                                {
                                    decryptedMessage = "[Ошибка дешифрования]";
                                }
                            }
                            var messageObj = new
                            {
                                message = decryptedMessage,
                                sender_id = msg["sender_id"]?.Value<int>() ?? 0,
                                timestamp = msg["timestamp"]?.Value<string>() ?? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                            };
                            result.Add(messageObj);
                        }
                        return Newtonsoft.Json.JsonConvert.SerializeObject(result);
                    }
                    return Newtonsoft.Json.JsonConvert.SerializeObject(new object[] { });
                }
                return Newtonsoft.Json.JsonConvert.SerializeObject(new object[] { });
            }
            catch (Exception ex)
            {
                return Newtonsoft.Json.JsonConvert.SerializeObject(new { error = ex.Message });
            }
        }

        public async Task<string> SendMessage(int contactId, string message, string sharedKey)
        {
            try
            {
                var response = await httpClient.GetAsync($"{apiBaseUrl}/dialogs/{userId}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var dialogs = JArray.Parse(json);
                    var dialog = dialogs.FirstOrDefault(d => d["initiator_id"].Value<int>() == contactId || d["receiver_id"].Value<int>() == contactId);
                    if (dialog == null)
                        return Newtonsoft.Json.JsonConvert.SerializeObject(new { error = "Dialog not found" });

                    int dialogId = dialog["id"]?.Value<int>() ?? throw new Exception("Dialog ID not found in response");
                    string encryptedMessage = message;
                    string encryptedKey = sharedKey;
                    if (!string.IsNullOrEmpty(sharedKey))
                    {
                        var (encMsg, encKey) = encryption.EncryptMessage(message, sharedKey, null);
                        encryptedMessage = encMsg;
                        encryptedKey = encKey;
                        encryption.SaveEncryptedKey(dialogId, sharedKey);
                        keyCache[dialogId] = sharedKey;
                    }

                    var data = new
                    {
                        dialog_id = dialogId,
                        sender_id = userId,
                        message = encryptedMessage,
                        encrypted_key = encryptedKey
                    };
                    var content = new StringContent(
                        Newtonsoft.Json.JsonConvert.SerializeObject(data),
                        Encoding.UTF8, "application/json");

                    response = await httpClient.PostAsync($"{apiBaseUrl}/send_message", content);
                    if (response.IsSuccessStatusCode)
                    {
                        using (var conn = new SQLiteConnection("Data Source=database.db;Version=3;"))
                        {
                            conn.Open();
                            string sql = "INSERT INTO messages (contact_id, message, encrypted_key, is_sent, timestamp) VALUES (?, ?, ?, ?, ?)";
                            using (var cmd = new SQLiteCommand(sql, conn))
                            {
                                cmd.Parameters.AddWithValue("contact_id", contactId);
                                cmd.Parameters.AddWithValue("message", message);
                                cmd.Parameters.AddWithValue("encrypted_key", encryptedKey);
                                cmd.Parameters.AddWithValue("is_sent", true);
                                cmd.Parameters.AddWithValue("timestamp", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }
                    return await response.Content.ReadAsStringAsync();
                }
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                return Newtonsoft.Json.JsonConvert.SerializeObject(new { error = ex.Message });
            }
        }

        public async Task<string> SetSharedKey(int contactId, string sharedKey)
        {
            try
            {
                var response = await httpClient.GetAsync($"{apiBaseUrl}/dialogs/{userId}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var dialogs = JArray.Parse(json);
                    var dialog = dialogs.FirstOrDefault(d => d["initiator_id"].Value<int>() == contactId || d["receiver_id"].Value<int>() == contactId);
                    if (dialog == null)
                        return Newtonsoft.Json.JsonConvert.SerializeObject(new { error = "Dialog not found" });

                    int dialogId = dialog["id"]?.Value<int>() ?? throw new Exception("Dialog ID not found in response");
                    using (var conn = new SQLiteConnection("Data Source=database.db;Version=3;"))
                    {
                        conn.Open();
                        string sql = "UPDATE contacts SET shared_key = ? WHERE id = ?";
                        using (var cmd = new SQLiteCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("shared_key", sharedKey ?? "");
                            cmd.Parameters.AddWithValue("id", contactId);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    if (!string.IsNullOrEmpty(sharedKey))
                    {
                        encryption.SaveEncryptedKey(dialogId, sharedKey);
                        keyCache[dialogId] = sharedKey;
                    }
                    return Newtonsoft.Json.JsonConvert.SerializeObject(new { success = true });
                }
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                return Newtonsoft.Json.JsonConvert.SerializeObject(new { error = ex.Message });
            }
        }

        public async Task<string> GenerateSharedKey(int contactId)
        {
            try
            {
                if (userId == 0)
                    return Newtonsoft.Json.JsonConvert.SerializeObject(new { error = "User ID not set. Please log in." });
                if (contactId == 0)
                    return Newtonsoft.Json.JsonConvert.SerializeObject(new { error = "Invalid contact ID." });

                string sharedKey = encryption.GenerateSharedKey();
                var response = await httpClient.GetAsync($"{apiBaseUrl}/dialogs/{userId}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var dialogs = JArray.Parse(json);
                    var dialog = dialogs.FirstOrDefault(d => d["initiator_id"].Value<int>() == contactId || d["receiver_id"].Value<int>() == contactId);
                    int dialogId;
                    if (dialog != null)
                    {
                        dialogId = dialog["id"]?.Value<int>() ?? throw new Exception("Dialog ID not found in response");
                    }
                    else
                    {
                        var data = new
                        {
                            initiator_id = userId,
                            receiver_id = contactId,
                            shared_key = sharedKey
                        };
                        var content = new StringContent(
                            Newtonsoft.Json.JsonConvert.SerializeObject(data),
                            Encoding.UTF8, "application/json");

                        response = await httpClient.PostAsync($"{apiBaseUrl}/initiate_dialog", content);
                        var responseContent = await response.Content.ReadAsStringAsync();

                        if (!response.IsSuccessStatusCode)
                            return responseContent;

                        var responseObj = JObject.Parse(responseContent);
                        dialogId = responseObj["dialog_id"]?.Value<int>() ?? throw new Exception("Dialog ID not found in initiate_dialog response");
                    }

                    using (var conn = new SQLiteConnection("Data Source=database.db;Version=3;"))
                    {
                        conn.Open();
                        string sql = "UPDATE contacts SET shared_key = ? WHERE id = ?";
                        using (var cmd = new SQLiteCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("shared_key", sharedKey ?? "");
                            cmd.Parameters.AddWithValue("id", contactId);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    encryption.SaveEncryptedKey(dialogId, sharedKey);
                    keyCache[dialogId] = sharedKey;
                    await NotifyContactsUpdate();
                    return Newtonsoft.Json.JsonConvert.SerializeObject(new { sharedKey });
                }
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                return Newtonsoft.Json.JsonConvert.SerializeObject(new { error = ex.Message });
            }
        }

        public async Task<string> UploadAvatar(int userId, string fileBase64, string fileName)
        {
            try
            {
                if (string.IsNullOrEmpty(fileBase64) || string.IsNullOrEmpty(fileName))
                    return Newtonsoft.Json.JsonConvert.SerializeObject(new { error = "File data or name is missing" });

                string base64Data = fileBase64.Contains(",") ? fileBase64.Split(',')[1] : fileBase64;
                byte[] fileBytes = Convert.FromBase64String(base64Data);

                var multipartContent = new MultipartFormDataContent();
                multipartContent.Add(new ByteArrayContent(fileBytes), "avatar", fileName);

                var response = await httpClient.PostAsync($"{apiBaseUrl}/upload_avatar/{userId}", multipartContent);
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                return Newtonsoft.Json.JsonConvert.SerializeObject(new { error = ex.Message });
            }
        }

        public async Task<string> GetAvatar(int userId)
        {
            try
            {
                string avatarUrl = $"{apiBaseUrl}/avatars/{userId}.png?t={DateTime.Now.Ticks}";
                return Newtonsoft.Json.JsonConvert.SerializeObject(new { avatarUrl });
            }
            catch (Exception ex)
            {
                return Newtonsoft.Json.JsonConvert.SerializeObject(new { error = ex.Message });
            }
        }

        public async Task<string> GetUserInfo()
        {
            try
            {
                string avatarUrl = $"{apiBaseUrl}/avatars/{userId}.png?t={DateTime.Now.Ticks}";
                return Newtonsoft.Json.JsonConvert.SerializeObject(new
                {
                    id = userId,
                    username,
                    avatarUrl
                });
            }
            catch (Exception ex)
            {
                return Newtonsoft.Json.JsonConvert.SerializeObject(new { error = ex.Message });
            }
        }

        private async void StartPollingForDialogs()
        {
            pollCts = new CancellationTokenSource();
            try
            {
                while (!pollCts.Token.IsCancellationRequested)
                {
                    await CheckForNewDialogs();
                    await Task.Delay(2000, pollCts.Token);
                }
            }
            catch (TaskCanceledException)
            {
            }
        }

        private async Task CheckForNewDialogs()
        {
            try
            {
                var response = await httpClient.GetAsync($"{apiBaseUrl}/dialogs/{userId}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var dialogs = JArray.Parse(json);
                    bool newContactAdded = false;

                    foreach (var dialog in dialogs)
                    {
                        int contactId = dialog["initiator_id"].Value<int>() == userId ? dialog["receiver_id"].Value<int>() : dialog["initiator_id"].Value<int>();
                        string sharedKey = dialog["shared_key"]?.Value<string>() ?? "";
                        int dialogId = dialog["id"]?.Value<int>() ?? throw new Exception("Dialog ID not found in dialog response");

                        var contactResponse = await httpClient.GetAsync($"{apiBaseUrl}/search/{contactId}");
                        if (!contactResponse.IsSuccessStatusCode) continue;
                        var contactJson = await contactResponse.Content.ReadAsStringAsync();
                        var contact = JObject.Parse(contactJson);
                        string username = contact["username"]?.Value<string>() ?? throw new Exception("Username not found in contact response");

                        using (var conn = new SQLiteConnection("Data Source=database.db;Version=3;"))
                        {
                            conn.Open();
                            string sql = "INSERT OR IGNORE INTO contacts (id, username, shared_key) VALUES (?, ?, ?)";
                            using (var cmd = new SQLiteCommand(sql, conn))
                            {
                                cmd.Parameters.AddWithValue("id", contactId);
                                cmd.Parameters.AddWithValue("username", username);
                                cmd.Parameters.AddWithValue("shared_key", sharedKey);
                                int rowsAffected = cmd.ExecuteNonQuery();
                                if (rowsAffected > 0)
                                {
                                    newContactAdded = true;
                                    if (!string.IsNullOrEmpty(sharedKey))
                                    {
                                        encryption.SaveEncryptedKey(dialogId, sharedKey);
                                        keyCache[dialogId] = sharedKey;
                                    }
                                    ShowNotification($"Новый диалог с {username}", $"ID: {contactId}");
                                }
                            }
                        }
                    }

                    if (newContactAdded)
                        await NotifyContactsUpdate();
                }
            }
            catch (Exception ex)
            {
            }
        }

        private void ShowNotification(string title, string message)
        {
            try
            {
                notifyIcon.ShowBalloonTip(3000, title, message, ToolTipIcon.Info);
            }
            catch (Exception ex)
            {
            }
        }

        private async Task NotifyContactsUpdate()
        {
            try
            {
                await webView.CoreWebView2.ExecuteScriptAsync("if (typeof loadContacts === 'function') loadContacts();");
            }
            catch (Exception ex)
            {
            }
        }

        public void OpenChat(int contactId, string username)
        {
            new ChatForm(contactId, username, userId, password, encryption).Show();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            pollCts?.Cancel();
            httpListener.Stop();
            notifyIcon.Visible = false;
            notifyIcon.Dispose();
        }

        [System.Runtime.InteropServices.ComVisible(true)]
        public class BackendHost
        {
            private readonly MainForm form;

            public BackendHost(MainForm form)
            {
                this.form = form;
            }

            public async Task<string> Login(string username, string password)
            {
                return await form.Login(username, password);
            }

            public async Task<string> Register(string username, string password)
            {
                return await form.Register(username, password);
            }

            public async Task<string> UpdateUsername(string newUsername)
            {
                return await form.UpdateUsername(newUsername);
            }

            public async Task<string> SearchUser(int userId)
            {
                return await form.SearchUser(userId);
            }

            public string GetContacts()
            {
                return form.GetContacts();
            }

            public async Task<string> GetMessages(int contactId)
            {
                return await form.GetMessages(contactId);
            }

            public async Task<string> SendMessage(int contactId, string message, string sharedKey)
            {
                return await form.SendMessage(contactId, message, sharedKey);
            }

            public async Task<string> SetSharedKey(int contactId, string sharedKey)
            {
                return await form.SetSharedKey(contactId, sharedKey);
            }

            public async Task<string> GenerateSharedKey(int contactId)
            {
                return await form.GenerateSharedKey(contactId);
            }

            public async Task<string> UploadAvatar(int userId, string fileBase64, string fileName)
            {
                return await form.UploadAvatar(userId, fileBase64, fileName);
            }

            public async Task<string> GetAvatar(int userId)
            {
                return await form.GetAvatar(userId);
            }

            public async Task<string> GetUserInfo()
            {
                return await form.GetUserInfo();
            }

            public void OpenChat(int contactId, string username)
            {
                form.OpenChat(contactId, username);
            }
        }
    }
}