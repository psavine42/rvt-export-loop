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
    public static class Applicator
    {
        public static void ExportViewsProcessLoopNew(
            UIApplication uiapp, string source_path,
            List<List<string>> category_colors, string target_folder)
        {
            
            Application app = uiapp.Application;
            //app.FailuresProcessing += FaliureProcessor;

            //copy from server
            string moved_path = FileManager.MoveAndRecord(source_path);
            moved_path = ReviFileManager.saveNewCentral(app, source_path, moved_path);

            //open and activate
            //UIDocument uidoc = uiapp.OpenAndActivateDocument(moved_path);
            Document doc;

            //todo add dialog hooks here.
            //do exports
            //SheetsToJpeg(doc, target_folder, "---base");
            //ExportSheetsProcessLoop(doc, moved_path, category_colors, target_folder);
            //SheetsToJpeg(doc, target_folder, "---mask");

            //
            //UIDocument uidoc2 = uiapp.OpenAndActivateDocument(init_path);

            //doc.Close(false);
        }

        public static View viewCatOverride(
           Document doc, View view, List<OverrideCat> overides)
        {
            try
            {
                using (Transaction t = new Transaction(doc, "Set Overrides"))
                {
                    t.Start();
                    foreach (var ovr in overides)
                    {
                        try
                        {
                            var sccat = doc.Settings.Categories.get_Item(ovr.builtincategory);
                            if (sccat != null)
                            {
                                if (ovr.visible == false)
                                {
                                    view.SetCategoryHidden(sccat.Id, true);
                                }
                                else
                                {
                                    
                                    view.SetCategoryOverrides(sccat.Id, ovr.overrides);
                                }
                            }
                        }
                        catch (Exception) { }
                    }
                    doc.Regenerate();
                    t.Commit();

                }
            }
            catch (Exception) { }
            
            return view;
        }

        public static void ExportViewIfNoExists(Document doc, View view, string target_folder, string ext)
        {
            var base_export_loc = target_folder + Helper.AndHash(view.Name) + ext + view.Name;
            if (!File.Exists(base_export_loc + ".png"))
            {
                ExportView(doc, view, base_export_loc, ext);
            }
        }

        public static void ExportViewIrregardless(Document doc, View view, string target_folder, string ext)
        {
            var base_export_loc = target_folder + Helper.AndHash(view.Name) + ext + view.Name;
            ExportView(doc, view, base_export_loc, ext);
            
        }


        public static StringBuilder ExportSheetStats(Document doc, ViewSheet vs, StringBuilder sb)
        {
            var dictlist = new List<Dictionary<string, string>>();
            //List<ElementId> viewsInSHeet = vs.GetAllPlacedViews().ToList();
            List<ElementId> viewports = vs.GetAllViewports().ToList();
            string sheethash = Helper.AndHash(vs.Name);
            string num = vs.SheetNumber;
            
            var a = new FilteredElementCollector(doc, vs.Id)
                .OfCategory(BuiltInCategory.OST_TitleBlocks)
                .OfClass(typeof(FamilyInstance));

            string w = String.Empty;
            string h = String.Empty;
            if (a.Count() > 0)
            {
                FamilyInstance titleblock = a.First() as FamilyInstance;
                w = prmhelper.GetParameterValueString(titleblock, BuiltInParameter.SHEET_WIDTH);
                h = prmhelper.GetParameterValueString(titleblock, BuiltInParameter.SHEET_HEIGHT);
            }

            foreach (ElementId eil in viewports)
            {
                Viewport vport = doc.GetElement(eil) as Viewport;
                View view = doc.GetElement(vport.ViewId) as View;
                string viewhash = Helper.AndHash(view.Name);

                string vt = "";
                string vscale = "";
                try
                {
                    vt = view.ViewType.ToString();
                    vscale = view.Scale.ToString();
                }
                catch(Exception e) { }

                string v1 = "";
                string v2 = "";
                string v3 = "";
                string v4 = "";
                try
                {
                    var bbx = vport.GetBoxOutline();
                    v1 = bbx.MinimumPoint.X.ToString();
                    v2 = bbx.MaximumPoint.X.ToString();
                    v3 = bbx.MinimumPoint.Y.ToString();
                    v4 = bbx.MaximumPoint.Y.ToString();
                }
                catch (Exception) { }

                string l1 = "";
                string l2 = "";
                string l3 = "";
                string l4 = "";
                try
                {
                    var bbl = vport.GetLabelOutline();
                    l1 = bbl.MinimumPoint.X.ToString();
                    l2 = bbl.MaximumPoint.X.ToString();
                    l3 = bbl.MinimumPoint.Y.ToString();
                    l4 = bbl.MaximumPoint.Y.ToString();
                }
                catch (Exception) { }

                string newLine =
                    String.Format("{0},{1},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17}",
                    doc.Title, sheethash, viewhash, vs.Name, num, w, h, view.ViewName,
                    v1, v2, v3, v4, l1, l2, l3, l4, vt, vscale);
                //dictlist.Add(ds);
                sb.AppendLine(newLine);
            }
            return sb;
        }

        public static void ExportView(Document doc, View view,
            string view_export_loc, string ext)
        {
            
            try
            {
                //var sb = new StringBuilder();
                //sb.AppendLine();
                //sb.AppendLine(view_export_loc +
                //    " cbMIN " + view.CropBox.Min.ToString() +
                //" cbMax " + view.CropBox.Min.ToString() +
                //" bbxMIN " + view.get_BoundingBox(view).Min.ToString() +
                //    " bbxMAX " + view.get_BoundingBox(view).Max.ToString());

                //File.AppendAllText("C:\\data\\misc.txt", sb.ToString() + "\n");

                IList<ElementId> ImageExportList = new List<ElementId>();
                ImageExportList.Clear();
                ImageExportList.Add(view.Id);
                var BilledeExportOptions_3D_PNG = new ImageExportOptions
                {
                    ZoomType = ZoomFitType.FitToPage,
                    PixelSize = 4098,
                    FilePath = view_export_loc, 
                    FitDirection = FitDirectionType.Horizontal,
                    HLRandWFViewsFileType = ImageFileType.PNG,
                    ImageResolution = ImageResolution.DPI_600,
                    ExportRange = ExportRange.SetOfViews,
                };

                BilledeExportOptions_3D_PNG.SetViewsAndSheets(ImageExportList);
                doc.ExportImage(BilledeExportOptions_3D_PNG);
            }
            catch (Exception)  {  }
        }

        public static void AddOverride(Document doc, View view,
            ElementId pattern, List<List<string>> category_cols)
        {
            using (Transaction tx = new Transaction(doc))
            {
                tx.Start("SetViewtx");
                foreach (List<string> category_ovr in category_cols)
                {
                    try
                    {
                        OverrideCat ov = new OverrideCat(doc, pattern, category_ovr);
                        FilteredElementCollector col
                          = new FilteredElementCollector(doc, view.Id)
                            .WhereElementIsNotElementType()
                            .OfCategory(ov.builtincategory);

                        if (col.Count() > 0)
                        {
                            foreach (Element e in col)
                            {
                                view.SetElementOverrides(e.Id, ov.overrides);

                            }
                        }
                    }
                    catch (Exception) { }
                }
                tx.Commit();
            }
        
        }
    }
}
