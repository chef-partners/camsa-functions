using System.Net;
using System.Text.RegularExpressions;

namespace CAMSA.Functions
{
  public class AutomateMessage : BaseMessage, IMessage {
    public string sourcePackage { get; set; }
    public string logLevel { get; set; }
    public string message { get; set; }
    public string job { get; set; }
    public string function { get; set; }
    public string status { get; set; }
    public decimal requestTime { get; set; }

    public string GetLogFriendlyPackageName(){
        string str = sourcePackage;
        
        Regex rgx = new Regex("[^a-zA-Z0-9]");
        str = rgx.Replace(str, "") + "log";

        return str;
    }
}
}