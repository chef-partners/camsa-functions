using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace CAMSA.Functions
{

  public interface IEntity
  {
    string GetPartitionKey();
    void Parse(string json);
    ResponseMessage GetResponseMessage();
    Config GetItem();
    void AddItem(string key, string value);
    void ClearItems();
    Dictionary<string, string> GetItems();
    Task<HttpResponseMessage> Process(HttpRequestMessage req, CloudTable table, ILogger log, string identifier, string category);
  }
}