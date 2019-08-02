using Microsoft.Extensions.Logging;
using System;
using System.Text.RegularExpressions;
using System.Globalization;

namespace CAMSA.Functions
{
  public static class AutomateLogParser
  {
    public static AutomateMessage ParseGenericLogMessage(string logMessage, string customerName, string subscriptionId, ILogger log)
    {
      if (logMessage.Contains("automate-load-balancer"))
      {
        return ParesAutomateLoadBalancerLog(logMessage, customerName, subscriptionId, log);
      }
      else
      {
        AutomateMessage automateMessage = new AutomateMessage();
        automateMessage.sourcePackage = "Unknown Entry";
        return automateMessage;
      }
    }

    public static AutomateMessage ParesAutomateLoadBalancerLog(string logMessage, string customerName, string subscriptionId, ILogger log)
    {
      Regex rx = new Regex(@"(.*) \[(.*)\]  ""(.*)"" (\d+) ""(.*)"" (\d+) ""(.*)"" ""(.*)"" ""(.*)"" ""(.*)"" ""(.*)"" (\d+)");
      Match m = rx.Match(logMessage);

      CultureInfo provider = CultureInfo.InvariantCulture;
      string dateFormat = "dd/MMM/yyyy:HH:mm:ss +ffff";
      log.LogDebug("==D A T E T I M E======================================================================");
      log.LogDebug(m.Groups[2].ToString());
      log.LogDebug("==D A T E T I M E======================================================================");

      AutomateMessage automateMessage = new AutomateMessage();
      automateMessage.sourcePackage = "automate-load-balancer";
      automateMessage.time = DateTime.ParseExact(m.Groups[2].ToString(), dateFormat, CultureInfo.InvariantCulture);
      automateMessage.message = m.Groups[3].ToString();
      automateMessage.status = m.Groups[4].ToString();
      automateMessage.requestTime = System.Convert.ToDecimal(m.Groups[5].ToString());
      automateMessage.customerName = customerName;
      automateMessage.subscriptionId = subscriptionId;
      return automateMessage;
    }
  }
}