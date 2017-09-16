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

using System.Text;
using Autodesk.Revit.UI.Events;
using Autodesk.Revit.DB.Events;

namespace TaskClient
{
    [Transaction(TransactionMode.Manual)]
    public class Test : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData,
            ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            List<string> paths = new List<string>(new string[] {
                      "C:\\data\\models\\-1801313011---bul-structural.rvt"  ,
                "C:\\Program Files\\Autodesk\\Revit 2017\\Samples\\rac_advanced_sample_project.rvt",
                "C:\\Program Files\\Autodesk\\Revit 2017\\Samples\\RST_basic_sample_project.rvt"
            });

            //UIDocument uidoc = uiapp.ActiveUIDocument;
            uiapp.DialogBoxShowing += new EventHandler<DialogBoxShowingEventArgs>(app_DialogBoxShowing);
            uiapp.Application.FailuresProcessing += new EventHandler<FailuresProcessingEventArgs>(Application_FailuresProcessing);

            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;


            //test2(doc);
            //doc.Close(false);
            return Result.Succeeded;
        }

        public void app_DialogBoxShowing(Object sender, DialogBoxShowingEventArgs args)
        {
            if (args is TaskDialogShowingEventArgs)
            {
                TaskDialogShowingEventArgs argsTask = args as TaskDialogShowingEventArgs;
                args.OverrideResult((int)TaskDialogResult.Close);
                
            }
            

        }

        private void Application_FailuresProcessing(object sender, FailuresProcessingEventArgs e)
        {
            FailuresAccessor failuresAccessor = e.GetFailuresAccessor();
            IEnumerable<FailureMessageAccessor> failureMessages =
              failuresAccessor.GetFailureMessages();

            if (failureMessages.Count() == 0)
            {
                // FailureProcessingResult.Continue is to let 
                // the failure cycle continue next step.
                e.SetProcessingResult(FailureProcessingResult.Continue);
                return;
            }
            else
            {
                foreach (FailureMessageAccessor failureMessage in failureMessages)
                {
                    if (failureMessage.GetSeverity() == FailureSeverity.Warning)
                    {
                        failuresAccessor.DeleteWarning(failureMessage);
                    }
                    else
                    {
                        failuresAccessor.ResolveFailure(failureMessage);
                    }
                }
            }
            //String transactionName = failuresAccessor.GetTransactionName();
            e.SetProcessingResult(FailureProcessingResult.Continue);
        }

        private void test2(Document doc)
        {

            List<ViewSheet> AllSheets = Helper.GetAllT<ViewSheet>(doc);
            var docpatterns = new DocPatterns(doc);
            // vs = AllSheets.First();
            foreach (ViewSheet vs in AllSheets)
            {
                // vs.SheetNumber;
                if (!vs.IsTemplate && vs.CanBePrinted && vs.ViewType == ViewType.DrawingSheet)
                {
                    // List<ElementId> viewsInSHeet = vs.GetAllPlacedViews().ToList();

                    //Base export
                    //Applicator.ExportViewIfNoExists(doc, vs, target_folder, "---base");
                   // var dict = Applicator.ExportSheetStats(doc, vs);
                    //rvClient.submitsheet("sheets", dict);

                }
            }
            TaskDialog.Show("o", "done");
        }

        private void test1(Document doc)
        {
            var category_colors = FileManager.ReadDefCsv(FileManager.data_location);

            DocPatterns patterns = new DocPatterns(doc);

            List<OverrideCat> rcs = new List<OverrideCat>();
            //foreach (var cs in category_colors)
            //{
            //    OverrideCat cst = new OverrideCat(patterns.Solidfill, cs);
            //    rcs.Add(cst);
            //}
            //FilteredElementCollector collec = new FilteredElementCollector(doc, doc.ActiveView.Id);

            //var collector 
            //    =  collec.WheToElements()
            //        .Select(x => x.Category)
            //        .Distinct(new CategoryComparer())
            //        .ToList();

            var iter = doc.Settings.Categories;
            //doc.Settings.Categories.AsQueryable();
           // iter.MoveNext();
            foreach (Category cat in iter)
            {
                try
                {
                   // iter.C
                    BuiltInCategory curcat = (BuiltInCategory)cat.Id.IntegerValue;

                    //doc.Settings.Categories
                    string ccat = curcat.ToString();
                    var found = category_colors.Where(x => x.First() == ccat);
                    OverrideCat cst;
                    if (found.Count() > 0)
                    {
                        cst = new OverrideCat(doc, patterns.Solidfill, found.First());
                    }
                    else
                    {
                        cst = new OverrideCat(patterns.Dotline, curcat);
                        cst.visible = false;
                    }
                    rcs.Add(cst);
                   // iter.MoveNext();
                }
                catch (Exception) { }
            }

            var viewsCol
              = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                 .OfCategory(BuiltInCategory.OST_Views)
                 .ToElements().Cast<View>().ToList();

            var watch = Stopwatch.StartNew();
 

            foreach (var vs in viewsCol)
            {
                string ext1 = "---base---";
                string ext2 = "---mask---";
                string view_export_loc1 = "C:\\data\\t1\\" + Helper.AndHash(vs.Name) + ext1 + vs.Name;
                string view_export_loc2 = "C:\\data\\t1\\" + Helper.AndHash(vs.Name) + ext2 + vs.Name;
                Applicator.ExportView(doc, vs, view_export_loc1, ext1);
                var rvs = Applicator.viewCatOverride(doc, vs, rcs);
                Applicator.ExportView(doc, rvs, view_export_loc2, ext2);
            }
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;

            //uiapp.OpenAndActivateDocument(paths[1]);
            //doc.Close(false);


            //var watch2 = Stopwatch.StartNew();
            //foreach (var vs in viewsCol)
            //{
            //    string ext1 = "---base---";
            //    string ext2 = "---mask---";
            //    string view_export_loc1 = "C:\\data\\t2\\" + Helper.AndHash(vs.Name) + ext1 + vs.Name;
            //    string view_export_loc2 = "C:\\data\\t2\\" + Helper.AndHash(vs.Name) + ext2 + vs.Name;
            //    Applicator.ExportView(doc, vs, view_export_loc1, ext1);
            //    Applicator.AddOverride(doc, vs, patterns.Solidfill, category_colors);
            //    Applicator.ExportView(doc, vs, view_export_loc2, ext2);
            //}
            //watch2.Stop();
            //var elapsedMs2 = watch2.ElapsedMilliseconds;

            TaskDialog.Show("opt", "base " + elapsedMs.ToString());
        }

        private static void run_local()
        {
            // var revit_path_list = FileManager.ReadDefTxt( FileManager.revit_paths).ToList();
            // //   .GetRange(20, 20);
            // // .Take(20);
            // int start_at = 40;
            // var category_colors = FileManager.ReadDefCsv(FileManager.data_location);
            // var csvd_models =  FileManager.ReadwholeCSV(FileManager.move_record);
            //// bool stop = false;
            // int step = 0;

            // foreach (string file_path in revit_path_list)
            // {
            //     step = step + 1;

            //     if (stop == true)
            //     {
            //         break;
            //     }
            //     if (step > start_at)
            //     {
            //         var found = csvd_models
            //         .Where(x => x.First() == file_path)
            //         .ToList();

            //         if (found.Count == 0)
            //         {

            //             //base target path + revit file hashname
            //             string target_folder = FileManager.mkdir(
            //                 FileManager.export_location
            //                 + Helper.AndHash(file_path)
            //                 + Path.GetFileNameWithoutExtension(file_path));

            //             try
            //             {
            //                 ExportViewsProcessLoopNew(uiapp,
            //                     file_path,
            //                     category_colors,
            //                     target_folder + "\\");
            //             }
            //             catch (Exception e)
            //             {
            //                 TaskDialog.Show("error", e.ToString());
            //             }
            //         }
            //     }
            // }
        }
    }
}
