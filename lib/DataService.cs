
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CAMSA.Functions
{

  public class DataService
  {

    private static ResponseMessage _response = new ResponseMessage();

    public ResponseMessage GetResponseMessage() 
    {
      return _response;
    }

    private CloudTable _table;
    private IEntity _entity;

    public DataService(CloudTable table, IEntity entity)
    {
      _table = table;
      _entity = entity;
    }


    /// <summary>
    /// Search for the requested item in the table and return it to the calling client
    /// </summary>
    /// <param name="table">Table object</param>
    /// <param name="entity">Entity object</param>
    /// <param name="identifier">Identifier used to find the item</param>
    /// <param name="category">Category, if specified, to be used as the Partition Key</param>
    /// <returns></returns>
    public async Task<dynamic> Get(string identifier, string category, bool returnObject = false)
    {
      // Initialise variables
      dynamic doc = null;
      string partitionKey = GetPartitionKey(category);
      _response = new ResponseMessage();
      // _response.SetError("", false, System.Net.HttpStatusCode.OK);

      // Retrieve the chosen value from the table
      TableOperation operation = TableOperation.Retrieve<Config>(partitionKey, identifier);
      TableResult result = await _table.ExecuteAsync(operation);

      // if a result has been found, get the data
      // otherwise set an error on the response object
      if (result.Result == null)
      {
        _response.SetError(
          string.Format("Unable to find item: {0}", identifier), 
          true,
          System.Net.HttpStatusCode.NotFound
        );
      }
      else
      {
        doc = (Config) result.Result;

        if (!returnObject)
        {
          // Create a dictionary to hold the return data
          // this is so that it is in the correct format to be consumed by the setup processes for CAMSA
          Dictionary<string, string> data = new Dictionary<string, string>();
          data.Add(identifier, doc.Value);

          doc = data;
        }
      }

      return doc;
    }

    /// <summary>
    /// GetAll the items in the table that correspond to the entity default partitionkey or the category
    /// </summary>
    /// <param name="table">Table object</param>
    /// <param name="entity">Entity type being sought</param>
    /// <param name="category">Category to be used instead of the default partition key</param>
    /// <returns></returns>
    public async Task<dynamic> GetAll(string category = null)
    {
      string partitionKey = GetPartitionKey(category);
      _response = new ResponseMessage();

      // clear all items from the entity
      // _entity.ClearItems();

      // Retrieve all the items from the table
      TableQuery<Config> query = new TableQuery<Config>().Where(
        TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey)
      );

      // Iterate around the table and get all the values
      TableContinuationToken token = null;
      do
      { 
        TableQuerySegment<Config> resultSegment = await _table.ExecuteQuerySegmentedAsync(query, token);
        token = resultSegment.ContinuationToken;

        foreach (Config item in resultSegment.Results)
        {
          _entity.AddItem(item.RowKey, item.Value);
        }
      } while (token != null);

      // return all the items
      return _entity.GetItems();
    }

    /// <summary>
    /// Method to insert new items into the table
    /// </summary>
    /// <param name="table">Table object</param>
    /// <param name="entity">Entity object</param>
    /// <returns></returns>
    public async Task<bool> Insert()
    {
     TableOperation insertOperation = TableOperation.InsertOrReplace(_entity.GetItem());

     await _table.ExecuteAsync(insertOperation);

     return true;
    }

    public async Task<bool> Delete(string identifier, string category) 
    {
      bool status = false;

      // Retrieve the item so it can be deleted
      dynamic item = await Get(identifier, category, true);

      if (item != null)
      {
        status = true;
        TableOperation deleteOperation = TableOperation.Delete(item);
        await _table.ExecuteAsync(deleteOperation);
      }

      return status;
    }

    /// <summary>
    /// Any Entity that is passed to the class will have a default partition key, however this
    /// can be overridden if an item has been assigned to a different category.
    /// This method returns the key based on the category if it has been specified or the entity default
    /// </summary>
    /// <param name="entity">Entity object</param>
    /// <param name="category">String category</param>
    /// <returns></returns>
    private string GetPartitionKey(string category)
    {
      string partitionKey = String.Empty;

      // Get the partition key from the entity so that the query against the table
      // can be run correctly
      if (String.IsNullOrEmpty(category))
      {
        partitionKey = _entity.GetPartitionKey();
      } 
      else
      {
        partitionKey = category;
      }

      return partitionKey;
    }
  }
}