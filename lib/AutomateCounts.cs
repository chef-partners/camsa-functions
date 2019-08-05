

using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json.Linq;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace CAMSA.Functions
{
  public class AutomateCounts
  {
    public static async Task Process(CloudTable settingTable, ILogger log)
    {
      // Create necessary objects to get information from the datastore
      IEntity config = new Config();

      // Get all the settings for the CentralLogging partition
      DataService ds = new DataService(settingTable, config);
      Configs config_store = await ds.GetAll(Constants.ConfigStorePartitionKey);
      Configs central_logging = await ds.GetAll("central_logging");

      // Create an instance of the LogAnalyticsWriter
      LogAnalyticsWriter log_analytics_writer = new LogAnalyticsWriter(log, config_store, central_logging);

      // Get the Automate token and fqdn from the config store
      string token_setting = config_store.logging_automate_token;
      string fqdn_setting = config_store.automate_fqdn;

      // Set the time that that count was performed
      DateTime time = DateTime.UtcNow;

      // Request the NodeCount data from the Automate Server
      NodeCount node_count = await GetData("node", config_store.logging_automate_token, config_store.automate_fqdn, log);
      node_count.time = time;
      node_count.subscriptionId = config_store.subscription_id;
      node_count.customerName = config_store.customer_name;

      // Submit the node count
      log_analytics_writer.Submit(node_count, "ChefAutomateAMAInfraNodeCount");

      // Request the UserCount data from the Automate Server
      UserCount user_count = await GetData("user", config_store.logging_automate_token, config_store.automate_fqdn, log);

      user_count.time = time;
      user_count.subscriptionId = config_store.subscription_id;
      user_count.customerName = config_store.customer_name;

      log_analytics_writer.Submit(user_count, "ChefAutomateAMAUserCount");

    }

    public static async Task<dynamic> GetData(string type, string token, string fqdn, ILogger log)
    {
      // Initialise variables
      dynamic count = null;
      string url = String.Empty;

      if (type == "node") {
        count = new NodeCount();
      } else {
        count = new UserCount();
      }

      if (String.IsNullOrEmpty(fqdn)) {
        log.LogWarning("Unable to retrieve count from Automate as the FQDN has not been supplied. (Type = {0})", type);
      } else {

        // based on the type, set the url that needs to be accessed
        if (type == "node")
        {
          url = String.Format("https://{0}/api/v0/cfgmgmt/stats/node_counts", fqdn);
        }
        else if (type == "user")
        {
          url = String.Format("https://{0}/api/v0/auth/users", fqdn);
        }

        // Attempt to get the data from the Specified automate server using the token
        try
        {
          ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;

          // Create a client and submit the request to the URL
          HttpClient client = new HttpClient();

          // Set a header that contains the token we need to use for authentication
          client.DefaultRequestHeaders.Add("x-data-collector-token", token);

          HttpResponseMessage response = await client.GetAsync(new Uri(url));

          // if the response is OK read the data
          if (response.IsSuccessStatusCode)
          {
            if (type == "node")
            {
              count = response.Content.ReadAsAsync<NodeCount>().Result;
            }
            else if (type == "user")
            {
              // Get the data from the response
              dynamic user = response.Content.ReadAsAsync<dynamic>().Result;

              // Turn the users into an array so they can be easily counted
              JArray users = (JArray)user.users;

              // Create a User object with so that the count can be set
              count = new UserCount();
              count.Total = users.Count;
            }

            // set the server address on the object
            count.ServerAddress = fqdn;
          }

        }
        catch (Exception excep)
        {
          log.LogError(String.Format("API Post Exception: {0}", excep.Message));
        }
      }

      // return the count
      return count;
    }
  }
}