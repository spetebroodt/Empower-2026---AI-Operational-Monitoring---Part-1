namespace Elements
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Runtime.CompilerServices;
	using System.Threading;
	using System.Threading.Tasks;
	using Skyline.DataMiner.Analytics.DataTypes;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Core.DataMinerSystem.Automation;
	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Net.Messages.Advanced;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	internal class ElementInstaller
	{
		private readonly IEngine engine;

		public ElementInstaller(IEngine engine)
		{
			this.engine = engine;
		}

		public void InstallDefaultContent()
		{
			#region element creation
			int viewID = CreateViews(new string[] { "DataMiner Catalog", "Empower 2026", "AI Operational Monitoring", "Behavioral Anomaly Detection Demo" });
			CreateElement("Empower 2026 - AI - Audio bit rate", "Empower 2026 - AI - Audio bit rate CBR-VBR", "0.0.0.1", viewID, "Default", "Default");
			CreateElement("Empower 2026 - AI - Task Manager", "Empower 2026 - AI - TSR", "1.0.0.4", viewID);

			viewID = CreateViews(new string[] { "DataMiner Catalog", "Empower 2026", "AI Operational Monitoring", "Pattern Matching Demo" });
			CreateElement("Empower 2026 - AI - Video server 1", "Empower 2026 - AI - Video Server", "0.0.0.1", viewID);
			CreateElement("Empower 2026 - AI - Video server 2", "Empower 2026 - AI - Video Server", "0.0.0.1", viewID);

			viewID = CreateViews(new string[] { "DataMiner Catalog", "Empower 2026", "AI Operational Monitoring", "Relational Anomaly Detection Demo" });
			string lastElementName = "Empower 2026 - AI - DAB Transmitter";
			CreateElement(lastElementName, "Empower 2026 - AI - Commtia DAB", "1.0.0.1", viewID, "TrendTemplate_PA_Demo", "AlarmTemplate_PA_Demo");
			Thread.Sleep(5000);
			#endregion

			#region Element data loading
			while (!engine.GetDms().ElementExists(lastElementName))
			{
				engine.GenerateInformation("Waiting for elements to be created...");
				Thread.Sleep(5000);
			}

			engine.GenerateInformation("Fetching elements");
			var videoServer1 = engine.FindElement("Empower 2026 - AI - Video server 1");
			var videoServer2 = engine.FindElement("Empower 2026 - AI - Video server 2");
			var audioBitRateElement = engine.FindElement("Empower 2026 - AI - Audio bit rate");
			var TSRElement= engine.FindElement("Empower 2026 - AI - Task Manager");

			if (videoServer1 != null && videoServer2 != null && audioBitRateElement != null && TSRElement != null)
			{
				engine.GenerateInformation("Specifying number of history points to read");
				videoServer1.SetParameter(10, 1); //fast version: read in all history, for empower put to 1 else to 10000
				videoServer2.SetParameter(10, 1); //fast version: read in all history, for empower put to 1 else to 10000
				audioBitRateElement.SetParameter(14, 5); //For empower, set to 5: every 5', 5 points will be read. This leads to 1364 points to be read, which is good. Else set to 10000
				TSRElement.SetParameter(10, 40); //!!!!!!!!!!!!!!!!!!!!FOR EMPOWER, SET THIS TO 40!!!!!!!!!!!!!!!!!!! Else set to 10000
				Thread.Sleep(5000);
				engine.GenerateInformation("Enabling history data read-in");
				videoServer1.SetParameter(102, 1);
				videoServer2.SetParameter(102, 1);
				audioBitRateElement.SetParameter(506, 1);
				TSRElement.SetParameter(506, 1);
			}
			engine.GenerateInformation("Finished installing elements");
			#endregion

		}

		private void AssignVisioToView(int viewID, string visioFileName)
		{
			var request = new AssignVisualToViewRequestMessage(viewID, new Skyline.DataMiner.Net.VisualID(visioFileName));

			engine.SendSLNetMessage(request);
		}

		private int? GetView(string viewName)
		{
			var views = engine.SendSLNetMessage(new GetInfoMessage(InfoType.ViewInfo));
			foreach (var m in views)
			{
				var viewInfo = m as ViewInfoEventMessage;
				if (viewInfo == null)
					continue;

				if (viewInfo.Name == viewName)
					return viewInfo.ID;
			}

			return null;
		}

		private int CreateNewView(string viewName, string parentViewName)
		{
			var request = new SetDataMinerInfoMessage
			{
				bInfo1 = int.MaxValue,
				bInfo2 = int.MaxValue,
				DataMinerID = -1,
				HostingDataMinerID = -1,
				IInfo1 = int.MaxValue,
				IInfo2 = int.MaxValue,
				Sa1 = new SA(new string[] { viewName, parentViewName }),
				What = (int)NotifyType.NT_ADD_VIEW_PARENT_AS_NAME,
			};

			var response = engine.SendSLNetSingleResponseMessage(request);
			if (!(response is SetDataMinerInfoResponseMessage infoResponse))
				throw new ArgumentException("Unexpected message returned by DataMiner");

			return infoResponse.iRet;
		}

		private int CreateViews(string[] viewNames)
		{
			int? firstNonExistingViewLevel = null;
			int? lastExistingViewID = null;
			string lastExistingViewName = null;

			for (int i = viewNames.Length - 1; i >= 0; --i)
			{
				int? viewID = GetView(viewNames[i]);
				if (viewID.HasValue)
				{
					lastExistingViewID = viewID;
					lastExistingViewName = viewNames[i];
					firstNonExistingViewLevel = i + 1;
					break;
				}
			}

			if (firstNonExistingViewLevel.HasValue && firstNonExistingViewLevel == viewNames.Length)
				return lastExistingViewID.Value;

			if (!firstNonExistingViewLevel.HasValue)
			{
				// No views in the tree already exist, so create all views starting from the root view
				lastExistingViewID = -1;
				lastExistingViewName = engine.GetDms().GetView(-1).Name;
				firstNonExistingViewLevel = 0;
			}

			for (int i = firstNonExistingViewLevel.Value; i < viewNames.Length; ++i)
			{
				lastExistingViewID = CreateNewView(viewNames[i], lastExistingViewName);
				lastExistingViewName = viewNames[i];
			}

			return lastExistingViewID.Value;
		}

		private void CreateElement(string elementName, string protocolName, string protocolVersion, int viewID,
			string trendTemplate = "Default", string alarmTemplate = "")
		{
			var request = new AddElementMessage
			{
				ElementName = elementName,
				ProtocolName = protocolName,
				ProtocolVersion = protocolVersion,
				TrendTemplate = trendTemplate,
				AlarmTemplate = alarmTemplate,
				ViewIDs = new int[] { viewID },
			};

			var dms = engine.GetDms();
			if (dms.ElementExists(elementName)) //Delete element first if it already exists
			{
				engine.GenerateInformation($"Atempting to delete {elementName}");
				var elementRequest = new GetElementByNameMessage(elementName);
				var elementResponse = engine.SendSLNetSingleResponseMessage(elementRequest);
				if (!(elementResponse is ElementInfoEventMessage elementInfo))
					throw new ArgumentException("Unexpected message returned by DataMiner");

				// Remove the element if it exists
				var deleteRequest = new SetElementStateMessage(elementInfo.DataMinerID, elementInfo.ElementID, Skyline.DataMiner.Net.Messages.ElementState.Deleted, true);
				engine.SendSLNetMessage(deleteRequest);
				System.Threading.Thread.Sleep(TimeSpan.FromSeconds(2));
			}

			//Verify deletion succeeded
			for (int i = 0; i < 10; ++i)
			{
				if (dms.ElementExists(elementName))
				{
					engine.GenerateInformation($"{elementName} still exists, waiting for deletion to complete...");
					Thread.Sleep(10000);
				}
				else
				{
					engine.GenerateInformation($"{elementName} deleted successfully");
					break;
				}
			}

			//create element
			engine.GenerateInformation($"Creating element {elementName} with protocol {protocolName} version {protocolVersion} in view ID {viewID}");
			engine.SendSLNetSingleResponseMessage(request);
		}
	}
}
