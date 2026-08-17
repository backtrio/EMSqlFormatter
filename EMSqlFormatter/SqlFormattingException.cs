using System;

namespace EMSqlFormatter
{
    internal sealed class SqlFormattingException : Exception
    {
        public SqlFormattingException(string strMessage)
            : base(strMessage)
        {
        }
    }
}
