using System.IO;

namespace NippoWriter.Model
{
    internal class MarkdownFileService
    {
        static MarkdownFileService()
        {
        }

        public static void storeMdFile(string filePath, string content)
        {

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

        /// <summary>
        /// 
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

        // DailyReportModelからmarkdownファイルの中身を作成する。
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