using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;

namespace CAMSA.Functions {

  /// <summary>
  /// ResponseMessage class
  /// 
  /// This class is used to build up the response to be sent to the client.
  /// It can set errors and the HTTP code for any response.
  /// Additionally it can send files to the client for download.
  /// </summary>
  public class ResponseMessage
  {
    /// <summary>
    /// Holds the message that is to be returned to the client
    /// It is a dictionary so that it can be deserialised into JSON easily
    /// </summary>
    /// <typeparam name="string">Value to be set for the key in the dictionary</typeparam>
    /// <typeparam name="dynamic">Dynamic value to be assigned to the key, this can be anytype of data</typeparam>
    private Dictionary<string, dynamic> _message = new Dictionary<string, dynamic>();

    /// <summary>
    /// Simple boolean value to denote if there is an error in the response
    /// This is used internally to determine the status code and the message that is
    /// returned to the client
    /// 
    /// See <see cref="ResponseMessage.SetError(bool)" /> or <see cref="ResponseMessage.SetError(string, bool, HttpStatusCode)" /> for
    /// to set the error property in the class.
    /// </summary>
    /// <value>
    /// Set whether there is an error. Default: false
    /// </value>
    /// <see>
    /// 
    private bool _isError = false;

    /// <summary>
    /// HTTP Status code property. This is set within the class and is used to return the 
    /// correct status code for the client.
    /// </summary>
    private HttpStatusCode _httpStatusCode = HttpStatusCode.OK;

    /// <summary>
    /// Constructor which allows the message, whether in error and the status code to be set
    /// This is a shortcut to allow the creation of the ResponseMessage with the message etc
    /// in one step so that a result can be returned easily.
    /// </summary>
    /// <param name="message">String of the message that needs to be returned</param>
    /// <param name="error">Boolean value stating if the response is in error</param>
    /// <param name="code">Http Status code to return</param>
    public ResponseMessage(string message, bool error, HttpStatusCode code)
    {
      SetError(message, error, code);
    }

    /// <summary>
    /// Override constructor to create a simple ResponseMesage object
    /// </summary>
    public ResponseMessage()
    {}

    /// <summary>
    /// Method returning a boolean stating if the response message is in error
    /// </summary>
    /// <returns>Boolean stating if in error or not</returns>
    public bool IsError()
    {
      return _isError;
    }

    /// <summary>
    /// Simple method to set the error property on the class
    /// </summary>
    /// <param name="error">Boolean value for the error. Default: true</param>
    public void SetError(bool error = true)
    {
      _isError = true;
    }

    /// <summary>
    /// Override method to set the error on the response with the message, error and StatusCode
    /// This is called by the constructor as the boolean value is passed to denote error or not
    /// </summary>
    /// <param name="message">String of the message that needs to be returned</param>
    /// <param name="error">Boolean value stating if the response is in error</param>
    /// <param name="code">Http Status code to return</param>
    public void SetError(string message, bool error, HttpStatusCode code)
    {
      SetMessage(message);
      _isError = error;
      _httpStatusCode = code;
    }

    /// <summary>
    /// Set a string message to be returned.
    /// 
    /// If the message attribute of the Dictionary has not been set it will be added as a new
    /// element. If it does exist then it will be overwtitten.
    /// </summary>
    /// <param name="message">String message</param>
    public void SetMessage(string message)
    {
      if (_message.ContainsKey("message"))
      {
        _message["message"] = message;
      }
      else
      {
        _message.Add("message", message);
      }
    }

    /// <summary>
    /// Sets the status code for the response
    /// </summary>
    /// <param name="code">HttpStatusCode object</param>
    public void SetStatusCode(HttpStatusCode code)
    {
      _httpStatusCode = code;
    }

    /// <summary>
    /// Returns an HttpResponseMessage to returned to the client
    /// If no data is passed then the _message object is serialised and retruned
    /// If a data object is supplied this is serialised and returned. This is used when returning data
    /// from the database for example.
    /// 
    /// All data is returned as JSON
    /// </summary>
    /// <param name="data">Dynamic object to return to the client</param>
    /// <returns>HttpResponseMessage with the specified data returned as JSON and with the status code</returns>
    public HttpResponseMessage CreateResponse(dynamic data = null)
    {
      string content;

      if (data == null) 
      {
        _message.Add("error", _isError);
        content = JsonConvert.SerializeObject(_message);
      }
      else 
      {
        if (IsError())
        {
          _message.Add("error", true);
          content = JsonConvert.SerializeObject(_message);
        }
        else
        {
          content = JsonConvert.SerializeObject(data);
        }
      }

      return new HttpResponseMessage(_httpStatusCode)
      {
        Content = new StringContent(content, Encoding.UTF8, "application/json")
      };
    }

    /// <summary>
    /// Override CreateResponse method to send a file to the calling client
    /// This is primarily used to download the generated Starter Kit for Chef
    /// </summary>
    /// <param name="path">Path to the file to send to the client</param>
    /// <returns>HttpResponseMessage</returns>
    public HttpResponseMessage CreateResponse(string path)
    {
      HttpResponseMessage response = new HttpResponseMessage();

      // Create a data stream of the specified file, it if it exists
      if (File.Exists(path))
      {
        // Create a data stream of the file
        Byte[] dataBytes = File.ReadAllBytes(path);
        MemoryStream dataStream = new MemoryStream(dataBytes);

        // Update the response object with the file stream
        response.Content = new StreamContent(dataStream);
        response.Content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment");
        response.Content.Headers.ContentDisposition.FileName = Path.GetFileName(path);
        response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

      }
      else
      {
        response.StatusCode = HttpStatusCode.NotFound;
      }

      return response;
    }
  }
}