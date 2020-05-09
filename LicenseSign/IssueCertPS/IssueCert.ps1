#https://docs.microsoft.com/en-us/powershell/module/pkiclient/new-selfsignedcertificate?view=win10-ps

$dateOfExpiration = New-Object DateTime 2030, 12, 31, 23, 59, 59, ([DateTimeKind]::Utc)

New-SelfSignedCertificate -Subject "AppDynamics DEXTER" -KeyFriendlyName "AppDynamics DEXTER" -CertStoreLocation "cert:\CurrentUser\My" -KeyExportPolicy Exportable -NotAfter $dateOfExpiration