using System.Windows;
using NippoWriter.ViewModel;

namespace NippoWriter.View
{
    /// <summary>
    /// UserSettingDialog.xaml の相互作用ロジック
    /// </summary>
    public partial class UserSettingDialog : Window
    {
        public UserSettingDialog()
        {
            InitializeComponent();
            UserSettingDialogVM vm = new();
            vm.RequestClose += () => this.DialogResult = true;
            this.DataContext = vm;
        }
    }
}
