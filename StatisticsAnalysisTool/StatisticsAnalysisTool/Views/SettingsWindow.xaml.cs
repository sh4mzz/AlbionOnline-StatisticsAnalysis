namespace StatisticsAnalysisTool.Views
{
    using Properties;
    using System.Windows;
    using System.Windows.Input;
    using LanguageController = Common.LanguageController;

    /// <summary>
    /// Interaktionslogik für SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow
    {
        public SettingsWindow()
        {
            InitializeComponent();
            InitializeSettings();
        }
        
        private void InitializeSettings()
        {
            // Language
            foreach (var langInfos in StatisticsAnalysisManager.LanguageController.FileInfos)
                CbLanguage.Items.Add(new LanguageController.FileInfo() { FileName = langInfos.FileName });

            CbLanguage.SelectedValue = LanguageController.CurrentLanguage;

            // Refresh rate
            CbRefreshRate.Items.Add(new RefreshRateStruct() {Name = StatisticsAnalysisManager.LanguageController.Translation("5_SECONDS"), Seconds = 5000});
            CbRefreshRate.Items.Add(new RefreshRateStruct() {Name = StatisticsAnalysisManager.LanguageController.Translation("10_SECONDS"), Seconds = 10000});
            CbRefreshRate.Items.Add(new RefreshRateStruct() {Name = StatisticsAnalysisManager.LanguageController.Translation("30_SECONDS"), Seconds = 30000});
            CbRefreshRate.Items.Add(new RefreshRateStruct() {Name = StatisticsAnalysisManager.LanguageController.Translation("60_SECONDS"), Seconds = 60000});
            CbRefreshRate.Items.Add(new RefreshRateStruct() {Name = StatisticsAnalysisManager.LanguageController.Translation("5_MINUTES"), Seconds = 300000});
            CbRefreshRate.SelectedValue = Settings.Default.RefreshRate;
            
            // Update item list by days
            CbUpdateItemListByDays.Items.Add(new UpdateItemListStruct() { Name = StatisticsAnalysisManager.LanguageController.Translation("EVERY_DAY"), Value = 1 });
            CbUpdateItemListByDays.Items.Add(new UpdateItemListStruct() { Name = StatisticsAnalysisManager.LanguageController.Translation("EVERY_3_DAYS"), Value = 3 });
            CbUpdateItemListByDays.Items.Add(new UpdateItemListStruct() { Name = StatisticsAnalysisManager.LanguageController.Translation("EVERY_7_DAYS"), Value = 7 });
            CbUpdateItemListByDays.Items.Add(new UpdateItemListStruct() { Name = StatisticsAnalysisManager.LanguageController.Translation("EVERY_14_DAYS"), Value = 14 });
            CbUpdateItemListByDays.Items.Add(new UpdateItemListStruct() { Name = StatisticsAnalysisManager.LanguageController.Translation("EVERY_28_DAYS"), Value = 28 });
            CbUpdateItemListByDays.SelectedValue = Settings.Default.UpdateItemListByDays;

            // ItemList source url
            TxtboxItemListSourceUrl.Text = Settings.Default.CurrentItemListSourceUrl;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

        private void MinimizeButton_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        private void Hotbar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            var refreshRateItem = (RefreshRateStruct)CbRefreshRate.SelectedItem;
            var updateItemListByDays = (UpdateItemListStruct)CbUpdateItemListByDays.SelectedItem;
            
            Settings.Default.RefreshRate = refreshRateItem.Seconds;

            Settings.Default.UpdateItemListByDays = updateItemListByDays.Value;
            Settings.Default.CurrentItemListSourceUrl = TxtboxItemListSourceUrl.Text;

            if (CbLanguage.SelectedItem is LanguageController.FileInfo langItem)
            {
                StatisticsAnalysisManager.LanguageController.SetLanguage(langItem.FileName);
                Settings.Default.CurrentLanguageCulture = langItem.FileName;
            }
            
            Close();
        }

        public struct RefreshRateStruct
        {
            public string Name { get; set; }
            public int Seconds { get; set; }
        }

        public struct UpdateItemListStruct
        {
            public string Name { get; set; }
            public int Value { get; set; }
        }

    }
}
