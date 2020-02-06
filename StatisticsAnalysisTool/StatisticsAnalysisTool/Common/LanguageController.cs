namespace StatisticsAnalysisTool.Common
{
    using Properties;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Windows;
    using System.Windows.Data;
    using System.Xml;

    public class LanguageController : IValueConverter
    {
        public static string CurrentLanguage;
        public static CultureInfo DefaultCultureInfo = (CurrentLanguage != null) ? new CultureInfo(CurrentLanguage) : new CultureInfo("en-US");
        public readonly List<FileInfo> FileInfos = new List<FileInfo>();

        public bool IsTranslationPossible;

        private Dictionary<string, string> _translations;

        public LanguageController()
        {
            if(SetFirstLanguageIfPossible())
            {
                IsTranslationPossible = true;
            }
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => Translation((string)parameter);

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public string Translation(string key)
        {
            try
            {
                if (_translations.TryGetValue(key, out var value))
                    return (!string.IsNullOrEmpty(value)) ? value : key;
            }
            catch (Exception ex)
            {
                Debug.Print(ex.ToString());
            }
            return key;
        }

        public bool SetFirstLanguageIfPossible()
        {
            InitializeLanguageFiles();

            if (SetLanguage(CultureInfo.CurrentCulture.Name))
                return true;

            if (SetLanguage(Settings.Default.CurrentLanguageCulture))
                return true;

            if (SetLanguage(FileInfos.FirstOrDefault()?.FileName))
                return true;

            return false;
        }

        public bool SetLanguage(string lang)
        {
            var fileInfo = (from fi in FileInfos where fi.FileName == lang select new FileInfo(fi.FileName, fi.FilePath)).FirstOrDefault();

            if (fileInfo == null)
                return false;

            ReadAndAddLanguageFile(fileInfo.FilePath);
            CurrentLanguage = fileInfo.FileName;
            return true;
        }

        private void ReadAndAddLanguageFile(string filePath)
        {
            _translations = null;
            _translations = new Dictionary<string, string>();
            var xmlReader = XmlReader.Create(filePath);
            while (xmlReader.Read())
            {
                if (xmlReader.Name == "translation" && xmlReader.HasAttributes)
                {
                    AddTranslationsToDictionary(xmlReader);
                }
            }
        }

        private void AddTranslationsToDictionary(XmlReader xmlReader)
        {
            while (xmlReader.MoveToNextAttribute())
            {
                if (_translations.ContainsKey(xmlReader.Value))
                {
                    MessageBox.Show($"{Translation("DOUBLE_VALUE_EXISTS_IN_THE_LANGUAGE_FILE")}: {xmlReader.Value}");
                }
                else if (xmlReader.Name == "name")
                {
                    _translations.Add(xmlReader.Value, xmlReader.ReadString());
                }
            }
        }

        public void InitializeLanguageFiles()
        {
            var languageFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Settings.Default.LanguageDirectoryName);

            if (Directory.Exists(languageFilePath))
            {
                var files = DirectoryController.GetFiles(languageFilePath, "*.xml");

                if (files == null)
                    return;

                foreach (var file in files)
                {
                    var fileNameWithoutExtension = new FileInfo(Path.GetFileNameWithoutExtension(file), file);
                    FileInfos.Add(fileNameWithoutExtension);
                }
            }
        }

        public class FileInfo
        {
            public string FileName { get; set; }
            public string FilePath { get; set; }
            public string EnglishName => CultureInfo.CreateSpecificCulture(FileName).EnglishName;
            public string NativeName => CultureInfo.CreateSpecificCulture(FileName).NativeName;

            public FileInfo() { }

            public FileInfo(string fileName, string filePath)
            {
                FileName = fileName;
                FilePath = filePath;
            }
        }
    }
}