using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;

namespace WriteNippoLocally.Model
{
    internal class UserSettings
    {
        [JsonInclude]
        public string SiteUrl { get; set; } = string.Empty;

        [JsonInclude]
        public string DestDirectory { get; set; } = string.Empty;

        [JsonInclude]
        public string FileNameFormat { get; set; } = "日報yyyy-mm-dd";

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
    }
}
