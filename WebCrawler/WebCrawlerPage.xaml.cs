using CsQuery;
using HtmlAgilityPack;
using Npgsql;
using RobotsTxt;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WpfAnimatedGif;

namespace WebCrawler
{
    /// <summary>
    /// Логика взаимодействия для WebCrawlerPage.xaml
    /// </summary>
    public partial class WebCrawlerPage : Page
    {
        public WebCrawlerPage()
        {
            InitializeComponent();
            // PostgeSQL-style connection string
            string connstring = String.Format("Server={0};Port={1};" +
                "User Id={2};Password={3};Database={4};",
                "localhost", "5432", "postgres",
                "ed371612", "postgres");
            // Making connection with Npgsql provider
            conn = new NpgsqlConnection(connstring);

           // img.Visibility = Visibility.Collapsed;
            Thread task = new Thread(startReind);
            task.Start();
           // startReind();

        }

        NpgsqlConnection conn;
        UrlClass urlClass;

        private static string GetWebText(string url)
        {
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
            request.UserAgent = "A .NET Web Crawler";
            WebResponse response = request.GetResponse();
            Stream stream = response.GetResponseStream();
            StreamReader reader = new StreamReader(stream);
            string htmlText = reader.ReadToEnd();
            return htmlText;
        }



        //WORK WITH ROBOTS TXT

        public UrlClass checkUrl(String url)
        {
            UrlClass urlClass = new UrlClass();
            url = urlClass.checkUrl(url);


            if (url == null)
            {
                MessageBox.Show("Нет доступа к сайту или не верно введена ссылка");
                return null;
            }

            var dom = CQ.CreateFromUrl(url);
            string text = dom["meta"].ToString();
            // MessageBox.Show(text.Contains("charset=\"UTF-8\"").ToString());
            if (!text.ToLower().Contains("charset=utf-8"))
            {
                if (!text.ToLower().Contains("charset=\"utf-8\""))
                {
                    MessageBox.Show("На данной веб-странице не используется UTF-8");
                    return null;
                }
            }

            return urlClass;
        }

        public void start(String url)
        {

            UrlClass urlClass = checkUrl(url);

            if (urlClass == null)
            {
                return;
            }

           
           
            UrlClass urlClass2 = new UrlClass(url);

            urlClass2.start(1);

        }

        public void startReind()
        {
           // Console.WriteLine("Начало работы метода Display");
            Reindexing r = new Reindexing();
            while (true)
            {
                Console.WriteLine("Начало работы метода startReind");
                r.startReindexing();
                Thread.Sleep(60*60*1000);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            img.Visibility = Visibility.Visible;

            start(firstNameTxtBox_Copy.Text);

            //Task tsk = new Task(startReind);
            //tsk.Start();

           // startReind();
            
           // tsk.Wait();

            //UrlClass nn = new UrlClass();
            // nn.IsUrlAvailable("https://www.youtube.com/watch?v=R2TEZ3S1I48");
            return;

            DataSet ds = new DataSet();
            DataTable dt = new DataTable();

            try
            {
                conn.Open();

                using (var cmd = new NpgsqlCommand())
                {
                    cmd.Connection = conn;

                    cmd.CommandText = "DELETE FROM webCrawler";
                    cmd.ExecuteNonQuery();

                    // Insert some data
                    cmd.CommandText = "INSERT INTO webCrawler (url) VALUES (@url)";
                    try
                    {
                        cmd.Parameters.AddWithValue("@url", "https://vk.com");
                        MessageBox.Show(GetWebText("https://vk.com"));
                        cmd.ExecuteNonQuery();
                    }

                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);

                    }
                    // Retrieve all rows
                    cmd.CommandText = "SELECT url FROM webCrawler";
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            MessageBox.Show((reader.GetString(0)));
                        }
                    }
                }


                // since we only showing the result we don't need connection anymore
                conn.Close();
            }
            catch (Exception msg)
            {
                // something went wrong, and you wanna know why
                MessageBox.Show(msg.ToString());
            }

        }

        private void firstNameTxtBox_Copy2_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void Button_Click2(object sender, RoutedEventArgs e)
        {
            string url = firstNameTxtBox_Copy2.Text;
            DataSet ds = new DataSet();
            DataTable dt = new DataTable();

            try
            {
                conn.Open();

                using (var cmd = new NpgsqlCommand())
                {
                    cmd.Connection = conn;
                    DateTime dateTime;
                    TimeSpan diff;

                    //    List<int> average = new List<int>();
                    Dictionary<string, string> dic = new Dictionary<string, string>();
                

                    cmd.CommandText = "SELECT newdate, link, hash, average FROM webCrawler where link=" + "\'" + url + "\'"; 


                    try
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string link = (string)reader["link"];
                                string hash = (string)reader["hash"];
                                dic.Add(link, hash);                                
                            }
                        }


                        foreach (KeyValuePair<string, string> kvp in dic)
                        {
                            HtmlDocument doc = new HtmlDocument();

                            try
                            {
                                try
                                {
                                    try
                                    {
                                        HtmlWeb hw = new HtmlWeb();

                                        doc = hw.Load(kvp.Key);//загрузка html страницы

                                    }

                                    catch
                                    {

                                        using (WebClient wc = new WebClient())
                                        {
                                            byte[] respones = wc.DownloadData(kvp.Key);
                                            String str = UrlClass.Unzip(respones);
                                            doc.LoadHtml(str);
                                        }

                                    }
                                }

                                catch (Exception ex)
                                {
                                    cmd.CommandText = "DELETE FROM webCrawler where link=" + "\'" + kvp.Key + "\'";
                                    cmd.ExecuteNonQuery();
                                }


                                UrlClass urlClass = new UrlClass();
                                String text = urlClass.getTxtFromWebsite(doc);
                                string hash = HashMD5.MD5Hash(text);
                                if (kvp.Value != hash)
                                {
                                    cmd.CommandText = "UPDATE webCrawler set hash=" + "\'" + hash + "\'" +", text=" +"\'"+text+ "\'"+" WHERE link=" + "\'" + kvp.Key + "\'";
                                    cmd.ExecuteNonQuery();
                                }

                              

                            }

                            catch (Exception ex)
                            {

                                Console.WriteLine(ex.Message);
                            }

                            //    Console.WriteLine(hours.ToString());

                        }
                    }



                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);

                    }

                }


                // since we only showing the result we don't need connection anymore
                conn.Close();
            }
            catch (Exception msg)
            {
                // something went wrong, and you wanna know why
                //  MessageBox.Show(msg.ToString());
            }



        }
    }
}