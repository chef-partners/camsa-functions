
namespace CAMSA.Functions
{
  class Constants {
    public static string ConfigStorePartitionKey = "CAMSA";
    public static string APIKeyEnvVarName = "FUNCTIONS_API_KEY";

    // Define chef server keys
    public static string ChefServerFQDNKey = "chefserver_fqdn";

    public static string ChefServerInternalIPAddressKey = "chef_internal_ip";
    public static string UserKey = "user";
    public static string UserKeyKey = "user_key";
    public static string UserPasswordKey = "user_password";
    public static string OrgKey = "org";
    public static string OrgKeyKey = "org_validator_key";

    // Define automate server keys
    public static string AutomateServerFQDNKey = "automate_fqdn";
    public static string AutomateServerInternalIPAddressKey = "automate_internal_ip";
    public static string AutomateCredentialsURLKey = "automate_credentials_url";
    public static string AutomateCredentialsAdminUsernameKey = "automate_credentials_username";
    public static string AutomateCredentialsAdminPasswordKey = "automate_credentials_password";
    public static string AutomateTokenKey = "user_automate_token";
    public static string AutomateLoggingTokenKey = "logging_automate_token";


  }
}