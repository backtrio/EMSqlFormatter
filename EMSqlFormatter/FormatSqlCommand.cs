using System;
using System.ComponentModel.Design;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace EMSqlFormatter
{
    internal sealed class FormatSqlCommand
    {
        private const string strProductName = "EM Sql Formatter";
        private readonly EMSqlFormatterPackage objPackage;

        private FormatSqlCommand(EMSqlFormatterPackage objPackage)
        {
            this.objPackage = objPackage ??
                throw new ArgumentNullException(nameof(objPackage));
        }

        public static async Task InitializeAsync(
            EMSqlFormatterPackage objPackage)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            OleMenuCommandService objCommandService =
                await objPackage.GetServiceAsync(typeof(IMenuCommandService))
                    as OleMenuCommandService;

            if (objCommandService == null)
            {
                ActivityLog.LogError(
                    strProductName,
                    "No fue posible obtener OleMenuCommandService. Los comandos no fueron registrados.");
                return;
            }

            FormatSqlCommand objInstance = new FormatSqlCommand(objPackage);

            objInstance.AddCommand(
                objCommandService,
                PkgCmdID.iFormatSelection,
                objInstance.ExecuteFormatSelection);

            objInstance.AddCommand(
                objCommandService,
                PkgCmdID.iFormatDocument,
                objInstance.ExecuteFormatDocument);

            objInstance.AddCommand(
                objCommandService,
                PkgCmdID.iOpenOptions,
                objInstance.ExecuteOpenOptions);
        }

        private void AddCommand(
            OleMenuCommandService objCommandService,
            int intCommandId,
            EventHandler objHandler)
        {
            CommandID objCommandId =
                new CommandID(Guids.oCommandSetGuid, intCommandId);

            MenuCommand objCommand =
                new MenuCommand(objHandler, objCommandId);

            objCommandService.AddCommand(objCommand);
        }

        private void ExecuteFormatSelection(object objSender, EventArgs objEventArgs)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                DTE2 objDte = GetActiveDte();
                TextSelection objSelection =
                    objDte.ActiveDocument.Selection as TextSelection;

                if (objSelection == null)
                {
                    ShowMessage(
                        "El editor activo no expone una selección de texto.",
                        OLEMSGICON.OLEMSGICON_WARNING);
                    return;
                }

                string strOriginalSql = objSelection.Text;

                if (string.IsNullOrWhiteSpace(strOriginalSql))
                {
                    ShowMessage(
                        "Selecciona una sentencia o bloque T-SQL completo antes de ejecutar el formato.",
                        OLEMSGICON.OLEMSGICON_INFO);
                    return;
                }

                SqlFormatterSettings objSettings = objPackage.GetFormatterSettings();
                string strFormattedSql =
                    SqlFormatterService.Format(strOriginalSql, objSettings);

                if (string.Equals(strOriginalSql, strFormattedSql, StringComparison.Ordinal))
                {
                    SetStatus(objDte, "La selección ya tiene el formato configurado.");
                    return;
                }

                ExecuteUndoableAction(
                    objDte,
                    "Formatear selección T-SQL",
                    () =>
                    {
                        ThreadHelper.ThrowIfNotOnUIThread();
                        objSelection.Insert(
                            strFormattedSql,
                            (int)vsInsertFlags.vsInsertFlagsContainNewText);
                    });

                SetStatus(objDte, "Selección T-SQL formateada correctamente.");
            }
            catch (SqlFormattingException objException)
            {
                ShowMessage(objException.Message, OLEMSGICON.OLEMSGICON_WARNING);
            }
            catch (Exception objException)
            {
                HandleUnexpectedError("formatear la selección", objException);
            }
        }

        private void ExecuteFormatDocument(object objSender, EventArgs objEventArgs)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                DTE2 objDte = GetActiveDte();
                TextDocument objTextDocument =
                    objDte.ActiveDocument.Object("TextDocument") as TextDocument;

                if (objTextDocument == null)
                {
                    ShowMessage(
                        "El documento activo no es un documento de texto.",
                        OLEMSGICON.OLEMSGICON_WARNING);
                    return;
                }

                EditPoint objStartPoint = objTextDocument.StartPoint.CreateEditPoint();
                EditPoint objEndPoint = objTextDocument.EndPoint.CreateEditPoint();
                string strOriginalSql = objStartPoint.GetText(objEndPoint);

                if (string.IsNullOrWhiteSpace(strOriginalSql))
                {
                    SetStatus(objDte, "El documento SQL está vacío.");
                    return;
                }

                SqlFormatterSettings objSettings = objPackage.GetFormatterSettings();
                string strFormattedSql =
                    SqlFormatterService.Format(strOriginalSql, objSettings);

                if (string.Equals(strOriginalSql, strFormattedSql, StringComparison.Ordinal))
                {
                    SetStatus(objDte, "El documento ya tiene el formato configurado.");
                    return;
                }

                ExecuteUndoableAction(
                    objDte,
                    "Formatear documento T-SQL",
                    () =>
                    {
                        ThreadHelper.ThrowIfNotOnUIThread();
                        objStartPoint.ReplaceText(
                            objEndPoint,
                            strFormattedSql,
                            (int)vsEPReplaceTextOptions.vsEPReplaceTextKeepMarkers);
                    });

                SetStatus(objDte, "Documento T-SQL formateado correctamente.");
            }
            catch (SqlFormattingException objException)
            {
                ShowMessage(objException.Message, OLEMSGICON.OLEMSGICON_WARNING);
            }
            catch (Exception objException)
            {
                HandleUnexpectedError("formatear el documento", objException);
            }
        }

        private void ExecuteOpenOptions(object objSender, EventArgs objEventArgs)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                objPackage.ShowFormatterOptions();
            }
            catch (Exception objException)
            {
                HandleUnexpectedError("abrir la configuración", objException);
            }
        }

        private static DTE2 GetActiveDte()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            DTE2 objDte = Package.GetGlobalService(typeof(SDTE)) as DTE2;

            if (objDte?.ActiveDocument == null)
            {
                throw new SqlFormattingException(
                    "No existe un documento activo. Abre una ventana de consulta en SSMS.");
            }

            return objDte;
        }

        private static void ExecuteUndoableAction(
            DTE2 objDte,
            string strActionName,
            Action objAction)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            bool boolOpenedUndoContext = !objDte.UndoContext.IsOpen;

            if (boolOpenedUndoContext)
            {
                objDte.UndoContext.Open(strActionName, false);
            }

            try
            {
                objAction();
            }
            catch
            {
                if (boolOpenedUndoContext && objDte.UndoContext.IsOpen)
                {
                    objDte.UndoContext.SetAborted();
                }

                throw;
            }
            finally
            {
                if (boolOpenedUndoContext && objDte.UndoContext.IsOpen)
                {
                    objDte.UndoContext.Close();
                }
            }
        }

        private static void SetStatus(DTE2 objDte, string strMessage)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                objDte.StatusBar.Text = strMessage;
            }
            catch (Exception)
            {
                // El estado es informativo y no debe invalidar el formato aplicado.
            }
        }

        private static void HandleUnexpectedError(
            string strAction,
            Exception objException)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            ActivityLog.LogError(strProductName, objException.ToString());

            ShowMessage(
                $"Ocurrió un error inesperado al {strAction}." +
                Environment.NewLine +
                Environment.NewLine +
                objException.Message +
                Environment.NewLine +
                Environment.NewLine +
                "Si observas cambios parciales en el editor, puedes deshacerlos con Ctrl+Z. " +
                "Revisa ActivityLog.xml para obtener el detalle técnico.",
                OLEMSGICON.OLEMSGICON_CRITICAL);
        }

        private static void ShowMessage(string strMessage, OLEMSGICON enIcon)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            VsShellUtilities.ShowMessageBox(
                ServiceProvider.GlobalProvider,
                strMessage,
                strProductName,
                enIcon,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }
    }
}
