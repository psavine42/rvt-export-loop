using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace TaskClient
{
    public static class FileManager
    {
        public static string init_path = "C:\\Program Files\\Autodesk\\Revit 2016\\Samples\\rac_basic_sample_project.rvt";
        public static string data_path = "C:\\Users\\psavine\\source\\revitdrag\\";
        public static string pdfout = "C:\\Users\\psavine\\pdfoutput\\";

        //public static string move_record = data_path + "file_moves.txt";
        public static string data_location = data_path + "revitcolordefs.csv";
        public static string shell_data = data_path + "revitcolordefs-shell.csv";
        //public static string logfile        = data_path + "logfile.txt";
        //public static string revit_paths = data_path + "rvtpaths.txt";

        public static string export_location = "C:\\data\\print\\";
        public static string model_path = "C:\\data\\models\\";

        public static string MoveAndRecord(string path)
        {
            StringBuilder sb = new StringBuilder();
            string file_name = Path.GetFileName(path);

            string new_path = FileManager.model_path + file_name;

            if (!File.Exists(new_path))
            {
                File.Copy(path, new_path);
            }
            return new_path;
        }

       


        public static void PostProcessPrint(string path)
        {
            string file_name = Path.GetFileName(path);
        }

        public static List<List<string>> ReadDefCsv(string path)
        {
            List<List<string>> listA = new List<List<string>>();

            using (var reader = new StreamReader(path))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var values = line.Split(',');
                    if (values.ElementAt(1) != "")
                    {
                        List<string> listB = new List<string>();
                        listB.Add(values[0]);
                        listB.Add(values[1]);
                        listB.Add(values[2]);
                        listB.Add(values[3]);
                        listA.Add(listB);
                    }
                }
            }
            return listA;
        }


        public static string mkdir(string s)
        {
            if (!Directory.Exists(s))
            {
                Directory.CreateDirectory(s);
            }
            return s;
        }

        public static List<List<string>> ReadwholeCSV(string path)
        {
            List<List<string>> listA = new List<List<string>>();

            using (var reader = new StreamReader(path))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var values = line.Split(',');

                    List<string> listB = new List<string>();
                    foreach (string val in values)
                    {
                        listB.Add(val);
                    }
                    listA.Add(listB);
                }
            }
            return listA;
        }

        public static List<string> ReadDefTxt(string path)
        {
            List<string> listA = new List<string>();
            using (var reader = new StreamReader(path))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    listA.Add(line);
                }
            }
            return listA;
        }


    }
}
