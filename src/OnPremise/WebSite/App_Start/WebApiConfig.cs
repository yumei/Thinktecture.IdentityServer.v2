namespace Thinktecture.IdentityServer.Web.App_Start
{
    #region

    using System.Web.Http;

    #endregion

    public class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            config.Routes.MapHttpRoute("DefaultApi", "api/{controller}/{action}/{username}", new {username = RouteParameter.Optional});
        }
    }
}