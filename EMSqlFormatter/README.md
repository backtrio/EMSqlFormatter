# EM Sql Formatter 1.1.4

Formateador T-SQL configurable para SQL Server Management Studio 21.

## Comandos en SSMS

La extensión agrega estos comandos en `Herramientas / Tools`:

- `Beautify SQL - Selección`;
- `Beautify SQL - Documento`;
- `Configurar EM Sql Formatter...`.

Los mismos comandos están disponibles en la barra `EM Sql Formatter`, con
botones para formatear la selección, formatear el documento y abrir la
configuración. Los botones utilizan iconos propios de 16 x 16 píxeles incluidos
en la extensión; el texto se mantiene como descripción emergente. Para mostrarla
por primera vez, haz clic derecho en el área de
barras de SSMS y activa `EM Sql Formatter`. SSMS conserva esta preferencia para
las siguientes sesiones.

El primer comando formatea únicamente la selección. La selección debe ser una
sentencia o bloque T-SQL válido de forma independiente. El segundo procesa el
documento SQL activo completo.

Cada reemplazo se registra como una sola operación y puede deshacerse con
`Ctrl+Z`.

## Configuración

El panel `Configurar EM Sql Formatter...` permite guardar opciones
persistentes dentro de SSMS:

- compatibilidad desde SQL Server 2012 hasta SQL Server 2025;
- palabras reservadas en mayúsculas, minúsculas o PascalCase;
- indentación de 1 a 8 espacios;
- saltos después de cada sentencia;
- alineación de cláusulas y definiciones de columnas;
- disposición de columnas `SELECT` y predicados `WHERE`;
- saltos antes de `FROM`, `JOIN`, `WHERE`, `GROUP BY`, `HAVING` y `ORDER BY`;
- disposición de listas `INSERT`;
- alineación e indentación de elementos `SET` en `UPDATE`;
- disposición de paréntesis y espacios en tipos de datos.

El panel también aparece bajo:

`Herramientas > Opciones > EM Sql Formatter > Formato`

## Seguridad y control de errores

Antes de modificar el editor, la extensión analiza todo el SQL. Si encuentra
errores, muestra hasta diez ubicaciones con línea y columna y cancela el
reemplazo.

ScriptDom no conserva comentarios al regenerar el árbol sintáctico. EM Sql
Formatter agrega una capa de preservación que identifica cada comentario por
tokens, formatea el SQL y reinserta los comentarios en una posición segura:

- los comentarios de una línea conservan el formato `-- comentario`;
- los comentarios de bloque o párrafo conservan el formato `/* ... */`;
- el contenido literal y el orden de los comentarios se mantienen;
- los comentarios al final de una línea permanecen asociados al código cercano;
- un documento compuesto únicamente por comentarios también se conserva.

Después de la reinserción se analiza nuevamente el resultado y se comparan sus
tokens ejecutables. Si no es posible conservar todos los comentarios o si un
comentario pudiera ocultar o alterar código, el formato se cancela antes de
modificar el editor. No existe una opción para permitir su eliminación.

La comparación de tokens también se ejecuta cuando el documento no contiene
comentarios. Cada token significativo del SQL original debe seguir presente en
el resultado; si ScriptDom intenta sustituir o eliminar alguno, la operación se
cancela sin reemplazar el contenido del editor.

Los errores internos se registran en `ActivityLog.xml`. Los errores de sintaxis
y la protección de comentarios son errores esperables y no contaminan ese log.

SSMS 21 carga su propia versión de ScriptDom antes que las extensiones. Las
opciones de espaciado de tipos se aplican dinámicamente cuando esa versión las
admite. Si no están disponibles, se omiten sin cancelar el resto del formato ni
modificar el contenido SQL.

## Comportamiento real de ScriptDom

El motor utilizado es `Microsoft.SqlServer.TransactSql.ScriptDom`. El parser y
el generador se seleccionan de acuerdo con la compatibilidad configurada.

ScriptDom regenera internamente la estructura completa del código y puede
intentar agregar `AS`, terminadores `;`, expandir `COMMIT TRANSACTION` u omitir
palabras opcionales como `FROM` en un `DELETE`. Antes de devolver el resultado,
EM Sql Formatter proyecta nuevamente los tokens originales sobre el diseño
formateado. Por tanto:

- no agrega `AS` si no estaba presente;
- no agrega ni elimina terminadores `;`;
- conserva `COMMIT` sin expandirlo;
- conserva `DELETE FROM` si así fue escrito;
- mantiene identificadores, literales, operadores y demás tokens originales;
- solamente cambia mayúsculas de keywords, espacios, indentación y líneas.

La validación posterior confirma que la secuencia de tokens sea equivalente y
que el resultado siga siendo T-SQL válido antes de reemplazar el editor.

La versión 1.1 todavía no implementa coma inicial ni un algoritmo propio para
ubicar `JOIN / ON`, `AND / OR` o comentarios. Esas reglas no están expuestas por
ScriptDom y requieren una capa adicional de formato.

## Requisitos para compilar

1. Visual Studio 2022 o Visual Studio 2026.
2. Workload `Visual Studio extension development`.
3. .NET Framework 4.7.2 Developer Pack.
4. SSMS 21 instalado.

## Compilación

1. Abre `EMSqlFormatter.sln` con Visual Studio.
2. Espera la restauración de NuGet.
3. Selecciona `Release` y `Any CPU`.
4. Ejecuta `Build > Rebuild Solution`.
5. Obtén el instalador desde:

`EMSqlFormatter\bin\Release\EMSqlFormatter.vsix`

## Instalación o actualización en SSMS 21

Cierra todas las ventanas de SSMS y ejecuta:

```powershell
& "C:\Program Files\Microsoft SQL Server Management Studio 21\Release\Common7\IDE\VSIXInstaller.exe" `
  ".\EMSqlFormatter\bin\Release\EMSqlFormatter.vsix"
```

El manifiesto utiliza:

```text
Producto:      Microsoft.VisualStudio.Ssms
Versión:       [21.0,22.0)
Arquitectura:  amd64
```

El VSIX de desarrollo no está firmado. SSMS 21 no ofrece soporte oficial para
extensiones de terceros, aunque no bloquea activamente su carga.

## Prueba funcional

Abre `prueba.sql`, ejecuta `Beautify SQL - Documento` y comprueba:

1. formato de `SELECT`, `JOIN`, `WHERE`, `CASE`, `INSERT`, `UPDATE` y
   `TRY / CATCH`;
2. cambio de mayúsculas y tamaño de indentación desde el panel;
3. rechazo de `SELECT FROM` sin modificar el documento;
4. conservación de comentarios `--` y `/* ... */`;
5. idempotencia al volver a formatear un documento con comentarios;
6. deshacer el formato completo con una sola operación `Ctrl+Z`.
