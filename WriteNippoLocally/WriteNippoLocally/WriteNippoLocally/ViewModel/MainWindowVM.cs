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

            // 今日のListsアイテム読み込み。なければ、フィールドのみ取得
            DailyReportModel? myReport = SharePointListsService.GetMyReport(DateTime.Now);
            if (myReport != null)
            {
                this.Report.Add(myReport);
            }
            else
            {
                // もとからあるアイテムを取得し、フィールドを設定する。
                DailyReportModel newReport = SharePointListsService.GetReportFields();
                this.Report.Add(newReport);
            }

            // mdファイル読み込み。なければ新規作成
            DateFileMapSettings dateFileMap = GetDateFileMapSettings();
            string todayKey = DateTime.Now.ToString("yyyyMMdd");
            if (dateFileMap.Map.ContainsKey(todayKey))
            {
                // markdown読み込み
                string filePath = dateFileMap.Map[todayKey];
                MarkdownFileService.ReadMdFile(Report[0], filePath);
                this.FilePath = filePath;
            }
            else
            {
                // markdown新規作成
                string mdContent = MarkdownFileService.CreateMdContent(Report[0]);
                string filePath = MarkdownFileService.CreateTodayMdFile(mdContent);

                string key = DateTime.Now.ToString("yyyyMMdd");
                dateFileMap.Map.Add(key, filePath);
                this.FilePath = filePath;
            }

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
