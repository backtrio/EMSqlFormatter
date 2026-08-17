using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace EMSqlFormatter
{
    internal static class SqlFormatterService
    {
        private const int intMaximumDisplayedErrors = 10;

        public static string Format(string strSql, SqlFormatterSettings objSettings)
        {
            if (string.IsNullOrWhiteSpace(strSql))
            {
                return strSql;
            }

            if (objSettings == null)
            {
                throw new ArgumentNullException(nameof(objSettings));
            }

            TSqlParser objParser = CreateParser(objSettings.enCompatibilityLevel);
            TSqlFragment objFragment;
            IList<ParseError> lstErrors;

            using (StringReader objReader = new StringReader(strSql))
            {
                objFragment = objParser.Parse(objReader, out lstErrors);
            }

            ThrowIfParseErrors(lstErrors);

            SqlScriptGeneratorOptions objGeneratorOptions =
                CreateGeneratorOptions(objSettings);

            SqlScriptGenerator objGenerator =
                CreateGenerator(objSettings.enCompatibilityLevel, objGeneratorOptions);

            objGenerator.GenerateScript(objFragment, out string strFormattedSql);

            strFormattedSql = SqlCommentPreserver.RestoreComments(
                strSql,
                objFragment,
                strFormattedSql,
                objParser,
                objSettings.enKeywordCasing);

            if (string.IsNullOrWhiteSpace(strFormattedSql))
            {
                throw new SqlFormattingException(
                    "ScriptDom no generó contenido para el SQL recibido. El editor no fue modificado.");
            }

            string strNormalizedSql = NormalizeLineEndings(strFormattedSql).Trim();

            if (HasFinalLineEnding(strSql))
            {
                strNormalizedSql += Environment.NewLine;
            }

            return strNormalizedSql;
        }

        private static void ThrowIfParseErrors(IList<ParseError> lstErrors)
        {
            if (lstErrors == null || lstErrors.Count == 0)
            {
                return;
            }

            string strErrorDetails = string.Join(
                Environment.NewLine,
                lstErrors
                    .Take(intMaximumDisplayedErrors)
                    .Select(objError =>
                        $"Línea {objError.Line}, columna {objError.Column}: {objError.Message}"));

            if (lstErrors.Count > intMaximumDisplayedErrors)
            {
                strErrorDetails += Environment.NewLine +
                    $"Se omitieron {lstErrors.Count - intMaximumDisplayedErrors} errores adicionales.";
            }

            throw new SqlFormattingException(
                "El SQL contiene errores de sintaxis y no fue modificado." +
                Environment.NewLine +
                Environment.NewLine +
                strErrorDetails);
        }

        private static TSqlParser CreateParser(SqlCompatibilityLevel enCompatibilityLevel)
        {
            switch (enCompatibilityLevel)
            {
                case SqlCompatibilityLevel.SqlServer2012:
                    return new TSql110Parser(initialQuotedIdentifiers: true);
                case SqlCompatibilityLevel.SqlServer2014:
                    return new TSql120Parser(initialQuotedIdentifiers: true);
                case SqlCompatibilityLevel.SqlServer2016:
                    return new TSql130Parser(initialQuotedIdentifiers: true);
                case SqlCompatibilityLevel.SqlServer2017:
                    return new TSql140Parser(initialQuotedIdentifiers: true);
                case SqlCompatibilityLevel.SqlServer2019:
                    return new TSql150Parser(initialQuotedIdentifiers: true);
                case SqlCompatibilityLevel.SqlServer2025:
                    return new TSql170Parser(initialQuotedIdentifiers: true);
                case SqlCompatibilityLevel.SqlServer2022:
                default:
                    return new TSql160Parser(initialQuotedIdentifiers: true);
            }
        }

        private static SqlScriptGenerator CreateGenerator(
            SqlCompatibilityLevel enCompatibilityLevel,
            SqlScriptGeneratorOptions objOptions)
        {
            switch (enCompatibilityLevel)
            {
                case SqlCompatibilityLevel.SqlServer2012:
                    return new Sql110ScriptGenerator(objOptions);
                case SqlCompatibilityLevel.SqlServer2014:
                    return new Sql120ScriptGenerator(objOptions);
                case SqlCompatibilityLevel.SqlServer2016:
                    return new Sql130ScriptGenerator(objOptions);
                case SqlCompatibilityLevel.SqlServer2017:
                    return new Sql140ScriptGenerator(objOptions);
                case SqlCompatibilityLevel.SqlServer2019:
                    return new Sql150ScriptGenerator(objOptions);
                case SqlCompatibilityLevel.SqlServer2025:
                    return new Sql170ScriptGenerator(objOptions);
                case SqlCompatibilityLevel.SqlServer2022:
                default:
                    return new Sql160ScriptGenerator(objOptions);
            }
        }

        private static SqlScriptGeneratorOptions CreateGeneratorOptions(
            SqlFormatterSettings objSettings)
        {
            SqlScriptGeneratorOptions objOptions = new SqlScriptGeneratorOptions
            {
                SqlVersion = GetSqlVersion(objSettings.enCompatibilityLevel),
                SqlEngineType = SqlEngineType.All,
                KeywordCasing = GetKeywordCasing(objSettings.enKeywordCasing),
                IndentationSize = objSettings.intIndentationSize,
                NumNewlinesAfterStatement = objSettings.intNewLinesAfterStatement,
                IncludeSemicolons = false,
                AlignClauseBodies = objSettings.boolAlignClauseBodies,
                AlignColumnDefinitionFields = objSettings.boolAlignColumnDefinitionFields,
                AsKeywordOnOwnLine = objSettings.boolAsKeywordOnOwnLine,
                MultilineSelectElementsList = objSettings.boolMultilineSelectElementsList,
                MultilineWherePredicatesList = objSettings.boolMultilineWherePredicatesList,
                NewLineBeforeFromClause = objSettings.boolNewLineBeforeFromClause,
                NewLineBeforeJoinClause = objSettings.boolNewLineBeforeJoinClause,
                NewLineBeforeWhereClause = objSettings.boolNewLineBeforeWhereClause,
                NewLineBeforeGroupByClause = objSettings.boolNewLineBeforeGroupByClause,
                NewLineBeforeHavingClause = objSettings.boolNewLineBeforeHavingClause,
                NewLineBeforeOrderByClause = objSettings.boolNewLineBeforeOrderByClause,
                AlignSetClauseItem = objSettings.boolAlignSetClauseItem,
                IndentSetClause = objSettings.boolIndentSetClause,
                MultilineSetClauseItems = objSettings.boolMultilineSetClauseItems,
                MultilineInsertTargetsList = objSettings.boolMultilineInsertTargetsList,
                MultilineInsertSourcesList = objSettings.boolMultilineInsertSourcesList,
                NewLineBeforeOpenParenthesisInMultilineList =
                    objSettings.boolNewLineBeforeOpenParenthesis,
                NewLineBeforeCloseParenthesisInMultilineList =
                    objSettings.boolNewLineBeforeCloseParenthesis
            };

            SetOptionalBooleanOption(
                objOptions,
                "SpaceBetweenDataTypeAndParameters",
                objSettings.boolSpaceBetweenDataTypeAndParameters);

            SetOptionalBooleanOption(
                objOptions,
                "SpaceBetweenParametersInDataType",
                objSettings.boolSpaceBetweenParametersInDataType);

            return objOptions;
        }

        private static void SetOptionalBooleanOption(
            SqlScriptGeneratorOptions objOptions,
            string strPropertyName,
            bool boolValue)
        {
            System.Reflection.PropertyInfo objProperty =
                objOptions.GetType().GetProperty(strPropertyName);

            if (objProperty == null ||
                !objProperty.CanWrite ||
                objProperty.PropertyType != typeof(bool))
            {
                return;
            }

            objProperty.SetValue(objOptions, boolValue, null);
        }

        private static KeywordCasing GetKeywordCasing(SqlKeywordCasing enKeywordCasing)
        {
            switch (enKeywordCasing)
            {
                case SqlKeywordCasing.Lowercase:
                    return KeywordCasing.Lowercase;
                case SqlKeywordCasing.PascalCase:
                    return KeywordCasing.PascalCase;
                case SqlKeywordCasing.Uppercase:
                default:
                    return KeywordCasing.Uppercase;
            }
        }

        private static SqlVersion GetSqlVersion(SqlCompatibilityLevel enCompatibilityLevel)
        {
            switch (enCompatibilityLevel)
            {
                case SqlCompatibilityLevel.SqlServer2012:
                    return SqlVersion.Sql110;
                case SqlCompatibilityLevel.SqlServer2014:
                    return SqlVersion.Sql120;
                case SqlCompatibilityLevel.SqlServer2016:
                    return SqlVersion.Sql130;
                case SqlCompatibilityLevel.SqlServer2017:
                    return SqlVersion.Sql140;
                case SqlCompatibilityLevel.SqlServer2019:
                    return SqlVersion.Sql150;
                case SqlCompatibilityLevel.SqlServer2025:
                    return SqlVersion.Sql170;
                case SqlCompatibilityLevel.SqlServer2022:
                default:
                    return SqlVersion.Sql160;
            }
        }

        private static bool HasFinalLineEnding(string strText)
        {
            return strText.EndsWith("\r\n", StringComparison.Ordinal) ||
                   strText.EndsWith("\n", StringComparison.Ordinal) ||
                   strText.EndsWith("\r", StringComparison.Ordinal);
        }

        private static string NormalizeLineEndings(string strText)
        {
            return strText
                .Replace("\r\n", "\n")
                .Replace("\r", "\n")
                .Replace("\n", Environment.NewLine);
        }
    }
}
