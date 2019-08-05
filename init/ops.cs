using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.Azure.WebJobs.Extensions.Storage;
using System.Net.Http.Headers;

namespace CAMSA.Functions
{

  /// <summary>
  /// Ops class to provide all HTTP based function, essentially creating an API
  /// 
  /// Using the `optype` supplied in the URL the method will perform the most appropriate
  /// processes based on the method and id if supplied
  /// </summary>
  public static class ops
  {
    [FunctionName("ops")]
    public static async Task<HttpResponseMessage> Run(
      [HttpTrigger(
        AuthorizationLevel.Anonymous,
        "get",
        "post",
        "delete",
        Route = "{optype}/{id?}/{category?}"
      )] HttpRequest req,
      string optype,
      string id,
      string category,
      [Table("settings")] CloudTable settingTable,
      ILogger log,
      ExecutionContext executionContext)
    {

      // Initialise variables
      HttpResponseMessage response = null;
      ResponseMessage msg = new ResponseMessage();
      IEntity entity = null;

      // if the category is null check to see if it has been set in the headers
      if (String.IsNullOrEmpty(category))
      {
        category = req.Headers["X-Category"]; // GetHeaderValue(req.Headers, "X-Category");
      }

      // Check that the correct authentication key has been used to access the function
      // First check to see if "code" has been set on the query string
      string suppliedKey = String.Empty;
      if (!String.IsNullOrEmpty(req.Query["code"])) {
        suppliedKey = req.Query["code"];
      } else {
        suppliedKey = req.Headers["x-functions-key"];
      }

      // if the supplied key matches that of the environment setting for the FUNCTION_API_KEY then
      // run the function
      string apiKey = Environment.GetEnvironmentVariable(Constants.APIKeyEnvVarName);
      if (suppliedKey == apiKey || String.IsNullOrEmpty(apiKey)) {

        // Perform the appropriate processes based on the optype
        switch (optype)
        {
          case "config":

            // create a new config entity
            entity = new Config(Constants.ConfigStorePartitionKey);
            response = await entity.Process(req, settingTable, log, id, category);
            break;

          case "starterKit":

            StarterKit sk = new StarterKit();
            response = await sk.Process(req, settingTable, log, category, executionContext);
            break;

          case "automateLog":

            AutomateLogListener all = new AutomateLogListener();
            response = await all.Process(req, settingTable, log, category);
            break;

          case "counts":
            await AutomateCounts.Process(settingTable, log);
            msg = new ResponseMessage();
            response = msg.CreateResponse();
            break;

          // Set a default response if the optype is not recognised
          default:
            msg = new ResponseMessage(
              String.Format("Specified type is not recognized: {0}", optype),
              true,
              HttpStatusCode.NotFound
            );
            return msg.CreateResponse();
        }
      } else {

        // An incorrect key has been supplied, so send back Not authorised
        if (String.IsNullOrEmpty(apiKey)) {
          msg = new ResponseMessage(
            "Function API key has not been set. Authorization is not possible",
            true,
            HttpStatusCode.InternalServerError
          );
        } else {
          msg = new ResponseMessage(
            String.Format("Supplied API Key is not valid"),
            true,
            HttpStatusCode.Unauthorized
          );
        }

        response = msg.CreateResponse();
      }

      return response;
    }

/*
    private static string GetHeaderValue(IHeaderDictionary headers, string key)
    {
      IEnumerable<string> values;

      headers[]

      if (headers.TryGetValues(key, out values))
      {
        return values.FirstOrDefault();
      }

      return null;
    }
 */
  }
}
