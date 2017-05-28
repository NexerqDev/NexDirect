using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace NexDirectLib.Ext
{
    // https://stackoverflow.com/questions/1777221/using-cookiecontainer-with-webclient-class
    [System.ComponentModel.DesignerCategory("")] // stop VS from treating this as a designer class thing
    public class CookieAwareWebClient : WebClient
    {
        private CookieContainer Cookies;

        public CookieAwareWebClient(CookieContainer c) : base()
        {
            Cookies = c;
        }

        protected override WebRequest GetWebRequest(Uri address)
        {
            WebRequest request = base.GetWebRequest(address);
            HttpWebRequest webRequest = request as HttpWebRequest;

            if (webRequest != null)
                webRequest.CookieContainer = Cookies;

            return request;
        }
    }
}
