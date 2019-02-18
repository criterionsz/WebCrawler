using CsQuery;
using HtmlAgilityPack;
using RobotsTxt;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;


namespace WebCrawler
{
  public class UrlClass
    {

        String baseUrl;

        List<String> listOld;
        List<String> list; //ссылки
        List<String> brokenList;
        HtmlWeb hw;
        HtmlDocument doc;
        SqlClass sqlClass;
        bool isGzip=false;
        bool isRobotsTxt=true;
        public UrlClass()
        {

        }
        public UrlClass(String url)
        {
            listOld = new List<string>();
            listOld.Add(url);

            brokenList = new List<string>();
            list = new List<string>();

            sqlClass = new SqlClass();

            hw = new HtmlWeb();
                      
            
        }

        public void Gzip2(String url)
        {
            string html;
            var request = (HttpWebRequest)HttpWebRequest.Create(url);

            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            HttpWebResponse response = null;
            try
            {
                response = (HttpWebResponse)request.GetResponse();
            }

            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            String responseData;
            using (StreamReader responseReader = new StreamReader(response.GetResponseStream()))
            {
                responseData = responseReader.ReadToEnd();
            }


            response.Dispose();
            response.Close();
            
            //doc = new HtmlDocument();

            doc=hw.Load(responseData);

        }

        public static void CopyTo(Stream src, Stream dest)
        {
            byte[] bytes = new byte[4096];

            int cnt;

            while ((cnt = src.Read(bytes, 0, bytes.Length)) != 0)
            {
                dest.Write(bytes, 0, cnt);
            }
        }

        public static string Unzip(byte[] bytes)
        {
            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(msi, CompressionMode.Decompress))
                {
                    //gs.CopyTo(mso);
                    CopyTo(gs, mso);
                }

                return Encoding.UTF8.GetString(mso.ToArray());
            }
        }


        public void Gzip(String lne)
        {
            isGzip = true;
            using (WebClient wc = new WebClient())
            {
                byte[] respones = wc.DownloadData(lne);
                String str = Unzip(respones);
                //doc = hw.Load(str);
                doc = new HtmlDocument();
                doc.LoadHtml(str);
                
            }
        }

        public void start(int depth)
        {
            for (int j = 0; j < depth; j++)
            {
                for (int i = 0; i < listOld.Count; i++)
                {
                    try
                    {
                        try
                        {
                           doc = hw.Load(listOld[i]);//загрузка Html страницы                           
                        }

                        catch
                        {
                            Gzip(listOld[i]);
                            
                        }
                       
                        ParseLinks(listOld[i]);
                        String text = getTxtFromWebsite(doc);
                        sqlClass.addToSql(listOld[i], text);
                     
                        isGzip = false;
                    }

                    catch (Exception ex)
                    {
                        brokenList.Add(listOld[i]);
                        Console.WriteLine(ex.Message);
                    }
                }

                for (int i = 0; i < list.Count; i++)
                {
                    Console.WriteLine(i + " " + list[i]);
                    try
                    {
                        try
                        {
                            doc = hw.Load(list[i]);//загрузка html страницы
                            
                        }

                        catch
                        
                        {
                            Gzip(listOld[i]);
                           
                        }
                       
                        String text = getTxtFromWebsite(doc);
                        sqlClass.addToSql(list[i], text);
                        isGzip = false;
                    }

                    catch (Exception ex)
                    {
                        brokenList.Add(list[i]);
                        Console.WriteLine(ex.Message);
                    }
                }

                foreach (String s in brokenList)
                {
                    Console.WriteLine(s);
                }
                listOld.Clear();
                listOld = list;
                list.Clear();
            }
        }


      public string getTxtFromWebsite(HtmlDocument doc)
        {
            
           // string text = doc.DocumentNode.SelectSingleNode("//body") == null ? "" : doc.DocumentNode.SelectSingleNode("//body").InnerHtml;
            doc.DocumentNode.Descendants()
                .Where(n => n.Name == "script" || n.Name == "style")
                .ToList()
                .ForEach(n => n.Remove());
            string s = doc.DocumentNode.OuterHtml;
         
            var dom = CQ.Create(s);
            string text = dom["body"].Text();
           /* text += doc.DocumentNode.SelectSingleNode("//title") == null ? "" : doc.DocumentNode.SelectSingleNode("//title").InnerText;
            text += doc.DocumentNode.SelectSingleNode("//p") == null ? "" : doc.DocumentNode.SelectSingleNode("//p").InnerText;
            text += doc.DocumentNode.SelectSingleNode("//h1") == null ? "" : doc.DocumentNode.SelectSingleNode("//h1").InnerText;*/
            text = Regex.Replace(text, @"\s+", " ").Trim();
            return text;
        }
        //Со страницы, которую ввел пользователь собираем ссылки
        public void ParseLinks(String url)
        {           
            baseUrl = getBaseUrl(url);
            try
            {
                downloadRobotsTxt();
            }

            catch (Exception ex)
            {
                isRobotsTxt = false;
            }
            
            //сбор ссылок со страницы
            foreach (HtmlNode link in doc.DocumentNode.SelectNodes("//a[@href]"))
            {
                string hrefValue = link.GetAttributeValue("href", string.Empty);

                hrefValue = checkHref(hrefValue);
                //robotsTxt("https://www.youtube.com");
                if (hrefValue != null)
                {
                    list.Add(hrefValue);
                }
                
            }

             
        }



        String urlInRobotsTxt(String url)
        {
            url = normalizationLink(url);
            if (isRobotsTxt)
            {
                if (robotsTxt(url) && (url != null))
                {
                    return url;
                }
                else
                {
                    return null;
                }
            }

            else
            {
                if ((url != null))
                {
                    return url;
                }
                else
                {
                    return null;
                }
            }
        }


        //Проверка собранных ссылок 
        //Нормализует такие ссылки как:
        //   //www.youtube.com/yt/dev/ru/; /watch?v=OE1FbxeU9_M; удаляет лишнее
        public String checkHref(String urlHref)
        {
            if (urlHref.Length > 1)
            {

                if (urlHref[0] == '/' && urlHref[1] == '/')
                {
                    urlHref = urlHref.Remove(0, 2);

                    return urlInRobotsTxt(urlHref);

                }

                if (urlHref[0] == '/' && !(urlHref[1] == '/'))
                {
                    urlHref = baseUrl + urlHref;
                    return urlInRobotsTxt(urlHref);
                }

                if (urlHref.Contains("http"))
                {
                    return urlInRobotsTxt(urlHref);
                }
            }

            else
            {
                if (urlHref[0] == '/')
                {
                    return baseUrl;
                }
            }

            return null;


        }

        
        UriBuilder uriBuilder;// = new UriBuilder(baseUrl + "/robots.txt");
        string contentsOfRobotsTxtFile;// = webClient.DownloadString(uriBuilder.Uri);
        Robots robots;
        //скачиваем текст из robots.txt 
        public void downloadRobotsTxt()
        {
            if (isGzip)
            {

                using (var webClient = new GZipWebClient())
                {
                    uriBuilder = new UriBuilder(baseUrl + "/robots.txt");
                    contentsOfRobotsTxtFile = webClient.DownloadString(uriBuilder.Uri);
                    robots = new Robots(contentsOfRobotsTxtFile);
                }
            }

            else
            {
                using (var webClient = new WebClient())
                {
                    uriBuilder = new UriBuilder(baseUrl + "/robots.txt");
                    contentsOfRobotsTxtFile = webClient.DownloadString(uriBuilder.Uri);
                    robots = new Robots(contentsOfRobotsTxtFile);
                }
            }
        }

        //Можно ли индексировать сайт
        public bool robotsTxt(String url)
        {
            //String urlBase = UrlClass.getBaseUrl(url);                          

            var uri = new Uri(url);
            bool canIGoThere = robots.IsPathAllowed("*", uri.PathAndQuery);

            return canIGoThere;
        }


        //извлекаем базовую ссылку
        //Например: https://www.youtube.com/watch?v=ePpPVE-GGJw 
        //Базовая ссылка: https://www.youtube.com/
        public static String getBaseUrl(String url)
        {
            Uri uri = new Uri(url);
            String baseUri = uri.GetLeftPart(System.UriPartial.Authority);

            return baseUri;
        }

        //нормализация ссылки
        //Если пользователь ввел youtube.com, то преобразует к http://www.youtube.com
        public String normalizationLink(String url)
        {

            bool b = Uri.IsWellFormedUriString(url, UriKind.Absolute);

            if (b)
            {
                return url;
            }

            else
            {

                if (!(url.Contains("http://") || url.Contains("https://")))
                {
                    if (!url.Contains("www"))
                    {
                        return "http://www." + url;
                    }

                    return "http://" + url;
                }

                //int i=url.IndexOf('/')+1; http://www." + url.Remove(0, i+1)
                return url;
            }


        }

        //доступен ли сайт
        public bool IsUrlAvailable(String url)
        {
             HttpWebRequest req;
             HttpWebResponse response;
            try
            {
                 req = (HttpWebRequest)WebRequest.Create(url);
                req.Proxy = null;
                 response = (HttpWebResponse)req.GetResponse();

               /* String responseData;
                using (StreamReader responseReader = new StreamReader(response.GetResponseStream()))
                {
                    responseData = responseReader.ReadToEnd();
                }*/

             /*   HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(responseData);
                string text = doc.DocumentNode.SelectSingleNode("//body").InnerText;
                text += doc.DocumentNode.SelectSingleNode("//title").InnerText;
                text = Regex.Replace(text, @"\s+", " ").Trim();
                Console.WriteLine(text);*/

                response.Close();
                response.Dispose();
               /* WebClient client = new WebClient();
                string downloadString = client.DownloadString(url);
                Console.WriteLine(downloadString);*/

              
                return true;
            }

            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
              
                return false;
            }
        }

        //Проверка сайта на валидность
        public String checkUrl(String url)
        {
            String normUrl = normalizationLink(url);
            if (IsUrlAvailable(normUrl))
            {
                return normUrl;
            }

            return null;
        }

        public void checkUrlList(List<String> str)
        {

            for (int i = 0; i < str.Count; i++)
            {
                String normUrl = urlInRobotsTxt(str[i]);
                if (normUrl != null)
                {
                    listOld.Add(normUrl);
                }
            }
        }
    }
}