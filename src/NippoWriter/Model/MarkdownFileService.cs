using System.IO;

namespace NippoWriter.Model
{
    internal class MarkdownFileService
    {
        static MarkdownFileService()
        {
        }

        /// <summary>
        /// ファイルを上書き保存する
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="content"></param>
        public static void StoreMdFile(string filePath, string content)
        {

            File.WriteAllText(filePath, content);

            return;
        }

        /// <summary>
        /// 今日の分のmdファイルを作成し、そのファイルパスを返す
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public static string CreateTodayMdFile(string content)
        {

            UserSettings? settings = UserSettings.GetUserSettings();
            string fileName = settings.GetFileName(DateTime.Now);
            string filePath = @$"{settings.DestDirectory}\{fileName}.md";

            File.WriteAllText(filePath, content);

            return filePath;
        }

        /// <summary>
        /// reportを受け取り、mdファイルと合わせた文字列を返す。
        /// </summary>
        /// <param name="report"></param>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static string UpdateMdContent(DailyReportModel report, string filePath)
        {
            string mdContent = string.Empty;

            string[] lines = File.ReadAllLines(filePath);
            bool isReplace = false;
            // 行ごとに見る
            for (int i = 0; i < lines.Length; i++)
            {
                // ##見出しか最終行まで1行ずつ進める
                while (i < lines.Length && !(lines[i].StartsWith("## ")))
                {
                    if (!isReplace) mdContent += $"{lines[i]}\n";
                    i++;
                }
                if (i >= lines.Length) break;
                mdContent += $"{lines[i]}\n";

                string heading = heading = lines[i].Replace("## ", "");

                // フィールドのコンテンツを追加。置き換えフラグを設定
                isReplace = false;
                foreach (ReportField field in report.Fields)
                {
                    if (field.Title == heading)
                    {
                        mdContent += $"{field.Content}\n\n";
                        isReplace = true;
                        break;
                    }
                }

            }

            return mdContent;
        }

        /// <summary>
        /// 新規にmdファイルの文字列を作成する
        /// </summary>
        /// <param name="dailyReport"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static string CreateMdContent(DailyReportModel dailyReport, string? fileName = null)
        {
            string mdContent = string.Empty;

            // ファイル名をタイトルとする
            UserSettings settings = UserSettings.GetUserSettings();
            fileName ??= settings.GetFileName(DateTime.Now);

            mdContent += $"# {fileName}\n\n\n";

            // フィールドを追加する
            foreach (ReportField field in dailyReport.Fields)
            {
                mdContent += $"## {field.Title}\n";
                mdContent += $"{field.Content}\n\n";
            }

            return mdContent;
        }

        /// <summary>
        /// mdファイルを解析し、受け取ったreportのContentsを設定
        /// </summary>
        /// <param name="report"></param>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static DailyReportModel ReadMdFile(DailyReportModel report, string filePath)
        {
            // mdファイル読み込み
            string[] lines = File.ReadAllLines(filePath);

            List<ReportField> sections = [];
            string heading = string.Empty;
            foreach (string line in lines)
            {
                if (line.StartsWith("## "))
                {
                    bool foundSection = false;
                    string currentHeading = line.Replace("#", "").Replace(" ", "");
                    // セクションを初期化
                    report.Fields.ForEach(
                    section =>
                    {
                        if (section.Title == currentHeading)
                        {
                            section.Content = string.Empty;
                            foundSection = true;
                            heading = currentHeading;
                        }
                    });
                    if (foundSection) continue;
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
                            section.Content = section.Content.Replace("\n", "");
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

        private static string CheckHeading(string line, List<ReportField> fields)
        {
            // フィールドと一致する見出しあったら、そのContentを追加
            string matchHeading = string.Empty;
            if (line.StartsWith("##"))
            {
                string heading = line.Replace("#", "").Replace(" ", "");
                // セクションを初期化
                fields.ForEach(
                field =>
                {
                    if (field.Title == heading) matchHeading = field.Title;

                });
            }

            return matchHeading;
        }
    }
}