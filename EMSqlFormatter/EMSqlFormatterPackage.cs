using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace EMSqlFormatter
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration(
        "EM Sql Formatter",
        "Formateador T-SQL para SSMS 21",
        "1.1.4")]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideOptionPage(
        typeof(SqlFormatterOptionsPage),
        "EM Sql Formatter",
        "Formato",
        0,
        0,
        true)]
    [Guid(Guids.sPackageGuidString)]
    public sealed class EMSqlFormatterPackage : AsyncPackage
    {
        protected override async Task InitializeAsync(
            CancellationToken oCancellationToken,
            IProgress<ServiceProgressData> oProgress)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(oCancellationToken);

            await FormatSqlCommand.InitializeAsync(this);
        }

        internal SqlFormatterSettings GetFormatterSettings()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            SqlFormatterOptionsPage objOptionsPage =
                GetDialogPage(typeof(SqlFormatterOptionsPage)) as SqlFormatterOptionsPage;

            return objOptionsPage?.CreateSettings() ??
                   new SqlFormatterOptionsPage().CreateSettings();
        }

        internal void ShowFormatterOptions()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            ShowOptionPage(typeof(SqlFormatterOptionsPage));
        }
    }
}
