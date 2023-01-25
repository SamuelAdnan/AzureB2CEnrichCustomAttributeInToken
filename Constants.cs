using System;
using System.Collections.Generic;
using System.Text;

namespace B2CCustomPolicy
{
    public static class Constants
    {
        public static class ClaimTypes
        {
            public const string ObjectId = "oid";
        }

        public static class UserAttributes
        {
            public const string MobileAtt = nameof(MobileAtt);
            public const string DelegatedUserManagementRole = nameof(DelegatedUserManagementRole);
            public const string InvitationCode = nameof(InvitationCode);
            public const string CompanyId = nameof(CompanyId);
        }

       
    }
}
