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



------Updated Instructions 9/23/2020---------
There should never be a need to issue a new cert with the IssueCertificate program.
- Valid public cert is available in Dexter root project directory AppDynamics.DEXTER.public.cer
- Valid private cert is hosted on AppDynamics Google Drive. 
    - email: leon.muntingh@appdynamics.com for access

Signing a new license can be done in two ways
1. If private cert is installed in your local PC with export parameters enabled.
   - path_to_license_file: license file on which to overwrite the "Signature" field
        - default choose from any json from LicenseSign/LicenseFiles/*.
        - make sure to verify "ExpirationDateTime".
        - new license will OVERWRITE old license.
   - Name of the private cert installed in your PC cert store.

2. If you have private cert file located in your PC and you know the password of the cert file
Run LicenseSign.LocalCert with the following arguments [path_to_license_file path_to_private_certificate password_of_private_certificate]
    - path_to_license_file: license file on which to overwrite the "Signature" field
        - default choose from any json from LicenseSign/LicenseFiles/*
        - make sure to verify "ExpirationDateTime"
        - new license will OVERWRITE old license
    - path_to_private_certificate: from Google Drive located in LicenseSign/IssueCertificate/bin/Debug/AppDynamics.DEXTER.private.pfx
    - password_of_private_certificate: from Google Drive located in privatekeypassword.txt