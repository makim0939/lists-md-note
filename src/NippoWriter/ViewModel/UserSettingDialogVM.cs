using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using NippoWriter.Model;

namespace NippoWriter.ViewModel
{
    internal class UserSettingDialogVM : INotifyPropertyChanged
    {
        private string _dialogTitle;
        public string DialogTitle
        {
            get { return _dialogTitle; }
            set 
            {
                _dialogTitle = value;
                OnPropertyChanged();
            }
        }
        private string _siteUrl;
        public string SiteUrl
        {
            get { return _siteUrl; }
            set
            {
                _siteUrl = value;
                OnPropertyChanged();
            }
        }
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
        private string _fileNameFormat;
        public string FileNameFormat
        {
            get { return _fileNameFormat; }
            set
            {
                _fileNameFormat = value;
                OnPropertyChanged();
            }
        }

        public DelegateCommand SelectFolder { get; set; }
        public DelegateCommand StoreSettings { get; set; }

        public event Action? RequestClose;

        public UserSettingDialogVM()
        {
            UserSettings settings = UserSettings.GetUserSettings();
            // サイトURLが設定ファイルになかったら初期設定
            if (settings.SiteUrl == string.Empty)
            {
                _dialogTitle = "初期設定";
            }
            else
            {
                _dialogTitle = "ユーザ設定";
            }

            _siteUrl = settings.SiteUrl;
            _destDirectory = settings.DestDirectory;
            _fileNameFormat = settings.FileNameFormat;

            SelectFolder = new DelegateCommand(SelectFolderExecute);
            StoreSettings = new DelegateCommand(StoreSettingsExecute, StoreSettingsCanExecute);
        }

        // 設定を保存
        public void StoreSettingsExecute()
        {
            UserSettings settings = new UserSettings()
            {
                SiteUrl = SiteUrl,
                DestDirectory = DestDirectory,
                FileNameFormat = FileNameFormat,
            };
            settings.StoreUserSettings();

            RequestClose?.Invoke();
        }

        private bool StoreSettingsCanExecute()
        {
            bool canExecute = true;
            // 入力が不正の場合はExecute実行不可
            // サイトURLが正しいURLか
            if (SiteUrl == null)
            {
                canExecute = false;
            }
            else
            {
                bool isValidUri = Uri.TryCreate(SiteUrl, UriKind.Absolute, out Uri? uriResult);
                if (!isValidUri) canExecute = false;
                else if (uriResult == null) canExecute = false;
                else if (!(uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
                {
                    canExecute = false;
                }

                if (!(SiteUrl.Contains("sharepoint") && SiteUrl.Contains("Lists")))
                {
                    canExecute = false;
                }
            }

            // 存在するディレクトリか
            bool isFolder = Directory.Exists(DestDirectory);
            if (!isFolder) canExecute = false;

            // ファイル名の形式が正しいか
            // 使用不可
            if (FileNameFormat.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                canExecute = false;
            }
            // ファイル名のフォーマットに年月日は必須とする
            if (
                !(FileNameFormat.Contains("YYYY")
                && FileNameFormat.Contains("MM")
                && FileNameFormat.Contains("DD"))
              )
            {
                canExecute = false;
            }

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