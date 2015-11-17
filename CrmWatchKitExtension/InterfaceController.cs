using System;
using System.Collections.Generic;
using System.Linq;
using CrmShared;
using Foundation;
using WatchKit;

namespace CrmWatchKitExtension
{
	public partial class InterfaceController : WKInterfaceController
	{

		List<CrmTask> _rows = new List<CrmTask>();

		public InterfaceController(IntPtr handle) : base(handle)
		{
		}

		public override void Awake(NSObject context)
		{
			base.Awake(context);
		}


		public override void WillActivate()
		{
			// This method is called when the watch view controller is about to be visible to the user.
			//Console.WriteLine("{0} will activate", this);

			//LoadTableRows();
			_rows = new List<CrmTask>();

			GetTasks();
		}

		public override void DidDeactivate()
		{
			// This method is called when the watch view controller is no longer visible to the user.
			//Console.WriteLine("{0} did deactivate", this);
		}

		public override NSObject GetContextForSegue(string segueIdentifier, WKInterfaceTable table, nint rowIndex)
		{
			CrmTask task = _rows[(int)rowIndex];

			List<NSObject> keys = new List<NSObject> { new NSString("command"), new NSString("taskid") };
			List<NSObject> values = new List<NSObject> { new NSString("closetask"), new NSString(task.TaskId) };

			var cmdDict = NSDictionary.FromObjectsAndKeys(values.ToArray(), keys.ToArray());

			OpenParentApplication(cmdDict, (replyInfo, error) =>
			{
				if (error != null)
				{
					//Console.WriteLine(error);
					//TODO: Handle error
					return;
				}

				_rows = new List<CrmTask>();

				GetTasks();
			});

			return new NSString(task.Subject);
		}

		private void GetTasks()
		{
			var cmdDict = new NSDictionary("command", "gettasks");

			OpenParentApplication(cmdDict, (replyInfo, error) =>
			{
				if (error != null)
				{
					//Console.WriteLine(error);
					//TODO: Handle error
					return;
				}

				NSDictionary nativeDict = replyInfo["tasks"] as NSDictionary;

				if (nativeDict == null)
					return;

				myTable.SetNumberOfRows(nativeDict.Count(), "default");
				for (var i = 0; i < nativeDict.Count(); i++)
				{
					_rows.Add(new CrmTask()
					{
						TaskId = nativeDict.Keys[i].ToString(),
						Subject = nativeDict.Values[i].ToString()
					});
					var elementRow = (RowController)myTable.GetRowController(i);
					elementRow.myRowLabel.SetText(nativeDict.Values[i].ToString());
				}
			});
		}

		public override void DidSelectRow(WKInterfaceTable table, nint rowIndex)
		{
			var rowData = _rows[(int)rowIndex];
			Console.WriteLine("Row selected:" + rowData);

			CrmTask task = _rows[(int)rowIndex];

			List<NSObject> keys = new List<NSObject> { new NSString("command"), new NSString("taskid") };
			List<NSObject> values = new List<NSObject> { new NSString("closetask"), new NSString(task.TaskId) };

			var cmdDict = NSDictionary.FromObjectsAndKeys(keys.ToArray(), values.ToArray());

			OpenParentApplication(cmdDict, (replyInfo, error) =>
			{
				if (error != null)
				{
					//Console.WriteLine(error);
					//TODO: Handle error
					return;
				}
			});
		}
	}
}

