# EM Sql Formatter

[English](README.md) | [Español](README.es.md)

A configurable T-SQL formatter extension for SQL Server Management Studio 21.

## Features

- Format the current selection or the entire active document.
- Dedicated toolbar buttons for selection, document, and settings.
- Persistent formatting settings integrated into SSMS.
- Configurable compatibility from SQL Server 2012 through SQL Server 2025.
- Preservation of `--` line comments and `/* ... */` block comments.
- Syntax validation before changing the editor content.
- Protection of original identifiers, literals, operators, and SQL tokens.
- A complete formatting operation can be reverted with a single undo action.

## Requirements

- SQL Server Management Studio 21, 64-bit.
- Windows with .NET Framework 4.7.2 or later.

## Installation

1. Download `EMSqlFormatter.vsix` from the
   [latest release](https://github.com/backtrio/EMSqlFormatter/releases/latest).
2. Close every running SSMS window.
3. Run the VSIX file and confirm the installation.
4. Open SSMS.
5. Right-click the toolbar area and enable `EM Sql Formatter`.

The development VSIX is not digitally signed. Windows or the VSIX installer
may therefore display a trust warning.

## Usage

The extension adds the following commands:

- `Beautify SQL - Selección`: formats the currently selected T-SQL block.
- `Beautify SQL - Documento`: formats the complete active SQL document.
- `Configurar EM Sql Formatter...`: opens the formatting settings.

These commands are available from both the `EM Sql Formatter` toolbar and the
SSMS `Tools` menu.

The formatter parses and validates all input before replacing editor content.
If it detects invalid syntax or cannot guarantee preservation of the original
tokens and comments, it cancels the operation without changing the document.

`Ctrl+Z` does not execute the formatter. It only reverts a completed formatting
operation as a single undo action.

Detailed implementation notes are currently available in Spanish in
[EMSqlFormatter/README.md](EMSqlFormatter/README.md).

## Build

Development requirements:

- Visual Studio 2022 or Visual Studio 2026.
- The `Visual Studio extension development` workload.
- .NET Framework 4.7.2 Developer Pack.
- SSMS 21 installed.

Build steps:

1. Open `EMSqlFormatter.sln`.
2. Restore the NuGet packages.
3. Select `Release` and `Any CPU`.
4. Run `Build > Rebuild Solution`.
5. The installer will be generated at
   `EMSqlFormatter/bin/Release/EMSqlFormatter.vsix`.

## Tests

After building the Release configuration, run:

```powershell
.\tests\Test-SqlFormatterService.ps1 -pathConfiguration Release
```

The test suite covers formatting, idempotency, invalid SQL rejection, comment
preservation, compatibility levels, and keyword casing.

## License

This project is distributed under the [MIT License](LICENSE).

Copyright (c) 2026 EM Services.
