using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Text;
using System.Threading.Tasks;

namespace TaskClient
{
    class CategoryComparer : IEqualityComparer<Category>
    {
        #region Implementation of IEqualityComparer<in Category>

        public bool Equals(Category x, Category y)
        {
            if (x == null || y == null) return false;

            return x.Id.Equals(y.Id);
        }

        public int GetHashCode(Category obj)
        {
            return obj.Id.IntegerValue;
        }

        #endregion
    }

    public class DocPatterns
    {
        public ElementId Dotline { get; }
        public ElementId Solidfill { get; }
        public static ViewType[] types = { ViewType.AreaPlan, ViewType.CeilingPlan, ViewType.Detail,
            ViewType.Elevation, ViewType.EngineeringPlan, ViewType.FloorPlan,
            ViewType.Section, ViewType.ThreeD};

    public DocPatterns(Document doc)
        {
            this.Dotline
            = new FilteredElementCollector(doc)
              .OfClass(typeof(LinePatternElement))
              .ToElements().Where(x => x.Name == "Dot").First().Id;

            this.Solidfill
             = new FilteredElementCollector(doc)
               .OfClass(typeof(FillPatternElement)).FirstElementId();
        }
    }



    public class OverrideCat
    {
        public BuiltInCategory builtincategory { get; set; }
        public Category categ { get; set; }
        public OverrideGraphicSettings overrides { get; set; }
        public Boolean visible { get; set; }

        public OverrideCat(Document doc, ElementId pattern, List<string> category_ovr)
        {
            BuiltInCategory category = Helper.GetCategories(category_ovr[0]);

            var r = Convert.ToByte(category_ovr[1]);
            var g = Convert.ToByte(category_ovr[2]);
            var b = Convert.ToByte(category_ovr[3]);
            var color_c = new Color(r, g, b);

            foreach (Category elem in doc.Settings.Categories)
            {
                try
                {
                    if (elem != null)
                    {
                        var elemBcCat = (BuiltInCategory)elem.Id.IntegerValue;
                        string bcCat = elemBcCat.ToString();
                        if (category_ovr[0] == bcCat)
                        {
                            this.categ = elem;
                        }
                    }
                }
                catch (Exception) { }
            }

            var gSettings = new OverrideGraphicSettings();
            gSettings.SetSurfaceTransparency(0);

            gSettings.SetCutFillColor(color_c);
            gSettings.SetCutLineColor(color_c);
            gSettings.SetCutFillPatternVisible(true);
            gSettings.SetCutFillPatternId(pattern);
            gSettings.SetProjectionFillColor(color_c);
            gSettings.SetProjectionLineColor(color_c);
            gSettings.SetProjectionFillPatternId(pattern);

            this.builtincategory = category;
            this.overrides = gSettings;
            this.visible = true;
        }

        private void OverideByColor()
        {

        }

        public OverrideCat(ElementId pattern, BuiltInCategory category)
        {
            var gSettings = new OverrideGraphicSettings();
            var color_c = new Color(0, 0, 0);
       
            gSettings.SetProjectionLineColor(color_c);
            gSettings.SetProjectionLinePatternId(pattern);
            //gSettings.v
            builtincategory = category;
            overrides = gSettings;
            visible = true;
        }
    }
}
