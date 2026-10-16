using MailKit;
using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System.Collections.Specialized;
using System.Configuration;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;

namespace TimeTracker
{
    public partial class TimeTracker : Form
    {
        private DateTime? dayStartTime;
        private DateTime? breakStartTime;
        private TimeSpan totalProductiveTime;
        private TimeSpan totalBreakTime;
        private System.Windows.Forms.Timer? productiveTimer;
        private System.Windows.Forms.Timer? breakTimer;
        private string smtpServer_Host1 = "";
        private int smtpPort_Host1 = 587;
        private string senderEmail_Host1 = "";
        private string senderPassword_Host1 = "";
        private string recipientEmail_Host1 = "";
        private string smtpServer_Host2 = "";
        private int smtpPort_Host2 = 587;
        private string senderEmail_Host2 = "";
        private string senderPassword_Host2 = "";
        private string recipientEmail_Host2 = "";
        private Label? lblConfigMessage;
        private Button? btnProvideConfig;
        private static readonly object logLock = new object();
        TimeSpan allowedBreak;

        public TimeTracker()
        {
            InitializeComponent();            // 1. Initialize UI controls
            InitializeTimers();              // 2. Initialize timers

            SetInitialState();
            UpdateLabels();

            // 3. Ensure XML file exists
            XMLHelper.EnsureXmlFileExists();

            // 4. Load session from XML
            XMLHelper.LoadSession(out dayStartTime, out totalProductiveTime, out totalBreakTime);

            // 5. Prompt for config if absolutely missing
            if (IsAnySmtpConfigMissing())
            {
                MessageBox.Show("Please add email configuration for initial setup", "Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                PromptForMissingConfigValues();
            }

            // 6. Load latest config
            LoadConfigFromAppSettings();

            // 7. Check if config is valid; if not, show Config UI and return early
            if (!IsEmailConfigValid())
            {
                ShowConfigMissingUI();
                return;
            }

            // 8. If day has already started, re-enable timers and controls accordingly
            if (dayStartTime.HasValue)
            {
                btnDayStart.Enabled = false;

                DateTime now = DateTime.Now;
                TimeSpan breakElapsed = totalBreakTime;
                TimeSpan productiveElapsed;

                if (XMLHelper.IsOnBreak(out breakStartTime))
                {
                    btnBreakStart.Enabled = false;
                    btnBreakEnd.Enabled = true;

                    TimeSpan ongoingBreak = TimeSpan.Zero;
                    // Add elapsed break time from stored start time
                    var dtNow = DateTime.Now;
                    if (breakStartTime != null)
                    {
                        ongoingBreak = dtNow - breakStartTime.Value;
                    }
                    totalBreakTime += ongoingBreak;

                    // Deduct it from productive time
                    totalProductiveTime = dtNow - dayStartTime.Value - totalBreakTime;

                    breakTimer?.Start(); // resume break tracking
                }
                else
                {
                    // No active break — calculate productive time from now
                    btnBreakStart.Enabled = true;
                    btnBreakEnd.Enabled = false;

                    productiveElapsed = now - dayStartTime.Value - breakElapsed;
                    totalProductiveTime = productiveElapsed;

                    productiveTimer?.Start();
                }

                UpdateLabels();

                if (CanEndDay())
                    btnDayEnd.Enabled = true;
            }


        }

        private static bool IsAnySmtpConfigMissing()
        {
            var appSettings = ConfigurationManager.AppSettings;

            string[] requiredKeys = {"strNewSMTPHost_Host1", "intNewSMTPPort_Host1", "strNewSMTPFromEmail_Host1", "strToEmail_Host1", "strNewSMTPAuthPwd_Host1","strNewSMTPHost_Host2", "intNewSMTPPort_Host2", "strNewSMTPFromEmail_Host2", "strToEmail_Host2", "strNewSMTPAuthPwd_Host2","OfficialStartTime", "OfficialEndTime" };

            foreach (string key in requiredKeys)
            {
                if (string.IsNullOrWhiteSpace(appSettings[key]))
                    return true;
            }

            return false;
        }

        private void PromptForMissingConfigValues()
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            UpdateIfMissing(config, "strNewSMTPHost_Host1", "Enter SMTP Host for Host1:");
            UpdateIfMissing(config, "intNewSMTPPort_Host1", "Enter SMTP Port for Host1:");
            UpdateIfMissing(config, "strNewSMTPFromEmail_Host1", "Enter From Email for Host1:");
            UpdateIfMissing(config, "strToEmail_Host1", "Enter To Email for Host1:");
            string pwdHost1 = PromptFor("Enter SMTP Password for Host1:");

            UpdateIfMissing(config, "strNewSMTPHost_Host2", "Enter SMTP Host for Host2:");
            UpdateIfMissing(config, "intNewSMTPPort_Host2", "Enter SMTP Port for Host2:");
            UpdateIfMissing(config, "strNewSMTPFromEmail_Host2", "Enter From Email for Host2:");
            UpdateIfMissing(config, "strToEmail_Host2", "Enter To Email for Host2:");
            string pwdHost2 = PromptFor("Enter SMTP Password for Host2:");

            UpdateIfMissing(config, "OfficialStartTime", "Enter Official Start Time in 24 hours formate (HH:mm):");
            UpdateIfMissing(config, "OfficialEndTime", "Enter Official End Time in 24 hours formate (HH:mm):");
            
            if (!string.IsNullOrWhiteSpace(pwdHost2))
                UpdateEncryptedAppSetting("strNewSMTPAuthPwd_Host2", pwdHost2);
            if (!string.IsNullOrWhiteSpace(pwdHost1))
                UpdateEncryptedAppSetting("strNewSMTPAuthPwd_Host1", pwdHost1);
        }

        private bool IsEmailConfigValid()
        {
            var appSettings = ConfigurationManager.AppSettings;

            string[] requiredKeys = { "strNewSMTPHost_Host1", "intNewSMTPPort_Host1", "strNewSMTPFromEmail_Host1", "strToEmail_Host1", "strNewSMTPHost_Host2", "intNewSMTPPort_Host2", "strNewSMTPFromEmail_Host2", "strToEmail_Host2" };

            foreach (string key in requiredKeys)
            {
                if (string.IsNullOrWhiteSpace(appSettings[key]))
                    return false;
            }

            string? decryptedpwdHost1 = GetDecryptedSetting("strNewSMTPAuthPwd_Host1");
            string? decryptedpwdHost2 = GetDecryptedSetting("strNewSMTPAuthPwd_Host2");

            if (string.IsNullOrWhiteSpace(decryptedpwdHost1) || string.IsNullOrWhiteSpace(decryptedpwdHost2))
                return false;

            return true;
        }

        private string? GetDecryptedSetting(string key)
        {
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            string? encrypted = config.AppSettings.Settings[key]?.Value;
            return string.IsNullOrWhiteSpace(encrypted) ? null : DecryptValue(encrypted);
        }

        private void UpdateIfMissing(Configuration config, string key, string prompt)
        {
            var value = ConfigurationManager.AppSettings[key];
            if (!string.IsNullOrWhiteSpace(value))
                return;

            while (true)
            {
                string input = PromptFor(prompt);
                if (string.IsNullOrWhiteSpace(input)) return;

                // Email validation for specific keys
                if ((key.Contains("Email") || key.Contains("AuthUser")) && !IsValidEmail(input))
                {
                    MessageBox.Show("Invalid email format. Please try again.", "Validation Error");
                    continue;
                }

                var settings = config.AppSettings.Settings;
                if (settings[key] == null)
                    settings.Add(key, input);
                else
                    settings[key].Value = input;

                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
                break;
            }
        }

        private string PromptFor(string message)
        {
            bool isPasswordPrompt = message.ToLower().Contains("password");

            if (!isPasswordPrompt)
            {
                string strInputFromUser = Microsoft.VisualBasic.Interaction.InputBox(message, "Please enter required values to send emails");
                if (strInputFromUser == "")
                {
                    int stopsetup = (int)Microsoft.VisualBasic.Interaction.MsgBox("Application can not run without Email configuration please re-run", Microsoft.VisualBasic.MsgBoxStyle.OkOnly);
                    if (stopsetup == 1)
                    {
                        Environment.Exit(0);
                    }
                }
                return strInputFromUser;
            }

            // Show masked password input with toggle checkbox
            string? result = null;

            Form form = new Form()
            {
                Width = 350,
                Height = 180,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = "Password Input",
                StartPosition = FormStartPosition.CenterScreen
            };

            Label lblPrompt = new Label() { Left = 10, Top = 20, Width = 310, Text = message };
            TextBox txtPassword = new TextBox() { Left = 10, Top = 50, Width = 310, UseSystemPasswordChar = true };
            CheckBox chkShow = new CheckBox() { Left = 10, Top = 80, Text = "Show Password" };
            Button btnOK = new Button() { Text = "OK", Left = 160, Width = 75, Top = 110, DialogResult = DialogResult.OK };
            Button btnCancel = new Button() { Text = "Cancel", Left = 245, Width = 75, Top = 110, DialogResult = DialogResult.Cancel };

            chkShow.CheckedChanged += (s, e) =>
            {
                txtPassword.UseSystemPasswordChar = !chkShow.Checked;
            };

            form.Controls.AddRange(new Control[] { lblPrompt, txtPassword, chkShow, btnOK, btnCancel });
            form.AcceptButton = btnOK;
            form.CancelButton = btnCancel;

            if (form.ShowDialog() == DialogResult.OK)
                result = txtPassword.Text;

            form.Dispose();
            if (result != null)
            {
                return result;
            }
            else
            {
                return "";
            }
        }

        private static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private void InitializeTimers()
        {
            productiveTimer = new System.Windows.Forms.Timer();
            productiveTimer.Interval = 1000;
            productiveTimer.Tick += (s, e) =>
            {
                totalProductiveTime = totalProductiveTime.Add(TimeSpan.FromSeconds(1));
                UpdateLabels();
            };

            breakTimer = new System.Windows.Forms.Timer();
            breakTimer.Interval = 1000;
            breakTimer.Tick += (s, e) =>
            {
                totalBreakTime = totalBreakTime.Add(TimeSpan.FromSeconds(1));
                UpdateLabels();
            };
        }

        private void SetInitialState()
        {
            btnDayStart.Enabled = true;
            btnBreakStart.Enabled = false;
            btnBreakEnd.Enabled = false;
            btnDayEnd.Enabled = false;

            lblProductive.Text = "Productive Time : ";
            lblBreak.Text = "Break remaining: ";
            lblTotalTime.Text = "Total Time: ";

            totalProductiveTime = TimeSpan.Zero;
            totalBreakTime = TimeSpan.Zero;

            productiveTimer?.Stop();
            breakTimer?.Stop();

            chkHalfDay.Checked = false;
            chkHalfDay.Enabled = true;
        }

        private void UpdateLabels()
        {
            

            if (InvokeRequired)
            {
                Invoke(new Action(UpdateLabels));
                return;
            }

            if (dayStartTime != null)
            {
                lblStartTimer.Text = DateTime.Today.Add(dayStartTime.Value.TimeOfDay).ToString("hh:mm:ss tt").ToUpper();
            }

            TimeSpan timeSpan = TimeSpan.Zero;
            TimeSpan prodTime = (totalProductiveTime < timeSpan ? timeSpan : totalProductiveTime);


            lblProductiveTimer.Text = $"{FormatTime(prodTime)}";

            TimeSpan remainingBreakTime = allowedBreak - totalBreakTime;
            if (lblBreak.Text != "Break remaining: ")
            {
                lblBreak.Text = "Break remaining: ";
            }
            if (remainingBreakTime < TimeSpan.Zero)
            {
                remainingBreakTime = totalBreakTime;
                if (lblBreak.Text != "Total Break : ") 
                { 
                    lblBreak.Text = "Total Break : ";
                }
            }

            lblBreakTimer.Text = $"{FormatTime(remainingBreakTime)}";


            lblTotalTimer.Text = $"{FormatTime(totalProductiveTime + totalBreakTime)}";

            // Always re-evaluate Day End button state
            btnDayEnd.Enabled = CanEndDay() && !breakTimer.Enabled;

        }

        private static string FormatTime(TimeSpan time)
        {
            int totalHours = (int)time.TotalHours;
            return $"{totalHours:D2}:{time.Minutes:D2}:{time.Seconds:D2}";
        }


        private bool CanEndDay()
        {
            bool isHalfDay = chkHalfDay != null && chkHalfDay.Checked;

            string productiveKey = isHalfDay ? "HalfDayRequiredProductive" : "FullDayRequiredProductive";
            string breakKey = isHalfDay ? "HalfDayAllowedBreak" : "FullDayAllowedBreak";

            TimeSpan requiredProductive = GetTimeFromConfig(productiveKey, "RequiredProductiveFormat");
            allowedBreak = GetTimeFromConfig(breakKey, "AllowedBreakFormat");
            TimeSpan requiredTotalTime = requiredProductive + (totalBreakTime > allowedBreak ? totalBreakTime : allowedBreak);
            TimeSpan actualTotalTime = totalProductiveTime + totalBreakTime;

            if ( dayStartTime != null ) {
                TimeSpan baseTime = dayStartTime.Value.TimeOfDay;
                TimeSpan prpEndTime = baseTime + requiredTotalTime;
                if (prpEndTime < officialEndTime & !isHalfDay)
                {
                    lblEndTimer.Text = DateTime.Today.Add(officialEndTime).ToString("hh:mm:ss tt").ToUpper();
                }
                else
                {
                    lblEndTimer.Text = DateTime.Today.Add(prpEndTime).ToString("hh:mm:ss tt").ToUpper();
                }
            }

            if (totalProductiveTime < requiredProductive)
                return false;

            // Actual and required total time

            if (actualTotalTime < requiredTotalTime)
                return false;

            //Only enforce official end time check for FULL DAY if started early
            if (!isHalfDay && dayStartTime.HasValue && dayStartTime.Value.TimeOfDay < OfficialStartTime)
            {
                DateTime officialEnd = DateTime.Today.Add(officialEndTime);
                if (DateTime.Now < officialEnd)
                    return false;
            }

            return true;
        }

        private static TimeSpan GetTimeFromConfig(string key)
        {
            string value = ConfigurationManager.AppSettings[key];

            if (TimeSpan.TryParse(value, out TimeSpan time))
                return time;

            throw new ConfigurationErrorsException($"Invalid time format in config for key: {key}");
        }

        // Use it like this
        private TimeSpan OfficialStartTime => GetTimeFromConfig("OfficialStartTime");
        private TimeSpan officialEndTime => GetTimeFromConfig("OfficialEndTime");

        private TimeSpan GetTimeFromConfig(string key, string formatKey)
        {
            string valueStr = ConfigurationManager.AppSettings[key];
            string format = ConfigurationManager.AppSettings[formatKey]?.ToUpper();

            if (!double.TryParse(valueStr, out double value))
                return TimeSpan.Zero;

            return format switch
            {
                "HOURS" => TimeSpan.FromHours(value),
                "SECONDS" => TimeSpan.FromSeconds(value),
                _ => TimeSpan.FromMinutes(value) // Default to minutes
            };
        }


        private void chkHalfDay_CheckedChanged(object sender, EventArgs e)
        {
            UpdateLabels(); // Recheck Day End eligibility
        }

        private void btnDayStart_Click(object sender, EventArgs e)
        {
            Log("Day Start button clicked.");

            DateTime current = DateTime.Now;
            DateTime? manualTimeHost1 = null, manualTimeHost2 = null;

            var task1 = Task.Run(() => SendEmailAsync("START", "", smtpServer_Host1, (int)smtpPort_Host1, senderEmail_Host1, recipientEmail_Host1, senderPassword_Host1));
            var task2 = Task.Run(() => SendEmailAsync("START", "", smtpServer_Host2, (int)smtpPort_Host2, senderEmail_Host2, recipientEmail_Host2, senderPassword_Host2));
            Task.WaitAll(task1, task2);

            if (!task1.Result) manualTimeHost1 = PromptManualTime("Day Start Host1");
            if (!task2.Result) manualTimeHost2 = PromptManualTime("Day Start Host2");

            dayStartTime = manualTimeHost1 ?? manualTimeHost2 ?? current;

            totalProductiveTime = TimeSpan.Zero;
            totalBreakTime = TimeSpan.Zero;
            productiveTimer.Start();

            XMLHelper.EnsureXmlFileExists();
            XMLHelper.LogEvent("DayStart", dayStartTime.Value, task1.Result, task2.Result);
            XMLHelper.LoadSession(out dayStartTime, out totalProductiveTime, out totalBreakTime);

            btnDayStart.Enabled = false;
            btnBreakStart.Enabled = true;
            btnBreakEnd.Enabled = false;
            btnDayEnd.Enabled = false;
            ShowEmailStatus("Day Start", task1.Result, task2.Result);
        }

        private void btnBreakStart_Click(object sender, EventArgs e)
        {
            Log("Break Start button clicked.");

            DateTime current = DateTime.Now;
            DateTime? manualTimeHost1 = null, manualTimeHost2 = null;

            var task1 = Task.Run(() => SendEmailAsync("BREAK START", "", smtpServer_Host1, (int)smtpPort_Host1, senderEmail_Host1, recipientEmail_Host1, senderPassword_Host1));
            var task2 = Task.Run(() => SendEmailAsync("BREAK START", "", smtpServer_Host2, (int)smtpPort_Host2, senderEmail_Host2, recipientEmail_Host2, senderPassword_Host2));
            Task.WaitAll(task1, task2);

            if (!task1.Result) manualTimeHost1 = PromptManualTime("Break Start Host1");
            if (!task2.Result) manualTimeHost2 = PromptManualTime("Break Start Host2");

            breakStartTime = manualTimeHost1 ?? manualTimeHost2 ?? current;

            productiveTimer.Stop();
            breakTimer.Start();

            XMLHelper.LogEvent("BreakStart", breakStartTime.Value, task1.Result, task2.Result);
            XMLHelper.LoadSession(out dayStartTime, out totalProductiveTime, out totalBreakTime);

            btnBreakStart.Enabled = false;
            btnBreakEnd.Enabled = true;
            btnDayEnd.Enabled = false;
            ShowEmailStatus("Break Start", task1.Result, task2.Result);
        }

        private void btnBreakEnd_Click(object sender, EventArgs e)
        {
            Log("Break End button clicked.");

            DateTime current = DateTime.Now;
            DateTime? manualTimeHost1 = null, manualTimeHost2 = null;

            var task1 = Task.Run(() => SendEmailAsync("BREAK END", "", smtpServer_Host1, (int)smtpPort_Host1, senderEmail_Host1, recipientEmail_Host1, senderPassword_Host1));
            var task2 = Task.Run(() => SendEmailAsync("BREAK END", "", smtpServer_Host2, (int)smtpPort_Host2, senderEmail_Host2, recipientEmail_Host2, senderPassword_Host2));
            Task.WaitAll(task1, task2);

            if (!task1.Result) manualTimeHost1 = PromptManualTime("Break End Host1");
            if (!task2.Result) manualTimeHost2 = PromptManualTime("Break End Host2");

            DateTime breakEndTime = manualTimeHost1 ?? manualTimeHost2 ?? current;

            breakTimer?.Stop();

            TimeSpan duration = breakEndTime - breakStartTime.Value;
            totalBreakTime += duration;
            Log($"Actual break duration: {duration}");

            XMLHelper.LogEvent("BreakEnd", breakEndTime, task1.Result, task2.Result);
            XMLHelper.LoadSession(out dayStartTime, out totalProductiveTime, out totalBreakTime);

            productiveTimer?.Start();
            btnBreakStart.Enabled = true;
            btnBreakEnd.Enabled = false;
            btnDayEnd.Enabled = CanEndDay();
            ShowEmailStatus("Break End", task1.Result, task2.Result);
        }

        private void btnDayEnd_Click(object sender, EventArgs e)
        {
            Log("Day End button clicked.");

            productiveTimer?.Stop();
            breakTimer?.Stop();

            DateTime current = DateTime.Now;
            DateTime? manualTimeHost1 = null, manualTimeHost2 = null;

            var task1 = Task.Run(() => SendEmailAsync("END", "", smtpServer_Host1, (int)smtpPort_Host1, senderEmail_Host1, recipientEmail_Host1, senderPassword_Host1));
            var task2 = Task.Run(() => SendEmailAsync("END", "", smtpServer_Host2, (int)smtpPort_Host2, senderEmail_Host2, recipientEmail_Host2, senderPassword_Host2));
            Task.WaitAll(task1, task2);

            if (!task1.Result) manualTimeHost1 = PromptManualTime("Day End Host1");
            if (!task2.Result) manualTimeHost2 = PromptManualTime("Day End Host2");

            DateTime endTime = manualTimeHost1 ?? manualTimeHost2 ?? current;

            XMLHelper.LogEvent("DayEnd", endTime, task1.Result, task2.Result);
            XMLHelper.LoadSession(out dayStartTime, out totalProductiveTime, out totalBreakTime);

            try
            {
                string xmlFilePath = XMLHelper.GetXmlFilePath();
                if (File.Exists(xmlFilePath))
                {
                    File.Delete(xmlFilePath);
                }
            }
            catch (Exception ex)
            {
                Log($"Failed to delete session XML file: {ex.Message}");
            }

            ShowEmailStatus("Day End", task1.Result, task2.Result);
            SetInitialState();
        }

        private DateTime? PromptManualTime(string label)
        {
            Log($"Email failed. Prompting user for manual input for {label}.");

            string input = Microsoft.VisualBasic.Interaction.InputBox(
                $"{label} email failed. Please enter time in 24-hour format (HH:mm or HH:mm:ss):",
                $"{label} Time",
                DateTime.Now.ToString("HH:mm:ss")
            );

            if (string.IsNullOrWhiteSpace(input))
            {
                var result = MessageBox.Show("You left the input empty. Did you mean to cancel?", "Confirm Cancel",
                                             MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    Log($"{label} manual input canceled by user.");
                    return null;
                }
                else
                {
                    return PromptManualTime(label);
                }
            }

            if (DateTime.TryParseExact(input, new[] { "HH:mm", "HH:mm:ss" }, null, System.Globalization.DateTimeStyles.None, out DateTime manualTime))
            {
                return manualTime;
            }

            MessageBox.Show("Invalid input. Please enter a valid time in 24-hour format.");
            return PromptManualTime(label);
        }

        private async Task<bool> SendEmailAsync(string subject, string bodyPlaceholder, string smtpHost, int port, string fromEmail, string toEmail, string password)
        {
            try
            {
                // Determine domain key based on sender's email
                string? domainKey = fromEmail.Contains("Host1", StringComparison.OrdinalIgnoreCase) ? "Host1" : fromEmail.Contains("Host2", StringComparison.OrdinalIgnoreCase) ? "Host2" : null;

                if (domainKey == null)
                {
                    MessageBox.Show($"Unknown domain in email: {fromEmail}", "Domain Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                string signaturePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"signature_{domainKey}.txt");

                if (!File.Exists(signaturePath))
                {
                    MessageBox.Show($"Signature file missing: {signaturePath}", "Missing Signature", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                string signatureBody = await File.ReadAllTextAsync(signaturePath);

                var message = new MimeMessage();
                message.From.Add(MailboxAddress.Parse(fromEmail));
                foreach (var recipient in toEmail.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    message.To.Add(MailboxAddress.Parse(recipient.Trim()));
                }
                message.Subject = subject;
                message.Body = new TextPart("html") { Text = signatureBody };

                SecureSocketOptions sslOption = port switch
                {
                    465 => SecureSocketOptions.SslOnConnect,
                    587 => SecureSocketOptions.StartTls,
                    25 => SecureSocketOptions.None,
                    _ => SecureSocketOptions.Auto
                };
                var smtpClient = new SmtpClient();
                if (fromEmail.EndsWith("Host2_Domain", StringComparison.OrdinalIgnoreCase) ||
                    smtpHost.Contains("Host2_Domain", StringComparison.OrdinalIgnoreCase))
                {
                    smtpClient.ServerCertificateValidationCallback = (s, c, h, e) => true;
                }

                using (smtpClient)
                {
                    await smtpClient.ConnectAsync(smtpHost, port, sslOption);
                    await smtpClient.AuthenticateAsync(fromEmail, password);
                    await smtpClient.SendAsync(message);
                    await smtpClient.DisconnectAsync(true);
                }

                if (smtpHost.Contains("Host2_Domain"))
                {
                    using var imap = new ImapClient();
                    imap.ServerCertificateValidationCallback = (s, c, h, e) => true;
                    imap.Connect("Host2_Domain", 993, SecureSocketOptions.SslOnConnect);
                    imap.Authenticate(fromEmail, password);

                    var sent = imap.GetFolder(SpecialFolder.Sent) ?? imap.GetFolder("Sent") ?? imap.GetFolder("[Gmail]/Sent Mail");
                    if (sent != null && sent.Exists)
                    {
                        sent.Open(FolderAccess.ReadWrite);
                        var MailUID = sent.Append(message, MessageFlags.Seen);
                        if (MailUID is not null)
                        {
                            sent.AddFlags(MailUID.Value, MessageFlags.Seen, true);
                        }
                    }
                    imap.Disconnect(true);
                }

                Log($"Email sent from {fromEmail} to {toEmail} via {smtpHost}:{port}");
                return true;
            }
            catch (Exception ex)
            {
                Log($"Failed to send email from {fromEmail}. Error: {ex.Message}");
                MessageBox.Show($"Email failed: {ex.Message}", "Email Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private void LoadConfigFromAppSettings()
        {
            try
            {
                LoadConfigFromAppSettings_Host1();
                LoadConfigFromAppSettings_Host2();
            }
            catch (Exception ex)
            {
                Log("Error loading config: " + ex.Message);
                MessageBox.Show("Failed to load SMTP configuration.");
            }
        }

        private void LoadConfigFromAppSettings_Host1()
        {
            var smtphost = ConfigurationManager.AppSettings["strNewSMTPHost_Host1"];
            if (!string.IsNullOrWhiteSpace(smtphost))
            {
                smtpServer_Host1 = smtphost;
            }
            else
            {
                smtpServer_Host1 = "";
            }
            var smtphost1port = ConfigurationManager.AppSettings["intNewSMTPPort_Host1"];
            if (!string.IsNullOrWhiteSpace(smtphost1port))
            {
                smtpPort_Host1 = int.Parse(smtphost1port);
            }
            else
            {
                smtpPort_Host1 = 587;
            }
            var smtpEmail = ConfigurationManager.AppSettings["strNewSMTPFromEmail_Host1"];
            if (!string.IsNullOrWhiteSpace(smtpEmail))
            {
                senderEmail_Host1 = smtpEmail;
            }
            else
            {
                senderEmail_Host1 = "";
            }
            var smtpToEmail = ConfigurationManager.AppSettings["strToEmail_Host1"];
            if (!string.IsNullOrWhiteSpace(smtpToEmail))
            {
                recipientEmail_Host1 = smtpToEmail;
            }
            else
            {
                recipientEmail_Host1 = "";
            }
            string? encryptedPwdHost1 = ConfigurationManager.AppSettings["strNewSMTPAuthPwd_Host1"];
            if (!string.IsNullOrWhiteSpace(encryptedPwdHost1))
            {
                senderPassword_Host1 = IsBase64String(encryptedPwdHost1) ? DecryptValue(encryptedPwdHost1) : encryptedPwdHost1;
            }
            else
            {
                senderPassword_Host1 = string.Empty;
            }
        }
        private void LoadConfigFromAppSettings_Host2()
        {
            var smtphost = ConfigurationManager.AppSettings["strNewSMTPHost_Host2"];
            if (!string.IsNullOrWhiteSpace(smtphost))
            {
                smtpServer_Host2 = smtphost;
            }
            else
            {
                smtpServer_Host2 = "";
            }
            var smtphost1port = ConfigurationManager.AppSettings["intNewSMTPPort_Host2"];
            if (!string.IsNullOrWhiteSpace(smtphost1port))
            {
                smtpPort_Host2 = int.Parse(smtphost1port);
            }
            else
            {
                smtpPort_Host2 = 587;
            }
            var smtpEmail = ConfigurationManager.AppSettings["strNewSMTPFromEmail_Host2"];
            if (!string.IsNullOrWhiteSpace(smtpEmail))
            {
                senderEmail_Host2 = smtpEmail;
            }
            else
            {
                senderEmail_Host2 = "";
            }
            var smtpToEmail = ConfigurationManager.AppSettings["strToEmail_Host2"];
            if (!string.IsNullOrWhiteSpace(smtpToEmail))
            {
                recipientEmail_Host2 = smtpToEmail;
            }
            else
            {
                recipientEmail_Host2 = "";
            }
            string? encryptedPwdHost2 = ConfigurationManager.AppSettings["strNewSMTPAuthPwd_Host2"];
            if (!string.IsNullOrWhiteSpace(encryptedPwdHost2))
            {
                senderPassword_Host2 = IsBase64String(encryptedPwdHost2) ? DecryptValue(encryptedPwdHost2) : encryptedPwdHost2;
            }
            else
            {
                senderPassword_Host2 = string.Empty;
            }
        }


        private void btnUpdatePassword_Click(object sender, EventArgs e)
        {
            string choice = ShowPasswordDomainSelector();

            if (string.IsNullOrWhiteSpace(choice))
            {
                MessageBox.Show("No selection made. Operation cancelled.");
                return;
            }

            if (choice == "Host1" || choice == "BOTH")
            {
                string newPwdHost1 = PromptFor("Enter new SMTP password for Host1 domain:");
                if (!string.IsNullOrWhiteSpace(newPwdHost1))
                    UpdateEncryptedAppSetting("strNewSMTPAuthPwd_Host1", newPwdHost1);
            }

            if (choice == "Host2" || choice == "BOTH")
            {
                string newPwdHost2 = PromptFor("Enter new SMTP password for Host2 domain:");
                if (!string.IsNullOrWhiteSpace(newPwdHost2))
                    UpdateEncryptedAppSetting("strNewSMTPAuthPwd_Host2", newPwdHost2);
            }

            MessageBox.Show("Password(s) updated successfully. Please restart the app to apply changes.");
        }

        private string ShowPasswordDomainSelector()
        {
            string? result = null;

            Form form = new()
            {
                Width = 470,
                Height = 240,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = "Select Domain",
                StartPosition = FormStartPosition.CenterScreen,
                BackColor = Color.White
            };

            Label lblTitle = new Label()
            {
                Text = "Please select which domain password you want to update:",
                Left = 20,
                Top = 15,
                Width = 430
            };

            RadioButton rdoHost1 = new RadioButton() { Text = "Host1", Left = 40, Top = 50, AutoSize = true };
            RadioButton TimeTracker = new RadioButton() { Text = "Host2", Left = 40, Top = 80, AutoSize = true };
            RadioButton rdoBoth = new RadioButton() { Text = "Both", Left = 40, Top = 110, AutoSize = true };

            Button btnOK = new Button() { Text = "OK", Left = 150, Width = 90, Top = 150, Height = 40 };
            Button btnCancel = new Button() { Text = "Cancel", Left = 250, Width = 90, Top = 150, Height = 40, DialogResult = DialogResult.Cancel };

            form.Controls.AddRange(new Control[] { lblTitle, rdoHost1, TimeTracker, rdoBoth, btnOK, btnCancel });
            form.CancelButton = btnCancel;

            btnOK.Click += (s, e) =>
            {
                if (!rdoHost1.Checked && !TimeTracker.Checked && !rdoBoth.Checked)
                {
                    MessageBox.Show("Please select one option.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                form.DialogResult = DialogResult.OK;
                form.Close();
            };

            if (form.ShowDialog() == DialogResult.OK)
            {
                if (rdoBoth.Checked)
                    result = "BOTH";
                else if (rdoHost1.Checked)
                    result = "Host1";
                else if (TimeTracker.Checked)
                    result = "Host2";
            }

            form.Dispose();
            if (result != null)
            {
                return result;
            }
            else
            {
                return "";
            }
        }

        private void UpdateEncryptedAppSetting(string key, string plainTextValue)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(plainTextValue))
                {
                    Log($"Skipped updating {key} because value was empty or null.");
                    return;
                }

                string encrypted = EncryptValue(plainTextValue);

                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                var settings = config.AppSettings.Settings;

                if (settings[key] != null)
                    settings[key].Value = encrypted;
                else
                    settings.Add(key, encrypted);

                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");

                Log($"Updated encrypted appSetting: {key}");
            }
            catch (Exception ex)
            {
                Log("Error updating encrypted appSetting: " + ex.Message);
            }
        }

        private string EncryptValue(string input)
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            byte[] encryptedBytes = ProtectedData.Protect(inputBytes, null, DataProtectionScope.LocalMachine);
            return Convert.ToBase64String(encryptedBytes);
        }

        private string DecryptValue(string encryptedBase64)
        {
            byte[] encryptedBytes = Convert.FromBase64String(encryptedBase64);
            byte[] decryptedBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.LocalMachine);
            return Encoding.UTF8.GetString(decryptedBytes);
        }

        private bool IsBase64String(string val)
        {
            try { Convert.FromBase64String(val); return true; } catch { return false; }
        }


        private void ShowEmailStatus(string label, bool task1Success, bool task2Success)
        {
            if (task1Success && !task2Success)
            {
                MessageBox.Show($"{label} sent only to Host1.");
            }
            else if (task2Success && !task1Success)
            {
                MessageBox.Show($"{label} sent only to Host2.");
            }
            else if (!task1Success && !task2Success)
            {
                MessageBox.Show($"{label} failed to send to both Domain.");
            }
        }

        private void btnResetDay_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to reset today's session? This will delete all time tracking data for today.",
                                         "Confirm Reset", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                try
                {
                    // Overwrite XML with a fresh structure
                    XMLHelper.CreateFreshXmlForToday();

                    Log("Session reset: XML file reinitialized.");
                }
                catch (Exception ex)
                {
                    Log("Error resetting session XML: " + ex.Message);
                    MessageBox.Show("Failed to reset session: " + ex.Message);
                    return;
                }

                productiveTimer?.Stop();
                breakTimer?.Stop();

                dayStartTime = null;
                breakStartTime = null;
                totalProductiveTime = TimeSpan.Zero;
                totalBreakTime = TimeSpan.Zero;

                SetInitialState();
                UpdateLabels();

                MessageBox.Show("Day reset successfully.");
            }
        }
        private void CenterConfigUI()
        {
            if (lblConfigMessage != null)
            {
                int centerX = (this.ClientSize.Width - lblConfigMessage.Width) / 2;
                if (btnProvideConfig != null)
                {
                    int centerY = (this.ClientSize.Height - (lblConfigMessage.Height + btnProvideConfig.Height + 10)) / 2;
                    lblConfigMessage.Location = new Point(centerX, centerY);
                    btnProvideConfig.Location = new Point(
                        (this.ClientSize.Width - btnProvideConfig.Width) / 2,
                        lblConfigMessage.Bottom + 10
                    );
                }
            }
        }

        private void btnWorkTimeChange_Click(object sender, EventArgs e)
        {
            string startInput = PromptForWorkTimeChange("Enter new Official Start Time in 24 hours formate (HH:mm):");
            string endInput = PromptForWorkTimeChange("Enter new Official End Time in 24 hours formate (HH:mm):");

            if (TimeSpan.TryParse(startInput, out TimeSpan start) && TimeSpan.TryParse(endInput, out TimeSpan end))
            {
                UpdateAppSetting("OfficialStartTime", start.ToString(@"hh\:mm"));
                UpdateAppSetting("OfficialEndTime", end.ToString(@"hh\:mm"));
                MessageBox.Show("Work time updated successfully.");
            }
            else
            {
                MessageBox.Show("Invalid time format. Please enter time as HH:mm (e.g., 09:00).");
            }
        }

        private string PromptForWorkTimeChange(string message)
        {
            return Microsoft.VisualBasic.Interaction.InputBox(message, "Work Time Change");
        }

        private void UpdateAppSetting(string key, string value)
        {
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            if (config.AppSettings.Settings[key] != null)
                config.AppSettings.Settings[key].Value = value;
            else
                config.AppSettings.Settings.Add(key, value);

            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }

        private void ShowConfigMissingUI()
        {
            lblConfigMessage = new Label
            {
                Name = "lblConfigMessage",
                Text = "Some email configuration values are missing.\nPlease click below to provide the required settings.",
                AutoSize = false,
                Width = 350,
                Height = 60,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };

            btnProvideConfig = new Button
            {
                Name = "btnProvideConfig",
                Text = "Configure Now",
                Width = 150,
                Height = 30,
            };

            btnProvideConfig.Click += (s, e) =>
            {
                PromptForMissingConfigValues();
                LoadConfigFromAppSettings();

                if (IsEmailConfigValid())
                {
                    Controls.Remove(lblConfigMessage);
                    Controls.Remove(btnProvideConfig);
                    SetInitialState();

                    // Show main controls
                    btnDayStart.Visible = true;
                    btnBreakStart.Visible = true;
                    btnBreakEnd.Visible = true;
                    btnDayEnd.Visible = true;
                    btnResetDay.Visible = true;
                    chkHalfDay.Visible = true;
                    btnUpdatePassword.Visible = true;
                    lblProductive.Visible = true;
                    lblBreak.Visible = true;
                    lblTotalTime.Visible = true;
                    lblStartTimer.Visible = true;
                    lblEndTimer.Visible = true;
                    lblStartTm.Visible = true;
                    lblBreakTimer.Visible = true;
                    lblProductiveTimer.Visible = true;
                    lblTotalTimer.Visible = true;
                    lblCalEndTm.Visible = true;
                }
                else
                {
                    MessageBox.Show("Some required values are still missing.");
                }
            };

            Controls.Add(lblConfigMessage);
            Controls.Add(btnProvideConfig);
            CenterConfigUI();
            this.Resize += (s, e) => CenterConfigUI();

            // Hide main controls
            btnDayStart.Visible = false;
            btnBreakStart.Visible = false;
            btnBreakEnd.Visible = false;
            btnDayEnd.Visible = false;
            btnResetDay.Visible = false;
            btnWorkTimeChange.Visible = false;
            chkHalfDay.Visible = false;
            btnUpdatePassword.Visible = false;
            lblProductive.Visible = false;
            lblBreak.Visible = false;
            lblTotalTime.Visible = false;
            lblStartTimer.Visible = false;
            lblEndTimer.Visible = false;    
            lblStartTm.Visible = false;
            lblBreakTimer.Visible = false;
            lblProductiveTimer.Visible = false;
            lblTotalTimer.Visible = false; 
            lblCalEndTm.Visible = false;    

        }

        private void Log(string message)
        {
            string logFile = "tracker_log.txt";
            string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}";

            lock (logLock)
            {
                File.AppendAllText(logFile, logMessage + Environment.NewLine);
            }
        }
        
    }
}