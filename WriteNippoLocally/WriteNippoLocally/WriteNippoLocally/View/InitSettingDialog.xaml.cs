using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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
    /// InitSettingDialog.xaml の相互作用ロジック
    /// </summary>
    public partial class InitSettingDialog : Window
    {
        public InitSettingDialog()
        {
            InitializeComponent();
            InitSettingDialogVM  vm = new();
            vm.RequestClose += () => this.DialogResult = true;
            this.DataContext = vm;
        }
    }
}
