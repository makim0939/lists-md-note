using System.Windows;
using NippoWriter.ViewModel;

namespace NippoWriter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = new MainWindowVM();
            
        }

        private async void OnWindowLoaded(object? sender, RoutedEventArgs e)
        {
            MainWindowVM vm = (MainWindowVM)DataContext;
            if (vm == null) return;
            await vm.InitializeAsync();
        }

        // MainWindowのActivatedイベントハンドラ
        private void OnWindowActivated(object? sender, EventArgs e)
        {
            MainWindowVM vm = (MainWindowVM)DataContext;
            if (vm == null) return;
            vm.ReadMdFileExecute();
        }

        private void OnWindowDeactived(object? sender, EventArgs e)
        {
            MainWindowVM vm = (MainWindowVM)DataContext;
            if (vm == null) return;
            vm.StoreMdFileExecute();
        }

        // LostForcuseイベントハンドラ
        private void OnLostForcus(object? sender, EventArgs e)
        {
            MainWindowVM? vm = this.DataContext as MainWindowVM;
            if (vm == null) return;
            vm.StoreMdFileExecute();
        }

        // OnSelectionChangedイベントハンドラ
        private void OnSelectionChanged(object? sender, EventArgs e)
        {
            MainWindowVM? vm = this.DataContext as MainWindowVM;
            if (vm == null) return;
            vm.StoreMdFileExecute();
        }
    }
}