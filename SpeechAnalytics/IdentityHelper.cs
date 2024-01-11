using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SpeechAnalytics
{
   public class IdentityHelper
   {
      public IdentityHelper(ILogger<IdentityHelper> log)
      {
         this.log = log;
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
      private string _tenantId = String.Empty;
      public string TenantId
      {
         get => _tenantId; set
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

               if (string.IsNullOrWhiteSpace(this.ManagedIdentityClientId))
               {
                  if (!string.IsNullOrEmpty(this.TenantId))
                  {
                     log.LogInformation($"Creating DefaultAzureCredential for Tenant '{this.TenantId}', no ManagedIdentityClientId specified");
                     _tokenCred = new DefaultAzureCredential(new DefaultAzureCredentialOptions() { TenantId = this.TenantId });
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
                  if (!string.IsNullOrEmpty(this.TenantId))
                  {
                     cliOpts.TenantId = this.TenantId;
                     pwshOpts.TenantId = this.TenantId;
                  }

                  _tokenCred = new ChainedTokenCredential(new AzureCliCredential(cliOpts), new ManagedIdentityCredential(ManagedIdentityClientId = this.ManagedIdentityClientId), new AzurePowerShellCredential(pwshOpts));
                  log.LogInformation($"Creating ChainedTokenCredential with ManagedIdentityClientId of: '{this.ManagedIdentityClientId}'");
               }
            }
            return _tokenCred;
         }
      }
   }
}
