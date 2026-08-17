param(
    [string]$pathConfiguration = "Release",
    [string]$pathScriptDomOverride = ""
)

$ErrorActionPreference = "Stop"

$pathRepository = Split-Path -Parent $PSScriptRoot
$pathOutput = Join-Path $pathRepository "EMSqlFormatter\bin\$pathConfiguration"
$pathFormatter = Join-Path $pathOutput "EMSqlFormatter.dll"
$pathScriptDom = if ([string]::IsNullOrWhiteSpace($pathScriptDomOverride))
{
    Join-Path $pathOutput "Microsoft.SqlServer.TransactSql.ScriptDom.dll"
}
else
{
    $pathScriptDomOverride
}

if (-not (Test-Path -LiteralPath $pathFormatter))
{
    throw "No se encontró $pathFormatter. Compila la solución antes de ejecutar las pruebas."
}

if (-not (Test-Path -LiteralPath $pathScriptDom))
{
    throw "No se encontró la biblioteca ScriptDom: $pathScriptDom"
}

$objScriptDomAssembly = [Reflection.Assembly]::LoadFrom($pathScriptDom)
Write-Output "ScriptDom de prueba: $($objScriptDomAssembly.Location)"
Write-Output "Versión ScriptDom: $($objScriptDomAssembly.FullName)"
$objAssembly = [Reflection.Assembly]::LoadFrom($pathFormatter)
$objSettingsType = $objAssembly.GetType(
    "EMSqlFormatter.SqlFormatterSettings",
    $true)
$objSettings = [Activator]::CreateInstance($objSettingsType, $true)
$objServiceType = $objAssembly.GetType(
    "EMSqlFormatter.SqlFormatterService",
    $true)
$objFormatMethod = $objServiceType.GetMethod(
    "Format",
    [Reflection.BindingFlags]"Static,Public,NonPublic")

function Set-FormatterSetting
{
    param(
        [string]$strName,
        [object]$objValue
    )

    $objSettingsType.GetProperty($strName).SetValue($objSettings, $objValue)
}

function Invoke-Formatter
{
    param([string]$strSql)

    try
    {
        return [PSCustomObject]@{
            boolSuccess = $true
            strOutput = [string]$objFormatMethod.Invoke(
                $null,
                @($strSql, $objSettings))
            strError = $null
        }
    }
    catch
    {
        return [PSCustomObject]@{
            boolSuccess = $false
            strOutput = $null
            strError = $_.Exception.InnerException.Message
        }
    }
}

function Assert-Formatter
{
    param(
        [bool]$boolCondition,
        [string]$strTestName,
        [string]$strDetail
    )

    if (-not $boolCondition)
    {
        throw "Prueba fallida: $strTestName. $strDetail"
    }

    Write-Output "OK: $strTestName"
}

$objCompatibilityType =
    $objSettingsType.GetProperty("enCompatibilityLevel").PropertyType
$objKeywordCasingType =
    $objSettingsType.GetProperty("enKeywordCasing").PropertyType

Set-FormatterSetting "enCompatibilityLevel" (
    [Enum]::ToObject($objCompatibilityType, 160))
Set-FormatterSetting "enKeywordCasing" (
    [Enum]::ToObject($objKeywordCasingType, 0))
Set-FormatterSetting "intIndentationSize" 4
Set-FormatterSetting "intNewLinesAfterStatement" 1

$arrEnabledDefaults = @(
    "boolMultilineSelectElementsList",
    "boolMultilineWherePredicatesList",
    "boolNewLineBeforeFromClause",
    "boolNewLineBeforeJoinClause",
    "boolNewLineBeforeWhereClause",
    "boolNewLineBeforeGroupByClause",
    "boolNewLineBeforeHavingClause",
    "boolNewLineBeforeOrderByClause",
    "boolAlignSetClauseItem",
    "boolMultilineSetClauseItems",
    "boolMultilineInsertTargetsList",
    "boolMultilineInsertSourcesList",
    "boolNewLineBeforeCloseParenthesis",
    "boolSpaceBetweenDataTypeAndParameters",
    "boolSpaceBetweenParametersInDataType"
)

foreach ($strSettingName in $arrEnabledDefaults)
{
    Set-FormatterSetting $strSettingName $true
}

$strBasicSql =
    "select a.id,a.nombre from dbo.cliente a where a.activo=1 and a.estado=2"
$objBasicResult = Invoke-Formatter $strBasicSql
Assert-Formatter (
    $objBasicResult.boolSuccess -and
    $objBasicResult.strOutput -match "SELECT" -and
    $objBasicResult.strOutput -match "WHERE" -and
    -not $objBasicResult.strOutput.TrimEnd().EndsWith(";")) `
    "Formato SELECT" $objBasicResult.strError

$objSecondResult = Invoke-Formatter $objBasicResult.strOutput
Assert-Formatter (
    $objSecondResult.boolSuccess -and
    $objSecondResult.strOutput -ceq $objBasicResult.strOutput) `
    "Idempotencia" $objSecondResult.strError

$objInvalidResult = Invoke-Formatter "SELECT FROM"
Assert-Formatter (
    -not $objInvalidResult.boolSuccess -and
    $objInvalidResult.strError -match "errores de sintaxis") `
    "Rechazo de SQL inválido" $objInvalidResult.strError

$strSqlWithComments = @"
-- encabezado de una línea
SELECT a.id, /* comentario de campo */ a.nombre -- comentario al final
FROM dbo.cliente AS a
/*
Comentario de párrafo.
Debe conservar todas sus líneas.
*/
WHERE a.activo = 1;
-- comentario final
"@
$objCommentResult = Invoke-Formatter $strSqlWithComments
Assert-Formatter (
    $objCommentResult.boolSuccess -and
    $objCommentResult.strOutput.Contains("-- encabezado de una línea") -and
    $objCommentResult.strOutput.Contains("/* comentario de campo */") -and
    $objCommentResult.strOutput.Contains("-- comentario al final") -and
    $objCommentResult.strOutput.Contains("Comentario de párrafo.") -and
    $objCommentResult.strOutput.Contains("Debe conservar todas sus líneas.") -and
    $objCommentResult.strOutput.Contains("-- comentario final")) `
    "Conservación de comentarios" $objCommentResult.strError

$objCommentSecondResult = Invoke-Formatter $objCommentResult.strOutput
Assert-Formatter (
    $objCommentSecondResult.boolSuccess -and
    $objCommentSecondResult.strOutput -ceq $objCommentResult.strOutput) `
    "Idempotencia con comentarios" $objCommentSecondResult.strError

$strCommentsNearGeneratedTokens = @"
SELECT a.id /* alias del identificador */ identificador
FROM dbo.cliente a;
BEGIN TRANSACTION;
COMMIT -- cierre de transacción
"@
$objGeneratedTokensResult = Invoke-Formatter $strCommentsNearGeneratedTokens
Assert-Formatter (
    $objGeneratedTokensResult.boolSuccess -and
    $objGeneratedTokensResult.strOutput.Contains("/* alias del identificador */") -and
    $objGeneratedTokensResult.strOutput.Contains("-- cierre de transacción") -and
    $objGeneratedTokensResult.strOutput -notmatch "a\.id\s+AS\s+/\*" -and
    $objGeneratedTokensResult.strOutput -notmatch "COMMIT\s+TRANSACTION") `
    "Comentarios junto a tokens agregados por ScriptDom" `
    $objGeneratedTokensResult.strError

$strOnlyComments = "-- único contenido`r`n/* párrafo final */`r`n"
$objOnlyCommentsResult = Invoke-Formatter $strOnlyComments
Assert-Formatter (
    $objOnlyCommentsResult.boolSuccess -and
    $objOnlyCommentsResult.strOutput.Contains("-- único contenido") -and
    $objOnlyCommentsResult.strOutput.Contains("/* párrafo final */")) `
    "Documento compuesto solo por comentarios" $objOnlyCommentsResult.strError

$objFinalLineResult = Invoke-Formatter "select 1`r`n"
Assert-Formatter (
    $objFinalLineResult.boolSuccess -and
    $objFinalLineResult.strOutput.EndsWith([Environment]::NewLine)) `
    "Conservación de línea final" $objFinalLineResult.strError

$pathComplexSql = Join-Path $pathRepository "EMSqlFormatter\prueba.sql"
$strComplexSql = Get-Content -LiteralPath $pathComplexSql -Raw -Encoding UTF8
$objComplexResult = Invoke-Formatter $strComplexSql
Assert-Formatter (
    $objComplexResult.boolSuccess -and
    $objComplexResult.strOutput -match "CREATE OR ALTER PROCEDURE" -and
    $objComplexResult.strOutput -cmatch "DELETE\s+FROM\s+dbo\.tbl_temporal" -and
    $objComplexResult.strOutput -match "BEGIN TRY" -and
    $objComplexResult.strOutput -match "BEGIN CATCH") `
    "Procedimiento, DML y TRY/CATCH" $objComplexResult.strError

Set-FormatterSetting "enCompatibilityLevel" (
    [Enum]::ToObject($objCompatibilityType, 110))
$objCompatibilityResult = Invoke-Formatter (
    "CREATE OR ALTER PROCEDURE dbo.usp_nuevo AS SELECT 1;")
Assert-Formatter (
    -not $objCompatibilityResult.boolSuccess -and
    $objCompatibilityResult.strError -match "errores de sintaxis") `
    "Compatibilidad SQL Server 2012" $objCompatibilityResult.strError

Set-FormatterSetting "enCompatibilityLevel" (
    [Enum]::ToObject($objCompatibilityType, 160))

Set-FormatterSetting "enKeywordCasing" (
    [Enum]::ToObject($objKeywordCasingType, 1))
$objLowercaseResult = Invoke-Formatter "SELECT 1 FROM dbo.tabla"
Assert-Formatter (
    $objLowercaseResult.boolSuccess -and
    $objLowercaseResult.strOutput -match "^select" -and
    $objLowercaseResult.strOutput -match "from") `
    "Keywords en minúsculas" $objLowercaseResult.strError

Write-Output "Todas las pruebas del motor finalizaron correctamente."
