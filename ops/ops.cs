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
        AuthorizationLevel.Function,
        "get",
        "post",
        "delete",
        Route = "{optype}/{id?}/{category?}"
      )] HttpRequestMessage req,
      string optype,
      string id,
      string category,
      [Table("settings")] CloudTable settingTable,
      ILogger log,
      ExecutionContext executionContext) {

        // Initialise variables
        HttpResponseMessage response = null;
        ResponseMessage msg = new ResponseMessage();
        IEntity entity = null;

        // if the category is null check to see if it has been set in the headers
        if (String.IsNullOrEmpty(category)) {
          category = GetHeaderValue(req.Headers, "X-Category");
        }
        
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

          // Set a default response if the optype is not recognised
          default:
            msg = new ResponseMessage(
              String.Format("Specified type is not recognised: {0}", optype),
              true,
              HttpStatusCode.NotFound
            );
            return msg.CreateResponse();
        }

        return response;
      }
  
    private static string GetHeaderValue(HttpRequestHeaders headers, string key)
    {
      IEnumerable<string> values;

      if (headers.TryGetValues(key, out values))
      {
        return values.FirstOrDefault();
      }

      return null;
    }
  }
}