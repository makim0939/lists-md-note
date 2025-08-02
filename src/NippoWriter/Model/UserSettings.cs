using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;

namespace NippoWriter.Model
{
    internal class UserSettings
    {
        [JsonInclude]
        public string SiteUrl { get; set; } = string.Empty;

        [JsonInclude]
        public string DestDirectory { get; set; } = string.Empty;

        [JsonInclude]
        public string FileNameFormat { get; set; } = "日報YYYY-MM-DD";

        public UserSettings() { }

        // ユーザ設定を取得
        public static UserSettings GetUserSettings()
        {
            UserSettings? settings;
            try
            {
                string settingsStr = File.ReadAllText(@".\UserSettings.json");
                settings = JsonSerializer.Deserialize<UserSettings>(settingsStr);
            }
            catch
            {
                settings = null;
            }

            if (settings == null)
            {
                // ファイルが読み取れない場合はデフォルト設定
                settings = new UserSettings();
            }

            return settings;
        }
        // ユーザ設定を保存
        public bool StoreUserSettings()
        {
            bool result = true;
            string settingsJson = JsonSerializer.Serialize(this);
            try
            {
                File.WriteAllText(@".\UserSettings.json", settingsJson);
            }
            catch (Exception ex)
            {
                Console.WriteLine("設定ファイル書き出しエラー");
                Console.WriteLine(ex.Message);
                result = false;
            }

            return result;
        }


        // SiteUrlの取得補助
        //  "https://[company].sharepoint.com/sites/[site]/"の部分を取得
        public string GetSharePointUrl()
        {
            Uri uri = new Uri(this.SiteUrl);
            return uri.AbsoluteUri.Split("Lists")[0];
        }

        // "sites/[site]/Lists/[list]/AllItems.aspx"の部分を取得
        public string GetSitePath()
        {
            Uri uri = new Uri(this.SiteUrl);
            return uri.AbsolutePath;
        }

        // URLのパラメータからviewidを取得
        public string? GetViewId()
        {
            Uri uri = new Uri(this.SiteUrl);
            string? viewId = null;
            var prams = HttpUtility.ParseQueryString(uri.Query);
            foreach (string? key in prams.AllKeys)
            {
                if (key == "viewid")
                {
                    viewId = prams[key];
                }
            }
            return viewId;
        }

        // FileNameFormatの取得補助
        // 日付をあてはめた文字列を取得 ファイル名形式のルールを担う
        public string GetFileName(DateTime date)
        {
            string fileName = this.FileNameFormat;
            fileName = fileName.Replace("YYYY", date.ToString("yyyy"));
            fileName = fileName.Replace("MM", date.ToString("MM"));
            fileName = fileName.Replace("DD", date.ToString("dd"));

            return fileName;
        }
    }
}