# EM Sql Formatter

[English](README.md) | [Español](README.es.md)

Extensión de formato T-SQL configurable para SQL Server Management Studio 21.

## Características

- Formato de la selección actual o del documento completo.
- Barra de herramientas con botones para selección, documento y configuración.
- Panel de opciones persistentes dentro de SSMS.
- Compatibilidad configurable desde SQL Server 2012 hasta SQL Server 2025.
- Conservación de comentarios `--` y `/* ... */`.
- Validación sintáctica antes de modificar el editor.
- Protección de identificadores, literales, operadores y demás tokens originales.
- Formato completo reversible mediante una sola operación de deshacer.

## Requisitos

- SQL Server Management Studio 21 de 64 bits.
- Windows con .NET Framework 4.7.2 o superior.

## Instalación

1. Descarga `EMSqlFormatter.vsix` desde la
   [última versión](https://github.com/backtrio/EMSqlFormatter/releases/latest).
2. Cierra todas las ventanas de SSMS.
3. Ejecuta el archivo VSIX y confirma la instalación.
4. Abre SSMS.
5. Haz clic derecho en el área de barras y activa `EM Sql Formatter`.

El paquete de desarrollo no está firmado digitalmente, por lo que Windows o el
instalador pueden mostrar una advertencia de confianza.

## Uso

La extensión agrega los comandos siguientes:

- `Beautify SQL - Selección`: formatea el bloque T-SQL seleccionado.
- `Beautify SQL - Documento`: formatea el documento SQL activo completo.
- `Configurar EM Sql Formatter...`: abre la configuración de formato.

Los comandos están disponibles tanto en la barra `EM Sql Formatter` como en el
menú `Herramientas` de SSMS.

El formateador analiza y valida todo el contenido antes de reemplazarlo. Si
encuentra sintaxis inválida o no puede garantizar la conservación de los tokens
y comentarios originales, cancela la operación sin modificar el documento.

`Ctrl+Z` no ejecuta el formateador. Solamente permite revertir el formato
completo mediante una sola operación de deshacer.

La documentación técnica detallada está disponible en
[EMSqlFormatter/README.md](EMSqlFormatter/README.md).

## Compilación

Requisitos de desarrollo:

- Visual Studio 2022 o Visual Studio 2026.
- Workload `Visual Studio extension development`.
- .NET Framework 4.7.2 Developer Pack.
- SSMS 21 instalado.

Pasos:

1. Abre `EMSqlFormatter.sln`.
2. Restaura los paquetes NuGet.
3. Selecciona `Release` y `Any CPU`.
4. Ejecuta `Build > Rebuild Solution`.
5. El instalador se generará en
   `EMSqlFormatter/bin/Release/EMSqlFormatter.vsix`.

## Pruebas

Después de compilar en modo Release, ejecuta:

```powershell
.\tests\Test-SqlFormatterService.ps1 -pathConfiguration Release
```

Las pruebas cubren formato, idempotencia, rechazo de SQL inválido, conservación
de comentarios, niveles de compatibilidad y formato de palabras reservadas.

## Licencia

Este proyecto se distribuye bajo la [licencia MIT](LICENSE).

Copyright (c) 2026 EM Services.
