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
#endregion

namespace TaskClient
{
    public static class ReviFileManager
    {

        public static string newpathName(string source_path, string temp_file_path)
        {
            return Path.GetDirectoryName(temp_file_path)
               + "\\"
               + Helper.AndHash(source_path)
               + Path.GetFileName(temp_file_path);
        }

     


        public static string print_directory_Name(string source_path)
        {
            return
                   Helper.AndHash(source_path)
                   + Path.GetFileNameWithoutExtension(source_path);
        }



        public static Tuple<string, bool> base_name_for_Export(UIApplication uiapp,
            string source_path)
        {

            StringBuilder sb = new StringBuilder();
            Application app = uiapp.Application;
            string final_hashed_base_name = "";
            bool continueproc = true;
            //create base hash and folder name
            string target_hashed_folder = Helper.AndHash(source_path)
                                             + Path.GetFileNameWithoutExtension(source_path);

            //string target_hashed_folder = Path.GetDirectoryName(tmp);

           // TaskDialog.Show("x", target_hashed_folder + " ");

            string rvt_base_name     = Path.GetFileNameWithoutExtension(source_path);
            string moved_rvt_path    = FileManager.model_path + target_hashed_folder + ".rvt";
            string moved_folder_path = FileManager.export_location + target_hashed_folder;

            bool rvt_exists_at_start = File.Exists(moved_rvt_path);
            bool folder_exists_at_start = File.Exists(moved_folder_path);

            if (rvt_exists_at_start == false)
            {
                sb.AppendLine("RVT already exists " + rvt_exists_at_start.ToString());
                //check if exists by existing folder 
                var mathing_DirsByBaseName = Directory.EnumerateDirectories(FileManager.export_location, "*" + rvt_base_name);
                //if the folder exists, does ---shell exsist?

                if (mathing_DirsByBaseName.Count() > 1)
                {
                    
                    string temp_file_path = FileManager.MoveAndRecord(source_path);
                    sb.AppendLine("copying temp file and checking for sheet matches: " + temp_file_path);
                    OpenOptions op = new OpenOptions();
                    op.DetachFromCentralOption = DetachFromCentralOption.DetachAndDiscardWorksets;
                    ModelPath mp =
               ModelPathUtils.ConvertUserVisiblePathToModelPath(
                   temp_file_path);
                    UIDocument uidoc = uiapp.OpenAndActivateDocument(mp, op, false);
                    Document doc = uidoc.Document;
                    //doc.Save();
                    SaveAsOptions options = new SaveAsOptions();
                    
                   
                    options.OverwriteExistingFile = true;
                    options.MaximumBackups = 1;
                    
                    doc.SaveAs(temp_file_path, options);

                    List<ViewSheet> AllSheets = Helper.GetAllSheets(doc);
                    if (AllSheets.Count() > 0)
                    {
                        sb.AppendLine("POssible matches exist : " + mathing_DirsByBaseName.Count().ToString());
                        bool found_match_in_possible_dirs = false;

                        foreach (var possible_dir in mathing_DirsByBaseName)
                        {
                            var sheet_strings = AllSheets.Select(x => x.ViewName + " - Sheet - " + x.SheetNumber);
                            var sheet_files = Directory.GetFiles(possible_dir);

                            bool all_sheets_present = true;
                            foreach (string sheet_string in sheet_strings)
                            {
                                var cl = sheet_strings.Where(x => x.Contains(sheet_string)).Count();
                                if (cl == 0)
                                {
                                    all_sheets_present = false;
                                    break;
                                }
                            }
                            if (all_sheets_present == true)
                            {
                                final_hashed_base_name = Path.GetFileName(possible_dir);
                                found_match_in_possible_dirs = true;
                            }
                        }
                        if(found_match_in_possible_dirs == false)
                        {
                            sb.AppendLine("Matches do did not have same sheets");
                            final_hashed_base_name = Helper.ConsHash(source_path) + rvt_base_name;
                            continueproc = true;

                      
                            UIDocument uidoc2 = uiapp.OpenAndActivateDocument(FileManager.init_path);
                            doc.Close();

                            saveCentralProd(uiapp, temp_file_path, final_hashed_base_name);
                        }
                        else
                        {
                            sb.AppendLine("Match found with same sheets");
                            // final_hashed_base_name = Helper.ConsHash(source_path) + rvt_base_name;
                            
                            UIDocument uidoc2 = uiapp.OpenAndActivateDocument(FileManager.init_path);
                            doc.Close();
                        }
                    }
                    else
                    {
                        final_hashed_base_name = Helper.ConsHash(source_path) + rvt_base_name;
                        sb.AppendLine("No Sheets found...");
                        saveCentralProd(uiapp, temp_file_path, final_hashed_base_name);
                        continueproc = false;
                    }

                    
                }
                else if (mathing_DirsByBaseName.Count() == 1)
                {
                    final_hashed_base_name = Path.GetFileName(mathing_DirsByBaseName.First());
                }
                else
                {
                    //if no directory is found with the base_name - this was never exported...
                    // use good hash, 
                    sb.AppendLine("No Directory found creating new...");
                    final_hashed_base_name = Helper.ConsHash(source_path) + rvt_base_name;
                }
            }
            else
            {
                //if folder is present, then it is base folder
                sb.AppendLine("Directory Exists " + final_hashed_base_name);
                final_hashed_base_name = Path.GetFileName(moved_folder_path);
            }

            sb.AppendLine();
            //TaskDialog.Show("d", FileManager.export_location + final_hashed_base_name);
            string final_dir_path = FileManager.mkdir(FileManager.export_location + final_hashed_base_name );

            File.AppendAllText(final_dir_path + "\\log.txt", sb.ToString() );
            

            return new Tuple<string, bool>(final_hashed_base_name, continueproc);
        }


        public static string saveCentralProd(
            UIApplication uiapp,  string temp_file_path, string final_hashed_base_name)
        {
            Application app = uiapp.Application;
            FileInfo filePath = new FileInfo(temp_file_path);
            ModelPath mp =
                ModelPathUtils.ConvertUserVisiblePathToModelPath(
                    filePath.FullName);

            OpenOptions opt = new OpenOptions();
            opt.DetachFromCentralOption =
                DetachFromCentralOption.DetachAndDiscardWorksets;
            opt.AllowOpeningLocalByWrongUser = true;

            string new_path = FileManager.model_path + final_hashed_base_name + ".rvt";


            if (!File.Exists(new_path))
            {
                Document doc = app.OpenDocumentFile(mp, opt);

                SaveAsOptions options = new SaveAsOptions();
                options.OverwriteExistingFile = true;

                ModelPath modelPathout
                        = ModelPathUtils.ConvertUserVisiblePathToModelPath(new_path);
                doc.SaveAs(new_path, options);
                UIDocument uidoc2 = uiapp.OpenAndActivateDocument(FileManager.init_path);
                doc.Close(true);
            }

            return new_path;
        }

        public static string saveNewCentral(
            Application app, string source_path, string temp_file_path)
        {
            FileInfo filePath = new FileInfo(temp_file_path);
            ModelPath mp =
                ModelPathUtils.ConvertUserVisiblePathToModelPath(
                    filePath.FullName);

            OpenOptions opt = new OpenOptions();
            opt.DetachFromCentralOption =
                DetachFromCentralOption.DetachAndDiscardWorksets;
            opt.AllowOpeningLocalByWrongUser = true;
            
            string new_path = newpathName(source_path, temp_file_path);
              

            if (!File.Exists(new_path))
            {
                Document doc = app.OpenDocumentFile(mp, opt);

                SaveAsOptions options = new SaveAsOptions();
                options.OverwriteExistingFile = true;

                ModelPath modelPathout
                        = ModelPathUtils.ConvertUserVisiblePathToModelPath(new_path);
                doc.SaveAs(new_path, options);
                doc.Close(true);
            }

            return new_path;
        }

        public static void updateLinks(UIApplication uiapp)
        {
            Document doc = uiapp.ActiveUIDocument.Document;
            FilteredElementCollector links
                = new FilteredElementCollector(doc)
                  .OfCategory(BuiltInCategory.OST_RvtLinks);

            foreach (Element e in links)
            {
                if (e is RevitLinkInstance)
                {
                    RevitLinkInstance ee = e as RevitLinkInstance;
                    ExternalFileReference er = e.GetExternalFileReference();
                    if (er != null)
                    {
                        ModelPath mp = er.GetPath();
                        string userVisiblePath = ModelPathUtils.ConvertModelPathToUserVisiblePath(mp);

                        //if(ee.Is)

                    }
                }

            }

        }

    }
}
