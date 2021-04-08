using System;
using System.IO;
using System.Xml;
using System.Text;
using System.Linq;
using System.Timers;
using System.Reflection;
using System.ServiceProcess;
using System.Collections.Generic;
using System.Configuration.Install;

namespace GPIXmlSvc
{
    static class Program
    {
        public const string ServiceName = "GPIXmlSvc";
        private static int Interval;                                                                // Интервал опроса папки с .xml-файлами
        private static Timer serviceTimer;                                                          // Декларируем объект для создания таймера
        private static xmlData passportData;                                                        // Объект для хранения данных текущего паспорта
        private static string Products = string.Empty;                                              // Продукты по которым будут обрабатываться файлы
        private static string PIServer = string.Empty;                                              // Имя или адрес PI Server'а для записи значений
        private static byte Flags = 0x0;                                                            // Флаговая переменная для сохранения доп. настроек
        private static string FilesPath = string.Empty;                                             // Путь к базовой папке где хранятся .xml-файлы
        private static List<string> filesPaths = new List<string>();                                // Массив для хранения путей к .xml-файлам

        /// <summary>
        /// Главная точка входа для приложения.
        /// </summary>
        static void Main(string[] args)
        {
            if (!Environment.UserInteractive)                                                       // Проверка: Приложение запущено как служба или как исполняемый файл?
            {
                Log.Stream = Log.MsgStream.File;                                                    // Перенаправление логгирования в файл при запуске приложения в виде службы
                using (var GPIXmlSvc = new GPIXmlSvc())                                             // Запуск приложения в роли службы Windows
                    ServiceBase.Run(GPIXmlSvc);
            }
            else
            {
                string parameter = string.Concat(args);                                             // Получение аргументов переданных приложению из командной строки
                if (!String.IsNullOrEmpty(parameter))                                               // Если, что-то было передано, то переход к обработке аргументов 
                {
                    Console.OutputEncoding = System.Text.Encoding.GetEncoding(1251);                // Смена кодовой странице в окне консольного вывода
                    switch (parameter)
                    {
                        case "--install":                                                           // Обработка аргумента "--install"
                            ManagedInstallerClass.InstallHelper(new string[]
                            { Assembly.GetExecutingAssembly().Location });
                            break;
                        case "--uninstall":                                                         // Обработка аргумента "--uninstall"
                            ManagedInstallerClass.InstallHelper(new string[]
                            { "/u", Assembly.GetExecutingAssembly().Location });
                            break;
                    }
                    return;
                }
                Log.Stream = Log.MsgStream.Everywhere;                                              // Перенаправление логгирования на экран и в файл одновременно
                Start();                                                                            // Запуск стартовой процедуры загружающей все настройки файла
                ConsoleKeyInfo KeyPressed;
                do { KeyPressed = Console.ReadKey(true); }                                          // Получение кода последней нажатой клавиши на клавиатуре
                while (KeyPressed.Key != ConsoleKey.Escape);                                        // Завершение приложения, если была нажата клавиша 'Escape'
                Log.Message("Нажата клавиша 'Escape', завершение работы приложения...", 
                    null, Log.MsgType.Message);
            }
        }

        public static void Start()
        {
            ShowStartMessage();                                                                     // Вывод стартового сообщения
            IniFile.File = AppDomain.CurrentDomain.BaseDirectory
                + System.IO.Path.GetFileNameWithoutExtension
                (System.Reflection.Assembly.GetExecutingAssembly().Location) + ".ini";              // Путь к файлу настроек службы
            Log.Message("Загрузка файла настроек, путь к файлу:");                                  // Запись сообщения в журнал
            Log.Message(IniFile.File);                                                              // Запись сообщения в журнал
            FilesPath = IniFile.Read("Settings", "FilesPath");                                      // Чтение настройки из .ini-файла (Путь к обрабатываемой папке)
            if (String.IsNullOrEmpty(FilesPath))                                                    // Проверка корректности чтения файла настроек
            {
                Log.Message("Файл настроек не найден или имеет неправильный формат...",
                    null, Log.MsgType.Error);                                                       // Запись сообщения об ошибке в журнал
                Log.Message("Дальнейший запуск службы невозможен!",
                    null, Log.MsgType.Error);                                                       // Запись сообщения об ошибке в журнал
                Environment.Exit(-1);                                                               // Завершаем работу службы и возвращаем системе код ошибки "-1"
            }
            else
            {
                Log.Message("Файл настроек найден, начато чтение настроек");                        // Запись сообщения в журнал
                string tmpStr = IniFile.Read("Settings", "Logging");                                // Считываем настройки логирования из файла нстроек
                switch (tmpStr)                                                                     // Передаем настройки логгеру
                {
                    case "max":
                        Log.Message("Уровень фильтрации сообщений журнала установлен в 'Минимальный',");
                        Log.Message("будет сохранено максимальное количество сообщений. Подробность высокая.");
                        Log.Level = Log.MsgLevel.Max;                                                // Настраиваем логгер
                        break;
                    case "min":
                        Log.Message("Уровень фильтрации сообщений журнала установлен в 'Средний',");
                        Log.Message("будут сохранены системные сообщения и ошибки. Подробность средняя.");
                        Log.Level = Log.MsgLevel.Min;                                               // Настраиваем логгер
                        break;
                    case "err":
                        Log.Message("Уровень фильтрации сообщений журнала установлен в 'Критический',");
                        Log.Message("будут сохранены только сообщения об ошибках. Подробность низкая.");
                        Log.Level = Log.MsgLevel.Err;                                               // Настраиваем логгер
                        break;
                }
                Log.Message("Базовая директория загружена: " + FilesPath);                          // Запись сообщения в журнал
                Interval = Convert.ToInt32(IniFile.Read("Settings", "Interval"));
                Log.Message("Интервал опроса загружен и составляет: " + Interval + " минут");       // Запись сообщения в журнал
                Products = IniFile.Read("Settings", "Products");
                Log.Message("Список продуктов загружен: " + Products);                              // Запись сообщения в журнал
                PIServer = IniFile.Read("Settings", "PIServer");
                Log.Message("Адрес PI Server'а установлен: " + PIServer);                           // Запись сообщения в журнал
                Flags = Convert.ToByte(IniFile.Read("Settings", "Flags"), 8);
                Log.Message("Флаги дополнительных настроек загружены...");                          // Запись сообщения в журнал
                serviceTimer = new System.Timers.Timer();                                           // Создаем новый экземпляр таймера
                serviceTimer.Interval = TimeSpan.FromMinutes(Interval).TotalMilliseconds;           // Задаем интервал в миллисекундах пересчитанных из минут
                Log.Message("Интервал опроса установлен и составляет: "
                    + serviceTimer.Interval + " миллисекунд");                                      // Запись сообщения в журнал
                serviceTimer.Elapsed += new System.Timers.ElapsedEventHandler(TimerCallback);       // Определяем процедуру-обработчик вызываемую таймером
                serviceTimer.Start();                                                               // Немедленный запуск процедуры таймера
            }
        }

        public static void Stop()
        {

        }

        /// <summary>
        /// Процедура обработки "тика" таймера.
        /// </summary>
        private static void TimerCallback(Object source, System.Timers.ElapsedEventArgs e)
        {
            MainCycle();
        }

        /// <summary>
        /// Процедура главного цикла программы, вызываемая при каждом "тике" таймера.
        /// </summary>
        private static void MainCycle()
        {
            Log.Message("Начало цикла обработки .xml-файлов");                                                                                       // Запись сообщения в журнал
            serviceTimer.Enabled = false;                                                                                                            // Остановка таймера для предотвращений "наложения" циклов
            Log.Message("Остановка таймера службы до конца обработки .xml-файлов");                                                                  // Запись сообщения в журнал
            try
            {
                XmlFilesRead(FilesPath, Products);
                if (filesPaths.Count == 0)                                                                                                           // Проверка наличия файлов в списке для обработки
                {
                    Log.Message("Список файлов для обработки пуст!", null, Log.MsgType.Message);                                                     // Запись сообщения об ошибке в журнал
                    Log.Message(
                        "Проверьте корректность файла настроек и работоспособность службы DIM!"
                        , null, Log.MsgType.Message);                                                                                                // Запись сообщения об ошибке в журнал
                    serviceTimer.Enabled = true;
                    Log.Message("Запуск таймера для корректного продолжения работы службы");
                }
                else
                {
                    Log.Message("Список файлов для обработки успешно создан!");                                                                      // Запись сообщения в журнал
                    Log.Message("Будет обработано: " + filesPaths.Count + " .xml-файлов");                                                           // Запись сообщения в журнал
                    foreach (string file in filesPaths)                                                                                              // Циклический перебор всех файлов в списке
                    {
                        passportData = new xmlData();                                                                                                // Очистка объекта перед чтением данных из .xml-файла
                        XmlDataRead(file, ref passportData);                                                                                         // Чтение данных из .xml-файла
                        Log.Message("");                                                                                                             // Запись пустой строки (разделителя) в журнал
                        Log.Message("Обработан паспорт №" + passportData.Name);                                                                      // Запись сообщения в журнал
                        Log.Message("Дата создания паспорта: " + passportData.Date);                                                                 // Запись сообщения в журнал
                        Log.Message("Продукт:" + passportData.Product);                                                                              // Запись сообщения в журнал
                        Log.Message("");                                                                                                             // Запись пустой строки (разделителя) в журнал
                        string[] testIDs = IniFile.GetKeyNames(passportData.Product);                                                                // Загрузка всех идентификаторов показателей качества из файла настроек
                        if (testIDs.Length != 0)
                        {
                            Log.Message("Загружены настройки для продукта: " + passportData.Product);                                                // Запись сообщения в журнал
                            Log.Message("Поиск совпадающих показателей качества");                                                                   // Запись сообщения в журнал
                            string[] tests = passportData.TestID.Intersect(testIDs).ToArray();                                                       // Поиск совпадающих элементов в двух массивах при помощи Linq-функции
                            Log.Message("Будет записано в PI System: "
                                + (tests.Length + 2) + " показателей качества");                                                                     // Запись сообщения в журнал

                            string piTag = IniFile.Read(passportData.Product, "Name");                                                               // Чтение тэга соответствующего идентификатору показателя качества
                            string piTimestamp = Convert.ToString(DateTime.Parse(passportData.Date));                                                // Временная метка для записи в тэг (двойная конверсия нужна для корректного разбора даты)
                            string piValue = passportData.Name;
                            Log.Message("В тэг " + piTag + " записано значение: " + piValue
                                    + " на временную метку " + piTimestamp);                                                                         // Запись сообщения в журнал
                            SaveDataToPI(PIServer, piTag, piTimestamp, piValue);                                                                     // Запись значения в тэг
                            piTag = IniFile.Read(passportData.Product, "Product");                                                                   // Чтение тэга соответствующего идентификатору показателя качества
                            piValue = passportData.ProductName;
                            SaveDataToPI(PIServer, piTag, piTimestamp, piValue);                                                                     // Запись значения в тэг
                            Log.Message("В тэг " + piTag + " записано значение: " + piValue
                                    + " на временную метку " + piTimestamp);                                                                         // Запись сообщения в журнал
                            foreach (string test in tests)                                                                                           // Циклический перебор всех совпадающих показателей качества
                            {
                                string[] iniString = IniFile.Read(passportData.Product,                                                              // Чтение строки из файла настроек соответствующей идентификатору показателя качества
                                    test).Split(',');                                                                                                // Разделение строки на название тэга и цифру обозначающую тип знпчения для записи в тэг
                                piTag = iniString[0];                                                                                                // Сохраненеие названия тэга в переменную
                                switch (iniString[1])                                                                                                // Выбор типа значения, в зависисмости от цифры в файле настроек
                                {
                                    case "1":                                                                                                        // Чтение значения (Value) для записи в тэг
                                        piValue = passportData
                                            .TestValue[Array.IndexOf(passportData.TestID, test)];                                                    // Значение для записи в тэг
                                        break;
                                    case "2":                                                                                                        // Чтение значения для отчета (ReportValue) для записи в тэг
                                        piValue = passportData
                                            .TestReportValue[Array.IndexOf(passportData.TestID, test)];                                              // Значение для записи в тэг
                                        break;
                                }
                                try
                                {
                                    SaveDataToPI(PIServer, piTag, piTimestamp, piValue);                                                             // Запись значения в тэг
                                    Log.Message("В тэг " + piTag + " записано значение: " + piValue
                                                    + " на временную метку " + piTimestamp);                                                         // Запись сообщения в журнал
                                }
                                catch (Exception ex)
                                {
                                    Log.Message("Ошибка записи данных в тэг:" + piTag, ex, Log.MsgType.Error);
                                }
                            }
                            CsvDataRead(file);                                                                                                       // Обработка .csv-файлов (Если включена в настройках)
                            DeleteXmlFile(file);                                                                                                     // Вызов процедуры удаления файла             
                        }
                        else
                        {
                            Log.Message("Продукт/тэги показателей отсутствуют в файле настроек,", null, Log.MsgType.Error);
                            Log.Message("текущий файл обработан не будет!", null, Log.MsgType.Error);
                        }
                    }
                }
            }
            catch (Exception ex)                                                                                                                     // Обработка ошибок
            {
                Log.Message("Ошибка в основном цикле программы: ", ex, Log.MsgType.Error);                                                           // Запись сообщения об ошибке в журнал
            }
            filesPaths.Clear();
            Log.Message("Очистка списка обработанных .xml-файлов");                                                                                  // Запись сообщения в журнал                                                    
            Log.Message("Завершение цикла обработки .xml-файлов");                                                                                   // Запись сообщения в журнал
            serviceTimer.Enabled = true;                                                                                                             // Повторный запуск таймера
            Log.Message("Запуск таймера для корректного продолжения работы службы");                                                                 // Запись сообщения в журнал
        }

        /// <summary>
        /// Процедура чтения данных из .csv-файлов.
        /// </summary>
        private static void CsvDataRead(string file)
        {
            var Bit = (Flags & (1 << 0)) != 0;
            if (Bit)
            {
                bool Tanks = false;
                string Place = "";
                string Batch = "";
                string piTag = "";
                string piValue = "";
                string piTimestamp = "";
                
                Log.Message("Запуск обработки .csv-файлов...");
                string fileDirectory = Path.GetDirectoryName(file);                                                                      // Получение пути к папке из которой производится чтение текущего файла
                string[] fileNames = Directory.GetFiles(fileDirectory, "*.csv");                                                         // Получение имени csv-файла, если он есть в папке
                if (fileNames.Length > 0)                                                                                                // Проверка на наличе имени csv-файла, который будем парсить
                {
                    Log.Message("Найден файл для обработки: " + fileNames[0]);                                                           // Запись сообщения в журнал
                    foreach (string line in File.ReadLines(fileNames[0],
                        Encoding.GetEncoding(1251)))
                    {
                        if (line.Contains("Цистерны:"))                                                                                  // Поиск строки "Цистерны:" в файле
                            Tanks = true;                                                                                                // Установка флага
                        if (line.Contains("есто отбор") && !line.Contains("наличие"))                                                    // Поиск подстроки "Место отбора", при условии, что в ней отсутствует подстрока "наличие"
                        {
                            string[] subLines = line.Split(';');
                            foreach (string subLine in subLines)
                            {
                                if (!string.IsNullOrEmpty(subLine) && !subLine.Contains("есто отбор") && !subLine.Contains("наличие"))
                                {
                                    Place = subLine;
                                }
                            }
                        }
                        if (line.Contains("Одорант"))                                                                                    // Поиск подстроки "Одорант" (Для паспортов на пропаны)
                        {
                            string[] subLines = line.Split(';');
                            foreach (string subLine in subLines)
                            {
                                if (!string.IsNullOrEmpty(subLine) && !subLine.Contains("Одорант"))
                                {
                                    Place = subLine;
                                }
                            }
                        }
                        if (line.Contains("артия"))                                                                                      // Поиск строки "артия" в файле
                        {
                            string[] subLines = line.Split(';');
                            foreach (string subLine in subLines)
                            {
                                if (!string.IsNullOrEmpty(subLine) && subLine.All(char.IsDigit))
                                {
                                    Batch = subLine;
                                }
                            }
                        }

                    }
                }
                if (Tanks)                                                                                                               //
                    Place += ", Налив";
                Log.Message("Установлено место отбора: " + Place);                                                                       // Запись сообщения в журнал
                try
                {
                    piTag = IniFile.Read(passportData.Product, "Place");                                                                 // Чтение тэга соответствующего идентификатору показателя качества
                    piValue = Place;                                                                                                     // Подготовка значения к записи в тэг
                    piTimestamp = Convert.ToString(DateTime.Parse(passportData.Date));
                    SaveDataToPI(PIServer, piTag, piTimestamp, piValue);                                                                 // Запись значения в тэг
                    Log.Message("В тэг " + piTag + " записано значение: " + piValue
                                    + " на временную метку " + piTimestamp);                                                             // Запись сообщения в журнал
                    piTag = IniFile.Read(passportData.Product, "Batch");
                    piValue = Batch;
                    SaveDataToPI(PIServer, piTag, piTimestamp, piValue);
                    Log.Message("В тэг " + piTag + " записано значение: " + piValue
                                    + " на временную метку " + piTimestamp);
                }
                catch (Exception ex)
                {
                    Log.Message("Ошибка записи данных в тэг:" + piTag, ex, Log.MsgType.Error);
                }
            }
        }

        /// <summary>
        /// Процедура вывода стартового сообщения.
        /// </summary>
        private static void ShowStartMessage()
        {
            Log.Message("", null, Log.MsgType.Message);
            Log.Message("§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§", null, Log.MsgType.Message);
            Log.Message("§§§§§§§§§§§§§§§§§§§   Запуск приложения \"GPI Xml Service\"  §§§§§§§§§§§§§§§§§§§§§", null, Log.MsgType.Message);
            Log.Message("§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§§", null, Log.MsgType.Message);
            Log.Message("", null, Log.MsgType.Message);
        }

        /// <summary>  
        /// Удаление директории в которой лежит обработанный .xml-файл.  
        /// </summary>  
        /// <param name="FilePath">Полный путь к файлу.</param>
        private static void DeleteXmlFile(string FilePath)
        {
            try
            {
                Directory.Delete(Path.GetDirectoryName(FilePath), true);                            // Удаление папки со всем содержимым
                Log.Message("Удаление папки: '" + 
                    Path.GetDirectoryName(FilePath) + "'", null, Log.MsgType.Message);              // Запись сообщения в журнал
            }
            catch (Exception ex)                                                                    // Обработка ошибок
            {
                Log.Message("Ошибка удаления файлов: ", ex, Log.MsgType.Error);                     // Запись сообщения об ошибке в журнал
            }
        }

        /// <summary>
        /// Процедура записи значения тэга с указанной временной меткой.
        /// </summary>
        /// <param name="Server">Название или адрес PI Server'а.</param>
        /// <param name="Tag">Название тэга.</param>
        /// <param name="Timestamp">Временная метка на которую бедт записано значение.</param>
        /// <param name="Value">Значение для записи в тэг.</param>
        private static void SaveDataToPI(string Server, string Tag, string Timestamp, string Value)
        {
            try
            {
                PISDK.PISDK piSDK = new PISDK.PISDK();                                                  // Создание объекта для доступа к PI SDK
                PISDK.Server piServer = piSDK.Servers[Server];                                          // Подключение к PI Server'у
                PISDK.PIPoint piPoint = piServer.PIPoints[Tag];                                         // Назначение текущего тэга
                DateTime dateTime = DateTime.Parse(Timestamp);                                          // Назначение временной метки
                piPoint.Data.UpdateValue(Value, dateTime,
                    PISDK.DataMergeConstants.dmReplaceDuplicates, null);                                // Сохранение значения в тэг
            }
            catch (Exception ex)
            {
                Log.Message("Ошибка в процедуре SaveDataToPI: ", ex, Log.MsgType.Error);                // Запись сообщения об ошибке в журнал
            }
            
        }

        /// <summary>
        /// Процедура последовательного поиска файлов по всем продуктам
        /// </summary>
        /// <param name="FilesPath">Путь к базовой директории, с которой начинается поиск.</param>
        /// <param name="Product">Список продуктов разделенных запятой, по которым будет осуществляться поиск.</param>
        private static void XmlFilesRead(string FilesPath, string Product)
        {
            try
            {
                String[] Products = Product.Split(',');                                                 // Разделение строки продуктов и запись отдельных строк в массив
                foreach (var product in Products)                                                       // Последовательный перебор всех продуктов в массиве
                    XmlFilesFind(new DirectoryInfo(FilesPath), "*" + product + "*.xml", true);          // Вызов процедуры рекурсивного поиска .xml-файлов
            }
            catch (Exception ex)
            {
                Log.Message("Ошибка в процедуре XmlFilesRead: ", ex, Log.MsgType.Error);                // Запись сообщения об ошибке в журнал
            }
            
        }

        /// <summary>
        /// Процедура рекурсивного поиска файлов внутри базового каталога на основе маски (паттерна)
        /// </summary>
        /// <param name="Directory">Путь к базовой директории, с которой начинается поиск.</param>
        /// <param name="Pattern">Маска для фильтрации имен файлов.</param>
        /// <param name="Recursive">Поддержка рекурсивного вызова процедуры для обработки вложенных директорий.</param>
        private static void XmlFilesFind(DirectoryInfo Directory, string Pattern, bool Recursive)
        {
            try
            {
                foreach (FileInfo file in Directory.GetFiles(Pattern))                                  // Циклический перебор файлов внутри директории
                {
                    filesPaths.Add(file.FullName);                                                      // Добавление пути к файлу в массив, если он подходит по критериям (маске)
                }
                if (Recursive)                                                                          // Проверка флага поддержки рекурсии, если включен, то
                {
                    foreach (DirectoryInfo subDirectory in Directory.GetDirectories())                  // Циклический перебор подпапок внутри папки
                    {
                        XmlFilesFind(subDirectory, Pattern, Recursive);                                 // Рекурсивный вызов самой себя для добавления пути к файлу в массив
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Message("Ошибка в процедуре XmlFilesFind: ", ex, Log.MsgType.Error);            // Запись сообщения об ошибке в журнал
            }
        }

        /// <summary>
        /// Процедура для чтения данных из .xml-файла и записи данных в объект xmlData
        /// </summary>
        /// <param name="xmlFilePath">Полный путь к .xml-файлу.</param>
        /// <param name="passportData">Ссылка на объект типа xmlData для записи данных.</param>
        private static void XmlDataRead(string xmlFilePath, ref xmlData passportData)
        {
            try
            {
                int k = 0;
                XmlDocument xmlDocument = new XmlDocument();                                            // Создание объекта для загрузки .xml-файла
                xmlDocument.Load(xmlFilePath);                                                          // Загрузка .xml-файл находящийся по пути xmlFilePath
                XmlElement xmlRoot = xmlDocument.DocumentElement;                                       // Создание объекта с указателем на корневой элемент .xml-файла
                foreach (XmlNode xmlNode in xmlRoot)                                                    // Циклический перебор всех элементов .xml-файла начиная с корневого
                {
                    if (xmlNode.Name == "Name")                                                         // Если найден номера паспорта, то
                    {
                        string passportName = xmlNode.InnerText;                                        // Сохранение номера паспорта в переменную passportName
                        passportData.Name = passportName;                                               // Сохранение номера паспорта в массив passportData
                        int strPos = passportName.LastIndexOf("-") + 1;                                 // Определение позиции последнего по счету символа "-" в номере паспорта
                        string productName = passportName.Substring(strPos);                            // Выделение названия продукта из номера паспорта
                        passportData.Product = productName;                                             // Сохранение названия продукта в массив passportData
                    }

                    if (xmlNode.Name == "Product")                                                      // Если найдена дата создания паспорта, то
                    {
                        foreach (XmlNode xmlChildNode in xmlNode.ChildNodes)                            // Циклический перебор всех показателей
                        {
                            if (xmlChildNode.Name == "Name")                                            // Если найдено название показателя, то
                            {
                                passportData.ProductName = xmlChildNode.InnerText;                      // Сохранение названия показателя в массив passportData
                            }
                        }
                    }

                    if (xmlNode.Name == "CreationDate")                                                 // Если найдена дата создания паспорта, то
                    {
                        passportData.Date = xmlNode.InnerText;                                          // Сохранение даты создания паспорта в массив passportData
                    }

                    if (xmlNode.Name == "Tests")                                                        // Если найден корневой элемент блока измеренных показателей, то
                    {
                        int xmlNodes = xmlNode.ChildNodes.Count;                                        // Сохранение количества измеренных показателей в переменной xmlNodes
                        passportData.TestID = new string[xmlNodes];                                     // Выделение памяти для массива идентификаторов показателей
                        passportData.TestName = new string[xmlNodes];                                   // Выделение памяти для массива названий показателей
                        passportData.TestValue = new string[xmlNodes];                                  // Выделение памяти для массива значений показателей
                        passportData.TestReportValue = new string[xmlNodes];                            // Выделение памяти для массива значений показателей
                        foreach (XmlNode xmlChildNode in xmlNode.ChildNodes)                            // Циклический перебор всех показателей
                        {
                            if (xmlChildNode.Name == "QcfGnTestInfo")                                   // Если найден корневой элемент блока
                            {
                                foreach (XmlNode xmlChildChildNode in xmlChildNode.ChildNodes)          // Циклический перебор параметров текущего показателя
                                {
                                    if (xmlChildChildNode.Name == "TestId")                             // Если найден идентификатор показателя
                                    {
                                        k += 1;                                                         // Инкрементация счетчика показателей
                                        passportData.TestID[k - 1] = xmlChildChildNode.InnerText;       // Сохранение идентификатора показателя в массив passportData
                                    }

                                    if (xmlChildChildNode.Name == "Name")                               // Если найдено название показателя, то
                                    {
                                        passportData.TestName[k - 1] = xmlChildChildNode.InnerText;     // Сохранение названия показателя в массив passportData
                                    }

                                    if (xmlChildChildNode.Name == "Value")                              // Если найдено значение показателя, то
                                    {
                                        passportData.TestValue[k - 1] = xmlChildChildNode.InnerText;    // Сохранение значения показателя в массив passportData
                                    }

                                    if (xmlChildChildNode.Name == "ReportValue")                        // Если найдено значение для отчета (ReportValue), то
                                    {
                                        passportData.TestReportValue[k - 1] = 
                                            xmlChildChildNode.InnerText;                                // Сохранение значения для отчета (ReportValue) показателя в массив passportData
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Message("Ошибка в процедуре XmlDataRead: ", ex, Log.MsgType.Error);            // Запись сообщения об ошибке в журнал
            }
        }
    }
}

/// <summary>
/// Класс описывающий структуру для хранения данных загруженных из .xml-файла
/// </summary>
public class xmlData
{
    public string Name;                                                                         // Номер паспорта
    public string Date;                                                                         // Дата создания паспорта
    public string Product;                                                                      // Короткое название продукта
    public string ProductName;                                                                  // Полное название продукта из .xml-файла

    public string[] TestID;                                                                     // Массив идентификаторов показателей
    public string[] TestName;                                                                   // Массив названий показателей
    public string[] TestValue;                                                                  // Массив значений показателей
    public string[] TestReportValue;                                                            // Массив значений для отчета (ReportValue)
}