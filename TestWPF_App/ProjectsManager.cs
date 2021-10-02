using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;


namespace VolgaIT_Otbor
{
    class ProjectsManager
    {
        public static string sBasePath = System.AppDomain.CurrentDomain.BaseDirectory;
        public static string ProjectsPath = sBasePath + "content\\";

        public string[] Projects;
        public string[] ProjectFiles;

        public string[] Files;

        public List<string> lProjectsFolders = new List<string>();
        public List<string> lProjectFiles = new List<string>();

        // составляем список папок - реальных проектов
        public bool Get_Projects()
        {
            bool returnCode = true;
            try
            {
                lProjectsFolders.Clear();
                Projects = Directory.GetDirectories(ProjectsPath);
                //lProjectsFolders = Projects.ToList();

                string dirname = "";
                foreach (string dir in Projects)
                {
                    // Удаляем полный путь
                    dirname = Regex.Replace(dir, @"(.*)\\", String.Empty);
                    lProjectsFolders.Add(dirname);
                }
            }
            catch (Exception fail)
            {
                Log.LogExceptionMSG("ProjectManager", "Get_Projects", fail.Message, true);
                returnCode = false;
            }
            return returnCode;
        }

        // составляем список файлов в выбранном проекте (папке)
        public bool Get_Files(string folder)
        {
            bool returnCode = true;
            try
            {
                lProjectFiles.Clear();
                string cur_project = ProjectsPath + folder;
                //MessageBox.Show(cur_project);
                ProjectFiles = Directory.GetFiles(cur_project);

                string filename = "";
                foreach (string fn in ProjectFiles)
                {
                    // Удаляем полный путь
                    filename = Regex.Replace(fn, @"(.*)\\", String.Empty);
                    lProjectFiles.Add(filename);
                }
            }
            catch (Exception fail)
            {
                Log.LogExceptionMSG("ProjectManager", "Get_Files", fail.Message, true);
                returnCode = false;
            }
            return returnCode;
        }


    }
}
