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



namespace TaskClient
{
    public static class prmhelper
    {
        public static string GetParameterValueString(Element e, BuiltInParameter bip)
        {
            Parameter p = e.get_Parameter(bip);

            string s = string.Empty;

            if (null != p)
            {
                switch (p.StorageType)
                {
                    case StorageType.Integer:
                        s = p.AsInteger().ToString();
                        break;

                    case StorageType.ElementId:
                        s = p.AsElementId().IntegerValue.ToString();
                        break;

                    case StorageType.Double:
                        s = p.AsDouble().ToString();
                        break;

                    case StorageType.String:
                        s = p.AsDouble().ToString();
                        break;

                    default:
                        s = "";
                        break;
                }
            }
            return s;
        }
    }
}
