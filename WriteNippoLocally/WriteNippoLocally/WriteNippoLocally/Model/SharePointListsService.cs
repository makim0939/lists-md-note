using Microsoft.SharePoint.Client;

namespace WriteNippoLocally.Model
{
    // SharePointへの接続、リストの取得・送信を行う。
    class SharePointListsService
    {
        private ClientContext Context { get; set; }
        private User LoginUser { get; set; }
        private List TargetList { get; set; }
        private Microsoft.SharePoint.Client.View TargetView { get; set; }

        private SharePointListsService(ClientContext context)
        {
            Context = context;

            UserSettings settings = UserSettings.GetUserSettings();

            // ユーザを取得
            User loginUser = Context.Web.CurrentUser;
            Context.Load(loginUser);
            Context.ExecuteQuery();

            // リストを取得
            List list = Context.Web.GetList(settings.GetSitePath());
            Context.Load(list);
            Context.ExecuteQuery();

            
            //viewIdを取得
            string? viewId = settings.GetViewId();

            // ビューを取得
            ViewCollection views = list.Views;
            Context.Load(views);
            Context.ExecuteQuery();
            Microsoft.SharePoint.Client.View view = viewId != null ? views.GetById(Guid.Parse(viewId)) : views[0];

            LoginUser = loginUser;
            TargetList = list;
            TargetView = view;

        }

        // 非同期初期化
        // new SharePointListsService()の代わりにSharePointListsService.CreateAsync()でインスタンスを取得する
        public static async Task<SharePointListsService> CreateAsync()
        {
            UserSettings userSettings = UserSettings.GetUserSettings();
            ClientContext context = await SharePointAuthService.ExecuteDeviceCodeAuth(userSettings.GetSharePointUrl());
            //ClientContext context = SharePointAuthService.ExecuteAuth(userSettings.SiteUrl);

            var listsService = new SharePointListsService(context);

            return listsService;
        }

        public DailyReportModel GetReportFields()
        {
            // リストに含まれるフィールドを取得
            FieldCollection fields = TargetList.Fields;
            Context.Load(fields);
            Context.ExecuteQuery();

            // ビューに含まれるフィールドを取得
            ViewFieldCollection? viewFields = TargetView.ViewFields;
            Context.Load(viewFields);
            Context.ExecuteQuery();

            //　記入に必要なフィールドを抽出し、中身を初期化
            DailyReportModel reportFieldOnly = new DailyReportModel();
            foreach (string viewField in viewFields)
            {
                Field field = fields.GetFieldByInternalName(viewField);
                ReportField reportField = new();
                if (field.Title == "登録者" || field.Title == "日付")
                {
                    continue;
                }

                if (field.TypeAsString == "DateTime")
                {
                    reportField.Title = field.Title;
                    reportField.InternalName = field.InternalName;
                    reportField.Content = DateTime.Now.ToString("yyyy-MM-dd");
                    reportField.Type = field.TypeAsString;
                }
                else if (field.TypeAsString == "Choice")
                {
                    FieldChoice choiceField = Context.CastTo<FieldChoice>(field);
                    reportField.Title = field.Title;
                    reportField.InternalName = field.InternalName;
                    reportField.Content = choiceField.DefaultValue;
                    reportField.Choices = [.. choiceField.Choices];
                    reportField.Type = field.TypeAsString;
                }
                else
                {
                    reportField.Title = field.Title;
                    reportField.InternalName = field.InternalName;
                    reportField.Content = string.Empty;
                    reportField.Type = field.TypeAsString;
                }
                reportFieldOnly.Fields.Add(reportField);
            }

            return reportFieldOnly;
        }

        // 今日の自分のアイテムを取得する
        public DailyReportModel? GetMyReport(DateTime date)
        {

            // 著者と日付を指定
            // 日付と著者のフィールドを取得
            string DateInternalName = string.Empty;
            string AuthorInternalName = string.Empty;

            FieldCollection fields = TargetList.Fields;
            Context.Load(fields);
            Context.ExecuteQuery();

            foreach (Field field in fields)
            {
                if (field.Title == "日付" || field.InternalName == "Date" || field.InternalName == "date")
                {
                    DateInternalName = field.InternalName;
                    break;
                }
                else if (field.Title == "登録日時" || field.InternalName == "Created" || field.InternalName == "created")
                {
                    DateInternalName = field.InternalName;
                    break;
                }
            }
            foreach (Field field in fields)
            {
                if (field.Title == "登録者" || field.InternalName == "Author" || field.InternalName == "author")
                {
                    AuthorInternalName = field.InternalName;
                    break;
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
                              <Value Type='Integer'>{LoginUser.Id}</Value>
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
        public int Send(DailyReportModel report)
        {
            // 既存のアイテムを取得または新規作成
            ListItem item;
            if (report.Id == null)
            {
                ListItemCreationInformation itemCreateInfo = new ListItemCreationInformation();
                item = TargetList.AddItem(itemCreateInfo);
            }
            else
            {
                item = TargetList.GetItemById(report.Id.Value);
            }

            // SharePointに値を反映
            foreach (ReportField field in report.Fields)
            {
                item[field.InternalName] = field.Content;
            }
            item.Update();
            Context.ExecuteQuery();


            Context.Load(item);
            Context.ExecuteQuery();

            return item.Id;
        }

        // クエリに該当する中で1番目のアイテムを取得する
        private DailyReportModel? GetReport(CamlQuery query)
        {

            // ビューに含まれるフィールドを取得
            ViewFieldCollection? viewFields = TargetView.ViewFields;
            Context.Load(viewFields);
            Context.ExecuteQuery();

            // 表示名を取得するためにFieldCollection型でフィールドを再取得
            FieldCollection fields = TargetList.Fields;
            Context.Load(fields);
            Context.ExecuteQuery();

            // 受け取ったクエリでアイテムを取得
            ListItemCollection items = TargetList.GetItems(query);
            Context.Load(items);
            Context.ExecuteQuery();

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
                    var choiceField = Context.CastTo<FieldChoice>(field);
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