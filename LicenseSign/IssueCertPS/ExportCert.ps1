#https://docs.microsoft.com/en-us/powershell/module/pkiclient/new-selfsignedcertificate?view=win10-ps

$cert = Get-ChildItem -Path "cert:\CurrentUser\My" | where {$_.Subject -eq "CN=AppDynamics DEXTER"}
$cert

Export-Certificate -Cert $cert -FilePath "AppDynamics.DEXTER.cer"

$credential = Get-Credential
$password = $credential.Password

Export-PFXCertificate -Cert $cert -FilePath "AppDynamics.DEXTER.pfx" -Password $password
