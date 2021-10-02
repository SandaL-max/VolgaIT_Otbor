using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.IO;
using System.Text.RegularExpressions;


namespace VolgaIT_Otbor
{
    class DBManagement
    {
        public static string sBasePath = System.AppDomain.CurrentDomain.BaseDirectory;
        public static string sDBpath = sBasePath + "testDB.db";
        public static string ProjectsPath = sBasePath + "content\\";

        public List<Table_Projects> dbTable_Projects = new List<Table_Projects>();
        public List<Table_Files> dbTable_Files = new List<Table_Files>();
        public List<Table_Words> dbTable_Words = new List<Table_Words>();


        // Работа со словами

        // Загрузка слов
        public int Get_Words(string master_projectid, string master_fileid)
        {
            int wordsCount = 0;
            try
            {
                dbTable_Words.Clear();

                using (SQLiteConnection m_dbConnection = new SQLiteConnection(@"Data Source=" + sDBpath + "; Version=3; foreign keys=true;"))
                {
                    m_dbConnection.Open();

                    // Get projects from DB
                    string sql = "SELECT * FROM words WHERE projectid = " + master_projectid + " AND fileid = "+ master_fileid + " ORDER BY count DESC;";
                    SQLiteCommand sqlCommand = new SQLiteCommand(sql, m_dbConnection);
                    SQLiteDataReader reader = sqlCommand.ExecuteReader();

                    while (reader.Read())
                    {
                        string id = reader.GetValue(0).ToString();
                        string projectid = reader.GetValue(1).ToString();
                        string fileid = reader.GetValue(2).ToString();
                        string word = reader.GetValue(3).ToString();
                        string count = reader.GetValue(4).ToString();

                        dbTable_Words.Add(new Table_Words(id, projectid, fileid, word, count));
                        wordsCount++;
                    }

                    m_dbConnection.Close();
                }
            }
            catch (Exception fail)
            {
                Log.LogExceptionMSG("DBManagement", "Get_Words", fail.Message, true);
                wordsCount = -1;
            }
            return wordsCount;
        }

        // Добавление слова
        public bool Word_Add(string projectid, string fileid, string word, int count)
        {
            bool returnCode = true;
            try
            {
                using (SQLiteConnection m_dbConnection = new SQLiteConnection(@"Data Source=" + sDBpath + "; Version=3; foreign keys=true;"))
                {
                    m_dbConnection.Open();
                    word = word.Replace("'", "''");

                    string sql = "SELECT count FROM words WHERE projectid = " + projectid + " AND fileid = " + fileid + " AND word = '" + word + "';";
                    SQLiteCommand sqlCommand = new SQLiteCommand(sql, m_dbConnection);

                    object sqlObj = sqlCommand.ExecuteScalar();
                    if (sqlObj != null)
                    {
                        sql = "UPDATE words SET count = count + " + Convert.ToString(count) + " WHERE projectid = " + projectid + " AND fileid = " + fileid + " AND word = '" + word + "';";
                    }
                    else
                    {
                        sql = "INSERT INTO words (id, projectid, fileid, word, count) " +
                                "VALUES (NULL, " + projectid + ", " + fileid + ", '" + word + "', " + Convert.ToString(count) + ");";
                    }

                    sqlCommand = new SQLiteCommand(sql, m_dbConnection);
                    sqlCommand.ExecuteNonQuery();

                    m_dbConnection.Close();
                }
            }
            catch (Exception fail)
            {
                Log.LogExceptionMSG("DBManagement", "Word_Add", fail.Message, true);
                returnCode = false;
            }
            return returnCode;
        }

        // Просто выполнение команды - используеется для множественного добавления слов
        public bool SQLite_RunCommand(string sql)
        {
            bool returnCode = true;
            try
            {
                using (SQLiteConnection m_dbConnection = new SQLiteConnection(@"Data Source=" + sDBpath + "; Version=3; foreign keys=true;"))
                {
                    m_dbConnection.Open();

                    SQLiteCommand sqlCommand = new SQLiteCommand(sql, m_dbConnection);
                    sqlCommand = new SQLiteCommand(sql, m_dbConnection);
                    sqlCommand.ExecuteNonQuery();

                    m_dbConnection.Close();
                }
            }
            catch (Exception fail)
            {
                Log.LogExceptionMSG("DBManagement", "SQLite_RunCommand", fail.Message, true);
                returnCode = false;
            }
            return returnCode;
        }

        // Подготовка базы и удаление уже присутсвующей статистики парсинга
        public bool Words_Prepare(string fileid)
        {
            bool returnCode = true;
            try
            {
                using (SQLiteConnection m_dbConnection = new SQLiteConnection(@"Data Source=" + sDBpath + "; Version=3; foreign keys=true;"))
                {
                    m_dbConnection.Open();

                    string sql = "DELETE FROM words WHERE fileid = " + fileid + ";";
                    SQLiteCommand sqlCommand = new SQLiteCommand(sql, m_dbConnection);
                    sqlCommand.ExecuteNonQuery();

                    m_dbConnection.Close();
                }
            }
            catch (Exception fail)
            {
                Log.LogExceptionMSG("DBManagement", "Word_Prepare", fail.Message, true);
                returnCode = false;
            }
            return returnCode;
        }


        // Работа с файлами

        // Загрузка файлов проекта
        public bool Get_Files(ref List<string> lProjectFiles, string master_projectid, string folder)
        {
            bool returnCode = true;
            try
            {
                dbTable_Files.Clear();

                using (SQLiteConnection m_dbConnection = new SQLiteConnection(@"Data Source=" + sDBpath + "; Version=3; foreign keys=true;"))
                {
                    m_dbConnection.Open();

                    // Загрузить файлы соответствующего проекта из БД
                    string sql = "SELECT fileid, projectid, filename FROM files WHERE projectid = " + master_projectid + ";";
                    SQLiteCommand sqlCommand = new SQLiteCommand(sql, m_dbConnection);
                    SQLiteDataReader reader = sqlCommand.ExecuteReader();

                    string sqlremove = "";
                    while (reader.Read())
                    {
                        string fileid = reader.GetValue(0).ToString();
                        string projectid = reader.GetValue(1).ToString();
                        string filename = reader.GetValue(2).ToString();

                        // Если файл из БД не имеет соответсующего физического файла, тогда удалить его из БД
                        string fn = ProjectsPath + "\\" + folder + "\\" + filename;
                        if (File.Exists(fn))
                        {
                            dbTable_Files.Add(new Table_Files(fileid, projectid, filename));
                        }
                        else
                        {
                            sqlremove += "DELETE FROM files WHERE fileid = " + fileid + ";";
                            Log.LogExceptionMSG("DBManagement", "Get_Files", "Project: '" + folder + "' | File: '" + filename + "' not found and removed from DB.", false);
                        }

                    }

                    if (sqlremove != "") // Выполнить команду на удаление отсутствующих файлов из БД
                    {
                        sqlCommand = new SQLiteCommand(sqlremove, m_dbConnection);
                        sqlCommand.ExecuteNonQuery();
                    }

                    // Добавить новые найденные файлы в БД
                    foreach (string fn in lProjectFiles)
                    {
                        var match = dbTable_Files.Find(item => item.filename == fn);
                        if (match == null)
                        {
                            Add_File(master_projectid, fn);
                        }
                    }

                    m_dbConnection.Close();
                }
            }
            catch (Exception fail)
            {
                Log.LogExceptionMSG("DBManagement", "Get_Files", fail.Message, true);
                returnCode = false;
            }
            return returnCode;
        }

        // Добавление сушествующего файла в БД
        public bool Add_File(string projectid, string filename)
        {
            bool returnCode = true;
            try
            {
                using (SQLiteConnection m_dbConnection = new SQLiteConnection(@"Data Source=" + sDBpath + "; Version=3; foreign keys=true;"))
                {
                    m_dbConnection.Open();

                    string sql = "INSERT INTO files(fileid, projectid, filename) " +
                                    "VALUES(NULL, '" + projectid + "', '" + filename + "');";
                    SQLiteCommand sqlCommand = new SQLiteCommand(sql, m_dbConnection);
                    sqlCommand.ExecuteNonQuery();

                    sql = "SELECT last_insert_rowid();";
                    sqlCommand = new SQLiteCommand(sql, m_dbConnection);
                    string id = Convert.ToString((long)sqlCommand.ExecuteScalar());
                    m_dbConnection.Close();

                    dbTable_Files.Add(new Table_Files(id, projectid, filename));
                }
            }
            catch (Exception fail)
            {
                Log.LogExceptionMSG("DBManagement", "Add_File", fail.Message, true);
                returnCode = false;
            }
            return returnCode;
        }

        // Удаление файла из БД
        public bool Remove_File(int row_index, string projectid, string fileid)
        {
            bool returnCode = true;
            try
            {
                using (SQLiteConnection m_dbConnection = new SQLiteConnection(@"Data Source=" + sDBpath + "; Version=3; foreign keys=true;"))
                {
                    m_dbConnection.Open();

                    string sql = "DELETE FROM files WHERE projectid = " + projectid + " AND fileid = " + fileid + ";";
                    SQLiteCommand sqlCommand = new SQLiteCommand(sql, m_dbConnection);
                    sqlCommand.ExecuteNonQuery();

                    m_dbConnection.Close();

                    dbTable_Files.RemoveAt(row_index);
                }
            }
            catch (Exception fail)
            {
                Log.LogExceptionMSG("DBManagement", "Remove_File", fail.Message, true);
                returnCode = false;
            }
            return returnCode;
        }


        // Работа с проектами

        // Удаление проекта из БД
        public bool Remove_Project(int row_index, string projectid)
        {
            bool returnCode = true;
            try
            {
                using (SQLiteConnection m_dbConnection = new SQLiteConnection(@"Data Source=" + sDBpath + "; Version=3; foreign keys=true;"))
                {
                    m_dbConnection.Open();

                    string sql = "DELETE FROM projects WHERE projectid = " + projectid + ";";
                    SQLiteCommand sqlCommand = new SQLiteCommand(sql, m_dbConnection);
                    sqlCommand.ExecuteNonQuery();

                    m_dbConnection.Close();

                    dbTable_Projects.RemoveAt(row_index);
                }
            }
            catch (Exception fail)
            {
                Log.LogExceptionMSG("DBManagement", "Remove_Project", fail.Message, true);
                returnCode = false;
            }
            return returnCode;
        }

        // Добавление проекта
        public bool Add_Project(string s_Name, string s_Folder)
        {
            bool returnCode = true;
            try
            {
                using (SQLiteConnection m_dbConnection = new SQLiteConnection(@"Data Source=" + sDBpath + "; Version=3; foreign keys=true;"))
                {
                    m_dbConnection.Open();

                    string sql = "INSERT INTO projects(projectid, name, folder) " +
                                    "VALUES(NULL, '" + s_Name + "', '" + s_Folder + "');";                                
                    SQLiteCommand sqlCommand = new SQLiteCommand(sql, m_dbConnection);
                    sqlCommand.ExecuteNonQuery();

                    sql = "SELECT last_insert_rowid();";
                    sqlCommand = new SQLiteCommand(sql, m_dbConnection);
                    string project_id = Convert.ToString((long)sqlCommand.ExecuteScalar());
                    m_dbConnection.Close();

                    dbTable_Projects.Add(new Table_Projects(project_id, s_Name, s_Folder));
                }
            }
            catch (Exception fail)
            {
                Log.LogExceptionMSG("DBManagement", "Add_Project", fail.Message, true);
                returnCode = false;
            }
            return returnCode;
        }

        // Стартовая загрузка проектов из БД и сравнение со списком папок
        public bool Get_Projects(ref List<string> lProjectsFolders)
        {
            bool returnCode = true;
            try
            {
                dbTable_Projects.Clear();

                using (SQLiteConnection m_dbConnection = new SQLiteConnection(@"Data Source=" + sDBpath + "; Version=3; foreign keys=true;"))
                {
                    m_dbConnection.Open();

                    // Получить проекты из БД
                    string sql = "SELECT projectid, name, folder FROM projects;";
                    SQLiteCommand sqlCommand = new SQLiteCommand(sql, m_dbConnection);
                    SQLiteDataReader reader = sqlCommand.ExecuteReader();

                    string sqlremove = "";
                    while (reader.Read())
                    {
                        string projectid = reader.GetValue(0).ToString();
                        string name = reader.GetValue(1).ToString();
                        string folder = reader.GetValue(2).ToString();

                        // Проверить если проект из БД не имеет реальной физической папки, тогда удалить его из БД
                        var match = lProjectsFolders.FirstOrDefault(stringToCheck => stringToCheck.Contains(folder));

                        if (match != null) 
                        {
                            dbTable_Projects.Add(new Table_Projects(projectid, name, folder));
                        }
                        else
                        {
                            sqlremove += "DELETE FROM projects WHERE projectid = " + projectid +";";
                            Log.LogExceptionMSG("DBManagement", "Get_Projects", "Folder '" + folder + "' not found and removed from DB.", false);
                        }

                    }
                    
                    if (sqlremove != "") //выполнить команду удаления папок из БД
                    {
                        sqlCommand = new SQLiteCommand(sqlremove, m_dbConnection);
                        sqlCommand.ExecuteNonQuery();
                    }

                    // Добавить новые найденные папки в БД, как новые проекты
                    foreach (string dir in lProjectsFolders) 
                    {
                        var match = dbTable_Projects.Find(item => item.folder == dir);
                        if (match == null)
                        {
                            Add_Project("Проект: " + dir, dir);
                        }
                    }
                    
                    m_dbConnection.Close();
                }
            }
            catch (Exception fail)
            {
                Log.LogExceptionMSG("DBManagement", "Get_Projects", fail.Message, true);
                returnCode = false;
            }
            return returnCode;
        }


        // Создание БД и Таблиц // Функция отключена
        public bool MakeDB()
        {
            bool returnCode = true;
            try
            {
                using (SQLiteConnection m_dbConnection = new SQLiteConnection(@"Data Source=" + sDBpath + "; Version=3; foreign keys=true;"))
                {
                    string sql = "";
                    m_dbConnection.Open();

                    sql = "CREATE TABLE IF NOT EXISTS projects (" +
                                 "projectid INTEGER PRIMARY KEY AUTOINCREMENT, " +
                                 "name VARCHAR(255), " +
                                 "folder VARCHAR(255) " +
                                 ");";
                    SQLiteCommand sqlCommand = new SQLiteCommand(sql, m_dbConnection);
                    sqlCommand.ExecuteNonQuery();

                    sql = "CREATE TABLE IF NOT EXISTS files (" +
                                 "fileid INTEGER PRIMARY KEY AUTOINCREMENT, " +
                                 "projectid INTEGER, " +
                                 "filename VARCHAR(255), " +
                                 "FOREIGN KEY(projectid) REFERENCES projects(projectid) ON DELETE CASCADE" +
                                 ");";
                    sqlCommand = new SQLiteCommand(sql, m_dbConnection);
                    sqlCommand.ExecuteNonQuery();

                    sql = "CREATE TABLE IF NOT EXISTS words (" +
                                 "id INTEGER PRIMARY KEY AUTOINCREMENT, " +
                                 "projectid INTEGER, " +
                                 "fileid INTEGER, " +
                                 "word VARCHAR(255), " +
                                 "count INTEGER, " +
                                 "FOREIGN KEY(projectid) REFERENCES projects(projectid) ON DELETE CASCADE," +
                                 "FOREIGN KEY(fileid) REFERENCES files(fileid) ON DELETE CASCADE" +
                                 ");";
                    sqlCommand = new SQLiteCommand(sql, m_dbConnection);
                    sqlCommand.ExecuteNonQuery();

                    m_dbConnection.Close();
                }
            }
            catch (Exception fail)
            {
                Log.LogExceptionMSG("DBManagement", "MakeDB", fail.Message, true);
                returnCode = false;
            }
            return returnCode;
        }


        // вспомогательные классы
        public class Table_Words
        {
            public string id { get; set; }
            public string projectid { get; set; }
            public string fileid { get; set; }
            public string word { get; set; }
            public string count { get; set; }

            public Table_Words(string id, string projectid, string fileid, string word, string count)
            {
                this.id = id;
                this.projectid = projectid;
                this.fileid = fileid;
                this.word = word;
                this.count = count;
            }
        }

        public class Table_Files
        {
            public string fileid { get; set; }
            public string projectid { get; set; }
            public string filename { get; set; }

            public Table_Files(string fileid, string projectid, string filename)
            {
                this.fileid = fileid;
                this.projectid = projectid;
                this.filename = filename;
            }
        }

        public class Table_Projects
        {
            public string projectid { get; set; }
            public string name { get; set; }
            public string folder { get; set; }

            public Table_Projects(string projectid, string name, string folder)
            {
                this.projectid = projectid;
                this.name = name;
                this.folder = folder;
            }
        }


    }
}
