#region Namespaces
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
#endregion
 // copy "$(ProjectDir)*.addin" "$(AppData)\Autodesk\REVIT\Addins\2017"
//copy "$(ProjectDir)bin\debug\*.dll" "$(AppData)\Autodesk\REVIT\Addins\2017"
namespace TaskClient
{
    [Transaction(TransactionMode.Manual)]
    public class Command : IExternalCommand
    {
        #region General Warning Swallower
        FailureProcessingResult PreprocessFailures(
          FailuresAccessor a)
        {
            IList<FailureMessageAccessor> failures
            = a.GetFailureMessages();

            foreach (FailureMessageAccessor f in failures)
            {
                FailureSeverity fseverity = a.GetSeverity();

                if (fseverity == FailureSeverity.Warning)
                {
                    a.DeleteWarning(f);
                }
                else
                {
                    a.ResolveFailure(f);
                    return FailureProcessingResult.ProceedWithCommit;
                }
            }
            return FailureProcessingResult.Continue;
        }
        // // General Warning Swallower

     

        /* */
        public void Project_DialogBoxShowing(Object sender, DialogBoxShowingEventArgs args)
        {
            TaskDialogShowingEventArgs args2 = args as TaskDialogShowingEventArgs;
            if (null != args2)
            {
                string s = args2.DialogId;

                TaskDialog.Show("dl", "Dialog ID is:  " + s + "---");
                if (args2.DialogId == "TaskDialog_Audit_Warning")
                {
                    args.OverrideResult((int)TaskDialogResult.Yes);
                }
                if (args2.DialogId == "TaskDialog_Unresolved_References")
                {
                    // Buttons with custom text have custom IDs with incremental values
                    // starting at 1001 for the left-most or top-most button in a task dialog. 
                    // "Open Manage Links to correct the problem", "1001"
                    //args2.OverrideResult(1001);
                    // "Ignore and continue opening the project", "1002"
                    args2.OverrideResult(1002);
                }
            }
        }


        public void app_DialogBoxShowing(Object sender, DialogBoxShowingEventArgs args)
        {
            if (args is TaskDialogShowingEventArgs)
            {
                TaskDialogShowingEventArgs argsTask = args as TaskDialogShowingEventArgs;
                args.OverrideResult((int)TaskDialogResult.Close);
               // TaskDialogResult.
            }
        }

        #region bullshit
        private static string init_path = "C:\\Program Files\\Autodesk\\Revit 2016\\Samples\\rac_basic_sample_project.rvt";
        private static Random random = new Random();

        public Process process = Process.GetCurrentProcess();
        #endregion


        private void dismissFloorQuestion(object o, DialogBoxShowingEventArgs e)
        {
            // DialogBoxShowingEventArgs has two subclasses - TaskDialogShowingEventArgs & MessageBoxShowingEventArgs
            // In this case we are interested in this event if it is TaskDialog being shown. 
            //TaskDialog.Show("did", e.DialogId.ToString());

            TaskDialogShowingEventArgs t = e as TaskDialogShowingEventArgs;
            //if (t != null && t.Message == "The floor/roof overlaps the highlighted wall(s). Would you like to join geometry and cut the overlapping volume out of the wall(s)?")
            //{
            // Call OverrideResult to cause the dialog to be dismissed with the specified return value
            // (int) is used to convert the enum TaskDialogResult.No to its integer value which is the data type required by OverrideResult
            // e.OverrideResult((int)TaskDialogResult.No);
            //}
        }
        #endregion

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            List<string> paths = new List<string>(new string[] {
                "C:\\Program Files\\Autodesk\\Revit 2016\\Samples\\rac_advanced_sample_project.rvt",
                "C:\\Program Files\\Autodesk\\Revit 2016\\Samples\\RST_basic_sample_project.rvt"
            });

            UIApplication uiapp = commandData.Application;

            uiapp.DialogBoxShowing += new EventHandler<DialogBoxShowingEventArgs>(app_DialogBoxShowing);
            uiapp.Application.FailuresProcessing += new EventHandler<FailuresProcessingEventArgs>(Application_FailuresProcessing);
            uiapp.Application.FailuresProcessing += new EventHandler<FailuresProcessingEventArgs>(FaliureProcessor2);

            bool stop = false;
            int step = 0;

            while (stop == false)
            {
                step = step + 1;
                string source_path = rvClient.getNextTask();
                string res = "Ok";
                if (source_path == "DONE")
                {
                    stop = true;
                    break;
                }

                string final_hashed_base_name = "Fail";
  
                try
               {
                final_hashed_base_name = ExportViewsProcessLoopNew(uiapp, source_path);
                }
                catch (Exception e)
                {
                    res = e.ToString();
                }
                if (res == "Ok")
                {
                   string fnl = final_hashed_base_name + Path.GetFileName(source_path);
                   string dn = rvClient.submitComplete("done", source_path, fnl);
                }
                else
                {
                   string dn = rvClient.submitComplete("error", source_path, res);
                }
            }
            return Result.Succeeded;
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

        private static void FaliureProcessor(object sender, FailuresProcessingEventArgs e)
        {
            bool hasFailure = false;
            FailuresAccessor fas = e.GetFailuresAccessor();
            List<FailureMessageAccessor> fma = fas.GetFailureMessages().ToList();
            List<ElementId> ElemntsToDelete = new List<ElementId>();
            foreach (FailureMessageAccessor fa in fma)
            {
                try
                {

                    //use the following lines to delete the warning elements
                    List<ElementId> FailingElementIds = fa.GetFailingElementIds().ToList();
                    ElementId FailingElementId = FailingElementIds[0];
                    if (!ElemntsToDelete.Contains(FailingElementId))
                    {
                        ElemntsToDelete.Add(FailingElementId);
                    }

                    hasFailure = true;
                    fas.DeleteWarning(fa);

                }
                catch (Exception ex)
                {
                }
            }
            if (ElemntsToDelete.Count > 0)
            {
                fas.DeleteElements(ElemntsToDelete);
            }
            //use the following line to disable the message supressor after the external command ends
            //CachedUiApp.Application.FailuresProcessing -= FaliureProcessor;
            if (hasFailure)
            {
                e.SetProcessingResult(FailureProcessingResult.ProceedWithCommit);
            }
            e.SetProcessingResult(FailureProcessingResult.Continue);
        }

        private static void FaliureProcessor2(object sender, FailuresProcessingEventArgs e)
        {

            FailuresAccessor fa = e.GetFailuresAccessor();

            // Inside event handler, get all warnings

            IList<FailureMessageAccessor> a
              = fa.GetFailureMessages();

            int count = 0;

            foreach (FailureMessageAccessor failure in a)
            {
                TaskDialog.Show("Failure",
                  failure.GetDescriptionText());

                fa.ResolveFailure(failure);

                ++count;
            }

            if (0 < count
              && e.GetProcessingResult()
                == FailureProcessingResult.Continue)
            {
                e.SetProcessingResult(
                  FailureProcessingResult.ProceedWithCommit);
            }
        }

        

        private static string ExportViewsProcessLoopNew(UIApplication uiapp, string source_path)
        {
            Application app = uiapp.Application;
            //  Tuple<string, bool> export_task = 
            //       ReviFileManager.base_name_for_Export(uiapp, source_path);
            //   string final_hashed_base_name = export_task.Item1;
            string final_hashed_base_name = source_path;

            string target_folder = FileManager.export_location + final_hashed_base_name + "\\";
            string target_rvt = FileManager.model_path + final_hashed_base_name + ".rvt";
            StringBuilder sb = new StringBuilder();
            //TaskDialog.Show("r", target_folder + " " + target_rvt);

           // if (export_task.Item2 == true)
            //{
                UIDocument uidoc = uiapp.OpenAndActivateDocument(target_rvt);
                Document doc = uidoc.Document;
                doc.Save();
            
                //Actions - Export Bases, Export Mask, Export Shell, CSV DEfs. 
                if (!File.Exists(target_folder + "sheet_data.csv"))
                {
                    SheetsToCsv(doc, target_folder);
                }

                //if (!File.Exists(target_folder + "sheet_data.csv"))
                //{
                //   var category_colors = FileManager.ReadDefCsv(FileManager.export_location);
                // sb.AppendLine("sheet csv");
                //   JpegsFromSheets(doc, target_folder, category_colors);
                //}

                if (!File.Exists(target_folder + "shell"))
                {
                    var shell_ovrs = FileManager.ReadDefCsv(FileManager.shell_data);
                    sb.AppendLine("creating shell exports");
                    string target_folder2 = FileManager.mkdir(target_folder + "shell\\");
                    JpegsFromSheetsRs(doc, target_folder2, shell_ovrs);
                }

                File.AppendAllText(target_folder + "log.txt", sb.ToString());
                UIDocument uidoc2 = uiapp.OpenAndActivateDocument(init_path);
                doc.Close(false);
           // }
            return final_hashed_base_name;
        }

       

        private static void ViewToDxf(UIApplication uiapp, string source_path, 
            string target_folder)
        {
            Application app = uiapp.Application;
            //copy from server
            string moved_path = FileManager.MoveAndRecord(source_path);
            moved_path = ReviFileManager.saveNewCentral(app, source_path, moved_path);

            //open and activate
            UIDocument uidoc = uiapp.OpenAndActivateDocument(moved_path);

            Document doc = uidoc.Document;
            //uiapp.DialogBoxShowing += new EventHandler<DialogBoxShowingEventArgs>(OnDialogBoxShowing);

            DXFExportOptions opt = new DXFExportOptions();
            var dwgSettingsFilter = new ElementClassFilter(typeof(ExportDWGSettings));

            var fc = new FilteredElementCollector(doc)
                            .WherePasses(dwgSettingsFilter);

            foreach (ExportDWGSettings element in fc)
            {
                var options = element.GetDWGExportOptions();
                var layerTable = options.GetExportLayerTable();
                foreach (var layerItem in layerTable)
                {
                    var layerInfo = layerItem.Value;
                    //layerItem.
                    if (layerInfo.CategoryType == LayerCategoryType.Model)
                    {
                        layerInfo.ClearCutLayerModifiers();
                        // layerInfo.LayerName = 
   
                        var modifiers = layerInfo.GetLayerModifiers();
                        foreach (var modifier in modifiers)
                        {
                            // get modifier type
                            var modifierType = modifier.ModifierType;
                            // get separator
                            var separater = modifier.Separator;
                            
                        }
                    }
                }
            }
            //string filename;

            using (Transaction tx = new Transaction(doc))
            {
                tx.Start("Transaction Name");

               // foreach (ElementId id in ids)
               // {
                  //  Element e = doc.GetElement(id);

                    // . . .

                    // view.IsolateElementTemporary(partId);

                    // doc.Export(target_folder, filename, viewIds, opt);
               // }

                // We do not commit the transaction, because
                // we do not want any modifications saved.
                // The transaction is only created and started
                // because it is required by the
                // IsolateElementTemporary method.
                // Since the transaction is not committed, 
                // the changes are automatically discarded.

                tx.Commit();
            }
        

        }

        private static void SheetsToJpeg(Document doc, string target_folder, string ext)
        {
            ViewSet myViewSet = new ViewSet();
            var collector = Helper.GetAllSheets(doc);

            foreach (ViewSheet vs in collector)
            {
                var view_export_loc = target_folder + Helper.AndHash(vs.Name) + ext + vs.Name;
                if (!File.Exists(view_export_loc + ".png"))
                {
                    Applicator.ExportView(doc, vs, view_export_loc, ext);

                }
            }
        }

        private static void SheetsToCsv(Document doc, string target_folder)
        {
            List<ViewSheet> AllSheets = Helper.GetAllSheets(doc);
            StringBuilder sb = new StringBuilder();
            if (AllSheets.Count() > 0)
            {
                sb.AppendLine(
                    String.Format("{0},{1},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17}",
                    "file-name", "sheet-hash", "view-hash", "sheet-name", "sheet-number",
                    "sheet-width", "sheet-height", "view-name",
                    "view-min-x", "view-max-x", "view-min-y", "view-max-y",
                    "label-min-x", "label-max-x", "label-min-y", "label-max-y",
                    "view-type", "view-scale"));

                foreach (ViewSheet vs in AllSheets)
                {
                    try
                    {
                        sb = Applicator.ExportSheetStats(doc, vs, sb);
                    }
                    catch (Exception) { }
                }
                File.WriteAllText(target_folder + "sheet_data.csv", sb.ToString());
            }

        }


        private static void JpegsFromSheetsRs(Document doc,
            string target_folder, List<List<string>> shell_ovrs)
        {
            List<ViewSheet> AllSheets = Helper.GetAllSheets(doc);
            var docpatterns = new DocPatterns(doc);

            foreach (ViewSheet vs in AllSheets)
            {

                List<ElementId> viewsInSHeet = vs.GetAllPlacedViews().ToList();
                var base_export_loc = target_folder + Helper.SheetFileName(vs, "---shell");
                if (!File.Exists(base_export_loc + ".png"))
                {
                    //create overrides
                    foreach (ElementId viewid in viewsInSHeet)
                    {
                        View view = doc.GetElement(viewid) as View;
                        OverrideView(doc, view, docpatterns, shell_ovrs);
                    }
                    //Second Export
                    Applicator.ExportViewIfNoExists(doc, vs, target_folder, "---shell");
                }
            }
        }


        private static void JpegsFromSheets(Document doc, 
            string target_folder, List<List<string>> category_cols)
        {
            List<ViewSheet> AllSheets = Helper.GetAllSheets(doc);
            var docpatterns = new DocPatterns(doc);

            foreach (ViewSheet vs in AllSheets)
            {
                List<ElementId> viewsInSHeet = vs.GetAllPlacedViews().ToList();
                //Base export
                Applicator.ExportViewIfNoExists(doc, vs, target_folder, "---base");

                var base_export_loc = target_folder + Helper.SheetFileName(vs, "---mask");
                if (!File.Exists(base_export_loc + ".png"))
                {
                    //create overrides
                    foreach (ElementId viewid in viewsInSHeet)
                    {
                        View view = doc.GetElement(viewid) as View;
                        OverrideView(doc, view, docpatterns, category_cols);
                    }
                    //Second Export
                    Applicator.ExportViewIfNoExists(doc, vs, target_folder, "---mask");
                }

            }
        }



        private static void OverrideView(Document doc, View view , DocPatterns docpatterns, 
            List<List<string>> category_cols, bool elemovr = true)
        {
            ViewType[] types = { ViewType.AreaPlan, ViewType.CeilingPlan, ViewType.Detail,
            ViewType.Elevation, ViewType.EngineeringPlan, ViewType.FloorPlan, ViewType.Section, ViewType.ThreeD};
            if (types.Contains(view.ViewType))
            {
                makeAnnoOverrides(doc, view, category_cols);
                if (elemovr == true)
                {
                    viewOptions(doc, view);
                    Applicator.AddOverride(doc, view, docpatterns.Solidfill, category_cols);
                }
            }     
        }


        private static void ExportSheetsProcessLoop(Document doc, string source_path,
            List<List<string>> category_cols, string target_folder)
        {
            string proj_Name = doc.ProjectInformation.Name;
            StringBuilder sb = new StringBuilder();

            FilteredElementCollector viewsCol
              = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                 .OfCategory(BuiltInCategory.OST_Views);

            var docpatterns = new DocPatterns(doc);

            ViewType[] types = { ViewType.AreaPlan, ViewType.CeilingPlan, ViewType.Detail,
            ViewType.Elevation, ViewType.EngineeringPlan, ViewType.FloorPlan, ViewType.Section, ViewType.ThreeD};

            foreach (View view in viewsCol)
            {
                if (types.Contains(view.ViewType))
                {
                    makeAnnoOverrides(doc, view, category_cols);
                    viewOptions(doc, view);
                    Applicator.AddOverride(doc, view, docpatterns.Solidfill, category_cols);
                }
            }
        }


        private static void ExportViewsProcessLoop(Document doc, string source_path,
           List<List<string>> category_cols, string target_folder)
        {
            string proj_Name = doc.ProjectInformation.Name;
            StringBuilder sb = new StringBuilder();

            FilteredElementCollector viewsCol
              = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                 .OfCategory(BuiltInCategory.OST_Views);

            ElementId pattern
             = new FilteredElementCollector(doc)
               .OfClass(typeof(FillPatternElement)).FirstElementId();

            sb.AppendLine();
            sb.Append("PROJECT--:" + proj_Name);
            ViewType[] types = { ViewType.AreaPlan, ViewType.CeilingPlan, ViewType.Detail,
            ViewType.Elevation, ViewType.EngineeringPlan, ViewType.FloorPlan, ViewType.Section};

            foreach (View view in viewsCol)
            {
                if (types.Contains(view.ViewType) || view.ViewType == ViewType.ThreeD)
                {
                    makeAnnoOverrides(doc, view, category_cols);
                    viewOptions(doc, view);
                    Applicator.AddOverride(doc, view, pattern, category_cols);
                  
                }
            }
        }


        private static void viewOptions(Document doc, View view)
        {
            try
            {
                using (Transaction tx = new Transaction(doc))
                {
                    SunAndShadowSettings sunSettings = view.SunAndShadowSettings;
                    tx.Start("shade");
                    view.DisplayStyle = DisplayStyle.FlatColors;
                    tx.Commit();
                }
            }
            catch (Exception) { }

        }


        private static void AddOverride2(Document doc, View view, ElementId pattern, List<List<string>> category_colors)
        {
            try
            {
                FilteredElementCollector col
                      = new FilteredElementCollector(doc, view.Id)
                        .WhereElementIsNotElementType();

                StringBuilder sb = new StringBuilder();

                using (Transaction tx = new Transaction(doc))
                {
                    var tohide = new List<ElementId>();
                    tx.Start("SetViewtx");
                    try
                    {
                        foreach (Element elem in col)
                        {
                            try
                            {
                                // var catid = elem.Category.Id.IntegerValue;

                                var elemBcCat = (BuiltInCategory)elem.Category.Id.IntegerValue;
                                string bcCat = elemBcCat.ToString();

                                var category_ovrs = category_colors.Where(x => x.First() == elemBcCat.ToString()).ToList();

                                if (category_ovrs.Count > 0)
                                {
                                    var category_ovr = category_ovrs.First();
                                    var r = Convert.ToByte(category_ovr[1]);
                                    var g = Convert.ToByte(category_ovr[2]);
                                    var b = Convert.ToByte(category_ovr[3]);
                                    var color_c = new Color(r, g, b);

                                    var gSettings = new OverrideGraphicSettings();
                                    //gSettings.
                                    gSettings.SetCutFillColor(color_c);
                                    gSettings.SetCutLineColor(color_c);
                                    gSettings.SetCutFillPatternVisible(true);
                                    gSettings.SetCutFillPatternId(pattern);
                                    gSettings.SetProjectionFillColor(color_c);
                                    gSettings.SetProjectionLineColor(color_c);
                                    gSettings.SetProjectionFillPatternId(pattern);
                                    view.SetElementOverrides(elem.Id, gSettings);
                                }
                                else
                                {
                                    //!elem.Category.HasMaterialQuantities ||
                                    if (elemBcCat.ToString().Contains("Tag") || elemBcCat.ToString().Contains("Arrow")
                                        || elemBcCat.ToString().Contains("Text") || elemBcCat.ToString().Contains("Annotation")
                                        || elemBcCat.ToString().Contains("Line"))
                                    {
                                        tohide.Add(elem.Id);
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                sb.AppendLine();
                                sb.Append("Fail--OVERRIDES:" + view.ViewName + e.ToString());
                                //File.AppendAllText(FileManager.logfile, sb.ToString() + "\n");
                            }
                        }
                    }
                    catch (Exception)
                    {
                        // TaskDialog.Show("x", e.ToString());
                    }

                    SelectionFilterElement filterElement = SelectionFilterElement.Create(doc, "tags filter");
                    filterElement.SetElementIds(tohide);
                    // doc.Regenerate();
                    view.AddFilter(filterElement.Id);
                    view.SetFilterVisibility(filterElement.Id, false);
                    // view.

                    tx.Commit();
                }

            }
            catch (Exception) { }
        }


        private static void makeAnnoOverrides(
            Document doc, View view, List<List<string>> category_colors)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                var col = new FilteredElementCollector(doc, view.Id)
                               .WhereElementIsNotElementType();

                var colorcats = category_colors.Select(x => x.First()).ToList();

                using (Transaction tx = new Transaction(doc))
                {
                    var tohide = new List<ElementId>();
                    tx.Start("SetViewtxz");
   
                    foreach (Element elem in col)
                    {
                        try
                        {
                            if (elem.Category != null)
                            {
                                var elemBcCat =
                                    (BuiltInCategory)elem.Category.Id.IntegerValue;
                                string bcCat = elemBcCat.ToString();

                                if (!colorcats.Contains(bcCat))
                                {
                                    tohide.Add(elem.Id);
                                }
                            }
                        }
                        catch (Exception) { }
                    }
                    SelectionFilterElement filterElement =
                        SelectionFilterElement.Create(doc, Helper.RandomString(100));
                    filterElement.SetElementIds(tohide);
                    doc.Regenerate();
                    view.AddFilter(filterElement.Id);
                    view.SetFilterVisibility(filterElement.Id, false);
                    
                    tx.Commit();
                }
            }
            catch (Exception) { }
        }


   
    }
}
