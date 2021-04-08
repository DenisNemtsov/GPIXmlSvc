# GPIXmlSvc v1.8
##### Gazprom Inform Xml Service



Небольшое приложение в виде службы Windows разработанное для ООО "Газпром переработка" и предназначенное для извлечения данных из файлов, паспортов качества генерируемых ЛИМС и записи полученных данных в тэги PI Server'а ДУ ИУС П П.



#### Функциональные возможности:

1. Приложение может быть запущено как в интерактивном режиме, т.е. как обычное консольное приложение, так и в режиме службы Windows. Второй 
режим запуска требует ресгистрации службы в ОС.
2. Возможна самомстоятельная регистрация приложения как службы Windows при помощи команды: "**GPIXmlSvc.exe --install**" (без кавычек). Удаление службы можно произвести при помощи команды: "**GPIXmlSvc.exe --uninstall**" (без кавычек).
3. Программа не имеет графического интерфейса, поэтому все настройки осуществляются при помощи корректировки файла "**GPIXmlSvc.ini**", краткое описание которого содержится в разделе "**Настройка приложения**".



#### Настройка приложения:

##### I. 	**Раздел "Settings"**

​	1). Пункт "Logging" - Может принимать следующие значения "max" - *минимальная фильтрация и максимальное логгирование*, "min" - *выдача только  системных сообщений (SysMsg, см. исходный код)*, "err" - *выдача только сообщений об ошибках*.
​	2). Пункт "Interval" - Интервал обработки входящих файлов в минутах.

##### II.	**Разделы продуктов ([ПРОДУКТ1], [ПРОДУКТ2], [ПРОДУКТ3] и т.д.)**

​	1). Пункт любой - После имени тэга указывается тип значения, которое необходимо взять, 1-*берем из значения (Value)*, 2-*берем с формы паспорта (ReportValue)*, например:
​		**[ШФЛУ]**
​		**125 = "SZSK:Pasport.SHFLU:Q.C1::AI:Q:::3,1"**



#### Известные проблемы:

1. Отсутствует ограничение размера файла журнала (лога), что может привести к его чрезмерному разрастанию. будет исправлено в следующих версиях.



#### История версий:

|       Дата       | Описание                                                     |
| :--------------: | :----------------------------------------------------------- |
| 29.11.2017(v1.1) | Исправлена ошибка очистки списка обрабатываемых файлов.      |
| 29.01.2017(v1.2) | Добавлена обработка .csv-файлов и запись мест отбора проб в тэги. |
|                  | Изменен формат конфигурационного файла, добавлен параметр "Place". |
|                  | Изменен обработчик ошибок основного цикла, теперь при получении ошибки записи данных в тэг, он будет пропущен, а обработка файла продолжена. |
| 30.01.2017(v1.2) | Добавлена обработка типа значения читаемого из .xml-файла с включением поддержки в файле настроек (Цифра после названия тэга). |
| 04.04.2018(v1.3) | Проект полностью пересобран, добавлена возможность установки службы при помощи передачи параметров исполняемому файлу. |
|                  | Исправлена ошибка вызывавшая сбой при чтении .xml-файла.     |
| 25.05.2018(v1.3) | Дополнено описание пунктов файла настроек.                   |
|                  | Добавлен анализ наличия номеров цистерн в паспорте и корректировка места отбора в случае их наличия (К месту отбора добавляется строка ", Налив"). |
| 28.05.2018(v1.3) | Исправлена ошибка при формировании места отбора.             |
| 11.04.2019(v1.4) | Добавлены обработчики ошибок во все значимые процедуры.      |
| 28.08.2019(v1.5) | Добавлено чтение номера партии из .csv-файла и запись в тэг. |
| 09.10.2019(v1.6) | Исправлен алгоритм разбора паспортов в новом формате.        |
|                  | Добавлена проверка наличия продукта в файле настроек.        |
| 20.08.2020(v1.7) | Добавлена возможность отключения парсинга .csv-файлов, для чего необходимо сбросить нулевой бит переменной Flags в файле настроек. |
|                  | Исправлены системные сообщения в соответсвии с пожеланиями пользователей. |
| 03.02.2021(v1.8) | Изменен алгоритм поиска места отбора в .csv-файле, добавлена обработка нестандартных паспортов на пропаны. |