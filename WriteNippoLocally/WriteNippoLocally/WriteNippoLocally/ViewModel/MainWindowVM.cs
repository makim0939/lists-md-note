using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WriteNippoLocally.Model;
using WriteNippoLocally.View;

namespace WriteNippoLocally.ViewModel
{

    class MainWindowVM
    {
        public ObservableCollection<DailyReportModel> Report { get; set; } = new ObservableCollection<DailyReportModel>();
        public DateTime SelectedDate { get; set; } = DateTime.Now; // 選択されている日付
        public string FilePath { get; set; } = "";

        //コマンド
        public DelegateCommand SendDailyReport { get; set; }
        public DelegateCommand StoreMdFile { get; set; }
        public DelegateCommand CreateTemplate { get; set; }
        public DelegateCommand SelectDate { get; set; }

        //コンストラクタ
        public MainWindowVM()
        {
            // 設定ファイルにSiteUrlとDestDirectoryがなければ初期設定ウィンドウを表示
            UserSettings userSettings = UtilsService.GetUserSettings();
            if(userSettings.SiteUrl == string.Empty || userSettings.DestDirectory == string.Empty)
            {
                InitSettingDialog dialog = new();
                bool? result = dialog.ShowDialog();
            }

            // フィールド情報のみからDailyReportModelインスタンスを作成
            DailyReportModel report = SharePointListsService.GetReportFields();

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
            DailyReportModel? todayReport = SharePointListsService.GetMyReport(DateTime.Now);
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

            // コマンドを追加
            SendDailyReport = new DelegateCommand(SendDailyReportExecute);
            StoreMdFile = new DelegateCommand(StoreMdFileExecute);
        }

        private void SendDailyReportExecute()
        {
            Report[0].Id = SharePointListsService.Send(Report[0]);
        }

        public void StoreMdFileExecute()
        {
            MarkdownFileService.storeMdFile(Report[0], this.FilePath);
        }

        public void ReadMdFileExecute()
        {
            DailyReportModel report = MarkdownFileService.ReadMdFile(Report[0], this.FilePath);
            this.Report.Remove(Report[0]);
            this.Report.Add(report);
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
    }
}
