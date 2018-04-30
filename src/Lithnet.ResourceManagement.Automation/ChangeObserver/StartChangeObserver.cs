using Lithnet.ResourceManagement.Automation.RMConfigConverter;
using Lithnet.ResourceManagement.Client;
using Microsoft.ResourceManagement.WebServices;
using Microsoft.ResourceManagement.WebServices.IdentityManagementOperation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Lithnet.ResourceManagement.Automation.ChangeObserver
{
    [Cmdlet(VerbsLifecycle.Start, "RMObserver")]
    public class StartChangeObserver : AsyncCmdlet
    {
        [Parameter(ValueFromPipeline = false, Mandatory = true, Position = 1)]
        public ObserverSetting RMObserverSetting { get; set; }

        [Parameter(ValueFromPipeline = false, Mandatory = true, Position = 2)]
        public ConverterSetting RMConverterSetting { get; set; }

        [Parameter(ValueFromPipeline = false, Mandatory = false, Position = 2)]
        public string ExportDirectory { get; set; }

        [Parameter(ValueFromPipeline = false, Mandatory = false, Position = 2)]
        public SwitchParameter FullSyncOnStartup { get; set; }

        private const string MESSAGESOURCE = "Change Observer";
        private string requestor;
        private DateTime Start;
        private DateTime lastImport;
        private bool hasException;

        private List<ResourceObject> observedObjects;


        protected override async Task BeginProcessingAsync()
        {
            int loadingRetryOnException = 1;
            Start = DateTime.Now;



            if (ExportDirectory != null)
                RMObserverSetting.ExportDirectory = ExportDirectory;

            if(String.IsNullOrEmpty(RMObserverSetting.ExportDirectory))
            {
                Dictionary<string, PSObject> result = Host.UI.Prompt(
                    "EXPORT DIRECTORY",
                    "Enter the path to the export directory",
                    new System.Collections.ObjectModel.Collection<FieldDescription>() { new FieldDescription("Directory") }
                    );                
                RMObserverSetting.ExportDirectory = result["Directory"].ToString();
            }
                
                

            if (!Directory.Exists(RMObserverSetting.ExportDirectory))
                try
                {
                    Directory.CreateDirectory(RMObserverSetting.ExportDirectory);
                }
                catch (Exception ex)
                {
                    hasException = true;
                    WriteError(new ErrorRecord(ex, "1", ErrorCategory.WriteError, RMObserverSetting.ExportDirectory));
                    throw ex;
                }

            PrintHeader();

            do
            {
                try
                {
                    observedObjects = new List<ResourceObject>();
                    DateTime startTime = DateTime.Now;
                    var loadingTasks = new List<Task>();

                    foreach (var config in RMObserverSetting.ObserverObjectSettings)
                    {
                        Host.UI.WriteLine(
                            string.Format("{0,-35} : {1}", config.ObjectType, config.XPathExpression));

                        loadingTasks.Add(Task.Run(() =>
                        {
                            ObjectTypeDefinition objectType = ResourceManagementSchema.GetObjectType(config.ObjectType);
                            var attributes = objectType.Attributes.Select(t => t.SystemName);
                            observedObjects.AddRange(RmcWrapper.Client.GetResources(config.XPathExpression, attributes));
                        }));
                    };

                    Host.UI.WriteLine("");
                    Host.UI.Write("Loading object to observer");

                    while (!Task.WhenAll(loadingTasks).IsCompleted)
                    {
                        await Task.Delay(200);
                        Host.UI.Write(".");
                    };

                    Host.UI.WriteLine("");
                    Host.UI.WriteLine(String.Format("{0} Objects loaded in {1}", observedObjects.Count, DateTime.Now.Subtract(startTime).ToString()));
                    Host.UI.WriteLine("");
                    hasException = false;
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(ex, "2", ErrorCategory.ReadError, null));
                    hasException = true;
                    loadingRetryOnException++;
                    Host.UI.WriteLine(ConsoleColor.Green, Host.UI.RawUI.BackgroundColor, String.Format("An exception has occured while loading. Retry initial load ({0} / 3)", loadingRetryOnException));
                }
            } while (hasException || loadingRetryOnException > 3);

            if (!hasException)
            {
                if (FullSyncOnStartup.IsPresent)
                {
                    Host.UI.Write("Processing initial synchronization");
                    await ProcessingFullSynchronization();
                    Host.UI.WriteLine("");
                    Host.UI.Write("Initial synchronization completed.");
                }

                Host.UI.WriteLine("");
                Host.UI.WriteLine(ConsoleColor.Green, Host.UI.RawUI.BackgroundColor, "Initializing completed");
                Host.UI.WriteLine(ConsoleColor.Green, Host.UI.RawUI.BackgroundColor, "Observing changes can be abortet by pressing ESC");
                Host.UI.WriteLine("");
            }
        }

        protected override async Task ProcessRecordAsync()
        {
            if (!hasException)
            {
                lastImport = DateTime.Now;
                ProcessingChanges(Start);

                while (true)
                {
                    if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape) break;
                                        
                    ProcessingChanges(lastImport);
                    lastImport = DateTime.Now;                    
                    await Task.Delay(RMObserverSetting.ChangeDetectionInterval);                    
                }
            }
        }

        protected override async Task EndProcessingAsync()
        {
            if (!hasException)
            {
                ChoiceDescription yes = new ChoiceDescription("&Yes", "(recommended) for relation integrity. The actual export directory will be deleted and recreated.");
                ChoiceDescription no = new ChoiceDescription("&No", "Observer will be stopped permanenty.");

                System.Collections.ObjectModel.Collection<ChoiceDescription> choices = new System.Collections.ObjectModel.Collection<ChoiceDescription>() { yes, no };
                string caption = "Observing object changes is stopped";
                string message = "Do you want to proceed an full sync?";
                int result = Host.UI.PromptForChoice(caption, message, choices, 0);

                if (result == 0)
                {
                    Host.UI.WriteLine("");
                    Host.UI.Write("Processing full synchronization");
                    await ProcessingFullSynchronization();
                    Host.UI.WriteLine("");
                    Host.UI.Write("Processing full synchronization finished.");
                }
            }
        }

        private void PrintHeader()
        {
            int headerLength = 73;
            string title = "lithnet resourcemanagement change observer ";

            Host.UI.WriteLine("");
            Host.UI.WriteLine(String.Concat(Enumerable.Repeat("#", headerLength)));
            Host.UI.WriteLine(String.Format("#{0}#", String.Concat(Enumerable.Repeat(" ", headerLength - 2))));
            Host.UI.WriteLine(String.Format("#{0}{1}{0}#",
                                String.Concat(Enumerable.Repeat(" ", (73 - title.Length - 2) / 2)),
                                title));
            Host.UI.WriteLine(String.Format("#{0}#", String.Concat(Enumerable.Repeat(" ", headerLength - 2))));
            Host.UI.WriteLine(String.Concat(Enumerable.Repeat("#", headerLength)));

            Host.UI.WriteLine("");
            Host.UI.WriteLine("");
            Host.UI.WriteLine("");
            Host.UI.WriteLine(String.Format("{0,-35} : {1}", "Observer output will be made on", RMObserverSetting.ExportDirectory));
            Host.UI.WriteLine(String.Format("{0,-35} : {1}", "Observer listening change mode is", RMObserverSetting.ChangeMode.ToString()));
            Host.UI.WriteLine(String.Format("{0,-35} :", "Observer ObjectType listenings", RMObserverSetting.ChangeMode.ToString()));
            Host.UI.WriteLine("");
            Host.UI.WriteLine(String.Format("{0,-35} : {1}", "ObjectType", "XPathExpression"));
            Host.UI.WriteLine(String.Format("{0,-35} : {1}", String.Concat(Enumerable.Repeat("-", 35)), String.Concat(Enumerable.Repeat("-", 35))));
            Host.UI.WriteLine("Observer is listening for changes");
        }

        private async Task<ISearchResultCollection> GetResources(string Filter, string ObjectType)
        {
            ObjectTypeDefinition objectType = ResourceManagementSchema.GetObjectType(ObjectType);
            var attributes = objectType.Attributes.Select(t => t.SystemName);

            return await Task.FromResult<ISearchResultCollection>(
                RmcWrapper.Client.GetResourcesAsync(Filter, attributes));
        }

        private async Task<ISearchResultCollection> GetRequestsAsync(DateTime RequestsSince)
        {
            string xPath = GetRequestXPath(RequestsSince);
            WriteDebug(String.Format("Searching for Request with Filter {0}", xPath));
            return await Task.FromResult<ISearchResultCollection>(
                RmcWrapper.Client.GetResources(
                    xPath,
                    new List<string>() { "Target", "TargetObjectType", "Operation" }
                ));
        }

        private async Task ProcessingFullSynchronization()
        {
            DateTime startTime = DateTime.Now;

            // Cleanup Export Directory
            if (Directory.Exists(RMObserverSetting.ExportDirectory))
            {
                Directory.GetFiles(RMObserverSetting.ExportDirectory).ToList().ForEach(item =>
                {
                    File.Delete(item);
                });

                Directory.GetDirectories(RMObserverSetting.ExportDirectory).ToList().ForEach(item =>
                {
                    Directory.Delete(item, true);
                });
            }

            Directory.CreateDirectory(RMObserverSetting.ExportDirectory);

            var initSyncTasks = new List<Task>();

            observedObjects.ForEach((item) =>
            {
                initSyncTasks.Add(Task.Run(() =>
                {
                    try
                    {
                        var converterSetting = GetObjectSetting(item, RMConverterSetting);
                        var observerSetting = RMObserverSetting.ObserverObjectSettings.Where(c => c.ObjectType == item.ObjectTypeName).FirstOrDefault();

                        Converter.SerializeConfigFile(
                                   Converter.ConvertToRMConfig(item, RMConverterSetting),
                                   observerSetting.AttributeSeparations,
                                   Converter.GetFilePath(item, converterSetting, RMObserverSetting.ExportDirectory)
                                   );
                    }
                    catch (Exception ex)
                    {
                        WriteError(new ErrorRecord(
                                                ex,
                                                String.Format("Could not serialize {0} {1}",
                                                            item.ObjectType,
                                                            item.ObjectID.Value),
                                                ErrorCategory.NotSpecified,
                                                item));
                    }
                }));
            });

            while (!Task.WhenAll(initSyncTasks).IsCompleted)
            {
                await Task.Delay(200);
                Host.UI.Write(".");

            };
            Host.UI.WriteLine();
            Host.UI.WriteLine(String.Format("{0} Objects synchronized in {1}", observedObjects.Count, DateTime.Now.Subtract(startTime).ToString()));
        }

        private async void ProcessingChanges(DateTime RequestsSince)
        {
            WriteDebug("Starting Processing changes");

            ISearchResultCollection requests = await GetRequestsAsync(RequestsSince);
            foreach (ResourceObject request in requests)
            {
                List<ObserverRequest> observerRequests;
                if (request.Attributes["TargetObjectType"].StringValue == "	msidmCompositeType")
                {
                    WriteVerbose(String.Format("Detected new msidmCompositeType with ID {0} and operation {1}", request.ObjectID.Value, request.Attributes["Operation"].StringValue));
                    observerRequests = GetObserverRequestFromCompositeRequest(request);
                }
                else
                {
                    WriteVerbose(String.Format("Detected new request with ID {0} and operation {1}", request.ObjectID.Value, request.Attributes["Operation"].StringValue));
                    observerRequests = new List<ObserverRequest>{
                                                                    new ObserverRequest(
                                                                        request.Attributes["Target"].ReferenceValue,
                                                                        request.Attributes["TargetObjectType"].StringValue,
                                                                        request.Attributes["Operation"].StringValue)
                                                                };
                }

                foreach (ObserverRequest observerRequest in observerRequests)
                {

                    var observerObjectSetting = RMObserverSetting.ObserverObjectSettings.Where(c => c.ObjectType == observerRequest.TargetObjectType).FirstOrDefault();

                    if (observerObjectSetting == null)
                    {
                        WriteVerbose("Change is skipped. No ObserverConfiguration for this ObjectType is configured.");
                        continue;
                    }

                    // ^(Create|Get|Put|Delete|Enumerate|Pull|SystemEvent)$
                    switch (observerRequest.RequestOperationType)
                    {
                        case "Create":
                            var filesCreated = ProcessingCreate(observerRequest.TargetObjectID, observerObjectSetting);
                            WriteModificationToConsole(filesCreated);
                            break;
                        case "Put":
                            var filesModifications = ProcessingPut(observerRequest.TargetObjectID, observerObjectSetting);
                            WriteModificationToConsole(filesModifications);
                            break;
                        case "Delete":
                            var filesDeleted = ProcessingDelete(observerRequest.TargetObjectID, observerObjectSetting);
                            WriteModificationToConsole(filesDeleted);
                            break;
                        default:
                            break;
                    }

                }
            }
        }

        private List<ObserverRequest> GetObserverRequestFromCompositeRequest(ResourceObject request)
        {
            List<ObserverRequest> observerRequests = new List<ObserverRequest>();
            ResourceObject compositeRequest = RmcWrapper.Client.GetResource(request.ObjectID);

            string parameterXMLString = String.Format("<RequestParameters>{0}</RequestParameters>", compositeRequest.Attributes["RequestParameter"].StringValue);

            XElement requestParameters = XElement.Parse(parameterXMLString);
            foreach (var requestParameter in requestParameters.Elements())
            {
                var objectID = new UniqueIdentifier(requestParameter.Element("Target").Value);
                var objectType = observedObjects.Where(o => o.ObjectID == objectID).FirstOrDefault();

                if(objectType != null)                
                    observerRequests.Add(
                    new ObserverRequest(
                        objectID,
                        objectType.ObjectTypeName,
                        requestParameter.Element("Operation").Value));                
                else
                    WriteVerbose("msidmCompositeType RequestParameter is skipped. Could not find Target in observed objects.");
            }
            return observerRequests;
        }

        private void WriteModificationToConsole(Dictionary<string, string> modifications)
        {
            foreach (var item in modifications)
                Host.UI.WriteLine(string.Format("{0,-8} : {1}", item.Value, item.Key.ToString().Remove(0, RMObserverSetting.ExportDirectory.Length + 1)));
        }

        private Dictionary<string, string> ProcessingCreate(ResourceObject obj)
        {
            ObserverObjectSetting observerObjectSetting = RMObserverSetting.ObserverObjectSettings.Where(c => c.ObjectType == obj.ObjectTypeName).FirstOrDefault();
            return ProcessingCreate(obj, observerObjectSetting);
        }

        private Dictionary<string, string> ProcessingCreate(ResourceObject obj, ObserverObjectSetting observerObjectSetting)
        {
            WriteDebug("Retrieve create " + obj.ObjectID.Value);
            if (IsFiltered(obj, observerObjectSetting))
            {
                WriteVerbose(String.Format("Create Request of Target type {0} with ID {1} is filtered by ObserverObjectSetting.", obj.ObjectTypeName, obj.ObjectID.Value));
                return new Dictionary<string, string>();
            }

            ObjectSetting converterSetting = GetObjectSetting(obj, RMConverterSetting);
            ConfigFile configFile = Converter.ConvertToRMConfig(obj, RMConverterSetting);

            List<string> filesExported = Converter.SerializeConfigFile(
                                configFile,
                                observerObjectSetting.AttributeSeparations,
                                Converter.GetFilePath(obj, converterSetting, RMObserverSetting.ExportDirectory)
                                );

            observedObjects.Add(obj);
            return filesExported.ToDictionary(k => k, v => "Added");
        }

        private Dictionary<string, string> ProcessingCreate(UniqueIdentifier id, ObserverObjectSetting observerObjectSetting)
        {
            ResourceObject obj = RmcWrapper.Client.GetResource(id);
            return ProcessingCreate(obj, observerObjectSetting);
        }

        private Dictionary<string, string> ProcessingPut(ResourceObject obj, ObserverObjectSetting objectSetting)
        {
            WriteDebug("Retrieve put " + obj.ObjectID.Value);

            Dictionary<string, string> filesDeleted = ProcessingDelete(obj.ObjectID, objectSetting);
            Dictionary<string, string> filesCreated = ProcessingCreate(obj, objectSetting);

            Dictionary<string, string> filesModified = new Dictionary<string, string>();
            foreach (var file in filesCreated.Keys)
            {
                var item = filesDeleted.FirstOrDefault(kvp => kvp.Key == file);

                if (item.Value == null)
                    filesModified.Add(file, "Added");
                else
                {
                    filesModified.Add(file, "Updated");
                    filesDeleted.Remove(item.Key);
                }
            }

            foreach (var file in filesDeleted.Keys)
            {
                filesModified.Add(file, "Deleted");
            }

            return filesModified;
        }

        private Dictionary<string, string> ProcessingPut(UniqueIdentifier id, ObserverObjectSetting observerObjectSetting)
        {
            ResourceObject obj = RmcWrapper.Client.GetResource(id);
            return ProcessingPut(obj, observerObjectSetting);
        }

        private Dictionary<string, string> ProcessingPut(ResourceObject resourceObject)
        {
            ObserverObjectSetting observerObjectSetting = RMObserverSetting.ObserverObjectSettings.Where(c => c.ObjectType == resourceObject.ObjectTypeName).FirstOrDefault();
            return ProcessingPut(resourceObject, observerObjectSetting);
        }

        private Dictionary<string, string> ProcessingDelete(UniqueIdentifier id, ObserverObjectSetting objectSetting)
        {
            Dictionary<string, string> filesDeleted = new Dictionary<string, string>();
            WriteDebug("Retrieve delete " + id.Value);

            var obj = observedObjects.Where(o => o.ObjectID == id).FirstOrDefault();

            if (obj == null)
            {
                WriteVerbose("Target is not observed. Delete Request will be skipped.");
                return filesDeleted;
            }

            ObjectSetting converterSetting = GetObjectSetting(obj, RMConverterSetting);

            foreach (string s in Converter.GetSerializedFiles(
                obj,
                converterSetting,
                RMObserverSetting.ExportDirectory,
                objectSetting.AttributeSeparations))
            {
                if (File.Exists(s))
                {
                    File.Delete(s);
                    filesDeleted.Add(s, "Deleted");
                }
            }

            observedObjects.Remove(obj);
            return filesDeleted;
        }

        private bool IsFiltered(ResourceObject obj, ObserverObjectSetting objectSetting)
        {
            if (objectSetting.XPathExpression == "/" + obj.ObjectTypeName)
                return false;
            else
            {
                var xpath = AddObjectIDToXPath(obj, objectSetting.XPathExpression);
                var o = RmcWrapper.Client.GetResources(xpath, 1);
                return o == null;
            }
        }

        private string AddObjectIDToXPath(ResourceObject resourceObject, string XPath)
        {
            return String.Format("/{0}[(ObjectID = '{1}') and {2}",
                resourceObject.ObjectTypeName,
                resourceObject.ObjectID.GetGuid().ToString(),
                XPath.Substring(resourceObject.ObjectTypeName.Length + 2));
        }

        private ObjectSetting GetObjectSetting(ResourceObject resourceObject, ConverterSetting converterSetting)
        {
            return converterSetting.Configurations
                                    .Where(o => o.ObjectType == resourceObject.ObjectTypeName)
                                    .First();
        }

        private string GetRequestXPath(DateTime LastImport)
        {
            List<XPathQuery> queries = new List<XPathQuery>();
            if (LastImport == null)
                LastImport = DateTime.MinValue;
            else if (LastImport == DateTime.MinValue)
                LastImport = DateTime.Now;

            queries.Add(new XPathQuery("msidmCompletedTime", ComparisonOperator.GreaterThan, LastImport));
            queries.Add(new XPathQuery("RequestStatus", ComparisonOperator.Equals, "Completed"));

            if (RMObserverSetting.ChangeMode == ChangeModeType.My)
            {
                if (requestor == null)
                {
                    string filter = string.Format("/Person[AccountName = '{0}' and Domain = '{1}']",
                        Environment.UserName,
                        Environment.UserDomainName);

                    ResourceObject identity = RmcWrapper.Client.GetResources(filter).First();
                    requestor = identity.ObjectID.Value;
                }
                queries.Add(new XPathQuery("Creator", ComparisonOperator.Equals, requestor));
            }

            XPathQueryGroup xPathGroup = new XPathQueryGroup(GroupOperator.And, queries);
            XPathExpression expression = new XPathExpression("Request", xPathGroup);
            return expression.ToString();
        }
    }
}


