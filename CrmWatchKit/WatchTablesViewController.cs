using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Foundation;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json.Linq;
using UIKit;

namespace CrmWatchKit
{
	public partial class WatchTablesViewController : UIViewController
	{
		//CRM Organization Url
		private const string ServiceUrl = "https://org.crm.dynamics.com";
		//Client Id from Azure portal
		private const string ClientId = "00000000-0000-0000-0000-000000000000";
		//Doesn't matter what this is, it just must match what is in the Azure portal 
		private const string RedirectUrl = "http://localhost/CrmWebApi";
		//Azure App Endpoints: FEDERATION METADATA DOCUMENT (only copy until after the GUID)
		private const string Authority = "https://login.microsoftonline.com/00000000-0000-0000-0000-000000000000";

		public AuthenticationResult AuthResult;
		private AuthenticationContext _authContext;

		public WatchTablesViewController(IntPtr handle) : base(handle)
		{
		}

		public override void DidReceiveMemoryWarning()
		{
			// Releases the view if it doesn't have a superview.
			base.DidReceiveMemoryWarning();

			// Release any cached data, images, etc that aren't in use.
		}

		#region View lifecycle

		public override void ViewDidLoad()
		{
			base.ViewDidLoad();

			// Perform any additional setup after loading the view, typically from a nib.
			Label1.Text = !string.IsNullOrEmpty(NSUserDefaults.StandardUserDefaults.StringForKey("AccessToken")) ?
				"Logged In" :
				"Logged Out";
		}

		public override void ViewWillAppear(bool animated)
		{
			base.ViewWillAppear(animated);
		}

		public override void ViewDidAppear(bool animated)
		{
			base.ViewDidAppear(animated);
		}

		public override void ViewWillDisappear(bool animated)
		{
			base.ViewWillDisappear(animated);
		}

		public override void ViewDidDisappear(bool animated)
		{
			base.ViewDidDisappear(animated);
		}

		#endregion

		async partial void LoginButton_TouchUpInside()
		{
			AdalInitializer.Initialize();

			_authContext =
						new AuthenticationContext(Authority, false);

			AuthResult = await _authContext.AcquireTokenAsync(ServiceUrl, ClientId, new Uri(RedirectUrl),
				new PlatformParameters(UIApplication.SharedApplication.KeyWindow.RootViewController));

			NSUserDefaults.StandardUserDefaults.SetString(AuthResult.AccessToken, "AccessToken");
			NSDate tokenExpiration = (NSDate)AuthResult.ExpiresOn.UtcDateTime;
			NSString tokenExpirationKey = new NSString("AccessTokenExpirationDate");
			NSUserDefaults.StandardUserDefaults.SetValueForKey(tokenExpiration, tokenExpirationKey);

			string userId = await WhoAmI(AuthResult.AccessToken);

			if (!string.IsNullOrEmpty(userId))
				NSUserDefaults.StandardUserDefaults.SetString(userId, "UserId");

			Label1.Text = "Logged In";
		}

		partial void UIButton39_TouchUpInside(UIButton sender)
		{
			if (_authContext != null)
			{
				_authContext.TokenCache?.Clear();

				string requestUrl = "https://login.windows.net/common/oauth2/logout";
				Task.Run(async () =>
				{
					var client = new HttpClient();
					var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
					await client.SendAsync(request);
				});
			}

			Label1.Text = "Logged Out";
		}

		private async Task<string> WhoAmI(string accessToken)
		{
			using (HttpClient httpClient = new HttpClient())
			{
				httpClient.BaseAddress = new Uri(ServiceUrl);
				httpClient.Timeout = new TimeSpan(0, 2, 0);
				httpClient.DefaultRequestHeaders.Add("OData-MaxVersion", "4.0");
				httpClient.DefaultRequestHeaders.Add("OData-Version", "4.0");
				httpClient.DefaultRequestHeaders.Accept.Add(
					new MediaTypeWithQualityHeaderValue("application/json"));
				httpClient.DefaultRequestHeaders.Authorization =
					new AuthenticationHeaderValue("Bearer", accessToken);

				//The URL will change in 2016 to include the API version - api/data/v8.0/WhoAmI
				HttpResponseMessage whoAmIResponse =
					await httpClient.GetAsync("api/data/WhoAmI");

				if (whoAmIResponse.IsSuccessStatusCode)
				{
					JObject jWhoAmIResponse =
						JObject.Parse(whoAmIResponse.Content.ReadAsStringAsync().Result);
					return jWhoAmIResponse["UserId"].ToString();
				}
			}

			return null;
		}
	}
}

