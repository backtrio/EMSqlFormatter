using System.ComponentModel;
using Microsoft.VisualStudio.Shell;

namespace EMSqlFormatter
{
    public sealed class SqlFormatterOptionsPage : DialogPage
    {
        private int intIndentationSizeValue = 4;
        private int intNewLinesAfterStatementValue = 1;

        [Category("General")]
        [DisplayName("Compatibilidad T-SQL")]
        [Description("Versión de SQL Server utilizada para analizar y generar el código.")]
        [DefaultValue(SqlCompatibilityLevel.SqlServer2022)]
        public SqlCompatibilityLevel enCompatibilityLevel { get; set; } =
            SqlCompatibilityLevel.SqlServer2022;

        [Category("General")]
        [DisplayName("Mayúsculas y minúsculas")]
        [Description("Formato aplicado a las palabras reservadas de T-SQL.")]
        [DefaultValue(SqlKeywordCasing.Uppercase)]
        public SqlKeywordCasing enKeywordCasing { get; set; } =
            SqlKeywordCasing.Uppercase;

        [Category("General")]
        [DisplayName("Tamaño de indentación")]
        [Description("Cantidad de espacios por nivel. Valores permitidos: 1 a 8.")]
        [DefaultValue(4)]
        public int intIndentationSize
        {
            get => intIndentationSizeValue;
            set => intIndentationSizeValue = Limit(value, 1, 8);
        }

        [Category("General")]
        [DisplayName("Saltos después de cada sentencia")]
        [Description("Cantidad de saltos de línea después de una sentencia. Valores permitidos: 1 a 3.")]
        [DefaultValue(1)]
        public int intNewLinesAfterStatement
        {
            get => intNewLinesAfterStatementValue;
            set => intNewLinesAfterStatementValue = Limit(value, 1, 3);
        }

        [Category("SELECT y cláusulas")]
        [DisplayName("Alinear cuerpos de cláusulas")]
        [DefaultValue(false)]
        public bool boolAlignClauseBodies { get; set; }

        [Category("Definiciones")]
        [DisplayName("Alinear campos de columnas")]
        [DefaultValue(false)]
        public bool boolAlignColumnDefinitionFields { get; set; }

        [Category("SELECT y cláusulas")]
        [DisplayName("AS en línea independiente")]
        [DefaultValue(false)]
        public bool boolAsKeywordOnOwnLine { get; set; }

        [Category("SELECT y cláusulas")]
        [DisplayName("Columnas SELECT en múltiples líneas")]
        [DefaultValue(true)]
        public bool boolMultilineSelectElementsList { get; set; } = true;

        [Category("SELECT y cláusulas")]
        [DisplayName("Predicados WHERE en múltiples líneas")]
        [DefaultValue(true)]
        public bool boolMultilineWherePredicatesList { get; set; } = true;

        [Category("SELECT y cláusulas")]
        [DisplayName("Nueva línea antes de FROM")]
        [DefaultValue(true)]
        public bool boolNewLineBeforeFromClause { get; set; } = true;

        [Category("SELECT y cláusulas")]
        [DisplayName("Nueva línea antes de JOIN")]
        [DefaultValue(true)]
        public bool boolNewLineBeforeJoinClause { get; set; } = true;

        [Category("SELECT y cláusulas")]
        [DisplayName("Nueva línea antes de WHERE")]
        [DefaultValue(true)]
        public bool boolNewLineBeforeWhereClause { get; set; } = true;

        [Category("SELECT y cláusulas")]
        [DisplayName("Nueva línea antes de GROUP BY")]
        [DefaultValue(true)]
        public bool boolNewLineBeforeGroupByClause { get; set; } = true;

        [Category("SELECT y cláusulas")]
        [DisplayName("Nueva línea antes de HAVING")]
        [DefaultValue(true)]
        public bool boolNewLineBeforeHavingClause { get; set; } = true;

        [Category("SELECT y cláusulas")]
        [DisplayName("Nueva línea antes de ORDER BY")]
        [DefaultValue(true)]
        public bool boolNewLineBeforeOrderByClause { get; set; } = true;

        [Category("UPDATE")]
        [DisplayName("Alinear elementos SET")]
        [DefaultValue(true)]
        public bool boolAlignSetClauseItem { get; set; } = true;

        [Category("UPDATE")]
        [DisplayName("Indentar cláusula SET")]
        [DefaultValue(false)]
        public bool boolIndentSetClause { get; set; }

        [Category("UPDATE")]
        [DisplayName("Elementos SET en múltiples líneas")]
        [DefaultValue(true)]
        public bool boolMultilineSetClauseItems { get; set; } = true;

        [Category("INSERT")]
        [DisplayName("Columnas destino en múltiples líneas")]
        [DefaultValue(true)]
        public bool boolMultilineInsertTargetsList { get; set; } = true;

        [Category("INSERT")]
        [DisplayName("Valores de origen en múltiples líneas")]
        [DefaultValue(true)]
        public bool boolMultilineInsertSourcesList { get; set; } = true;

        [Category("Listas y tipos")]
        [DisplayName("Nueva línea después de abrir paréntesis")]
        [DefaultValue(false)]
        public bool boolNewLineBeforeOpenParenthesis { get; set; }

        [Category("Listas y tipos")]
        [DisplayName("Nueva línea antes de cerrar paréntesis")]
        [DefaultValue(true)]
        public bool boolNewLineBeforeCloseParenthesis { get; set; } = true;

        [Category("Listas y tipos")]
        [DisplayName("Espacio entre tipo y parámetros")]
        [DefaultValue(true)]
        public bool boolSpaceBetweenDataTypeAndParameters { get; set; } = true;

        [Category("Listas y tipos")]
        [DisplayName("Espacio entre parámetros del tipo")]
        [DefaultValue(true)]
        public bool boolSpaceBetweenParametersInDataType { get; set; } = true;

        internal SqlFormatterSettings CreateSettings()
        {
            return new SqlFormatterSettings
            {
                enCompatibilityLevel = enCompatibilityLevel,
                enKeywordCasing = enKeywordCasing,
                intIndentationSize = intIndentationSize,
                intNewLinesAfterStatement = intNewLinesAfterStatement,
                boolAlignClauseBodies = boolAlignClauseBodies,
                boolAlignColumnDefinitionFields = boolAlignColumnDefinitionFields,
                boolAsKeywordOnOwnLine = boolAsKeywordOnOwnLine,
                boolMultilineSelectElementsList = boolMultilineSelectElementsList,
                boolMultilineWherePredicatesList = boolMultilineWherePredicatesList,
                boolNewLineBeforeFromClause = boolNewLineBeforeFromClause,
                boolNewLineBeforeJoinClause = boolNewLineBeforeJoinClause,
                boolNewLineBeforeWhereClause = boolNewLineBeforeWhereClause,
                boolNewLineBeforeGroupByClause = boolNewLineBeforeGroupByClause,
                boolNewLineBeforeHavingClause = boolNewLineBeforeHavingClause,
                boolNewLineBeforeOrderByClause = boolNewLineBeforeOrderByClause,
                boolAlignSetClauseItem = boolAlignSetClauseItem,
                boolIndentSetClause = boolIndentSetClause,
                boolMultilineSetClauseItems = boolMultilineSetClauseItems,
                boolMultilineInsertTargetsList = boolMultilineInsertTargetsList,
                boolMultilineInsertSourcesList = boolMultilineInsertSourcesList,
                boolNewLineBeforeOpenParenthesis = boolNewLineBeforeOpenParenthesis,
                boolNewLineBeforeCloseParenthesis = boolNewLineBeforeCloseParenthesis,
                boolSpaceBetweenDataTypeAndParameters = boolSpaceBetweenDataTypeAndParameters,
                boolSpaceBetweenParametersInDataType = boolSpaceBetweenParametersInDataType
            };
        }

        private static int Limit(int intValue, int intMinimum, int intMaximum)
        {
            return intValue < intMinimum
                ? intMinimum
                : intValue > intMaximum
                    ? intMaximum
                    : intValue;
        }
    }
}
