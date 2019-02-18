using HtmlAgilityPack;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;


namespace WebCrawler
{
    class Reindexing
    {
        NpgsqlConnection conn;
        public Reindexing()
        {
            // PostgeSQL-style connection string
            string connstring = String.Format("Server={0};Port={1};" +
                "User Id={2};Password={3};Database={4};",
                "localhost", "5432", "postgres",
                "ed371612", "postgres");
            // Making connection with Npgsql provider
            conn = new NpgsqlConnection(connstring);
        }

        public bool flag = false;
        public string urlRe;
        public void startReindexing()
        {
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
                    // Insert some data
                
                   
                   cmd.CommandText = "SELECT newdate, link, hash, average FROM webCrawler";
                   

                    try
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                
                                string s = (string)reader["newdate"];
                                int average = (int)reader["average"];
                                string link = (string)reader["link"];
                                string hash = (string)reader["hash"];
                   
                                dateTime = DateTime.Parse(s);
                                diff = dateTime - DateTime.UtcNow;
                                double hours = diff.TotalHours;

                                
                                    if (average < 720)
                                    {
                                        if ((int)hours == 0 || (hours + 0.5) < 5)
                                        {
                                            dic.Add(link, hash);
                                        }
                                    }
                                 
                              
                            }
                        }


                        foreach (KeyValuePair<string, string> kvp in dic)
                        {
                             HtmlDocument doc=new HtmlDocument();
                                
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
                                         string text = urlClass.getTxtFromWebsite(doc);
                                         string hash = HashMD5.MD5Hash(text);
                                         if (kvp.Value != hash)
                                         {
                                             cmd.CommandText = "SELECT average FROM webCrawler where link="+"\'"+kvp.Key+"\'";
                                             int avrg = (int)cmd.ExecuteScalar();
                                             if (avrg > 1)
                                             {
                                                 avrg = avrg / 2;
                                             }
                                             DateTime dateTimenew = DateTime.UtcNow;
                                             DateTime newTime = dateTimenew.AddHours(avrg);
                                             cmd.CommandText = "UPDATE webCrawler set hash="+"\'"+hash+"\'"+", text=" +"\'"+text+ "\'"+", newdate="+"\'"+newTime+"\'" +", average="+"\'"+avrg +"\'"+" WHERE link="+"\'"+kvp.Key+"\'";
                                             cmd.ExecuteNonQuery();
                                         }

                                         else
                                         {
                                             cmd.CommandText = "SELECT average FROM webCrawler where link=" + "\'" + kvp.Key + "\'";
                                             int avrg = (int)cmd.ExecuteScalar();
                                             avrg = avrg * 2;

                                             DateTime dateTimenew = DateTime.UtcNow;
                                             DateTime newTime = dateTimenew.AddHours(avrg);
                                             cmd.CommandText = "UPDATE webCrawler set newdate="+"\'"+newTime+"\'" +", average=" +"\'"+ avrg + "\'" + " WHERE link="+"\'"+kvp.Key+"\'";
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
