using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using CrmShared;
using Foundation;
using Newtonsoft.Json.Linq;
using UIKit;

namespace CrmWatchKit
{
	// The UIApplicationDelegate for the application. This class is responsible for launching the
	// User Interface of the application, as well as listening (and optionally responding) to
	// application events from iOS.
	[Register("AppDelegate")]
	public class AppDelegate : UIApplicationDelegate
	{
		//CRM Organization Url
		private const string ServiceUrl = "https://org.crm.dynamics.com";

		// class-level declarations

		public override UIWindow Window
		{
			get;
			set;
		}

		// This method is invoked when the application is about to move from active to inactive state.
		// OpenGL applications should use this method to pause.
		public override void OnResignActivation(UIApplication application)
		{
		}

		// This method should be used to release shared resources and it should store the application state.
		// If your application supports background exection this method is called instead of WillTerminate
		// when the user quits.
		public override void DidEnterBackground(UIApplication application)
		{
		}

		// This method is called as part of the transiton from background to active state.
		public override void WillEnterForeground(UIApplication application)
		{
		}

		// This method is called when the application is about to terminate. Save data, if needed.
		public override void WillTerminate(UIApplication application)
		{
		}

		public override async void HandleWatchKitExtensionRequest
	   (UIApplication application, NSDictionary userInfo, Action<NSDictionary> reply)
		{
			if (userInfo.Values[0].ToString() == "gettasks")
			{
				string userId = NSUserDefaults.StandardUserDefaults.StringForKey("UserId");

				List<CrmTask> tasks = await GetCrmTasks(userId);

				var nativeDict = new NSMutableDictionary();
				foreach (CrmTask task in tasks)
				{
					nativeDict.Add((NSString)task.TaskId, (NSString)task.Subject);
				}

				reply(new NSDictionary(
					"count", NSNumber.FromInt32(tasks.Count),
					"tasks", nativeDict
					));
			}
			else if (userInfo.Values[0].ToString() == "closetask")
			{
				string taskId = userInfo.Values[1].ToString();
				CloseTask(taskId);

				reply(new NSDictionary(
					"count", 0,
					"something", 0
					));
			}
		}

		private async void CloseTask(string taskId)
		{
			JObject task = new JObject { { "statecode", 1 }, { "statuscode", 5 } };

			using (HttpClient httpClient = new HttpClient())
			{
				httpClient.BaseAddress = new Uri(ServiceUrl);
				httpClient.Timeout = new TimeSpan(0, 2, 0);
				httpClient.DefaultRequestHeaders.Add("OData-MaxVersion", "4.0");
				httpClient.DefaultRequestHeaders.Add("OData-Version", "4.0");
				httpClient.DefaultRequestHeaders.Accept.Add(
					new MediaTypeWithQualityHeaderValue("application/json"));
				httpClient.DefaultRequestHeaders.Authorization =
					new AuthenticationHeaderValue("Bearer", NSUserDefaults.StandardUserDefaults.StringForKey("AccessToken"));

				HttpResponseMessage closeResponse =
						await httpClient.SendAsJsonAsync(new HttpMethod("PATCH"), "api/data/tasks(" + taskId + ")", task);

				if (!closeResponse.IsSuccessStatusCode)
					return;
			}
		}

		private async Task<List<CrmTask>> GetCrmTasks(string userId)
		{
			List<CrmTask> tasks = new List<CrmTask>();

			using (HttpClient httpClient = new HttpClient())
			{
				httpClient.BaseAddress = new Uri(ServiceUrl);
				httpClient.Timeout = new TimeSpan(0, 2, 0);
				httpClient.DefaultRequestHeaders.Add("OData-MaxVersion", "4.0");
				httpClient.DefaultRequestHeaders.Add("OData-Version", "4.0");
				httpClient.DefaultRequestHeaders.Accept.Add(
					new MediaTypeWithQualityHeaderValue("application/json"));
				httpClient.DefaultRequestHeaders.Authorization =
					new AuthenticationHeaderValue("Bearer", NSUserDefaults.StandardUserDefaults.StringForKey("AccessToken"));

				HttpResponseMessage retrieveResponse =
					await httpClient.GetAsync("api/data/tasks?$select=subject&$top=10&$filter=ownerid eq " + userId + " and statecode eq 0&$orderby=createdon desc");

				if (!retrieveResponse.IsSuccessStatusCode)
					return tasks;

				JObject jRetrieveResponse =
					JObject.Parse(retrieveResponse.Content.ReadAsStringAsync().Result);

				var values = jRetrieveResponse["value"];

				foreach (JToken jToken in values)
				{
					CrmTask task = new CrmTask();
					task.TaskId = jToken["activityid"].ToString();
					task.Subject = jToken["subject"].ToString();
					tasks.Add(task);
				}

				return tasks;
			}
		}
	}
}

