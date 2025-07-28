using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Shapes;
using static System.Windows.Forms.LinkLabel;

namespace WriteNippoLocally.Model
{
    internal  class MarkdownFileService
    {
        static MarkdownFileService() 
        {
        }

        public static void storeMdFile(DailyReportModel dailyReport, string filePath)
        {
            // 各フィールドの内容をmdファイルに上書き保存
            string content = CreateMdContent(dailyReport);

            File.WriteAllText(filePath, content);

            return;
        }

        // 今日の分のmdファイルの新規作成
        // 作成したファイルのパスを戻り値とする。
        public static string CreateTodayMdFile(string content)
        {

            UserSettings? settings = UserSettings.GetUserSettings();
            string fileName = settings.GetFileName(DateTime.Now);
            string filePath = @$"{settings.DestDirectory}\{fileName}.md";

            File.WriteAllText(filePath, content);

            return filePath;
        }

        // DailyReportModelからmarkdownファイルの中身を作成する。
        public static string CreateMdContent(DailyReportModel dailyReport)
        {
            string mdContent = string.Empty;

            // ファイル名をタイトルとする
            UserSettings? settings = UserSettings.GetUserSettings();
            string fileName = settings.GetFileName(DateTime.Now);

            mdContent += $"# {fileName}\n\n\n";

            // フィールドを追加する
            foreach (ReportField field in dailyReport.Fields)
            {
                mdContent += $"## {field.Title}\n";
                mdContent += $"{field.Content}\n\n";
            }

            return mdContent;
        }

        //mdファイルのセクションを解析し、DailyReportModel型に変換
        public static DailyReportModel ReadMdFile(DailyReportModel report, string filePath)
        {
            // mdファイル読み込み
            string[] lines = File.ReadAllLines(filePath);

            List<ReportField> sections = [];
            string heading = string.Empty;
            foreach (string line in lines)
            {
                if (line.StartsWith("##"))
                {
                    heading = line.Replace("#", "").Replace(" ", "");
                    // セクションを初期化
                    report.Fields.ForEach(
                    section =>
                    {
                        if (section.Title == heading)
                        {
                            section.Content = string.Empty;
                        }
                    });
                    continue;
                }

                report.Fields.ForEach(
                    section =>
                    {
                        if (section.Title == heading)
                        {
                            section.Content += $"{line}\n";
                        }
                    });
            }

            // 入力種別毎に調整
            report.Fields.ForEach(
                section =>
                {
                    switch (section.Type)
                    {
                        case "Text":
                            section.Content = section.Content.Replace("\n", " ");
                            break;
                        case "Note":
                            section.Content = section.Content.TrimEnd('\n');
                            break;
                        case "Choice":
                            section.Content = section.Content.Replace("\n", "");
                            break;
                        default:
                            break;
                    }
                });

            return report;
        }
    }
}
