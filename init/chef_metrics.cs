using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json.Linq;

namespace CAMSA.Functions
{
  public static class chef_metrics
  {
    [FunctionName("chef_metrics")]
    public static async void Run(
        [QueueTrigger(
                "chef-statsd",
                Connection = "AzureWebJobsStorage")
            ]
            string rawmetric,
        [Table("settings")] CloudTable settingTable,
        ILogger log)
    {
      // Instantiate objects to get relevant data from the configuration store
      IEntity config = new Config();
      DataService ds = new DataService(settingTable, config);

      // Get all the settings for the CentralLogging partition
      Configs config_store = await ds.GetAll();
      Configs central_logging = await ds.GetAll("central_logging");

      // Create an instance of the LogAnalyticsWriter
      LogAnalyticsWriter log_analytics_writer = new LogAnalyticsWriter(log, config_store, central_logging);

      // Create a datetime variable that is the Linux Epoch time
      System.DateTime dateTime = new System.DateTime(1970, 1, 1, 0, 0, 0, 0);

      // Parse the raw metric json into an object
      Newtonsoft.Json.Linq.JObject statsd = Newtonsoft.Json.Linq.JObject.Parse(rawmetric);

      // Iterate around the series data and create a message for each one
      var metrics = (JArray)statsd["series"];
      foreach (JObject metric in metrics)
      {

        // create message to send to Log Analytics
        ChefMetricMessage message = new ChefMetricMessage();

        // determine the time of the event
        DateTime time = dateTime.AddSeconds((double)metric["points"][0][0]);

        // set the properties of the object
        message.metricName = (string)metric["metric"];
        message.metricType = (string)metric["type"];
        message.metricHost = (string)metric["host"];
        message.time = time;
        message.metricValue = (double)metric["points"][0][1];
        message.customerName = config_store.customer_name;
        message.subscriptionId = config_store.subscription_id;

        // Submit the metric to Log Analytics
        log_analytics_writer.Submit(message, "statsd_log");
      }
    }
  }
}
