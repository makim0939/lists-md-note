using dotenv.net;
using Microsoft.SharePoint.Client;
using PnP.Framework;
using PnP.PowerShell.Commands.Utilities;

namespace NippoWriter.Model
{
    internal class SharePointAuthService
    {
        private class Credentials
        {
            public string ClientId { get; private set; }
            public string TenantId { get; private set; }
            public string ClientSecret { get; private set; }

            public Credentials()
            {
                DotEnv.Load();
                string? clientId = Environment.GetEnvironmentVariable("CLIENT_ID");
                string? tenantId = Environment.GetEnvironmentVariable("TENANT_ID");
                string? clientSecret = Environment.GetEnvironmentVariable("CLIENT_SECRET");

                // 認証情報がなかった場合はエラー
                if (clientId == null || tenantId == null || clientSecret == null)
                {
                    throw new Exception();
                }
                ClientId = clientId;
                TenantId = tenantId;
                ClientSecret = clientSecret;
            }
        }
        public SharePointAuthService() { }

        /// <summary>
        ///  EntraIDでのアプリ登録不要の認証方法でClientContext取得
        /// </summary>
        /// <param name="siteUrl">SPOサイトのURL</param>
        /// <returns name="context">認証されたClientContext</returns>
        public static ClientContext ExecuteAuth(string siteUrl)
        {
            ClientContext context = BrowserHelper.GetWebLoginClientContext(siteUrl, true);
            return context;
        }

        /// <summary>
        /// 認可コードフローで認証されたClientContext取得
        /// EntraIDでのアプリ登録必須
        /// </summary>
        /// <param name="siteUrl">SPOサイトのURL</param>
        /// <returns name="context">認証されたClientContext</returns>
        public static async Task<ClientContext> GetContextByAuthCodeGrant(string siteUrl)
        {
            var credentials = new Credentials();
            var authManager = new AuthenticationManager(credentials.ClientId, "http://localhost", credentials.TenantId);
            using var context = await authManager.GetContextAsync(siteUrl);

            return context;
        }
    }
}