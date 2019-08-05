using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace CAMSA.Functions
{

  public class AutomateLogListener
  {
    private ResponseMessage _response = new ResponseMessage();
    public async Task<HttpResponseMessage> Process(HttpRequest req, 
                                                   CloudTable table,
                                                   ILogger log,
                                                   string category)
    {
      HttpResponseMessage msg;

      // Only respond to an HTTP Post
      if (req.Method == "POST")
      {

        // Create dataservice to access data in the config table
        Config config = new Config();
        DataService ds = new DataService(table, config);

        // Get all the settings for the CentralLogging partition
        Configs config_store = await ds.GetAll(category);
        Configs central_logging = await ds.GetAll("centralLogging");

        // Get the body of the request
        string body = await new StreamReader(req.Body).ReadToEndAsync();
        string[] logs = body.Split('}');

        // Create an instance of the LogAnalyticsWriter
        LogAnalyticsWriter log_analytics_writer = new LogAnalyticsWriter(log, config_store, central_logging);

        // Create an instance of AutomateLog which will hold the data that has been submitted
        AutomateLog data = new AutomateLog();

        // iterate around each item in the logs
        string appended_item;
        string log_name;
        foreach (string item in logs)
        {
          appended_item = item;
          if (!appended_item.EndsWith("}"))
          {
            appended_item += "}";
          }

          // output the item to the console
          log.LogInformation(item);

          // if the item is not empty, process it
          if (!string.IsNullOrEmpty(item))
          {
            // Deserialise the item into the AutomateLog object
            data = JsonConvert.DeserializeObject<AutomateLog>(appended_item as string);

            // From this data create an AutomateMessage object
            AutomateMessage automate_message = AutomateLogParser.ParseGenericLogMessage(data.MESSAGE_s, config_store.customer_name, config_store.subscription_id, log);

            // if the message is known then submit to LogAnalytics
            if (automate_message.sourcePackage.ToLower() != "uknown entry")
            {
              // Determine the log name of the message
              log_name = automate_message.sourcePackage.Replace("-", "") + "log";

              // Submit the data
              log_analytics_writer.Submit(automate_message, log_name);
            }
          }
        }

        _response.SetMessage("Log data accepted");
        msg = _response.CreateResponse();

      }
      else
      {
        _response.SetError("HTTP Method not supported", true, HttpStatusCode.BadRequest);
        msg = _response.CreateResponse();
      }

      return msg;
    }
  }
}