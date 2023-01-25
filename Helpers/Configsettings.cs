
using System;
using System.Collections.Generic;
using System.Text;

namespace B2CCustomPolicy.Helpers
{
    public class Configsettings
    {

        public static string TenantId { get { return System.Environment.GetEnvironmentVariable("Instance"); } }

        public static string ClientId { get { return System.Environment.GetEnvironmentVariable("ClientId"); } }


        public static string ClientSecret { get { return System.Environment.GetEnvironmentVariable("ClientSecret"); } }


        public static string SignUpSignInPolicyId { get { return System.Environment.GetEnvironmentVariable("SignUpSignInPolicyId", EnvironmentVariableTarget.Process); } }


        public static string Domain { get { return System.Environment.GetEnvironmentVariable("Domain", EnvironmentVariableTarget.Process); } }


        public static string BASIC_AUTH_USERNAME { get { return System.Environment.GetEnvironmentVariable("BASIC_AUTH_USERNAME", EnvironmentVariableTarget.Process); } }

        public static string BASIC_AUTH_PASSWORD { get { return System.Environment.GetEnvironmentVariable("BASIC_AUTH_PASSWORD", EnvironmentVariableTarget.Process); } }

         
    }
}
