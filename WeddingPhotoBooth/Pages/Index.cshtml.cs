using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.IO;
using System.Drawing.Printing;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing;
using System.Net.Mail;
using System.Net;

namespace WeddingPhotoBooth.Pages
{
    public class IndexModel : PageModel
    {
        public static string SessionKey { get; set; }
        private static int CurrentPhotoCount { get; set; }
        private const string PHOTO_DIRECTORY = @"C:\PhotoBooth";
        public void OnGet()
        {
            GenerateSessionKey();
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
                            break;
                        case 2:
                            PrintPhotos();
                            break;
                        case 3:
                            PrintPhotos();
                            SendEmail(completeData.EmailAddress);
                            break;
                        default:
                            throw new Exception("Invalid Option");
                    }
                }
                return StatusCode(200);
            }
            catch(Exception e)
            {
                return StatusCode(500);
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
            using (var frame = Image.FromFile(Path.Combine(PHOTO_DIRECTORY, "template.png")))
            using (var img1 = Image.FromFile(Path.Combine(PHOTO_DIRECTORY, SessionKey, "1.png")))
            using (var img2 = Image.FromFile(Path.Combine(PHOTO_DIRECTORY, SessionKey, "2.png")))
            using (var img3 = Image.FromFile(Path.Combine(PHOTO_DIRECTORY, SessionKey, "3.png")))
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
                    foreach(PhotoTemplateImageSetting setting in settings)
                    {
                        Bitmap bmp = null;
                        if(loop % 3 == 0)
                        {
                            bmp = bmp3; 
                        }
                        else if(loop % 2 == 0)
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
                bitmap.RotateFlip(RotateFlipType.Rotate90FlipNone);
                bitmap.Save(Path.Combine(PHOTO_DIRECTORY, SessionKey, "photoStrip.png"), ImageFormat.Png);
                return true;
            }
        }

        private void GenerateSessionKey()
        {
            SessionKey = Guid.NewGuid().ToString();
            DirectoryInfo di = new DirectoryInfo(Path.Combine(PHOTO_DIRECTORY, SessionKey));
            di.Create();
            CurrentPhotoCount = 0;
        }
        
        public IActionResult OnPostSubmitPhotos([FromBody]string[] imageDataArray)
        {
            try
            {
                int i = 0;
                while (i < imageDataArray.Length)
                {
                    string fileNameWitPath = $@"{Path.Combine(PHOTO_DIRECTORY, SessionKey, (i + 1).ToString())}.png";
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
                return StatusCode(200);
            }
            catch (Exception)
            {
                return StatusCode(500);
            }
        }

        private void SendEmail(string emailAddress)
        {
            SmtpClient smtpClient = new SmtpClient("smtp.gmail.com", 587)
            {
                Credentials = new System.Net.NetworkCredential("chrisandkristinawedding2020@gmail.com", "C#Coder21"),
                EnableSsl = true
            };
            MailMessage mail = new MailMessage
            {
                Subject = "Your Photo Booth Photos - Chris & Kristina's Wedding 2020"
            };

            mail.Attachments.Add(new Attachment(Path.Combine(PHOTO_DIRECTORY, SessionKey, "photoStrip.png")));
            mail.Attachments.Add(new Attachment(Path.Combine(PHOTO_DIRECTORY, SessionKey, "1.png")));
            mail.Attachments.Add(new Attachment(Path.Combine(PHOTO_DIRECTORY, SessionKey, "2.png")));
            mail.Attachments.Add(new Attachment(Path.Combine(PHOTO_DIRECTORY, SessionKey, "3.png")));
            

            mail.From = new MailAddress("chrisandkristinawedding2020@gmail.com");
            mail.To.Add(new MailAddress(emailAddress));

            smtpClient.Send(mail);
        }

        private void PrintPhotos()
        {
            try
            {
                PrintDocument pd = new PrintDocument();
                pd.PrinterSettings.PrinterName = @"Canon SELPHY CP1300 WS";
                //pd.PrinterSettings.PrinterName = @"HP807FE1 (HP OfficeJet Pro 6960)";
                //pd.PrinterSettings.PrinterName = @"HP3B556A (HP Officejet 6600)";
                pd.DefaultPageSettings.Color = true;
                pd.OriginAtMargins = false;

                pd.PrintPage += new PrintPageEventHandler
                    (this.printImage);
                pd.Print();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void printImage(object sender, PrintPageEventArgs ev)
        {
            Image i = Image.FromFile(Path.Combine(PHOTO_DIRECTORY, SessionKey, "photoStrip.png"));

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
    }
}