namespace EMSqlFormatter
{
    internal sealed class SqlFormatterSettings
    {
        public SqlCompatibilityLevel enCompatibilityLevel { get; set; }
        public SqlKeywordCasing enKeywordCasing { get; set; }
        public int intIndentationSize { get; set; }
        public int intNewLinesAfterStatement { get; set; }
        public bool boolAlignClauseBodies { get; set; }
        public bool boolAlignColumnDefinitionFields { get; set; }
        public bool boolAsKeywordOnOwnLine { get; set; }
        public bool boolMultilineSelectElementsList { get; set; }
        public bool boolMultilineWherePredicatesList { get; set; }
        public bool boolNewLineBeforeFromClause { get; set; }
        public bool boolNewLineBeforeJoinClause { get; set; }
        public bool boolNewLineBeforeWhereClause { get; set; }
        public bool boolNewLineBeforeGroupByClause { get; set; }
        public bool boolNewLineBeforeHavingClause { get; set; }
        public bool boolNewLineBeforeOrderByClause { get; set; }
        public bool boolAlignSetClauseItem { get; set; }
        public bool boolIndentSetClause { get; set; }
        public bool boolMultilineSetClauseItems { get; set; }
        public bool boolMultilineInsertTargetsList { get; set; }
        public bool boolMultilineInsertSourcesList { get; set; }
        public bool boolNewLineBeforeOpenParenthesis { get; set; }
        public bool boolNewLineBeforeCloseParenthesis { get; set; }
        public bool boolSpaceBetweenDataTypeAndParameters { get; set; }
        public bool boolSpaceBetweenParametersInDataType { get; set; }
    }

    public enum SqlCompatibilityLevel
    {
        SqlServer2012 = 110,
        SqlServer2014 = 120,
        SqlServer2016 = 130,
        SqlServer2017 = 140,
        SqlServer2019 = 150,
        SqlServer2022 = 160,
        SqlServer2025 = 170
    }

    public enum SqlKeywordCasing
    {
        Uppercase = 0,
        Lowercase = 1,
        PascalCase = 2
    }
}
