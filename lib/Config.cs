

using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;


namespace CAMSA.Functions
{

  /// <summary>
  /// Calls representing a configuration object in the settings table
  /// This is also inherits the TableEntity object from WindowsAzure which means
  /// that the extra fields required for table storage are automatically added to the class
  /// </summary>
  public class Config : TableEntity, IEntity
  {
    /// <summary>
    /// Value field
    /// Holds the value for the named parameter in storage
    /// </summary>
    /// <value>String</value>
    public string Value { get; set; }

    /// <summary>
    /// A single Config item
    /// </summary>
    /// <valueConfig object></value>
    private Config item { get; set; }

    /// <summary>
    /// Private items Dictionary holding all the values that have been found in storage
    /// that match the criteria.
    /// </summary>
    private Dictionary<string, string> _items = new Dictionary<string, string>();

    /// <summary>
    /// Internal property used to hold the responsemessage to be returned to the client
    /// </summary>
    /// <returns>ResponseMessage</returns>
    private ResponseMessage _response = new ResponseMessage();

    /// <summary>
    /// Class constructor which takes the name of the parition key for the items being 
    /// sought.
    /// </summary>
    /// <param name="partitionKey">String of the name of the partition key to use</param>
    public Config(string partitionKey)
    {
      this.PartitionKey = partitionKey;
    }

    /// <summary>
    /// Override constructor that is used when performing a query against the whole table
    /// </summary>
    public Config() 
    {}

    /// <summary>
    /// Set the RowKey field for the TableEntity
    /// </summary>
    /// <param name="key">String representing the name of the key</param>
    public void SetRowKey(string key)
    {
      this.RowKey = key;
    }

    /// <summary>
    /// Sets the Value field with the value specified
    /// </summary>
    /// <param name="value">Value to be assigned to the item</param>
    public void SetValue(string value)
    {
      Value = value;
    }

    /// <summary>
    /// Returns the parition key that is in use for the item
    /// </summary>
    /// <returns>String name of the partition key</returns>
    public string GetPartitionKey()
    {
      return this.PartitionKey;
    }

    /// <summary>
    /// Returns the current item
    /// </summary>
    /// <returns>Config object</returns>
    public Config GetItem()
    {
      return item;
    }

    /// <summary>
    /// Returns all the items that are in the _items collection
    /// </summary>
    /// <returns>Config objects</returns>
    public Dictionary<string, string> GetItems()
    {
      return _items;
    }

    /// <summary>
    /// Reset the internal items array
    /// </summary>
    public void ClearItems()
    {
      this._items = new Dictionary<string, string>();
    }

    /// <summary>
    /// Add a new item to the _items collection
    /// </summary>
    /// <param name="key">String name for the key</param>
    /// <param name="value">String value</param>
    public void AddItem(string key, string value)
    { 
      _items.Add(key, value);
    }

    /// <summary>
    /// Method to process the request that has come into the API
    /// </summary>
    /// <param name="req">Request object</param>
    /// <param name="table">Table object on which operations will be performed</param>
    /// <param name="log">Logger interface</param>
    /// <param name="identifier">Identifier passed into the API</param>
    /// <param name="category">Category passed into the request</param>
    /// <returns>HttpResponseMessage to return to the client</returns>
    public async Task<HttpResponseMessage> Process(HttpRequestMessage req, CloudTable table, ILogger log, string identifier, string category)
    {
      // Initialise variables
      HttpResponseMessage response = null;
      ResponseMessage msg = new ResponseMessage();

      // Create a dataservice object to use
      DataService ds = new DataService(table, this);

      // Using the method of the request determine if retrieving or upserting an item
      switch (req.Method.Method)
      {
        case "GET":

          dynamic result;

          // attempt to get the data based on whether the identifier is set or not
          if (String.IsNullOrEmpty(identifier))
          {

            log.LogInformation("All configs have been requested from the '{0}' category", category);

            // no identifier has been supplied so retrieve all records
            result = await ds.GetAll(category);
          }
          else
          {
            log.LogInformation("Configuration item has been requested from the '{0}' category: {1}", category, identifier);

            result = await ds.Get(identifier, category);
          }

          // get the response from the datservice and setup the response for the client
          // check that the result is not null, if it is then set the response accordlingly
          msg = ds.GetResponseMessage();
          if (result == null) {
            
            // set the properties on the message object
            msg.SetError(String.Format("Item cannot be found: {0}", identifier), true, HttpStatusCode.NotFound);
            response = msg.CreateResponse();
          }
          else
          {
            response = msg.CreateResponse(result);
          }
          
          break;

        case "POST":
        case "PUT":

          // get the json data from the request
          string json = await req.Content.ReadAsStringAsync();

          // parse the json
          Parse(json);

          // Determine if there are any errors
          msg = GetResponseMessage();
          if (msg.IsError())
          {
            response = msg.CreateResponse();
          }
          else
          {
            // attempt to insert the data
            bool status = await ds.Insert();
          }
          break;

        case "DELETE":

          // attempt to remove the item from the data store
          result = await ds.Delete(identifier, category);

          msg = ds.GetResponseMessage();
          response = msg.CreateResponse();

          break;
      }

      // Return the response
      return response;
    }

    /// <summary>
    /// Parse the JSON string that has been sent into the API
    /// </summary>
    /// <param name="json"></param>
    public void Parse(string json)
    {
      // Attempt to deserialise the JSON into a data dicrionary
      Dictionary<string, string> data = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

      // Retrieve the partition key from the class
      // This is so that the incoming objects do not need to specify the key and the code itself
      // can look after this
      string partitionKey = GetPartitionKey();

      // If the data contains a category overwrite the partition key and remove the category from the data
      if (data.ContainsKey("category"))
      {
        partitionKey = data["category"];
        data.Remove("category");
      }

      // Create a new Config item
      item = new Config(partitionKey);

      // Iterate around the data
      Dictionary<string, string>.KeyCollection keys = data.Keys;
      foreach (string key in keys)
      {
        item.SetRowKey(key);
        item.SetValue(data[key]);
      }
    }

    /// <summary>
    /// Return the response that has been built up by the class
    /// </summary>
    /// <returns>ResponseMessage</returns>
    public ResponseMessage GetResponseMessage()
    {
      return _response;
    }


  }
}