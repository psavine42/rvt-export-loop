using System;
using System.Collections.Generic;
using System.Diagnostics;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Linq;
using Autodesk.Revit.UI.Selection;
using System.IO;
using System.Windows;
//using System.Windows.Forms;
using System.Text;
using System.Runtime.InteropServices;

namespace TaskClient
{


    public static class Printmanager
    {
        //public static void printsheets(PrintManager view) { }
        //public static string print_path = "C:\\data\\print\\";
        [DllImport("user32")]
        private static extern int GetWindowLongA(IntPtr hWnd, int index);

        [DllImport("USER32.DLL")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        //[DllImport("USER32.DLL")]
        //private static extern bool EnumWindows(EnumWindowsProc enumFunc, int lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        private const int GWL_STYLE = -16;

        private const ulong WS_VISIBLE = 0x10000000L;
        private const ulong WS_BORDER = 0x00800000L;
        private const ulong TARGETWINDOW = WS_BORDER | WS_VISIBLE;

        public static void print(Document doc)
        {
            ViewSet myViewSet = new ViewSet();
            var collector =
                new FilteredElementCollector(doc)
                .OfClass(typeof(ViewSheet))
                .Cast<ViewSheet>().ToList();

            foreach (ViewSheet vs in collector)
            {
                if (!vs.IsTemplate && vs.CanBePrinted &&
                    vs.ViewType == ViewType.DrawingSheet)
                {
                    myViewSet.Insert(vs);
                    using (Transaction tx = new Transaction(doc))
                    {
                        tx.Start("Transaction Name");
                        vs.Print(true);
                        tx.Commit();
                    }
                }
            }

            var printsettings = doc.GetPrintSettingIds();

            string setName = Helper.RandomString(10);
            PrintManager pm = doc.PrintManager;

            using (Transaction tx = new Transaction(doc))
            {
                tx.Start("Transaction Name");


                pm.SelectNewPrintDriver("Adobe PDF");

                //PaperSizeSet ps = pm.PaperSizes;
                pm.PrintRange = PrintRange.Select;
                pm.CombinedFile = false;
                pm.PrintToFile = true;
                pm.PrintSetup.CurrentPrintSetting.PrintParameters.RasterQuality = RasterQualityType.Presentation;
                pm.PrintSetup.CurrentPrintSetting.PrintParameters.ColorDepth = ColorDepthType.Color;
                pm.PrintSetup.CurrentPrintSetting.PrintParameters.ViewLinksinBlue = false;


                ViewSheetSetting viewSheetSetting = pm.ViewSheetSetting;

                // try
                // {
                // Save the current view sheet set to another view/sheet set with the specified name.
                viewSheetSetting.SaveAs(setName);
                //}
                //catch (Exception) { }
                viewSheetSetting.CurrentViewSheetSet.Views = myViewSet;
                viewSheetSetting.Save();
                pm.Apply();



                pm.PrintToFileName = FileManager.pdfout + setName + Convert.ToString(setName.GetHashCode()) + ".pdf";
                pm.SubmitPrint();
                tx.Commit();
            }

            using (Transaction tx = new Transaction(doc))
            {
                tx.Start("print Name");
                pm.SubmitPrint();
                tx.Commit();
            }

        }
    }
}
