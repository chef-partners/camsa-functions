using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System.Collections.Generic;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace CAMSA.Functions
{

  /// <summary>
  /// This class is responsible for reading data from the Config table
  /// and building up a Zip file which contains all this information
  /// </summary>
  public class StarterKit
  {

    private Configs config_store;
    private string chefRepoPath;
    private string extrasPath;
    private string dotChefPath;

    private ILogger _logger;

    public async Task<HttpResponseMessage> Process(HttpRequest req,
                                                   CloudTable table,
                                                   ILogger logger,
                                                   string category,
                                                   Microsoft.Azure.WebJobs.ExecutionContext executionContext)
    {
      // Initialise variables
      HttpResponseMessage response = null;
      ResponseMessage msg = new ResponseMessage();
      StringBuilder db = new StringBuilder();
      
      _logger = logger;

      // The StarterKit will only response to GET requests
      if (req.Method == "GET") {

        logger.LogInformation("StarterKit has been requested");
        
        // Create a config object to pass to the dataservice
        Config config = new Config(Constants.ConfigStorePartitionKey);

        // Create a data service object to get all the data
        DataService ds = new DataService(table, config);

        string clientKeyFilename = string.Empty;
        string validatorKeyFilename = string.Empty;

        // Define the paths for the structure and then ensure they exist
        chefRepoPath = Path.Combine(executionContext.FunctionDirectory, "chef-repo");
        extrasPath = Path.Combine(chefRepoPath, "extras");
        dotChefPath = Path.Combine(chefRepoPath, ".chef");

        string keyFilename = string.Empty;
        string keyPath = string.Empty;
        string key = string.Empty;

        // Delete the repo path if it already exists
        // This is to prevent old files from being added to the zip that has been requseted
        if (Directory.Exists(chefRepoPath))
        {
          logger.LogInformation("Deleting existing path: {0}", chefRepoPath);
          Directory.Delete(chefRepoPath, true);
        }

        // Create the directories again
        if (!Directory.Exists(extrasPath))
        {
          logger.LogInformation("Creating directory: {0}", extrasPath);
          Directory.CreateDirectory(extrasPath);
        }

        if (!Directory.Exists(dotChefPath))
        {
          logger.LogInformation("Creating directory: {0}", dotChefPath);
          Directory.CreateDirectory(dotChefPath);
        }        

        // Get all the configuration items from the config store
        config_store = await ds.GetAll(category);
        config_store.DeriveServerURLs();

        // Write out the org and user keys
        WriteKey("org");
        WriteKey("user");

        // Patche the necessary templates
        // Create the template compiler
        Mustache.FormatCompiler compiler = new Mustache.FormatCompiler();

        // Build up a dictionary of the files to be rendered and the base path to use
        Dictionary<string, string> templates = new Dictionary<string, string>();
        templates.Add("credentials.txt", chefRepoPath);
        templates.Add("chef_extension.json", extrasPath);
        templates.Add("knife.rb", dotChefPath);

        string path;
        string data;
        Mustache.Generator generator;

        foreach(KeyValuePair<string, string> entry in templates) {
          path = Path.Combine(executionContext.FunctionAppDirectory, "templates", entry.Key);
          generator = compiler.Compile(File.ReadAllText(path));
          data = generator.Render(config_store);
          File.WriteAllText(Path.Combine(entry.Value, entry.Key), data);
        }

        // Create a json file of the config_store as this can be read by other languages if need be
        path = Path.Combine(extrasPath, "credentials.json");
        data = JsonConvert.SerializeObject(config_store, Formatting.Indented);
        File.WriteAllText(path, data);

        // Determine the path for the zip file
        string zipPath = Path.Combine(executionContext.FunctionDirectory, "starter_kit.zip");

        // Remove the file if it already exists
        if (File.Exists(zipPath)) {
          File.Delete(zipPath);
        }

        // Create the zip file from the chef repo directory
        ZipFile.CreateFromDirectory(chefRepoPath, zipPath);

        response = msg.CreateResponse(zipPath);

        // delete the zip file and the chefrepo
        Directory.Delete(chefRepoPath, true);
        File.Delete(zipPath);
        
      } else {
        msg.SetError("HTTP Method not supported", true, HttpStatusCode.BadRequest);
        response = msg.CreateResponse();
      }
      return response;
    }

    private void WriteKey(string type)
    {
      
      string name = string.Empty;
      string key = string.Empty;
      string filename = string.Empty;
      string keyPath = string.Empty;

      // Based on the type determine the data to extract
      switch (type) {
        case "org":

          name = String.Format("{0}-validator", config_store.org);
          key = config_store.org_validator_key;
          filename = String.Format("{0}.pem", name);
          config_store.org_key_filename = filename;

          break;

        case "user": 

          name = config_store.user;
          key = config_store.user_key;
          filename = String.Format("{0}.pem", name);
          config_store.client_key_filename = filename;

          break;
      }

      // If the name and key are not null, write out the file
      if (!String.IsNullOrEmpty(name) && !String.IsNullOrEmpty(key)) {
        
        // Determine the filename of the key
        keyPath = Path.Combine(dotChefPath, filename);

        _logger.LogDebug("Writing out key file: {0}", keyPath);

        // decode the key
        string decoded = Encoding.UTF8.GetString(Convert.FromBase64String(key));

        File.WriteAllText(keyPath, decoded);
      }
    }



  }
}