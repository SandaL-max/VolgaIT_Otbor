using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Text.RegularExpressions;
using System.Data.SQLite;
//
namespace VolgaIT_Otbor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static string sBasePath = System.AppDomain.CurrentDomain.BaseDirectory;
        public static string ProjectsPath = sBasePath + "content\\";

        private static ProjectsManager ProjectsManager = new ProjectsManager();
        private static DBManagement DBManagement = new DBManagement();
        private static FileParser FileParser = new FileParser();

        public MainWindow()
        {
            InitializeComponent();
        }
       
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Просканировать папку "content" и сравнить проекты с записями в БД
                ProjectsManager.Get_Projects();
                //TestListBox.ItemsSource = ProjectsManager.lProjectsFolders;

                // Стартовая инициализация датагрида с проектами
                DBManagement.Get_Projects(ref ProjectsManager.lProjectsFolders);
                dg_Projects.ItemsSource = DBManagement.dbTable_Projects;
                dg_Select(ref dg_Projects, 0, "first");

                dg_Projects.Columns[0].Header = "#P";
                dg_Projects.Columns[1].Header = "Название проекта";
                dg_Projects.Columns[2].Header = "Папка";

                // Стартовая инициализация файлового датагрида
                string projectid = DBManagement.dbTable_Projects[0].projectid;
                string folder = DBManagement.dbTable_Projects[0].folder;
                ProjectsManager.Get_Files(folder);
                DBManagement.Get_Files(ref ProjectsManager.lProjectFiles, projectid, folder);
                dg_Files.ItemsSource = DBManagement.dbTable_Files;
                dg_Select(ref dg_Files, 0, "first");

                dg_Files.Columns[0].Header = "#F";
                dg_Files.Columns[1].Header = "#P";
                dg_Files.Columns[2].Header = "Файл";

                // Стартовая инициализация датагрида со статистикой слов
                string fileid = DBManagement.dbTable_Files[0].fileid;
                int wordsCount = DBManagement.Get_Words(projectid, fileid);
                dg_Words.ItemsSource = DBManagement.dbTable_Words;
                dg_Select(ref dg_Words, 0, "first");
                label_TotalWords.Content = "Всего слов: " + wordsCount.ToString();

                dg_Words.Columns[0].Header = "#";
                dg_Words.Columns[1].Header = "#P";
                dg_Words.Columns[2].Header = "#F";
                dg_Words.Columns[3].Header = "Слово";
                dg_Words.Columns[4].Header = "Количество";
            }
            catch (Exception fail)
            {
                Log.LogExceptionMSG("MainWindow", "MainWindow_Loaded", fail.Message, true);
            }
        }

        private void btn_Analyse_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // получаем размер буффера
                string val = cb_BufferSize.Text;
                int bufsize = 0;

                switch (val)
                {
                    case "128 bytes":
                        bufsize = 128;
                        break;
                    case "1 kb":
                        bufsize = 1024;
                        break;
                    case "8 kb":
                        bufsize = 8192;
                        break;
                    case "32 kb":
                        bufsize = 32768;
                        break;
                    case "64 kb":
                        bufsize = 65536;
                        break;
                    case "128 kb":
                        bufsize = 131072;
                        break;
                }

                // получаем данные о файле для парсинга
                string projectid = dg_GetSelectedValue(ref dg_Projects, 0);
                string folder = dg_GetSelectedValue(ref dg_Projects, 2);
                string fileid = dg_GetSelectedValue(ref dg_Files, 0);
                string filename = dg_GetSelectedValue(ref dg_Files, 2);

                int wordsCount = FileParser.Parse(ref DBManagement, bufsize, projectid, folder, fileid, filename);
                if (wordsCount > 0)
                {
                    //TestListBox_Words.ItemsSource = FileParser.lWords;
                    // отображение статистики слов
                    wordsCount = DBManagement.Get_Words(projectid, fileid);
                    dg_Words.ItemsSource = DBManagement.dbTable_Words;
                    dg_Select(ref dg_Words, 0, "first");
                    label_TotalWords.Content = "Всего слов: " + wordsCount.ToString();
                }
            }
            catch (Exception fail)
            {
                Log.LogExceptionMSG("MainWindow", "btn_Analyse_Click", fail.Message, true);
            }
        }

        private void btn_RemoveFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (dg_Files.Items.Count > 0)
                {
                    // получаем данные о файле для удаления из БД и с диска
                    int row_index = dg_Files.SelectedIndex;
                    string projectid = dg_GetSelectedValue(ref dg_Projects, 0);
                    string folder = dg_GetSelectedValue(ref dg_Projects, 2);
                    string fileid = dg_GetSelectedValue(ref dg_Files, 0);
                    string filename = dg_GetSelectedValue(ref dg_Files, 2);
                    DBManagement.Remove_File(row_index, projectid, fileid);
                    dg_Select(ref dg_Files, row_index - 1, "prev");

                    // обновить грид со словами
                    int wordsCount = 0;

                    if (dg_Files.Items.Count > 0) // загрузить статистику слов для другого выбранного файла
                    {
                        projectid = dg_GetSelectedValue(ref dg_Projects, 0);
                        fileid = DBManagement.dbTable_Files[row_index - 1].fileid;// dg_GetSelectedValue(ref dg_Files, 0);
                        wordsCount = DBManagement.Get_Words(projectid, fileid);
                    }
                    else
                    { // если файлов в проекте не осталось, просто очистить грид со словами
                        DBManagement.dbTable_Words.Clear();
                    }
                    dg_Words.ItemsSource = DBManagement.dbTable_Words;
                    dg_Select(ref dg_Words, 0, "first");
                    label_TotalWords.Content = "Всего слов: " + wordsCount.ToString();

                    // удалить файл
                    string filePath = ProjectsPath + folder + "\\" + filename;
                    if (File.Exists(filePath))
                    {
                        //MessageBox.Show(filePath);
                        File.Delete(filePath);
                    }

                }
            }
            catch (Exception fail)
            {
                Log.LogExceptionMSG("MainWindow", "btn_RemoveFile_Click", fail.Message, true);
            }
        }

        private void dg_Projects_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (dg_Projects.SelectedCells.Count > 0)
                {
                    string projectid = dg_GetSelectedValue(ref dg_Projects, 0);
                    string folder = dg_GetSelectedValue(ref dg_Projects, 2);
                    if (projectid != null) {
                        ProjectsManager.Get_Files(folder);

                        DBManagement.Get_Files(ref ProjectsManager.lProjectFiles, projectid, folder);
                        dg_Files.ItemsSource = DBManagement.dbTable_Files;
                        dg_Select(ref dg_Files, 0, "first");
                    }
                }
            }
            catch (Exception fail)
            {
                Log.LogExceptionMSG("MainWindow", "dg_Projects_SelectionChanged", fail.Message, true);
            }
        }

        private void dg_Files_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if ((dg_Projects.SelectedCells.Count > 0) && (dg_Files.SelectedCells.Count > 0))
                {
                    string projectid = dg_GetSelectedValue(ref dg_Projects, 0);
                    string fileid = dg_GetSelectedValue(ref dg_Files, 0);
                    if ((projectid != null) && (fileid != null))
                    {
                        int wordsCount = DBManagement.Get_Words(projectid, fileid);
                        label_TotalWords.Content = "Всего слов: " + wordsCount.ToString();
                        dg_Words.ItemsSource = DBManagement.dbTable_Words;
                        dg_Select(ref dg_Words, 0, "first");
                    }
                }
            }
            catch (Exception fail)
            {
                Log.LogExceptionMSG("MainWindow", "dg_Files_SelectionChanged", fail.Message, true);
            }
        }


        private void btn_ProjectAdd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DBManagement.Add_Project(tb_newproject_name.Text, tb_newproject_folder.Text);
                dg_Projects.ItemsSource = DBManagement.dbTable_Projects;
                dg_Select(ref dg_Projects, dg_Projects.Items.Count-1, "last");
            }
            catch (Exception fail)
            {
                Log.LogExceptionMSG("MainWindow", "btn_ProjectAdd_Click", fail.Message, true);
            }
        }

        private void btn_ProjectRemove_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (dg_Projects.Items.Count > 0) { 
                    int row_index = dg_Projects.SelectedIndex;
                    string projectid = dg_GetSelectedValue(ref dg_Projects, 0);
                    string folder = dg_GetSelectedValue(ref dg_Projects, 2);
                    DBManagement.Remove_Project(row_index, projectid);
                    dg_Select(ref dg_Projects, row_index-1, "prev");

                    string folderPath = ProjectsPath + folder;
                    Directory.Delete(folderPath, true);

                    // Обновить панель файлов
                    row_index = row_index - 1;
                    if (row_index >= 0)
                    {
                        projectid = DBManagement.dbTable_Projects[row_index].projectid;
                        folder = DBManagement.dbTable_Projects[row_index].folder;
                        if (projectid != null)
                        {
                            ProjectsManager.Get_Files(folder);

                            DBManagement.Get_Files(ref ProjectsManager.lProjectFiles, projectid, folder);
                            dg_Files.ItemsSource = DBManagement.dbTable_Files;
                            dg_Select(ref dg_Files, 0, "first");
                        }
                        // обновить панель файлов
                        int wordsCount = 0;
                        if (dg_Files.Items.Count > 0) // загрузить статистику слов для первого файла
                        {
                            string fileid = DBManagement.dbTable_Files[0].fileid;// dg_GetSelectedValue(ref dg_Files, 0);
                            wordsCount = DBManagement.Get_Words(projectid, fileid);
                        }
                        else
                        { // если файлов в проекте не осталось, просто очистить грид со словами
                            DBManagement.dbTable_Words.Clear();
                        }
                        dg_Words.ItemsSource = DBManagement.dbTable_Words;
                        dg_Select(ref dg_Words, 0, "first");
                        label_TotalWords.Content = "Всего слов: " + wordsCount.ToString();

                    }
                    else 
                    {
                        // проектов больше не осталось, очистить гриды с файлами и словами
                        DBManagement.dbTable_Files.Clear();
                        DBManagement.dbTable_Words.Clear();
                        dg_Files.Items.Refresh();
                        dg_Words.Items.Refresh();
                        label_TotalWords.Content = "Всего слов: 0";
                    }

                }
            }
            catch (Exception fail)
            {
                Log.LogExceptionMSG("MainWindow", "btn_ProjectRemove_Click", fail.Message, true);
            }
        }


        public bool dg_Select(ref DataGrid dg, int row_index, string rule)
        {
            bool returnCode = true;
            try
            {
                dg.Items.Refresh();
                dg.Focus();

                if (row_index < 0) { row_index = 0; }
                if (dg.Items.Count < row_index) { row_index = dg.Items.Count-1; }
                if (dg.Items.Count == 0) { row_index = 0; }

                if (dg.Items.Count > 0)
                {
                    switch (rule)
                    {
                        case "first":
                            dg.SelectedItem = dg.Items[row_index];
                            break;
                        case "last":
                            dg.SelectedItem = dg.Items[row_index];
                            break;
                        case "prev":
                            dg.SelectedItem = dg.Items[row_index];
                            break;
                    }
                }
                //Log.LogExceptionMSG("MainWindow", "dg_Select", "rule: " + rule + " | row_index: " + Convert.ToString(row_index) + " | selected index: " + Convert.ToString(dg.SelectedIndex) , true);
            }
            catch (Exception fail)
            {
                Log.LogExceptionMSG("MainWindow", "dg_Select", fail.Message, true);
                returnCode = false;
            }
            return returnCode;
        }

        private string dg_GetSelectedValue(ref DataGrid dg, int cell_num)
        {
            try
            {
                if (dg.SelectedCells.Count > 0)
                {
                    DataGridCellInfo cellInfo = dg.SelectedCells[cell_num];
                    if (cellInfo == null) return null;

                    DataGridBoundColumn column = cellInfo.Column as DataGridBoundColumn;
                    if (column == null) return null;

                    FrameworkElement element = new FrameworkElement() { DataContext = cellInfo.Item };
                    BindingOperations.SetBinding(element, TagProperty, column.Binding);

                    return element.Tag.ToString();
                }
                else
                {
                    return null;
                }
            }
            catch (Exception fail)
            {
                Log.LogExceptionMSG("MainWindow", "dg_GetSelectedValue", fail.Message, true);
                return null;
            }
        }

        private void SaveFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string folder = DBManagement.dbTable_Projects[0].folder;
                string fn = tb_newfilename.Text;
                if (fn != "")
                { 
                    string filename = ProjectsPath + folder + "\\" + fn;

                    if ((filename.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) >= 0) && (!File.Exists(System.IO.Path.Combine(ProjectsPath + folder, filename))))
                        {
                            // сохраняем файл
                            TextRange range;
                        FileStream fStream;
                        range = new TextRange(rtb.Document.ContentStart, rtb.Document.ContentEnd);
                        fStream = new FileStream(filename, FileMode.Create);
                        range.Save(fStream, DataFormats.Text);
                        fStream.Close();

                        //обновить дата грид
                        string projectid = dg_GetSelectedValue(ref dg_Projects, 0);
                        DBManagement.Add_File(projectid, fn);
                        dg_Files.Items.Refresh();                    
                    }
                    else
                    {
                        MessageBox.Show("Неверные символы в имени файла или такой файл уже существует!");
                    }
                }
                else 
                {
                    MessageBox.Show("Введите имя файла!");
                }
            }
            catch (Exception fail)
            {
                Log.LogExceptionMSG("MainWindow", "SaveFile_Click", fail.Message, true);
            }
        }
    }



    public class ListBoxFill
    {
        public static void AddMultiple(ref ListBox ListBoxForFill, string[] StrItems)
        {
            foreach(string El in StrItems)
            {
                ListBoxForFill.Items.Add(El);
            }
        }
    }
}
