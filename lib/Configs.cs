using Newtonsoft.Json;

namespace CAMSA.Functions
{

  public class Configs
  {
    [JsonIgnore]
    public string subscription_id { get; set; }

    [JsonIgnore]
    public string customer_name { get; set; }

    public string chefserver_fqdn { get; set; }

    public string chef_internal_ip { get; set; }
    public string user { get; set; }
    public string user_key { get; set; }
    public string user_password { get; set; }

    public string org { get; set; }
    public string org_validator_key { get; set; }
    public string automate_fqdn { get; set; }
    public string automate_internal_ip  { get; set; }
    public string automate_credentials_url { get; set; }
    public string automate_credentials_username { get; set; }
    public string automate_credentials_password { get; set; }
    public string user_automate_token { get; set; }
    public string logging_automate_token { get; set; }

    public string chef_server_url { get; set; }
    public string automate_server_url { get; set; }

    public string client_key_filename { get; set; }
    public string org_key_filename { get; set; }

    [JsonIgnore]
    public string workspace_id { get; set; }
    [JsonIgnore]
    public string workspace_key { get; set; }

    [JsonIgnore]
    public string central_workspace_id { get; set;}
    [JsonIgnore]
    public string central_workspace_key { get; set;}

    public void SetProperty(string name, object value)
    {
      var info = this.GetType().GetProperty(name);
      if (info == null) {
        return;
      }
      info.SetValue(this, value);
    }

    public void DeriveServerURLs() {
      chef_server_url = string.Format("https://{0}/organizations/{1}", chefserver_fqdn, org);
      automate_server_url = string.Format("https://{0}", automate_fqdn);
    }

    public bool HasCentralLogging() {
      bool status = false;

      if (string.IsNullOrEmpty(central_workspace_id) == false &&
          string.IsNullOrEmpty(central_workspace_key) == false) {
            status = true;
          }

      return status;
    }

  }
}