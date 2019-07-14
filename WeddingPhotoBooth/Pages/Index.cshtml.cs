using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.IO;
using System.Drawing.Printing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Drawing;
using System.Net.Mail;
using Newtonsoft.Json;
using WeddingPhotoBooth.Classes;
using Microsoft.Extensions.Configuration;

namespace WeddingPhotoBooth.Pages
{
    public class IndexModel : PageModel
    {
        public static string SessionKey { get; set; }
        private readonly string _photoBoothDirectory;
        private readonly IConfiguration _configuration;
        public IndexModel(IConfiguration configuration)
        {
            _configuration = configuration;
            _photoBoothDirectory = _configuration.GetValue<string>("PhotoBoothDirectory");
        }

        public IActionResult OnPostComplete([FromBody]CompleteData completeData)
        {
            try
            {
                if (CombineImages())
                {
                    switch (completeData.Option)
                    {
                        case 1:
                            SendEmail(completeData.EmailAddress);
                            WriteToJsonLog("Photos emailed", LogItemType.Complete, completeData.EmailAddress);
                            break;
                        case 2:
                            PrintPhotos();
                            WriteToJsonLog("Photos printed", LogItemType.Complete);
                            break;
                        case 3:
                            PrintPhotos();
                            SendEmail(completeData.EmailAddress);
                            WriteToJsonLog("Photos emailed & printed", LogItemType.Complete, completeData.EmailAddress);
                            break;
                        default:
                            throw new Exception("Invalid Option");
                    }
                }
                return StatusCode(200);
            }
            catch(Exception e)
            {
                WriteToJsonLog("Photo errored on complete", LogItemType.Error);
                return new ContentResult() { Content = e.Message, ContentType = "plain/text", StatusCode = 500 };
            }
        }

        private IEnumerable<PhotoTemplateImageSetting> GetTemplateImageSettings(Bitmap bmp)
        {
            var lockedBitmap = new LockBitmap(bmp);
            lockedBitmap.LockBits();
            Color magenta = Color.FromArgb(255, 0, 255);
            List<PhotoTemplateImageSetting> pList = new List<PhotoTemplateImageSetting>();
            for (int y = 0; y < lockedBitmap.Height; y++)
            {
                for (int x = 0; x < lockedBitmap.Width; x++)
                {
                    Color currentPixel = lockedBitmap.GetPixel(x, y);
                    Color LeftPixel = x == 0 ? Color.Empty : lockedBitmap.GetPixel(x - 1, y);
                    Color TopPixel = y == 0 ? Color.Empty : lockedBitmap.GetPixel(x, y - 1);

                    if (currentPixel == magenta && LeftPixel != magenta && TopPixel != magenta)
                    {
                        Tuple<int, int> position = new Tuple<int, int>(x - 1, y - 1);
                        int i = x;
                        int j = y;

                        while (lockedBitmap.GetPixel(i, y) == magenta)
                        {
                            i++;
                        }
                        while (lockedBitmap.GetPixel(x, j) == magenta)
                        {
                            j++;
                        }


                        pList.Add(new PhotoTemplateImageSetting()
                        {
                            Position = position,
                            Height = j - y + 3,
                            Width = i - x + 3
                        });
                    }
                }
            }
            lockedBitmap.UnlockBits();
            return pList;
        }

        private bool CombineImages()
        {
            try
            {
                string templatePath = _configuration.GetValue<string>("TemplateFullFilePath");
                using (var frame = Image.FromFile(templatePath))
                using (var img1 = Image.FromFile(Path.Combine(_photoBoothDirectory, "sessions", SessionKey, "1.jpeg")))
                using (var img2 = Image.FromFile(Path.Combine(_photoBoothDirectory, "sessions", SessionKey, "2.jpeg")))
                using (var img3 = Image.FromFile(Path.Combine(_photoBoothDirectory, "sessions", SessionKey, "3.jpeg")))
                using (var bmp1 = new Bitmap(img1, new Size(img1.Width, img1.Height)))
                using (var bmp2 = new Bitmap(img2, new Size(img2.Width, img2.Height)))
                using (var bmp3 = new Bitmap(img3, new Size(img3.Width, img3.Height)))
                using (var bitmap = new Bitmap(1844, 1240))
                {

                    var settings = GetTemplateImageSettings((Bitmap)frame);

                    using (var canvas = Graphics.FromImage(bitmap))
                    {
                        canvas.SmoothingMode = SmoothingMode.HighQuality;
                        canvas.CompositingQuality = CompositingQuality.HighQuality;
                        canvas.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        canvas.DrawImage(frame, 0, 0, frame.Width, frame.Height);
                        int loop = 1;
                        foreach (PhotoTemplateImageSetting setting in settings)
                        {
                            Bitmap bmp = null;
                            if (loop % 3 == 0)
                            {
                                bmp = bmp3;
                            }
                            else if (loop % 2 == 0)
                            {
                                bmp = bmp2;
                            }
                            else if (loop % 1 == 0)
                            {
                                bmp = bmp1;
                            }
                            canvas.DrawImage(bmp, new Rectangle(setting.Position.Item1, setting.Position.Item2, setting.Width, setting.Height));
                            loop++;

                        }

                        canvas.Save();
                    }
                    //bitmap.RotateFlip(RotateFlipType.Rotate90FlipNone);
                    bitmap.Save(Path.Combine(_photoBoothDirectory, "sessions", SessionKey, "photoStrip.jpeg"), ImageFormat.Jpeg);
                    WriteToJsonLog("Photo strip saved to file", LogItemType.System);
                    return true;
                }
            }
            catch (Exception)
            {
                WriteToJsonLog("Photo errored on combine", LogItemType.Error);
                return false;
            }
        }

        public IActionResult OnPostGenerateSessionKey()
        {
            SessionKey = Guid.NewGuid().ToString();
            DirectoryInfo di = new DirectoryInfo(Path.Combine(_photoBoothDirectory, "sessions", SessionKey));
            di.Create();
            WriteToJsonLog("Session started", LogItemType.Start);
            return new ContentResult() { Content = SessionKey, ContentType = "plain/text", StatusCode = 200 };
        }
        
        public IActionResult OnPostSubmitPhotos([FromBody]string[] imageDataArray)
        {
            try
            {
                int i = 0;
                while (i < imageDataArray.Length)
                {
                    string fileNameWitPath = $@"{Path.Combine(_photoBoothDirectory, "sessions", SessionKey, (i + 1).ToString())}.jpeg";
                    using (FileStream fs = new FileStream(fileNameWitPath, FileMode.Create))
                    {
                        using (BinaryWriter bw = new BinaryWriter(fs))
                        {
                            byte[] data = Convert.FromBase64String(imageDataArray[i]);
                            bw.Write(data);
                            bw.Close();
                        }
                    }
                    i++;
                }
                WriteToJsonLog("Saved photos to file", LogItemType.System);
                return StatusCode(200);
            }
            catch (Exception)
            {
                WriteToJsonLog("Photos errored on save", LogItemType.Error);
                return StatusCode(500);
            }
        }

        private void SendEmail(string emailAddress)
        {
            IConfigurationSection emailSection = _configuration.GetSection("Email");
            string smtpClientUrl = emailSection.GetValue<string>("SMTPClientUrl");
            int smtpClientPort = emailSection.GetValue<int>("SMTPClientPort");
            string sender = emailSection.GetValue<string>("EmailAddress");
            string emailPassword = emailSection.GetValue<string>("EmailPassword");
            string emailSubject = emailSection.GetValue<string>("EmailSubject");

            SmtpClient smtpClient = new SmtpClient(smtpClientUrl, smtpClientPort)
            {
                Credentials = new System.Net.NetworkCredential(sender, emailPassword),
                EnableSsl = true               
            };
            
            MailMessage mail = new MailMessage
            {
                Subject = emailSubject
            };

            mail.Attachments.Add(new Attachment(Path.Combine(_photoBoothDirectory, "sessions", SessionKey, "photoStrip.jpeg")));
            mail.Attachments.Add(new Attachment(Path.Combine(_photoBoothDirectory, "sessions", SessionKey, "1.jpeg")));
            mail.Attachments.Add(new Attachment(Path.Combine(_photoBoothDirectory, "sessions", SessionKey, "2.jpeg")));
            mail.Attachments.Add(new Attachment(Path.Combine(_photoBoothDirectory, "sessions", SessionKey, "3.jpeg")));
            

            mail.From = new MailAddress(sender);
            mail.To.Add(new MailAddress(emailAddress));

            smtpClient.Send(mail);
        }

        private void PrintPhotos()
        {
            try
            {
                string printerName = _configuration.GetValue<string>("PrinterName");
                PrintDocument pd = new PrintDocument();
                pd.PrinterSettings.PrinterName = printerName;
                //pd.PrinterSettings.PrinterName = @"Canon SELPHY CP1300 WS";
                //pd.PrinterSettings.PrinterName = @"HP807FE1 (HP OfficeJet Pro 6960)";
                //pd.PrinterSettings.PrinterName = @"HP3B556A (HP Officejet 6600)";
                pd.DefaultPageSettings.Color = true;
                pd.OriginAtMargins = false;

                pd.PrintPage += new PrintPageEventHandler
                    (this.PrintImage);
                pd.Print();
            }
            catch (Exception ex)
            {
                WriteToJsonLog("System errored on print", LogItemType.Error);
                Console.WriteLine(ex.Message);
            }
        }

        private void PrintImage(object sender, PrintPageEventArgs ev)
        {
            Image i = Image.FromFile(Path.Combine(_photoBoothDirectory, "sessions", SessionKey, "photoStrip.jpeg"));

            float newWidth = i.Width * 100 / i.HorizontalResolution;  // Convert to same units (100 ppi) as e.MarginBounds.Width
            float newHeight = i.Height * 100 / i.VerticalResolution;   // Convert to same units (100 ppi) as e.MarginBounds.Height

            float widthFactor = newWidth / ev.MarginBounds.Width;
            float heightFactor = newHeight / ev.MarginBounds.Height;


            if (widthFactor > 1 | heightFactor > 1) // if the image is wider or taller than the printable area then adjust...
            {
                if (widthFactor > heightFactor)
                {
                    newWidth = newWidth / widthFactor;
                    newHeight = newHeight / widthFactor;
                }
                else
                {
                    newWidth = newWidth / heightFactor;
                    newHeight = newHeight / heightFactor;
                }
            }

            ev.Graphics.DrawImage(i, 12, 12, (int)(newWidth*2)-12, (int)(newHeight*2)-12);
        }

        public IActionResult OnPostDeleteSession()
        {
            try
            {
                DirectoryInfo di = new DirectoryInfo(Path.Combine(_photoBoothDirectory, "sessions", SessionKey));
                di.Delete(true);
                WriteToJsonLog("Session deleted", LogItemType.System);
                return StatusCode(200);
            }
            catch (Exception e)
            {
                WriteToJsonLog("Error on deleting session", LogItemType.Error);
                return StatusCode(500);
            }
        }

        public IActionResult OnPostLogAction([FromBody]string action)
        {
            try
            {
                WriteToJsonLog(action, LogItemType.System);
            }
            catch (Exception)
            {

            }
            return StatusCode(200);
        }


        private List<LogSession> LoadJsonLog()
        {
            List<LogSession> items;
            string path = _configuration.GetValue<string>("LogFullFilePath");
            FileInfo f = new FileInfo(path);
            if (!f.Exists)
            {
                FileStream fs = f.Create();
                fs.Close();                
            }
            
            using (StreamReader r = new StreamReader(path))
            {
                string json = r.ReadToEnd();
                items = JsonConvert.DeserializeObject<List<LogSession>>(json);
            }
            return items;
        }

        private void WriteToJsonLog(string action, LogItemType logItemType, string emailAddress = null)
        {
            try
            {
                string path = _configuration.GetValue<string>("LogFullFilePath");
                List<LogSession> items = LoadJsonLog() ?? new List<LogSession>();

                LogSession session = items.FirstOrDefault(x => x.SessionKey == SessionKey);
                if (session == null)
                {
                    session = new LogSession()
                    {
                        SessionKey = SessionKey,
                        CreateDate = DateTime.Now,
                        Items = new List<LogItem>()
                    };

                    session.Items.Add(new LogItem()
                    {
                        Action = action,
                        EmailAddress = emailAddress,
                        LogItemType = logItemType,
                        LogDate = DateTime.Now
                    });
                    items.Add(session);
                }
                else
                {
                    items.Remove(session);
                    session.Items.Add(new LogItem()
                    {
                        Action = action,
                        EmailAddress = emailAddress,
                        LogItemType = logItemType,
                        LogDate = DateTime.Now
                    });
                    items.Add(session);
                }

                items = items.OrderBy(x => x.CreateDate).ToList();
                string data = JsonConvert.SerializeObject(items);
                using (StreamWriter writer = new StreamWriter(path))
                {
                    writer.Write(data);
                }
            }
            catch (Exception)
            {

            }
        }
    }
}