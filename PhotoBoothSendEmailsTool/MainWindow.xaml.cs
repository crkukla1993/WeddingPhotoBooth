using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Win32;
using MimeKit;
using Newtonsoft.Json;
using PhotoBoothSendEmailsTool.Classes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WeddingPhotoBooth.Classes;

namespace PhotoBoothSendEmailsTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private readonly string _photoBoothDirectory;
        private readonly IConfiguration _configuration;
        public MainWindow()
        {
            InitializeComponent();
            var builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.development.json", optional: true)
            .AddEnvironmentVariables();
            _configuration = builder.Build();
            _photoBoothDirectory = _configuration.GetValue<string>("PhotoBoothDirectory");
        }

        private void SelectLogFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                string fileText = File.ReadAllText(openFileDialog.FileName);
                IEnumerable<LogSession> logSessionList = JsonConvert.DeserializeObject<IEnumerable<LogSession>>(fileText);
                List<EmailLogItem> EmailLogItemList = logSessionList
                    .Where(x => x.Items.Any(y => y.LogItemType == LogItemType.Complete && y.EmailAddress != null))
                    .Select(x => new EmailLogItem(
                        x.Offline, 
                        x.SessionKey, 
                        x.Items.LastOrDefault(y => y.LogItemType == LogItemType.Complete && y.EmailAddress != null)?.EmailAddress,
                        x.Offline)).ToList();
                dataGrid.Visibility = Visibility.Visible;
                failures.Visibility = Visibility.Hidden;
                dataGrid.ItemsSource = EmailLogItemList;
            }
        }

        private static string FailedToSend = "";
        private void SendSelectedEmails_Click(object sender, RoutedEventArgs e)
        {
            FailedToSend = "";
            var itemList = dataGrid.Items;
            Mouse.OverrideCursor = Cursors.Wait;
            foreach(EmailLogItem item in itemList)
            {
                if (item.SendEmail)
                {
                    SendEmail(item.EmailAddress, item.SessionID); 
                }
            }
            MessageBox.Show("Emails sent!");
            if(FailedToSend != "")
            {
                failures.Text = FailedToSend;
                failures.Visibility = Visibility.Visible;
            }
            Mouse.OverrideCursor = Cursors.Arrow;
            dataGrid.ItemsSource = null;
        }

        private void SendEmail(string emailAddress, string sessionKey)
        {
            try
            {
                IConfigurationSection emailSection = _configuration.GetSection("Email");
                string smtpClientUrl = emailSection.GetValue<string>("SMTPClientUrl");
                int smtpClientPort = emailSection.GetValue<int>("SMTPClientPort");
                string sender = emailSection.GetValue<string>("EmailAddress");
                string emailPassword = emailSection.GetValue<string>("EmailPassword");
                string emailSubject = emailSection.GetValue<string>("EmailSubject");
                // create message
                var email = new MimeMessage();
                email.From.Add(MailboxAddress.Parse(sender));
                email.To.Add(MailboxAddress.Parse(emailAddress));
                email.Subject = emailSubject;

                var builder = new BodyBuilder
                {
                    TextBody = "Your photobooth photos are attached!"
                };

                string photoStripPath = System.IO.Path.Combine(_photoBoothDirectory, "sessions", sessionKey, "photoStrip.jpeg");
                string photo1 = System.IO.Path.Combine(_photoBoothDirectory, "sessions", sessionKey, "1.jpeg");
                string photo2 = System.IO.Path.Combine(_photoBoothDirectory, "sessions", sessionKey, "2.jpeg");
                string photo3 = System.IO.Path.Combine(_photoBoothDirectory, "sessions", sessionKey, "3.jpeg");

                builder.Attachments.Add(photoStripPath);
                builder.Attachments.Add(photo1);
                builder.Attachments.Add(photo2);
                builder.Attachments.Add(photo3);

                email.Body = builder.ToMessageBody();

                using var smtp = new SmtpClient();
                smtp.Connect(smtpClientUrl, smtpClientPort, SecureSocketOptions.StartTls);
                smtp.Authenticate(sender, emailPassword);
                smtp.Send(email);
                smtp.Disconnect(true);
            }
            catch (Exception)
            {
                lock (FailedToSend)
                {
                    FailedToSend += FailedToSend == "" ? "Failed to send:\n" + sessionKey : "\n" + sessionKey;
                }
            }
        }
    }
}
