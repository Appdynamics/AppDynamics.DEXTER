#https://docs.microsoft.com/en-us/powershell/module/pkiclient/new-selfsignedcertificate?view=win10-ps

$credential = Get-Credential
$password = $credential.Password

Import-PfxCertificate -FilePath "AppDynamics.DEXTER.pfx" -CertStoreLocation "cert:\CurrentUser\My" -Exportable -Password $password
