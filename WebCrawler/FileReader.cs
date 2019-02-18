using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebCrawler
{
    class FileReader
    {
        String path;
        String[] lines;
        public void OpenDialog()
        {
            OpenFileDialog myDialog = new OpenFileDialog();
            myDialog.Filter = "Файлы(*.TXT;*.DOC;*.DOCX)|*.TXT;*.DOC;*.DOCX";
            myDialog.CheckFileExists = true;
            myDialog.Multiselect = true;
           
            if (myDialog.ShowDialog() == true)
            {
                path = myDialog.FileName;
                readTxt();
                
            }
                       
        }

        public void readTxt(){
             lines = File.ReadAllLines(path);

            for (int i = 0; i < lines.Length; i++)
            {
                Console.WriteLine(lines[i]);
            }
        }
    }
}
