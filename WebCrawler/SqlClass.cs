using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebCrawler
{
    class SqlClass
    {
        NpgsqlConnection conn;

        public SqlClass()
        {
            // PostgeSQL-style connection string
            string connstring = String.Format("Server={0};Port={1};" +
                "User Id={2};Password={3};Database={4};",
                "localhost", "5432", "postgres",
                "ed371612", "postgres");
            // Making connection with Npgsql provider
            conn = new NpgsqlConnection(connstring);
        }

        public void addToSql(String url, String text)
        {
            DataSet ds = new DataSet();
            DataTable dt = new DataTable();

            try
            {
                conn.Open();

                using (var cmd = new NpgsqlCommand())
                {
                    cmd.Connection = conn;
                    DateTime time=DateTime.UtcNow;
                    DateTime newTime = time.AddDays(3);
                  //  cmd.CommandText = "DELETE FROM webCrawler";
                    //cmd.ExecuteNonQuery();

                    // Insert some data
                    cmd.CommandText = "INSERT INTO webCrawler (link, hash, text, olddate, newdate, average) VALUES (@link, @hash, @text, @olddate, @newdate, @average)";
                    try
                    {
                        cmd.Parameters.AddWithValue("@link", url);
                        cmd.Parameters.AddWithValue("@hash", HashMD5.MD5Hash(text));
                        cmd.Parameters.AddWithValue("@text", text);
                        cmd.Parameters.AddWithValue("@olddate", time);
                        cmd.Parameters.AddWithValue("@newdate", newTime);
                        cmd.Parameters.AddWithValue("@average", 72);
                        //MessageBox.Show(GetWebText("https://vk.com"));
                        cmd.ExecuteNonQuery();
                    }

                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);

                    }
                    // Retrieve all rows
                  /*  cmd.CommandText = "SELECT link FROM webCrawler";
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Console.WriteLine((reader.GetString(0)));
                        }
                    }*/
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
