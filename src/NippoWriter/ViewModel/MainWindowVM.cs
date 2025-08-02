using System.IO;
using System.Collections.ObjectModel;
using NippoWriter.Model;
using NippoWriter.View;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NippoWriter.ViewModel
{

    class MainWindowVM : INotifyPropertyChanged
    {
        //===== プロパティ ===== //
        private SharePointListsService ListsService { get; set; }
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
        public DateTime SelectedDate { get; set; }
        private string _fileName { get; set; } = "";
        public string FileName
        {
            get { return _fileName; }
            set
            {
                _fileName = value;
                OnPropertyChanged();
            }
        }
        private string _filePath { get; set; } = "";
        public string FilePath
        {
            get { return _filePath; }
            set
            {
                FileName = Path.GetFileNameWithoutExtension(value);
                _filePath = value;
            }
        }

        // ===== コマンド ===== //
        public DelegateCommand SendDailyReport { get; set; }
        public DelegateCommand ShowUserSettingDialog { get; set; }
        //public DelegateCommand CreateTemplate { get; set; }
        public DelegateCommand SelectPrevDate { get; set; }
        public DelegateCommand SelectNextDate { get; set; }

        //===== コンストラクタ ===== //
        public MainWindowVM()
        {
            // 設定ファイルにSiteUrlとDestDirectoryがなければ初期設定ウィンドウを表示
            UserSettings userSettings = UserSettings.GetUserSettings();
            if (userSettings.SiteUrl == string.Empty || userSettings.DestDirectory == string.Empty)
            {
                UserSettingDialog dialog = new();
                bool? result = dialog.ShowDialog();
            }

            // コマンドを追加
            SendDailyReport = new DelegateCommand(SendDailyReportExecute, SendDailyReportCanExecute);
            ShowUserSettingDialog = new DelegateCommand(ShowUserSettingDialogExecute);
            SelectPrevDate = new DelegateCommand(SelectPrevDateExecute);
            SelectNextDate = new DelegateCommand(SelectNextDateExecute, SelectNextDateCanExecute);

        }

        /// <summary>
        /// デバイスコードフローによる認証を利用。（EntraIDのアプリ登録が必要）
        /// 非同期処理でSharePointListsServiceインスタンスを取得し、起動時の処理を行う。
        /// Loadedイベントハンドラから呼び出す。
        /// </summary>
        public async Task InitializeAsync()
        {
            ListsService = await SharePointListsService.CreateAsync();
            InitializeReport();
        }

        /// <summary>
        /// EntraIDのアプリ登録が不要な認証を利用。
        /// SharePointListsServiceインスタンスを取得し、起動時の処理を行う。
        /// 認証方法の参考：<see href="https://akennel.com/snippets/web-login-pnp"/>
        /// </summary>
        public void Initialize()
        {
            ListsService = new SharePointListsService();
            InitializeReport();
        }

        /// <summary>
        /// mdファイルを読み込みor新規作成して、Reportを初期化する。
        /// </summary>
        private void InitializeReport()
        {
            SelectedDate = DateTime.Now;

            // フィールド情報のみからDailyReportModelインスタンスを作成
            DailyReportModel report = ListsService.GetReportFields();

            // mdファイル読み込み。なければ新規作成
            DateFileMapSettings dateFileMap = DateFileMapSettings.GetDateFileMapSettings();
            string todayKey = SelectedDate.ToString("yyyyMMdd");
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

                // 日付とファイルの紐づけ情報を保存
                dateFileMap.Map.Add(todayKey, filePath);
                DateFileMapSettings.StoreDateFileMapSettings(dateFileMap);
            }

            FilePath = filePath;

            // 今日のListsアイテムを読み込みReportに反映する
            DailyReportModel? todayReport = ListsService.GetMyReport(SelectedDate);
            if (todayReport != null) report = MergeReportContents(report, todayReport);

            Report.Add(report);
        }

        // ===== コマンド Execute・CanExecute ===== //
        // SPO送信
        /// <summary>
        /// SPOに現在のReportを送信する
        /// </summary>
        private void SendDailyReportExecute()
        {
            if (Report.Count == 0) return;
            int id = ListsService.Send(Report[0]);
            if (id > 0) MessageBox.Show("SPOに送信しました。", "Nippo Wirter");
            Report[0].Id = id;

        }
        /// <summary>
        /// 「送信」ボタンの有効・無効切り替え
        /// </summary>
        private bool SendDailyReportCanExecute()
        {
            return Report.Count > 0;
        }

        // 日付選択
        /// <summary>
        /// 前日に切り替え
        /// </summary>
        private void SelectPrevDateExecute()
        {
            SelectedDate = SelectedDate.AddDays(-1);
            ChangeDate();
        }
        /// <summary>
        /// 翌日に切り替え
        /// </summary>
        private void SelectNextDateExecute()
        {
            SelectedDate = SelectedDate.AddDays(1);
            ChangeDate();
        }
        /// <summary>
        /// 翌日に切り替え可能・不可能切り替え
        /// </summary>
        private bool SelectNextDateCanExecute()
        {
            bool canExecute = true;
            if (SelectedDate.Date == DateTime.Now.Date) canExecute = false;
            return canExecute;
        }

        // ユーザ設定
        /// <summary>
        /// ユーザ設定画面を開く
        /// </summary>
        private void ShowUserSettingDialogExecute()
        {
            var dialog = new UserSettingDialog();
            bool? result = dialog.ShowDialog();
        }

        // ===== イベントハンドラから呼ぶメソッド ===== // 
        /// <summary>
        /// 現在の内容をmdファイルとして保存する。
        /// 入力フォームのイベントハンドラから呼び出される。
        /// </summary>
        public void StoreMdFileExecute()
        {
            if (Report.Count == 0) return;
            string content = MarkdownFileService.CreateMdContent(Report[0], FileName);
            MarkdownFileService.StoreMdFile(FilePath, content);
        }

        /// <summary>
        /// 対応するmdファイルから内容を読み取る。
        /// 入力フォームのイベントハンドラから呼び出される。
        /// </summary>
        public void ReadMdFileExecute()
        {
            if (Report.Count == 0) return;
            DailyReportModel report = MarkdownFileService.ReadMdFile(Report[0], FilePath);
            Report.Remove(Report[0]);
            Report.Add(report);
        }

        // ==== このクラスで使用するprivateメソッド =====
        /// <summary>
        /// targetReportのコンテンツが空の部分をmergingReportのコンテンツで埋めて統合する。
        /// </summary>
        /// <param name="targetReport">マージを受けるReport</param> 
        /// <param name="mergingReport">対象にマージする追加のReport</param>
        /// <returns name="filledReport">空の部分をリストアイテムで満たした日報モデルインスタンス</returns>
        private DailyReportModel MergeReportContents(DailyReportModel targetReport, DailyReportModel mergingReport)
        {
            DailyReportModel mergedReport = targetReport;

            // Idを反映
            mergedReport.Id = mergingReport.Id;

            // 空のセクションがあれば、アイテムの内容を反映
            mergedReport.Fields.ForEach(field =>
            {
                if (string.IsNullOrWhiteSpace(field.Content))
                {
                    foreach (ReportField mergingField in mergingReport.Fields)
                    {
                        if (field.InternalName == mergingField.InternalName)
                        {
                            field.Content = mergingField.Content;
                        }
                    }
                }
            });

            return mergingReport;
        }
        /// <summary>
        /// 日付を切り替えたときに、mdファイルとその日のリストアイテムを読み込む
        /// </summary>
        private void ChangeDate()
        {
            var report = ListsService.GetReportFields();

            if (Report.Count > 0) Report.Remove(Report[0]);

            // 選択した日付のmdファイルと日報を取得
            DateFileMapSettings dateFileMap = DateFileMapSettings.GetDateFileMapSettings();
            string key = SelectedDate.ToString("yyyyMMdd");
            string filePath = string.Empty;
            if (dateFileMap.Map.ContainsKey(key))
            {
                // markdown読み込み
                filePath = dateFileMap.Map[key];
                report = MarkdownFileService.ReadMdFile(report, filePath);
                Report.Add(report);
            }

            FilePath = filePath;

            // 今日のListsアイテムを読み込みReportに反映する
            DailyReportModel? todayReport = ListsService.GetMyReport(SelectedDate);
            if (todayReport != null) MergeReportContents(report, todayReport);

            if (Report.Count == 0) FileName = $"{SelectedDate:yyyy-MM-dd}のファイルがありません";
            IsReportExist = Report.Count > 0;
        }

        /// <summary>
        /// データ変更通知ようの汎用メソッド
        /// </summary>
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