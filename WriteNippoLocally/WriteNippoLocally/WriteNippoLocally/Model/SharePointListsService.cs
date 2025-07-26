using View = Microsoft.SharePoint.Client.View;
using Microsoft.SharePoint.Client;
using PnP.PowerShell.Commands.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using CamlBuilder;
using Microsoft.SharePoint.Client.Search.Query;
using System.Web;

namespace WriteNippoLocally.Model
{
    // SharePointへの接続、リストの取得・送信を行う。
    static class SharePointListsService
    {
        private static ClientContext context;
        private static User loginUser;
        private static List list;
        private static Microsoft.SharePoint.Client.View view;

        static SharePointListsService()
        {

            UserSettings userSetting = UtilsService.GetUserSettings();

            Uri uri = new Uri(userSetting.SiteUrl);
            string siteUrl = uri.AbsoluteUri.Split("Lists")[0];
            string sitePath = uri.AbsolutePath;

            //viewIdを取得
            string? viewId = null;
            var prams = HttpUtility.ParseQueryString(uri.Query);
            foreach (string? key in prams.AllKeys)
            {
                if (key == "viewid")
                {
                    viewId = prams[key];
                }
            }

            // SharePointに接続
            context = BrowserHelper.GetWebLoginClientContext(siteUrl, true);
            loginUser = context.Web.CurrentUser;
            context.Load(loginUser);
            context.ExecuteQuery();

            // リストを取得
            list = context.Web.GetList(sitePath);
            context.Load(list);
            context.ExecuteQuery();

            // ビューを取得
            ViewCollection views = list.Views;
            context.Load(views);
            context.ExecuteQuery();
            view = viewId != null ? views.GetById(Guid.Parse(viewId)) : views[0];
        }



        public static DailyReportModel GetReportFields()
        {
            CamlQuery query = CamlQuery.CreateAllItemsQuery();
            DailyReportModel? report = GetReport(query);
            if (report == null)
            {
                throw new Exception("リストにアイテムが存在しません。");
            }

            // リストに含まれるフィールドを取得
            FieldCollection fields = list.Fields;
            context.Load(fields);
            context.ExecuteQuery();

            // ビューに含まれるフィールドを取得
            ViewFieldCollection? viewFields = view.ViewFields;
            context.Load(viewFields);
            context.ExecuteQuery();

            //　記入に必要なフィールドを抽出し、中身を初期化
            DailyReportModel reportFieldOnly = new DailyReportModel();
            foreach (string viewField in viewFields)
            {
                Field field = fields.GetByTitle(viewField);
                ReportField reportField = new();
                if (field.Title == "登録者" || field.Title == "日付")
                {
                    continue;
                }
                if (field.Title == "気分")
                {
                    FieldChoice choiceField = context.CastTo<FieldChoice>(field);
                    reportField.Title = "気分";
                    reportField.InternalName = field.InternalName;
                    reportField.Title = field.Title;
                    reportField.Content = "ノリノリ";
                    reportField.Choices = [.. choiceField.Choices];
                    continue;
                }

                if (field.TypeAsString == "DateTime")
                {
                    reportField.Content = DateTime.Now.ToString("yyyy-MM-dd");
                }
                else if (field.TypeAsString == "Choice")
                {
                    FieldChoice choiceField = context.CastTo<FieldChoice>(field);
                    reportField.Content = choiceField.Choices[0];
                }
                else
                {
                    reportField.Content = string.Empty;
                }
                reportFieldOnly.Fields.Add(reportField);
            }

            return reportFieldOnly;
        }

        // 今日の自分のアイテムを取得する。
        public static DailyReportModel? GetMyReport(DateTime date)
        {

            // 著者と日付を指定
            // 日付と著者のフィールドを取得
            string DateInternalName = string.Empty;
            string AuthorInternalName = string.Empty;

            FieldCollection fields = list.Fields;
            context.Load(fields);
            context.ExecuteQuery();

            foreach (Field field in fields)
            {
                if (field.Title == "日付" || field.InternalName == "Date" || field.InternalName == "date")
                {
                    DateInternalName = field.InternalName;
                }
                if (field.Title == "登録者" || field.InternalName == "Author" || field.InternalName == "author")
                {
                    AuthorInternalName = field.InternalName;
                }

            }

            // アイテムを取得
            CamlQuery query = new CamlQuery();
            query.ViewXml =
                $@"  
                    <View>
                      <Query>
                        <Where>
                          <And>
                            <Eq>
                              <FieldRef Name='{AuthorInternalName}' LookupId='TRUE' />
                              <Value Type='Integer'>{loginUser.Id}</Value>
                            </Eq>
                            <Eq>
                              <FieldRef Name='{DateInternalName}' />
                              <Value Type='DateTime' IncludeTimeValue='FALSE'>{date.ToString("yyyy-MM-dd")}</Value>
                            </Eq>
                          </And>
                        </Where>
                      </Query>
                   </View>
                ";

            DailyReportModel? report = GetReport(query);
            return report;
        }

        // SharePointListsにデータを送信する
        // 追加したアイテムのIdを戻り値とする
        public static int Send(DailyReportModel report)
        {
            // 既存のアイテムを取得または新規作成
            ListItem item;
            if (report.Id == null)
            {
                ListItemCreationInformation itemCreateInfo = new ListItemCreationInformation();
                item = list.AddItem(itemCreateInfo);
            }
            else
            {
                item = list.GetItemById(report.Id.Value);
            }

            // SharePointに値を反映
            foreach (ReportField field in report.Fields)
            {
                item[field.InternalName] = field.Content;
            }
            item.Update();
            context.ExecuteQuery();


            context.Load(item);
            context.ExecuteQuery();

            return item.Id;
        }

        // クエリに該当する中で1番目のアイテムを取得する
        private static DailyReportModel? GetReport(CamlQuery query)
        {

            // ビューに含まれるフィールドを取得
            ViewFieldCollection? viewFields = view.ViewFields;
            context.Load(viewFields);
            context.ExecuteQuery();

            // 表示名を取得するためにFieldCollection型でフィールドを再取得
            FieldCollection fields = list.Fields;
            context.Load(fields);
            context.ExecuteQuery();

            // 受け取ったクエリでアイテムを取得
            ListItemCollection items = list.GetItems(query);
            context.Load(items);
            context.ExecuteQuery();

            if (items.Count == 0) return null;
            ListItem item = items[0];

            // 格納する情報を取得
            DailyReportModel report = new DailyReportModel();
            report.Id = item.Id;
            foreach (string internalFieldName in viewFields)
            {
                object value = item[internalFieldName];

                // フィールドの表示名を取得
                Field field = fields.GetFieldByInternalName(internalFieldName);
                string displayName = field.Title;

                // 不要なフィールドは除く
                if (displayName == "登録者") continue;

                // フィールドのタイプを取得
                string type = field.TypeAsString;
                List<string> choices = [];
                if (type == "Choice")
                {
                    var choiceField = context.CastTo<FieldChoice>(field);
                    choices = [.. choiceField.Choices];
                }

                // フィールド値の型に応じて文字列に変換
                string content = string.Empty;
                if (value is string)
                {
                    content = (string)value;
                }
                else if (value is DateTime)
                {
                    content = ((DateTime)value).ToUniversalTime().ToString();
                }
                else if (value is FieldUserValue)
                {
                    var authorName = ((FieldUserValue)value).LookupValue;
                    content = authorName;
                }
                else if (value is null)
                {
                    content = string.Empty;
                }
                else
                {
                    content = value.ToString() ?? string.Empty;
                }

                // DailyReportModelインスタンスに格納
                ReportField section = new ReportField
                {
                    Title = displayName,
                    InternalName = internalFieldName,
                    Content = content,
                    Type = type,
                    Choices = choices
                };
                report.Fields.Add(section);
            }

            return report;
        }
    }
}