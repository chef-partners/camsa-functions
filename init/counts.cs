using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;

namespace CAMSA.Functions
{
  public static class counts
  {
    [FunctionName("counts")]
    public static async void Run(
        [TimerTrigger("0 */5 * * * *")]
        TimerInfo myTimer,
        [Table("settings")] CloudTable settingTable,
        ILogger log)
    {
      // Get the current counts from the Automate Server
      await AutomateCounts.Process(settingTable, log);
    }
  }
}
