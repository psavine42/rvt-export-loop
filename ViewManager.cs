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


namespace TaskClient
{
    class ViewManager
    {

        public static string init_path = "C:\\Program Files\\Autodesk\\Revit 2016\\Samples\\rac_basic_sample_project.rvt";

        public static void ExportViewsProcessLoopNew(
           UIApplication uiapp, string source_path,
           List<List<string>> category_colors, string target_folder)
        {
            Application app = uiapp.Application;

            //copy from server
            string moved_path = FileManager.MoveAndRecord(source_path);
            moved_path = ReviFileManager.saveNewCentral(app, source_path, moved_path);

            //open and activate
            UIDocument uidoc = uiapp.OpenAndActivateDocument(moved_path);
            Document doc = uidoc.Document;

            JpegsFromSheets(doc, target_folder, category_colors);
            UIDocument uidoc2 = uiapp.OpenAndActivateDocument(init_path);
            doc.Close(false);
        }

        public static void JpegsFromSheets(Document doc, string target_folder,
           List<List<string>> category_cols)
            {
                List<Category> tohide = GetCategoriesToHide(doc, category_cols);
                List<ViewSheet> AllSheets = Helper.GetAllT<ViewSheet>(doc);
                DocPatterns docpatterns = new DocPatterns(doc);
                List<OverrideCat> tohideOverride = GetCategoryOverrides(doc, docpatterns, category_cols);

                foreach (ViewSheet vs in AllSheets)
                {
                    if (!vs.IsTemplate && vs.CanBePrinted &&
                        vs.ViewType == ViewType.DrawingSheet)
                    {
                      
                        Applicator.ExportViewIfNoExists(doc, vs, target_folder, "---base");
                        List<ElementId> viewsInSHeet = vs.GetAllPlacedViews().ToList();
                        var base_export_loc = target_folder + Helper.AndHash(vs.Name) + "---mask" + vs.Name;
                        if (!File.Exists(base_export_loc + ".png"))
                        {
                            foreach (ElementId viewid in viewsInSHeet)
                            {
                                View view = doc.GetElement(viewid) as View;
                                OverrideView(doc, view, tohideOverride, tohide);
                            }
                            Applicator.ExportViewIfNoExists(doc, vs, target_folder, "---mask");
                        }
                    }
                }
            }

        public static List<OverrideCat> GetCategoryOverrides(
            Document doc, DocPatterns patterns, List<List<string>> category_cols)
        {
            List<OverrideCat> ovrs = new List<OverrideCat>();
            foreach (List<string> category_ovr in category_cols)
            {
                OverrideCat ov = new OverrideCat(doc, patterns.Solidfill, category_ovr);
            }
            return ovrs;
        }

        public static void OverrideView(Document doc, View view,
            List<OverrideCat> toOverride, List<Category> tohide )
        {

            ViewType[] types = { ViewType.AreaPlan, ViewType.CeilingPlan, ViewType.Detail,
            ViewType.Elevation, ViewType.EngineeringPlan, ViewType.FloorPlan, ViewType.Section, ViewType.ThreeD};

            if (types.Contains(view.ViewType))
            {
                try
                {
                    using (Transaction tx = new Transaction(doc))
                    {
                        tx.Start("shade");
                        foreach (Category cat in tohide)
                        {
                            try
                            { view.SetCategoryHidden(cat.Id, true); }
                            catch (Exception) { }
                        }
                        doc.Regenerate();
                        tx.Commit();
                    }
                }
                catch (Exception) { }
                viewOptions(doc, view);
                try
                {
                    using (Transaction tx = new Transaction(doc))
                    {
                        tx.Start("SetViewtx");
                        foreach (OverrideCat ov in toOverride)
                        {
                            try
                            { view.SetCategoryOverrides(ov.categ.Id, ov.overrides);  }
                            catch (Exception) { }

                        }
                        doc.Regenerate();
                        tx.Commit();
                    }
                }
                catch (Exception) { }
            }
        }

        public static void viewOptions(Document doc, View view)
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

        public static new List<Category> GetCategoriesToHide(
          Document doc, List<List<string>> category_colors)
        {
            var tohide = new List<Category>();
            try
            {
                var categories = doc.Settings.Categories;
                var colorcats = category_colors.Select(x => x.First()).ToList();

                foreach (Category elem in categories)
                {
                    try
                    {
                        if (elem != null)
                        {
                            var elemBcCat = (BuiltInCategory)elem.Id.IntegerValue;
                            string bcCat = elemBcCat.ToString();
                            if (!colorcats.Contains(bcCat))
                            {
                                tohide.Add(elem);
                            }
                        }
                    }
                    catch (Exception) { }
                }
            }
            catch (Exception) { }
            return tohide;
        }
    

    }

    
}
