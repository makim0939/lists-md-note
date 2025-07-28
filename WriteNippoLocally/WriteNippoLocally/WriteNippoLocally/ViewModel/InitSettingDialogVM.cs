using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WriteNippoLocally.Model;
using System.Windows.Forms;
using System.Windows;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Web;
using System.Windows.Input;


namespace WriteNippoLocally.ViewModel
{
    class InitSettingDialogVM : INotifyPropertyChanged
    {
        public string SiteUrl { get; set; }
        private string _destDirectory;
        public string DestDirectory
        {
            get { return _destDirectory; }
            set
            {
                _destDirectory = value;
                OnPropertyChanged();
            }
        }
        private bool _canDoButtonClick;
        public bool CanDoButtonClick
        {
            get { return _canDoButtonClick; }
            set
            {
                _canDoButtonClick = value;
                OnPropertyChanged();
            }
        }
        public DelegateCommand SelectFolder { get; set; }
        public DelegateCommand StoreSettings { get; set; }

        public InitSettingDialogVM()
        {
            UserSettings settings = UserSettings.GetUserSettings();
            this.SiteUrl = settings.SiteUrl;
            this.DestDirectory = settings.DestDirectory;

            SelectFolder = new DelegateCommand(SelectFolderExecute);
            StoreSettings = new DelegateCommand(StoreSettingsExecute, StoreSettingsCanExecute);
        }

        public event Action RequestClose;
        public void StoreSettingsExecute()
        {

            UserSettings settings = new UserSettings()
            {
                SiteUrl = this.SiteUrl,
                DestDirectory = this.DestDirectory
            };

            // 設定を保存
            string settingsJson = JsonSerializer.Serialize(settings);
            try
            {
                File.WriteAllText(@".\UserSettings.json", settingsJson);
            }
            catch (Exception ex)
            {
                Console.WriteLine("設定ファイル書き出しエラー");
                Console.WriteLine(ex.Message);
            }

            RequestClose?.Invoke();

        }

        private bool StoreSettingsCanExecute()
        {
            bool canExecute = true;
            // 入力が不正の場合はExecute実行不可
            // サイトURLが正しいURLか
            if (this.SiteUrl == null)
            {
                canExecute = false;
            }
            else
            {
                bool isValidUri = Uri.TryCreate(this.SiteUrl, UriKind.Absolute, out Uri? uriResult);
                if (!isValidUri) canExecute = false;
                else if (uriResult == null) canExecute = false;
                else if (!(uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
                {
                    canExecute = false;
                }

                if (!(this.SiteUrl.Contains("sharepoint") && this.SiteUrl.Contains("Lists")))
                {
                    canExecute = false;
                }
            }
            
            // 存在するディレクトリか
            bool isFolder = Directory.Exists(this.DestDirectory);
            if (!isFolder) canExecute = false;

            CanDoButtonClick = canExecute;

            return canExecute;
        }

        private void SelectFolderExecute()
        {
            DestDirectory = ShowSelectFolderDialog();
        }

        public string ShowSelectFolderDialog()
        {
            FolderBrowserDialog dialog = new()
            {
                Description = "フォルダを選択してください",
                ShowNewFolderButton = true
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                return dialog.SelectedPath;
            }

            return string.Empty;
        }

        // データ変更通知用
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
