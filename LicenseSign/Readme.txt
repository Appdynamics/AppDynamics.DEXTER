First, issue the new certificate via running IssueCertificate console exe
That is in .NET 4.7 so windows only
Somehow when you issue it this way, export and then reimport by hand the private key is still readable

BTW 
Tried issuing certificates via PowerShell (IssueCertPS) and it works, but when exported and then reimported, the private key 
is not parseable

Second, import the certificate into My personal store. Only from there will it have private key readable

Third, sign the license file via LicenseSign
This is .NET Core 3.1
Uses private key from the cert in My personal store to sign the license file

Fourth, read the license file and validate signature from DEXTER