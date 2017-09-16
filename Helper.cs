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

namespace TaskClient
{
    public static class Helper
    {
        public static List<T> GetAllT<T>(Document doc)
        {
            return new FilteredElementCollector(doc)
                .OfClass(typeof(T))
                .Cast<T>().ToList();
        }

        public static List<ElementId> viewsNotOnSheets(Document doc)
        {
            List<ViewSheet> AllSheets = GetAllT<ViewSheet>(doc);
            List<ElementId> AllViews = GetAllT<View>(doc).Select(x => x.Id).ToList();

            List<ElementId> ViewsNotOnSheet = new List<ElementId>();

            foreach (ViewSheet sheet in AllSheets)
            {
                foreach (ElementId viewid in sheet.GetAllPlacedViews())
                {
                    if (!AllViews.Contains(viewid))
                    {
                        ViewsNotOnSheet.Add(viewid);
                    }
                }
            }
            return ViewsNotOnSheet;
        }

        public static string SheetFileName(ViewSheet vs, string ext)
        {
            //  '1749434936------baseDO NOT INCLUDE - BUILDING C DRAWING LIST - Sheet - G-C001 - DO NOT INCLUDE - BUILDING C DRAWING LIST'
            return Helper.AndHash(vs.Name) + ext + vs.Name + " - Sheet - " + vs.SheetNumber + " - " + vs.Name;
        }

        public static void SheetFromOrphanViews(Document doc)
        {
            var views = viewsNotOnSheets(doc);

            ElementId titleBlockId = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_TitleBlocks)
                .WhereElementIsElementType()
                .FirstElementId();

            using (Transaction t = new Transaction(doc, "Create Sheets"))
            {
                t.Start();
               
                foreach (ElementId eid in views)
                {
                    View view = doc.GetElement(eid) as View;
                    view.CropBoxActive = true;
                    view.CropBoxVisible = false;

                    ViewSheet sheet = ViewSheet.Create(doc, titleBlockId);
                    Viewport.Create(doc, sheet.Id, eid, XYZ.Zero);

                    doc.Regenerate();

                    ElementId vport = sheet.GetAllViewports().First();
                    Viewport port = doc.GetElement(vport) as Viewport;
                    var max = port.GetBoxOutline().MaximumPoint;
                    var min = port.GetBoxOutline().MinimumPoint;
                   
                }
                t.Commit();
            }
        }

        private static Random random = new Random();

        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public static BuiltInCategory GetCategories(string name)
        {
            var bics = Enum.GetValues(typeof(BuiltInCategory))
                .Cast<BuiltInCategory>()
                .ToList()
                .Where(x => x.ToString() == name)
                .First();
            return bics;
        }

        public static void cropExtentAnno(Document doc, View view)
        {
            try
            {
                var els
                  = new FilteredElementCollector(doc, view.Id)
                    .WhereElementIsNotElementType();

                var bbv = view.get_BoundingBox(view);
                var maxx = bbv.Max.X;
                var maxy = bbv.Max.Y;
                var maxz = bbv.Max.Z;

                var minx = bbv.Min.X;
                var miny = bbv.Min.Y;
                var minz = bbv.Min.Z;

                var tohide = new List<ElementId>();

                foreach (Element e in els)
                {
                    try
                    {
                        var elemBcCat = (BuiltInCategory)e.Category.Id.IntegerValue;
                        string bcCat = elemBcCat.ToString();
                        if (bcCat.Contains("Tag")
                            || bcCat.Contains("Arrow")
                            || bcCat.Contains("Callo")
                            || bcCat.Contains("OST_Grid")
                            || bcCat.Contains("Elevation")
                            || bcCat.Contains("Annotation")
                            || bcCat.Contains("View")
                            //|| bcCat.Contains("Line") 
                            || bcCat.Contains("Level")
                            || bcCat.Contains("Section")
                            || bcCat.Contains("Dimension")
                                        //|| bcCat.Contains("Text")
                                        )
                        {

                            var bbx = e.get_BoundingBox(view);
                            if (maxx < bbx.Max.X || maxy < bbx.Max.Y || maxz < bbx.Max.Z ||
                                minx > bbx.Min.X || miny > bbx.Min.Y || minz > bbx.Min.Z)
                            {
                                tohide.Add(e.Id);
                            }
                        }
                        //if (bcCat.Contains("OST_Grid"))
                        //{
                        //    var bbx = e.get_BoundingBox(view);
                        //    //Intersect
                        //    if(bbx.)
                        //}
                    }
                    catch (Exception) { }
                }
                using (Transaction tx = new Transaction(doc))
                {
                    tx.Start("shade");
                    SelectionFilterElement filterElement = SelectionFilterElement.Create(doc, RandomString(100));
                    filterElement.SetElementIds(tohide);
                    doc.Regenerate();

                    view.AddFilter(filterElement.Id);
                    view.SetFilterVisibility(filterElement.Id, false);

                    tx.Commit();
                }
            }
            catch (Exception) { }
        }

        public static List<ViewSheet> GetAllSheets(Document doc)
        {
            List<ViewSheet> AllSheets = Helper.GetAllT<ViewSheet>(doc);
            return AllSheets
                    .Where(x => x.IsTemplate == false)
                        .Where(x => x.CanBePrinted == true)
                      .Where(x => x.ViewType == ViewType.DrawingSheet).ToList();
        }

        public static void outlineView(Document doc, View view)
        {
            try
            {
                var bbv = view.get_BoundingBox(view);
                List<XYZ> bbx = boxtolinePoints(bbv);
                viewdetailBox(doc, view, bbx[0], bbx[1], bbx[2], bbx[3]);
            }
            catch (Exception) { }
        }

        public static List<XYZ> boxtolinePoints(BoundingBoxXYZ bbv)
        {
            var maxx = bbv.Max.X;
            var maxy = bbv.Max.Y;
            var maxz = bbv.Max.Z;

            var minx = bbv.Min.X;
            var miny = bbv.Min.Y;
            var minz = bbv.Min.Z;
            var min = maxz - minz;

            XYZ p00 = new XYZ(maxx, maxy, maxz);
            XYZ p11 = new XYZ(minx, miny, minz);

            XYZ p10 = new XYZ(minx, maxy, minz);
            XYZ p01 = new XYZ(maxx, miny, maxz);
            return new List<XYZ>(new XYZ[] { p00, p01, p10, p11 });
        }


        public static string AndHash(string s)
        {
            var net = "\\\\sv-cifs01.tocci.com\\group";
            string z = "z:";
            if (s.StartsWith("\\\\sv-cifs01.tocci.com\\group"))
                return Convert.ToString(s.Replace(net, z).GetHashCode()) + "---";
            else
                return Convert.ToString(s.GetHashCode()) + "---";
        }

        public static string ConsHash(string s)
        {
            var net = "\\\\sv-cifs01.tocci.com\\group";
            string z = "z:";
            string outs = "";
            if (s.StartsWith("\\\\sv-cifs01.tocci.com\\group"))
            { outs = s.Replace(net, z); }
            else
            { outs = s;  }

            string str = Convert.ToString(MurmurHash2.Hash(Encoding.ASCII.GetBytes(outs)));
            return "H-" + str + "---";
        }

        public static void viewdetailBox(Document doc, View view, XYZ p00, XYZ p01, XYZ p10, XYZ p11)
        {
            try
            {
                using (Transaction tx = new Transaction(doc))
                {
                    tx.Start("shade");
                    Line newLine1 = Line.CreateBound(p00, p10);
                    Line newLine2 = Line.CreateBound(p00, p01);
                    Line newLine3 = Line.CreateBound(p10, p11);
                    Line newLine4 = Line.CreateBound(p01, p11);
                    doc.Create.NewDetailCurve(doc.ActiveView, newLine1);
                    doc.Create.NewDetailCurve(doc.ActiveView, newLine2);
                    doc.Create.NewDetailCurve(doc.ActiveView, newLine3);
                    doc.Create.NewDetailCurve(doc.ActiveView, newLine4);
                    tx.Commit();
                }
            }
            catch (Exception) { }
        }

    }
}
