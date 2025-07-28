using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WriteNippoLocally.ViewModel;

namespace WriteNippoLocally.View
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
