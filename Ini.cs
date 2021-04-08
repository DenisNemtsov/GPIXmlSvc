using System;
using System.Text;
using System.Runtime.InteropServices;

namespace GPIXmlSvc
{
    class IniFile
    {
        /// <summary>  
        /// Переменая для хранения полного пути к .ini-файлу. 
        /// </summary>
        /// <remarks>
        /// Если переменная "File" не задана до начала вызова методов класса, то
        /// будет предпринята попытка поиска файла с таким же именем и в той же 
        /// директории, откуда запущен исполняемый файл приложения.
        /// </remarks>
        /// <example> 
        /// (Например: Если Ваше приложение "С:\Dev\Startme.exe", то искомый файл 
        ///  настроек будет "С:\Dev\Startme.ini")
        /// </example>
        public static string File; //Имя файла.
        public const int MaxSectionSize = 32767; // Размер буфера для хранения имен секций.

        /// <summary>  
        /// Подключение функции WritePrivateProfileString из библиотеки kernel32.dll. 
        /// </summary>
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        static extern long WritePrivateProfileString(string Section, string Key, string Value, string FilePath);

        /// <summary>  
        /// Подключение функции GetPrivateProfileString из библиотеки kernel32.dll, для функции Read. 
        /// </summary>
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        static extern int GetPrivateProfileString(string Section, string Key, string Default, StringBuilder RetVal, int Size, string FilePath);

        /// <summary>  
        /// Подключение функции GetPrivateProfileString из библиотеки kernel32.dll, для функции GetKeyNames. 
        /// </summary>
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        static extern int GetPrivateProfileString(string Section, string Key, string Default, IntPtr RetVal, int Size, string FilePath);

        /// <summary>  
        /// Чтение .ini-файла и возврат значения ключа из заданной секции. 
        /// </summary>
        /// <param name="Section">Название секции в .ini-файле.</param>
        /// <param name="Key">Название ключа в .ini-файле.</param>
        public static string Read(string Section, string Key)
        {
            CheckFileName(File);
            if (Section == null)
                throw new ArgumentNullException("Section");

            if (Key == null)
                throw new ArgumentNullException("Key");

            var RetVal = new StringBuilder(255);
            GetPrivateProfileString(Section, Key, "", RetVal, 255, File);
            return RetVal.ToString();
        }

        /// <summary>  
        /// Запись значения ключа в заданную секцию .ini-файла.
        /// </summary>
        /// <param name="Section">Название секции в .ini-файле.</param>
        /// <param name="Key">Название ключа в .ini-файле.</param>
        /// <param name="Value">Значение ключа в .ini-файле.</param>
        public static void Write(string Section, string Key, string Value)
        {
            CheckFileName(File);
            if (Section == null)
                throw new ArgumentNullException("Section");

            if (Key == null)
                throw new ArgumentNullException("Key");

            WritePrivateProfileString(Section, Key, Value, File);
        }

        /// <summary>  
        /// Удаление ключа из заданной секции .ini-файла.
        /// </summary>
        /// <param name="Key">Название ключа в .ini-файле.</param>
        /// <param name="Section">Название секции в .ini-файле.</param>
        public static void DeleteKey(string Key, string Section = null)
        {
            Write(Section, Key, null);
        }

        /// <summary>  
        /// Удаление заданной секции из .ini-файла.
        /// </summary>
        /// <param name="Section">Название секции в .ini-файле.</param>
        public static void DeleteSection(string Section = null)
        {
            Write(Section, null, null);
        }

        /// <summary>  
        /// Проверка наличия заданной секции в .ini-файле.
        /// </summary>
        /// <param name="Section">Название секции в .ini-файле.</param>
        /// <param name="Key">Название ключа в .ini-файле.</param>
        public bool KeyExists(string Key, string Section = null)
        {
            return Read(Section, Key).Length > 0;
        }

        /// <summary>  
        /// Выборка в строковый массив всех ключей принадлежащих секции заданной параметром <paramref name="Section"/> .
        /// </summary>
        /// <param name="Section">Название секции в .ini-файле.</param>
        public static string[] GetKeyNames(string Section)
        {
            int len;
            string[] retval;

            CheckFileName(File);
            if (Section == null)
                throw new ArgumentNullException("Section");

            IntPtr ptr = Marshal.AllocCoTaskMem(IniFile.MaxSectionSize);
            try
            {
                len = GetPrivateProfileString(Section, null, null, ptr, IniFile.MaxSectionSize, File);
                if (len == 0)
                {
                    retval = new string[0];
                }
                else
                {
                    string buff = Marshal.PtrToStringAuto(ptr, len - 1);
                    retval = buff.Split('\0');
                }
            }
            finally
            {
                Marshal.FreeCoTaskMem(ptr);
            }
            return retval;
        }

        /// <summary>  
        /// Проверка имени файла, если пустое, то присваивается имя по умолчанию, см. <see cref="Переменая для хранения полного пути к .ini-файлу."/>.
        /// </summary>
        /// <param name="File">Полный путь к имени файла .ini-файле.</param>
        private static string CheckFileName(string File)
        {
            if (String.IsNullOrEmpty(File))
                return AppDomain.CurrentDomain.BaseDirectory + System.IO.Path.GetFileNameWithoutExtension(System.Reflection.Assembly.GetExecutingAssembly().Location) + ".ini";
            else
                return File;
        }
    }
}
