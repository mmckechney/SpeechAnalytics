using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using SpeechAnalyticsLibrary;
using SpeechAnalyticsLibrary.Models;

namespace SpeechAnalyticsLibrary
{
   public class IdentityHelper
   {
      public IdentityHelper(ILogger<IdentityHelper> log, AnalyticsSettings settings)
      {
         this.log = log;
            this.TenantId = settings.CosmosDB.TenantId;
      }
      private CancellationTokenSource src = new CancellationTokenSource();
      private ILogger log;
      private string _managedIdentityClientId = string.Empty;
      public string ManagedIdentityClientId
      {
         get
         {
            return _managedIdentityClientId;
         }
         set
         {
            _managedIdentityClientId = value;
            if (!string.IsNullOrEmpty(_managedIdentityClientId))
            {
               log.LogInformation($"ManagedIdentityClientId value set to {_managedIdentityClientId}");
            }
         }
      }
      private string _tenantId = string.Empty;
      public string TenantId
      {
         get => _tenantId; 
         set
         {
            _tenantId = value;
            if (!string.IsNullOrEmpty(_tenantId))
            {
               log.LogInformation($"TenantId value set to {_tenantId}");
            }
         }
      }

      private TokenCredential _tokenCred = null;
      internal TokenCredential TokenCredential
      {
         get
         {

            if (_tokenCred == null)
            {

               if (string.IsNullOrWhiteSpace(ManagedIdentityClientId))
               {
                  if (!string.IsNullOrEmpty(TenantId))
                  {
                     log.LogInformation($"Creating DefaultAzureCredential for Tenant '{TenantId}', no ManagedIdentityClientId specified");
                     _tokenCred = new DefaultAzureCredential(new DefaultAzureCredentialOptions() { TenantId = TenantId });
                  }
                  else
                  {
                     log.LogInformation("Creating DefaultAzureCredential, no ManagedIdentityClientId specified");
                     _tokenCred = new DefaultAzureCredential();
                  }

               }
               else
               {
                  AzureCliCredentialOptions cliOpts = new AzureCliCredentialOptions();
                  AzurePowerShellCredentialOptions pwshOpts = new AzurePowerShellCredentialOptions();
                  if (!string.IsNullOrEmpty(TenantId))
                  {
                     cliOpts.TenantId = TenantId;
                     pwshOpts.TenantId = TenantId;
                  }

                  _tokenCred = new ChainedTokenCredential(new AzureCliCredential(cliOpts), new ManagedIdentityCredential(ManagedIdentityClientId = ManagedIdentityClientId), new AzurePowerShellCredential(pwshOpts));
                  log.LogInformation($"Creating ChainedTokenCredential with ManagedIdentityClientId of: '{ManagedIdentityClientId}'");
               }
            }
            return _tokenCred;
         }
      }
   }
}
