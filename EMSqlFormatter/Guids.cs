using System;

namespace EMSqlFormatter
{
    internal static class Guids
    {
        public const string sPackageGuidString =
            "ca40bbd5-c3e6-4aab-8922-bff7087ccf01";

        public const string sCommandSetGuidString =
            "4c10404c-f9f3-475e-9bb5-1301d4384735";

        public static readonly Guid oCommandSetGuid =
            new Guid(sCommandSetGuidString);
    }
}
