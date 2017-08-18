using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NexDirectLib.Util
{
    public static class CookieStoreSerializer
    {
        public static async Task<string> SerializeCookies(CookieContainer cookies)
        {
            // make it serializable
            // [
            //     {
            //         name: "name",
            //         domain: "google.com",
            //         path: "/",
            //         value: "awidhaiowdhioawdhioawd"
            //     }
            // ]
            var cookieStore = new JArray();

            // holy this is pretty bad; have to use reflection
            // https://stackoverflow.com/questions/13675154/how-to-get-cookies-info-inside-of-a-cookiecontainer-all-of-them-not-for-a-spe/36665793#36665793
            Hashtable domainsTable = (Hashtable)cookies.GetType()
                .InvokeMember("m_domainTable",
                    BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Instance,
                    null,
                    cookies,
                    new object[] { }
                );

            // iterate domains
            foreach (var key in domainsTable.Keys)
            {
                string domain = key as string;
                if (domain == null)
                    continue;
                if (domain.StartsWith("."))
                    domain = domain.Substring(1);

                // the paths list https://stackoverflow.com/questions/15983166/how-can-i-get-all-cookies-of-a-cookiecontainer
                SortedList pathsList = (SortedList)domainsTable[key].GetType().InvokeMember("m_list",
                    BindingFlags.NonPublic |
                    BindingFlags.GetField |
                    BindingFlags.Instance,
                    null,
                    domainsTable[key],
                    new object[] { });

                // iterate paths of domain
                foreach (var listKey in pathsList.Keys)
                {
                    string path = listKey as string;
                    if (path == null)
                        continue;

                    // Look for http cookies.
                    _cookieInDomainToJArray(ref cookieStore, string.Format("http://{0}{1}", domain, path), cookies);

                    // Look for https cookies
                    _cookieInDomainToJArray(ref cookieStore, string.Format("https://{0}{1}", domain, path), cookies);
                }
            }

            return await Task.Factory.StartNew(() => JsonConvert.SerializeObject(cookieStore));
        }

        private static void _cookieInDomainToJArray(ref JArray store, string strDomain, CookieContainer cookies)
        {
            Uri domain;
            if (!Uri.TryCreate(strDomain, UriKind.RelativeOrAbsolute, out domain))
                return;

            if (cookies.GetCookies(domain).Count > 0)
            {
                foreach (Cookie cookie in cookies.GetCookies(domain))
                {
                    var jCookie = new JObject();
                    jCookie.Add("name", cookie.Name);
                    jCookie.Add("domain", cookie.Domain);
                    jCookie.Add("path", cookie.Path);
                    jCookie.Add("value", cookie.Value);

                    if (store.Contains(jCookie))
                        continue;

                    store.Add(jCookie);
                }
            }
        }

        public static async Task<CookieContainer> DeserializeCookies(string json)
        {
            var cookies = new CookieContainer();
            JArray cookieStore = await Task.Factory.StartNew(() => JsonConvert.DeserializeObject<JArray>(json));
            
            foreach (JObject jsonCookie in cookieStore)
            {
                cookies.Add(new Cookie(
                    jsonCookie["name"].ToString(),
                    jsonCookie["value"].ToString(),
                    jsonCookie["path"].ToString(),
                    jsonCookie["domain"].ToString()
                ));
            }

            return cookies;
        }
    }
}
