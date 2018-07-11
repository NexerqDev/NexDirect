using NexDirectLib;
using NexDirectLib.Structures;
using NexDirectLib.Providers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Text.RegularExpressions;
using System.Net.Http;
using System.Net;
using System.IO;

namespace NexDirect.Dialogs
{
    /// <summary>
    /// Interaction logic for Captcha.xaml
    /// </summary>
    public partial class Captcha : Window
    {
        private BeatmapSet set;

        public Captcha(BeatmapSet set)
        {
            InitializeComponent();
            this.set = set;
        }

        private string url => "https://bloodcat.com/osu/s/" + set.Id;

        private string sync;
        private string hash;

        private Regex syncRegex = new Regex("<input name=\"sync\" type=\"hidden\" value=\"(\\d+)\">");
        private Regex hashRegex = new Regex("<input name=\"hash\" type=\"hidden\" value=\"(.+?)\">");
        private Regex imgRegex = new Regex("<img src=\"data:image/jpeg;base64,(.+?)\" class=\"d-block mw-100\">");
        private async Task loadCaptcha()
        {
            using (var handler = new HttpClientHandler() { UseCookies = true, CookieContainer = Bloodcat.Cookies })
            using (var client = new HttpClient(handler))
            {
                var response = await client.GetAsync(url);
                if (((int)response.StatusCode) != 401) // wtf are we doing here then
                    Close();

                string data = await response.Content.ReadAsStringAsync();
                Match syncm = syncRegex.Match(data);
                if (!syncm.Success)
                    throw new Exception("couldnt get sync");
                sync = syncm.Groups[1].Value;

                Match hashm = hashRegex.Match(data);
                if (!hashm.Success)
                    throw new Exception("couldnt get hash");
                hash = hashm.Groups[1].Value;

                Match imgm = imgRegex.Match(data);
                if (!imgm.Success)
                    throw new Exception("couldnt get img");
                byte[] rawImg = Convert.FromBase64String(imgm.Groups[1].Value);
                using (var ms = new MemoryStream(rawImg))
                {
                    BitmapSource b = BitmapFrame.Create(ms, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                    captchaImage.Source = b;
                }

                captchaInput.IsEnabled = true;
                submitButton.IsEnabled = true;
                captchaInput.Focus();
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            loadCaptcha();
        }

        private async void submitButton_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrWhiteSpace(captchaInput.Text))
                return;

            submitButton.IsEnabled = false;
            var _formData = new Dictionary<string, string>();
            _formData.Add("response", captchaInput.Text);
            _formData.Add("sync", sync);
            _formData.Add("hash", hash);
            byte[] formData = await new FormUrlEncodedContent(_formData).ReadAsByteArrayAsync();

            // i think we have to do it this way to have more control to abort the request early
            //var response = await client.PostAsync(url, formData);
            var request = WebRequest.Create(url) as HttpWebRequest;
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = formData.Length;
            request.CookieContainer = Bloodcat.Cookies;
            using (var stream = request.GetRequestStream())
                stream.Write(formData, 0, formData.Length);

            var response = (HttpWebResponse)request.GetResponse();
            request.Abort(); // directly cut it off before we dl the entire map
            if (((int)response.StatusCode) == 500)
            {
                MessageBox.Show("Invalid captcha code... try again");
                await loadCaptcha();
                submitButton.IsEnabled = true;
                return;
            }

            Close();
        }
    }
}
