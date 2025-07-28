using Microsoft.SharePoint.Client;
using PnP.PowerShell.Commands.Utilities;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using WriteNippoLocally.Model;
using WriteNippoLocally.ViewModel;

namespace WriteNippoLocally
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

        private void PlayGround()
        {

            //SharePointService service = new SharePointService();
            //service.GetMyReport(DateTime.Now);

            ////DailyReportModel test = new DailyReportModel();
            ////Console.WriteLine();

            //ClientContext? context = BrowserHelper.GetWebLoginClientContext("https://globaldenso.sharepoint.com/sites/JP_110381", true);

            //User user = context.Web.CurrentUser;
            //context.Load(user);
            //context.ExecuteQuery();
            //Debug.WriteLine($"名前は... {user.Title}！");



            //Console.WriteLine();
            //var list = context.Web.GetList("/sites/JP_110381/Lists/251/view.aspx");
            //context.Load(list);
            //context.ExecuteQuery();


            //FieldCollection fields = list.Fields;
            //context.Load(fields);
            //context.ExecuteQuery();

            //foreach (Field field in fields)
            //{
            //    Debug.WriteLine($"Internal Name: {field.InternalName}, Display Name: {field.Title}");
            //}

            //CamlQuery query = CamlQuery.CreateAllItemsQuery();
            //Microsoft.SharePoint.Client.ListItemCollection items = list.GetItems(query);

            //context.Load(items);
            //context.ExecuteQuery();

            //foreach (Microsoft.SharePoint.Client.ListItem item in items)
            //{
            //    Debug.WriteLine(item["hitokoto"]);
            //}
        }
    }
}