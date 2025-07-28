using System;
using System.IO;
using System.Collections.ObjectModel;
using System.Text.Json;
using WriteNippoLocally.Model;
using WriteNippoLocally.View;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WriteNippoLocally.ViewModel
{

    class MainWindowVM : INotifyPropertyChanged
    {
        public ObservableCollection<DailyReportModel> Report { get; set; } = new ObservableCollection<DailyReportModel>();
        private bool _isReportExist { get; set; } = true;
        public bool IsReportExist
        {
            get { return _isReportExist; }
            set
            {
                _isReportExist = value;
                OnPropertyChanged();
            }
        }
        public DateTime _selectedDate = DateTime.Now;
        public DateTime SelectedDate
        { 
            get { return _selectedDate; }
            set
            {
                _selectedDate = value;
                OnPropertyChanged();
            }
        }

        public string FilePath { get; set; } = "";

        private SharePointListsService ListsService { get; set; }

        //コマンド
        public DelegateCommand SendDailyReport { get; set; }
        public DelegateCommand StoreMdFile { get; set; }
        public DelegateCommand ShowUserSettingDialog { get; set; }
        public DelegateCommand CreateTemplate { get; set; }
        public DelegateCommand SelectPrevDate { get; set; }
        public DelegateCommand SelectNextDate { get; set; }

        //コンストラクタ
        public MainWindowVM()
        {
            // 設定ファイルにSiteUrlとDestDirectoryがなければ初期設定ウィンドウを表示
            UserSettings userSettings = UserSettings.GetUserSettings();
            if (userSettings.SiteUrl == string.Empty || userSettings.DestDirectory == string.Empty)
            {
                InitSettingDialog dialog = new();
                bool? result = dialog.ShowDialog();
            }

            // コマンドを追加
            SendDailyReport = new DelegateCommand(SendDailyReportExecute);
            StoreMdFile = new DelegateCommand(StoreMdFileExecute);
            ShowUserSettingDialog = new DelegateCommand(ShowUserSettingDialogExecute);
            SelectPrevDate = new DelegateCommand(SelectPrevDateExecute, SelectPrevDateCanExecute);
            SelectNextDate = new DelegateCommand(SelectNextDateExecute, SelectNextDateCanExecute);
            
        }

        // 非同期初期化
        // MainWindowのLoadedイベントハンドラから呼び出す。
        // コンストラクタで非同期処理を行えないため。
        public async Task InitializeAsync()
        {
            ListsService = await SharePointListsService.CreateAsync();

            // フィールド情報のみからDailyReportModelインスタンスを作成
            DailyReportModel report = ListsService.GetReportFields();

            // mdファイル読み込み。なければ新規作成
            DateFileMapSettings dateFileMap = GetDateFileMapSettings();
            string todayKey = DateTime.Now.ToString("yyyyMMdd");
            string filePath = string.Empty;
            if (dateFileMap.Map.ContainsKey(todayKey))
            {
                // markdown読み込み
                filePath = dateFileMap.Map[todayKey];
                report = MarkdownFileService.ReadMdFile(report, filePath);
            }
            else
            {
                // markdown新規作成
                string mdContent = MarkdownFileService.CreateMdContent(report);
                filePath = MarkdownFileService.CreateTodayMdFile(mdContent);

                string key = DateTime.Now.ToString("yyyyMMdd");
                dateFileMap.Map.Add(key, filePath);
            }
            this.FilePath = filePath;

            // 日付とファイルパスを紐づける設定ファイルを更新
            string settingsJson = JsonSerializer.Serialize(dateFileMap);
            try
            {
                File.WriteAllText(@".\DateFileMapSettings.json", settingsJson);
            }
            catch (Exception ex)
            {
                Console.WriteLine("設定ファイル書き出しエラー");
                Console.WriteLine(ex.Message);
            }

            // 今日のListsアイテムを読み込みReportに反映する
            DailyReportModel? todayReport = ListsService.GetMyReport(DateTime.Now);
            if (todayReport != null)
            {
                // 空のセクションがあれば、アイテムの内容を反映
                for (int i = 0; i < report.Fields.Count; i++)
                {
                    if (string.IsNullOrWhiteSpace(report.Fields[i].Content))
                    {
                        report.Fields[i].Content = todayReport.Fields[i].Content;
                    }
                }
            }
            Report.Add(report);
        }

        private void SendDailyReportExecute()
        {
            if (Report.Count == 0) return;
            Report[0].Id = ListsService.Send(Report[0]);
        }

        public void StoreMdFileExecute()
        {
            if (Report.Count == 0) return;
            MarkdownFileService.storeMdFile(Report[0], this.FilePath);
        }

        public void ReadMdFileExecute()
        {
            if (Report.Count == 0) return;
            DailyReportModel report = MarkdownFileService.ReadMdFile(Report[0], this.FilePath);
            this.Report.Remove(Report[0]);
            this.Report.Add(report);
        }

        // SelectDateコマンド
        private void SelectPrevDateExecute()
        {
            SelectedDate = SelectedDate.AddDays(-1);
            ChangeDate();
        }
        private bool SelectPrevDateCanExecute()
        {
            return true;
        }
        private void SelectNextDateExecute()
        {
            SelectedDate = SelectedDate.AddDays(1);
            ChangeDate();
        }
        private bool SelectNextDateCanExecute()
        {
            bool canExecute = true;
            if (SelectedDate.Date == DateTime.Now.Date) canExecute = false;
            return canExecute;
        }

        private void ChangeDate()
        {
            var report = ListsService.GetReportFields();

            if (Report.Count > 0)
            {
                Report.Remove(Report[0]);
            }

            DateFileMapSettings dateFileMap = GetDateFileMapSettings();
            string key = SelectedDate.ToString("yyyyMMdd");
            // ファイルパスを更新
            FilePath = string.Empty;
            if (dateFileMap.Map.ContainsKey(key))
            {
                // markdown読み込み
                FilePath = dateFileMap.Map[key];
                report = MarkdownFileService.ReadMdFile(report, FilePath);
                Report.Add(report);
            }

            IsReportExist = Report.Count > 0;
        }

        private void ShowUserSettingDialogExecute()
        {
            var dialog = new UserSettingDialog();
            bool? result = dialog.ShowDialog();
        }

        private DateFileMapSettings GetDateFileMapSettings ()
        {
            DateFileMapSettings? dateFileMap;
            try
            {
                string settingsStr = File.ReadAllText(@".\DateFileMapSettings.json");
                dateFileMap = JsonSerializer.Deserialize<DateFileMapSettings>(settingsStr);
            }
            catch
            {
                dateFileMap = null;
            }

            if (dateFileMap == null)
            {
                // 読み込めなければデフォルト設定
                dateFileMap = new DateFileMapSettings();
            }

            return dateFileMap;
        }

        // データ変更通知用
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
