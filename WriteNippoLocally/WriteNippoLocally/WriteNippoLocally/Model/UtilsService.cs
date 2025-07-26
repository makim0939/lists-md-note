using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace WriteNippoLocally.Model
{
    static class UtilsService
    {
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
    }
}
