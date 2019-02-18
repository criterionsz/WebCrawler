using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace WebCrawler
{
    class GZipWebClient : WebClient
    {
        protected override WebRequest GetWebRequest(Uri address)
        {
            HttpWebRequest request = (HttpWebRequest)base.GetWebRequest(address);
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            return request;
        }

        public static String HtmlRead(String urlAddress)
        {

            
            // string urlAddress = "http://google.com";

             HttpWebRequest request = (HttpWebRequest)WebRequest.Create(urlAddress);
             request.Proxy = null;
             HttpWebResponse response = (HttpWebResponse)request.GetResponse();

             if (response.StatusCode == HttpStatusCode.OK)
             {
                 Stream receiveStream = response.GetResponseStream();
                 StreamReader readStream = null;

                 if (response.CharacterSet == null)
                 {
                     readStream = new StreamReader(receiveStream);
                 }
                 else
                 {
                     readStream = new StreamReader(receiveStream, Encoding.GetEncoding(response.CharacterSet));
                 }

                 string data = readStream.ReadToEnd();

                 response.Close();
                 readStream.Close();

                 return data;
             }

             return null;
         
        }
    }
}
