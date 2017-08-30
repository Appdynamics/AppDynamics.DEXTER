using System;
using System.Collections.Generic;

namespace AppDynamics.Dexter
{
    public class CredentialStore
    {
        public List<ControllerCredential> Credentials { get; set; }

        public override string ToString()
        {
            return String.Format("ControllerCredentials: {0}", this.Credentials.Count);
        }
    }
}
