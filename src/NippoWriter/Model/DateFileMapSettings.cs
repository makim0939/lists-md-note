using System.Text.Json.Serialization;
using System.Text.Json;
using System.IO;

namespace NippoWriter.Model
{
    internal class DateFileMapSettings
    {
        [JsonInclude]
        public Dictionary<string, string> Map { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// 日付とファイルの紐づけ情報を取得
        /// </summary>
        /// <returns name="">日付とファイルの紐づけ情報</returns>
        public static DateFileMapSettings GetDateFileMapSettings()
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
        /// <summary>
        /// 日付とファイルの紐づけ情報を保存
        /// </summary>
        /// <param name="settings">DateFileMapSettings</param>
        /// <returns name="result">成功/失敗</returns>
        public static bool StoreDateFileMapSettings(DateFileMapSettings settings)
        {
            bool result = false;
            string settingsJson = JsonSerializer.Serialize(settings);
            try
            {
                File.WriteAllText(@".\DateFileMapSettings.json", settingsJson);
                result = true;
            }
            catch (Exception ex)
            {
                result = false;
            }

            return result;
        }
    }
}
