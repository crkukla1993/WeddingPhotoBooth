using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.IO;
using System.Drawing.Printing;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing;
using System.Drawing.Imaging;
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

        public IActionResult OnPostComplete([FromBody] int option)
        {
            if (CombineImages())
            {
                if (option == 2 || option == 3)
                {
                    PrintPhotos();
                }
                else if(option == 1)
                {
                    SendEmail();
                }
                return StatusCode(200);
            }
            else
            {
                return StatusCode(500);
            }
        }

        private bool CombineImages()
        {
            using (var frame = Image.FromFile(Path.Combine(PHOTO_DIRECTORY, "template.png")))
            using (var img1 = Image.FromFile(Path.Combine(PHOTO_DIRECTORY, SessionKey, "1.jpeg")))
            using (var img2 = Image.FromFile(Path.Combine(PHOTO_DIRECTORY, SessionKey, "2.jpeg")))
            using (var img3 = Image.FromFile(Path.Combine(PHOTO_DIRECTORY, SessionKey, "3.jpeg")))
            using (var bmp1 = new Bitmap(img1, new Size(img1.Width, img1.Height)))
            using (var bmp2 = new Bitmap(img2, new Size(img2.Width, img2.Height)))
            using (var bmp3 = new Bitmap(img3, new Size(img3.Width, img3.Height)))
            using (var bitmap = new Bitmap(1379, 2041))
            {
                using (var canvas = Graphics.FromImage(bitmap))
                {
                    canvas.SmoothingMode = SmoothingMode.HighQuality;
                    canvas.CompositingQuality = CompositingQuality.HighQuality;
                    canvas.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    canvas.DrawImage(frame, 0, 0, 1379, 2041);
                    canvas.DrawImageUnscaledAndClipped(bmp1, new Rectangle(30, 28, 640, 480));
                    canvas.DrawImageUnscaledAndClipped(bmp2, new Rectangle(30, 538, 640, 480));
                    canvas.DrawImageUnscaledAndClipped(bmp3, new Rectangle(30, 1048, 640, 480));
                    canvas.DrawImageUnscaledAndClipped(bmp1, new Rectangle(710, 28, 640, 480));
                    canvas.DrawImageUnscaledAndClipped(bmp2, new Rectangle(710, 538, 640, 480));
                    canvas.DrawImageUnscaledAndClipped(bmp3, new Rectangle(710, 1048, 640, 480));
                    canvas.Save();
                }
                try
                {
                    bitmap.RotateFlip(RotateFlipType.Rotate90FlipNone);
                    bitmap.Save(Path.Combine(PHOTO_DIRECTORY, SessionKey, "photoStrip.jpeg"), ImageFormat.Jpeg);
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    return false;
                }
            }
        }

        private void GenerateSessionKey()
        {
            SessionKey = Guid.NewGuid().ToString();
            DirectoryInfo di = new DirectoryInfo(Path.Combine(PHOTO_DIRECTORY, SessionKey));
            di.Create();
            CurrentPhotoCount = 0;
        }
        
        public IActionResult OnPostSubmitPhoto([FromBody]string imageData, string key)
        {
            try
            {
                string fileNameWitPath = $@"{Path.Combine(PHOTO_DIRECTORY, SessionKey, (CurrentPhotoCount+1).ToString())}.jpeg";
                using (FileStream fs = new FileStream(fileNameWitPath, FileMode.Create))
                {
                    using (BinaryWriter bw = new BinaryWriter(fs))
                    {
                        byte[] data = Convert.FromBase64String(imageData);
                        bw.Write(data);
                        bw.Close();
                    }
                }
                CurrentPhotoCount++;
                return new JsonResult(CurrentPhotoCount);
            }
            catch (Exception)
            {
                return StatusCode(500);
            }
        }

        private void SendEmail()
        {
            SmtpClient smtpClient = new SmtpClient("smtp.gmail.com", 587);

            smtpClient.Credentials = new System.Net.NetworkCredential("chrisandkristinawedding2020@gmail.com", "C#Coder21");
            smtpClient.EnableSsl = true;
            MailMessage mail = new MailMessage();
            mail.Subject = "Your Photo Booth Photos - Chris & Kristina's Wedding 2020";

            mail.Attachments.Add(new Attachment(Path.Combine(PHOTO_DIRECTORY, SessionKey, "photoStrip.jpeg")));
            mail.Attachments.Add(new Attachment(Path.Combine(PHOTO_DIRECTORY, SessionKey, "1.jpeg")));
            mail.Attachments.Add(new Attachment(Path.Combine(PHOTO_DIRECTORY, SessionKey, "2.jpeg")));
            mail.Attachments.Add(new Attachment(Path.Combine(PHOTO_DIRECTORY, SessionKey, "3.jpeg")));
            

            mail.From = new MailAddress("chrisandkristinawedding2020@gmail.com");
            mail.To.Add(new MailAddress("crkukla1993@gmail.com"));

            smtpClient.Send(mail);
        }

        private static bool CheckForInternetConnection()
        {
            try
            {
                using (var client = new WebClient())
                using (client.OpenRead("http://clients3.google.com/generate_204"))
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        private void PrintPhotos()
        {
            try
            {
                PrintDocument pd = new PrintDocument();
                pd.PrinterSettings.PrinterName = @"Canon SELPHY CP1300 WS";
                //pd.PrinterSettings.PrinterName = @"HP3B556A (HP Officejet 6600)";
                pd.DefaultPageSettings.Color = false;
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
            Image i = Image.FromFile(Path.Combine(PHOTO_DIRECTORY, SessionKey, "photoStrip.jpeg"));

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