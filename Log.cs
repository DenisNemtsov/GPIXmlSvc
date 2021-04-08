using System;
using System.IO;

namespace GPIXmlSvc
{
    class Log
    {
        public enum MsgStream { File = 1, Display, Everywhere };
        public enum MsgType { Message = 1, SysMsg, Error };
        public enum MsgLevel { Max = 1, Min, Err };
        
        public static MsgStream Stream;                                                                                                             // Поток для вывода журнала: 1 - выводить в файл, 2 - выводить в консоль, 3 - выводить на экран + в консоль
        public static MsgLevel Level;                                                                                                               // Уровень подробности журнала: Max - все сообщения, Min - сист. сообщения и ошибки, Err - только ошибки 
        public static string File;                                                                                                                  // Путь к файлу журнала

        /// <summary>  
        /// Функция для добавления события в лог. В зависимости от установленного потока лог может
        /// сохранятся на диск, выводится на экран или транслироваться в оба потока одновременно.  
        /// </summary>  
        /// <param name="Message">Текст сообщения добавляемого в журнал.</param>
        /// <param name="Ex">Необяз. параметр: Указатель на системное сообщение об ошибке.</param>
        /// <param name="Type">Необяз. параметр: Тип события добавляемого в журнал.</param>
        public static void Message(string Message, Exception Ex = null, MsgType Type = MsgType.Message)                                             // Тип сообщения: Message - Информационное сообщение, SysMsg - Системное сообщение, Error - Сообщение об ошибке
        {
            if (String.IsNullOrEmpty(File))
                File = AppDomain.CurrentDomain.BaseDirectory + 
                    System.IO.Path.GetFileNameWithoutExtension(System.Reflection.Assembly.GetExecutingAssembly().Location) +
                    ".log";
            
            if (Level == 0)                                                                                                                         // Проверка, установлен ли уровень логирования
                Level = MsgLevel.Max;                                                                                                               // Если нет, то выставляем уровень по умолчанию (Макс.)

            if (Stream == 0)                                                                                                                        // Проверка, установлен ли поток для вывода лога
                Stream = MsgStream.File;                                                                                                            // Если нет, то выставляем поток по умолчанию (Только в файл)

            StreamWriter streamWriter = new StreamWriter(File, true);
            switch (Stream)
            {
                case MsgStream.File:                                                                // Вывод сообщений только в файл
                    switch (Type)
                    {
                        case MsgType.Message:
                            if (Level == MsgLevel.Max)
                            {
                                if (String.IsNullOrEmpty(Message))
                                    streamWriter.WriteLine(DateTime.Now.ToString() + " Сообщение: --------------------------------------------------------------------------------");
                                else
                                    streamWriter.WriteLine(DateTime.Now.ToString() + " Сообщение: " + Message);
                            }
                            break;
                        case MsgType.SysMsg:
                            if (Level == MsgLevel.Max || Level == MsgLevel.Min)
                            {
                                if (String.IsNullOrEmpty(Message))
                                    streamWriter.WriteLine(DateTime.Now.ToString() + " Сист. сообщ.: ----------------------------------------------------------------------");
                                else
                                    streamWriter.WriteLine(DateTime.Now.ToString() + " Сист. сообщ.: " + Message);
                            }
                            break;
                        case MsgType.Error:
                            if (Level == MsgLevel.Max || Level == MsgLevel.Min || Level == MsgLevel.Err)
                            {
                                if (Ex == null)
                                    streamWriter.WriteLine(DateTime.Now.ToString() + " Ошибка:    " + Message);
                                else
                                    streamWriter.WriteLine(DateTime.Now.ToString() + " Ошибка:    " + Message + "/r/n" + Ex.Source.ToString().Trim() + "; " + Ex.Message.ToString().Trim());
                            }
                            break;
                        default:
                            if (Level == MsgLevel.Max)
                            {
                                if (String.IsNullOrEmpty(Message))
                                    streamWriter.WriteLine(DateTime.Now.ToString() + " Сообщение: --------------------------------------------------------------------------------");
                                else
                                    streamWriter.WriteLine(DateTime.Now.ToString() + " Сообщение: " + Message);
                            }
                            break;
                    }
                    streamWriter.Flush();
                    streamWriter.Close();
                    break;
                case MsgStream.Display:                                                                // Вывод сообщений только на экран
                    Console.OutputEncoding = System.Text.Encoding.GetEncoding(1251);
                    switch (Type)
                    {
                        case MsgType.Message:
                            if (Level == MsgLevel.Max)
                            {
                                if (String.IsNullOrEmpty(Message))
                                {
                                    Console.WriteLine("-------------------------------------------------------------------------------");
                                }
                                else
                                {
                                    Console.WriteLine("Сообщение: " + Message);
                                }
                            }
                            break;
                        case MsgType.SysMsg:
                            if (Level == MsgLevel.Max || Level == MsgLevel.Min)
                            {
                                if (String.IsNullOrEmpty(Message))
                                {
                                    Console.WriteLine("-------------------------------------------------------------------------------");
                                }
                                else
                                {
                                    Console.WriteLine("Сист. сообщ.: " + Message);
                                }
                            }
                            break;
                        case MsgType.Error:
                            if (Level == MsgLevel.Max || Level == MsgLevel.Min || Level == MsgLevel.Err)
                            {
                                if (Ex == null)
                                {
                                    Console.WriteLine("Ошибка:    " + Message);
                                }
                                else
                                {
                                    Console.WriteLine("Ошибка:    " + Message + "/r/n" + Ex.Source.ToString().Trim() + "; " + Ex.Message.ToString().Trim());
                                }
                            }
                            break;
                        default:
                            if (Level == MsgLevel.Max)
                            {
                                if (String.IsNullOrEmpty(Message))
                                {
                                    Console.WriteLine("-------------------------------------------------------------------------------");
                                }
                                else
                                {
                                    Console.WriteLine("Сообщение: " + Message);
                                }
                            }
                            break;
                    }
                    break;
                case MsgStream.Everywhere:                                                             // Вывод сообщений в файл и на экран
                    Console.OutputEncoding = System.Text.Encoding.GetEncoding(1251);
                    switch (Type)
                    {
                        case MsgType.Message:
                            if (Level == MsgLevel.Max)
                            {
                                if (String.IsNullOrEmpty(Message))
                                {
                                    streamWriter.WriteLine(DateTime.Now.ToString() + " Сообщение: --------------------------------------------------------------------------------");
                                    Console.WriteLine("-------------------------------------------------------------------------------");
                                }
                                else
                                {
                                    streamWriter.WriteLine(DateTime.Now.ToString() + " Сообщение: " + Message);
                                    Console.WriteLine(Message);
                                }
                            }
                            break;
                        case MsgType.SysMsg:
                            if (Level == MsgLevel.Max || Level == MsgLevel.Min)
                            {
                                if (String.IsNullOrEmpty(Message))
                                {
                                    streamWriter.WriteLine(DateTime.Now.ToString() + "----------------------------------------------------------------------");
                                    Console.WriteLine("-------------------------------------------------------------------------------");
                                }
                                else
                                {
                                    streamWriter.WriteLine(DateTime.Now.ToString() + " Сист. сообщ.: " + Message);
                                    Console.WriteLine(Message);
                                }
                            }
                            break;
                        case MsgType.Error:
                            if (Level == MsgLevel.Max || Level == MsgLevel.Min || Level == MsgLevel.Err)
                            {
                                if (Ex == null)
                                {
                                    streamWriter.WriteLine(DateTime.Now.ToString() + " Ошибка:    " + Message);
                                    Console.WriteLine(Message);
                                }
                                else
                                {
                                    streamWriter.WriteLine(DateTime.Now.ToString() + " Ошибка:    " + Message + "/r/n" + Ex.Source.ToString().Trim() + "; " + Ex.Message.ToString().Trim());
                                    Console.WriteLine(Message + "/r/n" + Ex.Source.ToString().Trim() + "; " + Ex.Message.ToString().Trim());
                                }
                            }
                            break;
                        default:
                            if (Level == MsgLevel.Max)
                            {
                                if (String.IsNullOrEmpty(Message))
                                {
                                    streamWriter.WriteLine(DateTime.Now.ToString() + " Сообщение: --------------------------------------------------------------------------------");
                                    Console.WriteLine("-------------------------------------------------------------------------------");
                                }
                                else
                                {
                                    streamWriter.WriteLine(DateTime.Now.ToString() + " Сообщение: " + Message);
                                    Console.WriteLine(Message);
                                }
                            }
                            break;
                    }
                    streamWriter.Flush();
                    streamWriter.Close();
                    break;
            }
        }
    }
}