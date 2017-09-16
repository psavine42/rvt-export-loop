using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using System.Net.Http;
using System.Net;
using System.Windows.Forms;
using System.Collections.Specialized;
using System.IO;
using Newtonsoft.Json;
using Autodesk.Revit.UI;

namespace TaskClient
{
    public static class rvClient
    {
       // private static readonly HttpClient client = new HttpClient();
        private static string url = "http://localhost:3000/";

        public static string getNextTask()
        {
            string result = "";
            using (var wb = new WebClient())
            {
                result = wb.DownloadString(url + "new");
            }
            return result;
        }

        public static string submitComplete(string endpoint, string job, string hashed)
        {
            string result = "";
            using (var wb = new WebClient())
            {
                wb.QueryString.Add("job", job);
                wb.QueryString.Add("done", hashed);
                wb.QueryString.Add("real", "true");
                result = wb.DownloadString(url + endpoint);
            }
            return result;
        }

        public static void submitsheet(
            string job, List<Dictionary<string, string>> data)
        {
            string datastr = JsonConvert.SerializeObject(data);

            datastr = datastr.Replace("&", "");


           // MessageBox.Show(datastr);
            string result = "";
            using (var wb = new WebClient())
            {
                wb.QueryString.Add("job", job);
                wb.QueryString.Add("data", datastr);
                result = wb.DownloadString(url + "SheetInfo");
            }
            // return result;
        }


    }
}
