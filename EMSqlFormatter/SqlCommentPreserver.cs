using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace EMSqlFormatter
{
    internal static class SqlCommentPreserver
    {
        public static string RestoreComments(
            string strOriginalSql,
            TSqlFragment objOriginalFragment,
            string strFormattedSql,
            TSqlParser objParser,
            SqlKeywordCasing enKeywordCasing)
        {
            List<TSqlParserToken> lstOriginalTokens =
                objOriginalFragment.ScriptTokenStream?.ToList() ??
                new List<TSqlParserToken>();

            List<TSqlParserToken> lstOriginalComments = lstOriginalTokens
                .Where(IsComment)
                .ToList();

            List<TSqlParserToken> lstGeneratedTokens =
                GetTokenStream(objParser, strFormattedSql);

            List<IndexedToken> lstOriginalSignificantTokens =
                GetSignificantTokens(lstOriginalTokens);

            List<IndexedToken> lstGeneratedSignificantTokens =
                GetSignificantTokens(lstGeneratedTokens);

            TokenAlignment objAlignment = AlignTokens(
                lstOriginalSignificantTokens,
                lstGeneratedSignificantTokens);

            strFormattedSql = ProjectOriginalTokens(
                lstOriginalSignificantTokens,
                lstGeneratedTokens,
                objAlignment,
                enKeywordCasing);

            List<TSqlParserToken> lstFormattedTokens =
                GetTokenStream(objParser, strFormattedSql);

            List<IndexedToken> lstFormattedSignificantTokens =
                GetSignificantTokens(lstFormattedTokens);

            int[] arrTokenMap = BuildTokenMap(
                lstOriginalSignificantTokens,
                lstFormattedSignificantTokens);

            List<CommentInsertion> lstInsertions = BuildInsertions(
                strOriginalSql,
                strFormattedSql,
                lstOriginalTokens,
                lstOriginalSignificantTokens,
                lstFormattedSignificantTokens,
                arrTokenMap);

            string strSqlWithComments = ApplyInsertions(
                strFormattedSql,
                lstInsertions);

            ValidateResult(
                objParser,
                lstOriginalSignificantTokens,
                strFormattedSql,
                strSqlWithComments,
                lstOriginalComments);

            return strSqlWithComments;
        }

        private static List<CommentInsertion> BuildInsertions(
            string strOriginalSql,
            string strFormattedSql,
            List<TSqlParserToken> lstOriginalTokens,
            List<IndexedToken> lstOriginalSignificantTokens,
            List<IndexedToken> lstFormattedSignificantTokens,
            int[] arrTokenMap)
        {
            List<CommentInsertion> lstInsertions =
                new List<CommentInsertion>();

            int intPreviousSignificant = -1;
            int intNextSignificant = 0;

            for (int intTokenIndex = 0;
                 intTokenIndex < lstOriginalTokens.Count;
                 intTokenIndex++)
            {
                while (
                    intNextSignificant < lstOriginalSignificantTokens.Count &&
                    lstOriginalSignificantTokens[intNextSignificant].intStreamIndex < intTokenIndex)
                {
                    intPreviousSignificant = intNextSignificant;
                    intNextSignificant++;
                }

                TSqlParserToken objToken = lstOriginalTokens[intTokenIndex];

                if (!IsComment(objToken))
                {
                    continue;
                }

                int intMappedPrevious = intPreviousSignificant >= 0
                    ? arrTokenMap[intPreviousSignificant]
                    : -1;

                int intMappedNext = intNextSignificant < arrTokenMap.Length
                    ? arrTokenMap[intNextSignificant]
                    : -1;

                CommentPlacement enPlacement = GetPlacement(
                    strOriginalSql,
                    objToken);

                lstInsertions.Add(CreateInsertion(
                    strFormattedSql,
                    NormalizeLineEndings(objToken.Text),
                    enPlacement,
                    intMappedPrevious,
                    intMappedNext,
                    lstFormattedSignificantTokens,
                    intTokenIndex));
            }

            return lstInsertions;
        }

        private static CommentInsertion CreateInsertion(
            string strFormattedSql,
            string strComment,
            CommentPlacement enPlacement,
            int intMappedPrevious,
            int intMappedNext,
            List<IndexedToken> lstFormattedSignificantTokens,
            int intOriginalOrder)
        {
            if (enPlacement == CommentPlacement.Standalone)
            {
                if (intMappedNext >= 0)
                {
                    int intNextOffset =
                        lstFormattedSignificantTokens[intMappedNext].objToken.Offset;
                    int intLineStart = GetLineStart(strFormattedSql, intNextOffset);
                    string strIndentation = strFormattedSql.Substring(
                        intLineStart,
                        intNextOffset - intLineStart);

                    if (!string.IsNullOrWhiteSpace(strIndentation))
                    {
                        strIndentation = string.Empty;
                    }

                    return new CommentInsertion(
                        intLineStart,
                        strIndentation + strComment + Environment.NewLine,
                        intOriginalOrder);
                }

                string strPrefix = HasFinalLineEnding(strFormattedSql) ||
                                   strFormattedSql.Length == 0
                    ? string.Empty
                    : Environment.NewLine;

                return new CommentInsertion(
                    strFormattedSql.Length,
                    strPrefix + strComment + Environment.NewLine,
                    intOriginalOrder);
            }

            if (enPlacement == CommentPlacement.Trailing && intMappedPrevious >= 0)
            {
                TSqlParserToken objPreviousToken =
                    lstFormattedSignificantTokens[intMappedPrevious].objToken;

                int intPreviousEnd =
                    objPreviousToken.Offset + objPreviousToken.Text.Length;

                int intLineEnd = GetLineEnd(strFormattedSql, intPreviousEnd);

                return new CommentInsertion(
                    intLineEnd,
                    " " + strComment,
                    intOriginalOrder);
            }

            if (intMappedNext >= 0)
            {
                int intNextOffset =
                    lstFormattedSignificantTokens[intMappedNext].objToken.Offset;

                return new CommentInsertion(
                    intNextOffset,
                    strComment + " ",
                    intOriginalOrder);
            }

            if (intMappedPrevious >= 0)
            {
                TSqlParserToken objPreviousToken =
                    lstFormattedSignificantTokens[intMappedPrevious].objToken;

                return new CommentInsertion(
                    objPreviousToken.Offset + objPreviousToken.Text.Length,
                    " " + strComment,
                    intOriginalOrder);
            }

            return new CommentInsertion(
                0,
                strComment + Environment.NewLine,
                intOriginalOrder);
        }

        private static int[] BuildTokenMap(
            List<IndexedToken> lstOriginalTokens,
            List<IndexedToken> lstFormattedTokens)
        {
            int[] arrTokenMap = new int[lstOriginalTokens.Count];
            int intFormattedIndex = 0;

            for (int intOriginalIndex = 0;
                 intOriginalIndex < lstOriginalTokens.Count;
                 intOriginalIndex++)
            {
                TSqlParserToken objOriginalToken =
                    lstOriginalTokens[intOriginalIndex].objToken;

                while (
                    intFormattedIndex < lstFormattedTokens.Count &&
                    !TokensMatchForMapping(
                        objOriginalToken,
                        lstFormattedTokens[intFormattedIndex].objToken))
                {
                    intFormattedIndex++;
                }

                if (intFormattedIndex >= lstFormattedTokens.Count)
                {
                    throw new SqlFormattingException(
                        "No fue posible establecer una correspondencia segura entre el SQL original y el SQL formateado. " +
                        "El editor no fue modificado para evitar eliminar o sustituir tokens del SQL original." +
                        Environment.NewLine +
                        Environment.NewLine +
                        $"Token no localizado en el resultado: línea {objOriginalToken.Line}, " +
                        $"columna {objOriginalToken.Column}, valor '{objOriginalToken.Text}'.");
                }

                arrTokenMap[intOriginalIndex] = intFormattedIndex;
                intFormattedIndex++;
            }

            return arrTokenMap;
        }

        private static TokenAlignment AlignTokens(
            List<IndexedToken> lstOriginalTokens,
            List<IndexedToken> lstGeneratedTokens)
        {
            int[] arrOriginalToGenerated = Enumerable
                .Repeat(-1, lstOriginalTokens.Count)
                .ToArray();
            int[] arrGeneratedToOriginal = Enumerable
                .Repeat(-1, lstGeneratedTokens.Count)
                .ToArray();

            int intOriginalIndex = 0;
            int intGeneratedIndex = 0;

            while (
                intOriginalIndex < lstOriginalTokens.Count &&
                intGeneratedIndex < lstGeneratedTokens.Count)
            {
                if (TokensMatchForMapping(
                    lstOriginalTokens[intOriginalIndex].objToken,
                    lstGeneratedTokens[intGeneratedIndex].objToken))
                {
                    arrOriginalToGenerated[intOriginalIndex] = intGeneratedIndex;
                    arrGeneratedToOriginal[intGeneratedIndex] = intOriginalIndex;
                    intOriginalIndex++;
                    intGeneratedIndex++;
                    continue;
                }

                TokenSyncPoint objSyncPoint = FindSyncPoint(
                    lstOriginalTokens,
                    intOriginalIndex,
                    lstGeneratedTokens,
                    intGeneratedIndex);

                if (objSyncPoint == null)
                {
                    break;
                }

                intOriginalIndex += objSyncPoint.intOriginalDistance;
                intGeneratedIndex += objSyncPoint.intGeneratedDistance;
            }

            return new TokenAlignment(
                arrOriginalToGenerated,
                arrGeneratedToOriginal);
        }

        private static TokenSyncPoint FindSyncPoint(
            List<IndexedToken> lstOriginalTokens,
            int intOriginalStart,
            List<IndexedToken> lstGeneratedTokens,
            int intGeneratedStart)
        {
            const int intMaximumLookAhead = 128;
            TokenSyncPoint objBestPoint = null;

            int intOriginalLimit = Math.Min(
                lstOriginalTokens.Count - intOriginalStart,
                intMaximumLookAhead);
            int intGeneratedLimit = Math.Min(
                lstGeneratedTokens.Count - intGeneratedStart,
                intMaximumLookAhead);

            for (int intOriginalDistance = 0;
                 intOriginalDistance < intOriginalLimit;
                 intOriginalDistance++)
            {
                for (int intGeneratedDistance = 0;
                     intGeneratedDistance < intGeneratedLimit;
                     intGeneratedDistance++)
                {
                    if (intOriginalDistance == 0 && intGeneratedDistance == 0)
                    {
                        continue;
                    }

                    if (!TokensMatchForMapping(
                        lstOriginalTokens[intOriginalStart + intOriginalDistance].objToken,
                        lstGeneratedTokens[intGeneratedStart + intGeneratedDistance].objToken))
                    {
                        continue;
                    }

                    int intCost = intOriginalDistance + intGeneratedDistance;
                    int intConsecutiveMatches = CountConsecutiveMatches(
                        lstOriginalTokens,
                        intOriginalStart + intOriginalDistance,
                        lstGeneratedTokens,
                        intGeneratedStart + intGeneratedDistance);

                    if (objBestPoint == null ||
                        intCost < objBestPoint.intCost ||
                        (intCost == objBestPoint.intCost &&
                         intConsecutiveMatches > objBestPoint.intConsecutiveMatches))
                    {
                        objBestPoint = new TokenSyncPoint(
                            intOriginalDistance,
                            intGeneratedDistance,
                            intCost,
                            intConsecutiveMatches);
                    }
                }
            }

            return objBestPoint;
        }

        private static int CountConsecutiveMatches(
            List<IndexedToken> lstOriginalTokens,
            int intOriginalStart,
            List<IndexedToken> lstGeneratedTokens,
            int intGeneratedStart)
        {
            const int intMaximumMatches = 8;
            int intMatchCount = 0;

            while (
                intMatchCount < intMaximumMatches &&
                intOriginalStart + intMatchCount < lstOriginalTokens.Count &&
                intGeneratedStart + intMatchCount < lstGeneratedTokens.Count &&
                TokensMatchForMapping(
                    lstOriginalTokens[intOriginalStart + intMatchCount].objToken,
                    lstGeneratedTokens[intGeneratedStart + intMatchCount].objToken))
            {
                intMatchCount++;
            }

            return intMatchCount;
        }

        private static string ProjectOriginalTokens(
            List<IndexedToken> lstOriginalTokens,
            List<TSqlParserToken> lstGeneratedTokens,
            TokenAlignment objAlignment,
            SqlKeywordCasing enKeywordCasing)
        {
            StringBuilder objBuilder = new StringBuilder();
            StringBuilder objWhitespace = new StringBuilder();
            int intGeneratedSignificantIndex = 0;
            int intNextOriginalIndex = 0;
            string strPreviousToken = null;

            foreach (TSqlParserToken objGeneratedToken in lstGeneratedTokens)
            {
                if (objGeneratedToken.TokenType == TSqlTokenType.WhiteSpace)
                {
                    objWhitespace.Append(NormalizeLineEndings(objGeneratedToken.Text));
                    continue;
                }

                if (objGeneratedToken.TokenType == TSqlTokenType.EndOfFile ||
                    IsComment(objGeneratedToken))
                {
                    continue;
                }

                int intMappedOriginal =
                    objAlignment.arrGeneratedToOriginal[intGeneratedSignificantIndex];
                intGeneratedSignificantIndex++;

                if (intMappedOriginal < 0)
                {
                    continue;
                }

                string strFirstUpcomingToken = intNextOriginalIndex < intMappedOriginal
                    ? lstOriginalTokens[intNextOriginalIndex].objToken.Text
                    : objGeneratedToken.Text;

                AppendLayoutWhitespace(
                    objBuilder,
                    objWhitespace,
                    strPreviousToken,
                    strFirstUpcomingToken);

                while (intNextOriginalIndex < intMappedOriginal)
                {
                    string strOriginalToken = GetProjectedOriginalTokenText(
                        lstOriginalTokens[intNextOriginalIndex].objToken,
                        enKeywordCasing);

                    AppendToken(
                        objBuilder,
                        strPreviousToken,
                        strOriginalToken);

                    strPreviousToken = strOriginalToken;
                    intNextOriginalIndex++;
                }

                AppendToken(
                    objBuilder,
                    strPreviousToken,
                    objGeneratedToken.Text);

                strPreviousToken = objGeneratedToken.Text;
                intNextOriginalIndex = intMappedOriginal + 1;
            }

            while (intNextOriginalIndex < lstOriginalTokens.Count)
            {
                string strOriginalToken = GetProjectedOriginalTokenText(
                    lstOriginalTokens[intNextOriginalIndex].objToken,
                    enKeywordCasing);

                AppendToken(
                    objBuilder,
                    strPreviousToken,
                    strOriginalToken);

                strPreviousToken = strOriginalToken;
                intNextOriginalIndex++;
            }

            return objBuilder.ToString();
        }

        private static string GetProjectedOriginalTokenText(
            TSqlParserToken objToken,
            SqlKeywordCasing enKeywordCasing)
        {
            string strText = objToken.Text;

            if (objToken.TokenType == TSqlTokenType.Identifier ||
                string.IsNullOrEmpty(strText) ||
                !strText.All(char.IsLetter))
            {
                return strText;
            }

            switch (enKeywordCasing)
            {
                case SqlKeywordCasing.Lowercase:
                    return strText.ToLowerInvariant();
                case SqlKeywordCasing.PascalCase:
                    return char.ToUpperInvariant(strText[0]) +
                           strText.Substring(1).ToLowerInvariant();
                case SqlKeywordCasing.Uppercase:
                default:
                    return strText.ToUpperInvariant();
            }
        }

        private static void AppendLayoutWhitespace(
            StringBuilder objBuilder,
            StringBuilder objWhitespace,
            string strPreviousToken,
            string strCurrentToken)
        {
            if (objBuilder.Length == 0)
            {
                objWhitespace.Clear();
                return;
            }

            string strWhitespace = objWhitespace.ToString();
            objWhitespace.Clear();

            if (strWhitespace.IndexOfAny(new[] { '\r', '\n' }) >= 0)
            {
                objBuilder.Append(strWhitespace);
                return;
            }

            if (NeedsSpaceBetween(strPreviousToken, strCurrentToken))
            {
                objBuilder.Append(' ');
            }
        }

        private static void AppendToken(
            StringBuilder objBuilder,
            string strPreviousToken,
            string strCurrentToken)
        {
            if (objBuilder.Length > 0 &&
                !char.IsWhiteSpace(objBuilder[objBuilder.Length - 1]) &&
                NeedsSpaceBetween(strPreviousToken, strCurrentToken))
            {
                objBuilder.Append(' ');
            }

            objBuilder.Append(strCurrentToken);
        }

        private static bool NeedsSpaceBetween(
            string strPreviousToken,
            string strCurrentToken)
        {
            if (string.IsNullOrEmpty(strPreviousToken) ||
                string.IsNullOrEmpty(strCurrentToken))
            {
                return false;
            }

            char chrPrevious = strPreviousToken[strPreviousToken.Length - 1];
            char chrCurrent = strCurrentToken[0];

            if (chrCurrent == ',' ||
                chrCurrent == '.' ||
                chrCurrent == ';' ||
                chrCurrent == ')' ||
                chrCurrent == ']')
            {
                return false;
            }

            if (chrPrevious == '(' ||
                chrPrevious == '.' ||
                chrPrevious == '[')
            {
                return false;
            }

            return true;
        }

        private static string ApplyInsertions(
            string strFormattedSql,
            List<CommentInsertion> lstInsertions)
        {
            StringBuilder objBuilder = new StringBuilder(strFormattedSql);

            foreach (CommentInsertion objInsertion in lstInsertions
                .OrderByDescending(objItem => objItem.intOffset)
                .ThenByDescending(objItem => objItem.intOriginalOrder))
            {
                objBuilder.Insert(objInsertion.intOffset, objInsertion.strText);
            }

            return objBuilder.ToString();
        }

        private static void ValidateResult(
            TSqlParser objParser,
            List<IndexedToken> lstOriginalSignificant,
            string strFormattedSql,
            string strSqlWithComments,
            List<TSqlParserToken> lstOriginalComments)
        {
            List<TSqlParserToken> lstFormattedTokens =
                GetTokenStream(objParser, strFormattedSql);

            List<TSqlParserToken> lstFinalTokens =
                GetTokenStream(objParser, strSqlWithComments);

            List<TSqlParserToken> lstFinalComments = lstFinalTokens
                .Where(IsComment)
                .ToList();

            if (lstFinalComments.Count != lstOriginalComments.Count)
            {
                throw new SqlFormattingException(
                    "La validación final detectó que no se conservaron todos los comentarios. El editor no fue modificado.");
            }

            for (int intIndex = 0;
                 intIndex < lstOriginalComments.Count;
                 intIndex++)
            {
                string strOriginalComment =
                    NormalizeLineEndings(lstOriginalComments[intIndex].Text);
                string strFinalComment =
                    NormalizeLineEndings(lstFinalComments[intIndex].Text);

                if (!string.Equals(
                    strOriginalComment,
                    strFinalComment,
                    StringComparison.Ordinal))
                {
                    throw new SqlFormattingException(
                        "La validación final detectó una modificación en el contenido de un comentario. El editor no fue modificado.");
                }
            }

            List<IndexedToken> lstFormattedSignificant =
                GetSignificantTokens(lstFormattedTokens);
            List<IndexedToken> lstFinalSignificant =
                GetSignificantTokens(lstFinalTokens);

            if (lstOriginalSignificant.Count != lstFormattedSignificant.Count)
            {
                throw new SqlFormattingException(
                    "La validación de integridad detectó tokens agregados o eliminados por el generador. El editor no fue modificado.");
            }

            for (int intIndex = 0;
                 intIndex < lstOriginalSignificant.Count;
                 intIndex++)
            {
                if (!TokensMatchForMapping(
                    lstOriginalSignificant[intIndex].objToken,
                    lstFormattedSignificant[intIndex].objToken))
                {
                    throw new SqlFormattingException(
                        "La validación de integridad detectó una sustitución de código. El editor no fue modificado.");
                }
            }

            if (lstFormattedSignificant.Count != lstFinalSignificant.Count)
            {
                throw new SqlFormattingException(
                    "La reinserción de comentarios alteró tokens ejecutables. El editor no fue modificado.");
            }

            for (int intIndex = 0;
                 intIndex < lstFormattedSignificant.Count;
                 intIndex++)
            {
                if (!TokensMatchExactly(
                    lstFormattedSignificant[intIndex].objToken,
                    lstFinalSignificant[intIndex].objToken))
                {
                    throw new SqlFormattingException(
                        "La reinserción de comentarios alteró el código T-SQL. El editor no fue modificado.");
                }
            }

            IList<ParseError> lstFinalErrors;

            using (StringReader objReader = new StringReader(strSqlWithComments))
            {
                objParser.Parse(objReader, out lstFinalErrors);
            }

            if (lstFinalErrors != null && lstFinalErrors.Count > 0)
            {
                throw new SqlFormattingException(
                    "El SQL dejó de ser válido después de reinsertar los comentarios. El editor no fue modificado.");
            }
        }

        private static List<TSqlParserToken> GetTokenStream(
            TSqlParser objParser,
            string strSql)
        {
            IList<ParseError> lstErrors;

            using (StringReader objReader = new StringReader(strSql))
            {
                return objParser
                    .GetTokenStream(objReader, out lstErrors)
                    .ToList();
            }
        }

        private static List<IndexedToken> GetSignificantTokens(
            List<TSqlParserToken> lstTokens)
        {
            List<IndexedToken> lstResult = new List<IndexedToken>();

            for (int intIndex = 0; intIndex < lstTokens.Count; intIndex++)
            {
                if (!IsTrivia(lstTokens[intIndex]))
                {
                    lstResult.Add(new IndexedToken(lstTokens[intIndex], intIndex));
                }
            }

            return lstResult;
        }

        private static CommentPlacement GetPlacement(
            string strOriginalSql,
            TSqlParserToken objComment)
        {
            int intCommentStart = objComment.Offset;
            int intCommentEnd = objComment.Offset + objComment.Text.Length;
            int intLineStart = GetLineStart(strOriginalSql, intCommentStart);
            int intLineEnd = GetLineEnd(strOriginalSql, intCommentEnd);

            bool boolHasCodeBefore = !string.IsNullOrWhiteSpace(
                strOriginalSql.Substring(
                    intLineStart,
                    intCommentStart - intLineStart));

            bool boolHasCodeAfter = !string.IsNullOrWhiteSpace(
                strOriginalSql.Substring(
                    intCommentEnd,
                    intLineEnd - intCommentEnd));

            if (objComment.TokenType == TSqlTokenType.SingleLineComment)
            {
                return boolHasCodeBefore
                    ? CommentPlacement.Trailing
                    : CommentPlacement.Standalone;
            }

            if (!boolHasCodeBefore && !boolHasCodeAfter)
            {
                return CommentPlacement.Standalone;
            }

            if (boolHasCodeBefore && !boolHasCodeAfter)
            {
                return CommentPlacement.Trailing;
            }

            return CommentPlacement.Inline;
        }

        private static int GetLineStart(string strText, int intOffset)
        {
            int intIndex = Math.Min(intOffset, strText.Length) - 1;

            while (intIndex >= 0 &&
                   strText[intIndex] != '\r' &&
                   strText[intIndex] != '\n')
            {
                intIndex--;
            }

            return intIndex + 1;
        }

        private static int GetLineEnd(string strText, int intOffset)
        {
            int intIndex = Math.Min(intOffset, strText.Length);

            while (intIndex < strText.Length &&
                   strText[intIndex] != '\r' &&
                   strText[intIndex] != '\n')
            {
                intIndex++;
            }

            return intIndex;
        }

        private static bool TokensMatchForMapping(
            TSqlParserToken objLeft,
            TSqlParserToken objRight)
        {
            return objLeft.TokenType == objRight.TokenType &&
                   string.Equals(
                       objLeft.Text,
                       objRight.Text,
                       StringComparison.OrdinalIgnoreCase);
        }

        private static bool TokensMatchExactly(
            TSqlParserToken objLeft,
            TSqlParserToken objRight)
        {
            return objLeft.TokenType == objRight.TokenType &&
                   string.Equals(
                       objLeft.Text,
                       objRight.Text,
                       StringComparison.Ordinal);
        }

        private static bool IsTrivia(TSqlParserToken objToken)
        {
            return objToken.TokenType == TSqlTokenType.WhiteSpace ||
                   objToken.TokenType == TSqlTokenType.EndOfFile ||
                   IsComment(objToken);
        }

        private static bool IsComment(TSqlParserToken objToken)
        {
            return objToken.TokenType == TSqlTokenType.SingleLineComment ||
                   objToken.TokenType == TSqlTokenType.MultilineComment;
        }

        private static bool HasFinalLineEnding(string strText)
        {
            return strText.EndsWith("\r\n", StringComparison.Ordinal) ||
                   strText.EndsWith("\n", StringComparison.Ordinal) ||
                   strText.EndsWith("\r", StringComparison.Ordinal);
        }

        private static string NormalizeLineEndings(string strText)
        {
            return (strText ?? string.Empty)
                .Replace("\r\n", "\n")
                .Replace("\r", "\n")
                .Replace("\n", Environment.NewLine);
        }

        private enum CommentPlacement
        {
            Standalone,
            Trailing,
            Inline
        }

        private sealed class IndexedToken
        {
            public IndexedToken(TSqlParserToken objToken, int intStreamIndex)
            {
                this.objToken = objToken;
                this.intStreamIndex = intStreamIndex;
            }

            public TSqlParserToken objToken { get; }
            public int intStreamIndex { get; }
        }

        private sealed class CommentInsertion
        {
            public CommentInsertion(
                int intOffset,
                string strText,
                int intOriginalOrder)
            {
                this.intOffset = intOffset;
                this.strText = strText;
                this.intOriginalOrder = intOriginalOrder;
            }

            public int intOffset { get; }
            public string strText { get; }
            public int intOriginalOrder { get; }
        }

        private sealed class TokenAlignment
        {
            public TokenAlignment(
                int[] arrOriginalToGenerated,
                int[] arrGeneratedToOriginal)
            {
                this.arrOriginalToGenerated = arrOriginalToGenerated;
                this.arrGeneratedToOriginal = arrGeneratedToOriginal;
            }

            public int[] arrOriginalToGenerated { get; }
            public int[] arrGeneratedToOriginal { get; }
        }

        private sealed class TokenSyncPoint
        {
            public TokenSyncPoint(
                int intOriginalDistance,
                int intGeneratedDistance,
                int intCost,
                int intConsecutiveMatches)
            {
                this.intOriginalDistance = intOriginalDistance;
                this.intGeneratedDistance = intGeneratedDistance;
                this.intCost = intCost;
                this.intConsecutiveMatches = intConsecutiveMatches;
            }

            public int intOriginalDistance { get; }
            public int intGeneratedDistance { get; }
            public int intCost { get; }
            public int intConsecutiveMatches { get; }
        }
    }
}
