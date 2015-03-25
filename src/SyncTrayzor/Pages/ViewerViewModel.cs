using Stylet;
using SyncTrayzor.SyncThing;
using SyncTrayzor.Xaml;
using SyncTrayzor.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Navigation;
using SyncTrayzor.Localization;
using SyncTrayzor.Services.Config;
using System.Threading;
using SyncTrayzor.Properties;

namespace SyncTrayzor.Pages
{
    public class ViewerViewModel : Screen
    {
        private readonly IWindowManager windowManager;
        private readonly ISyncThingManager syncThingManager;

        private readonly object cultureLock = new object(); // This can be read from many threads
        private CultureInfo culture;
        
        private SyncThingState syncThingState { get; set; }
        public bool ShowSyncThingStarting { get { return this.syncThingState == SyncThingState.Starting; } }
        public bool ShowSyncThingStopped { get { return this.syncThingState == SyncThingState.Stopped; ; } }

        public BindableCollection<FolderViewModel> Folders { get; set; }

        public ViewerViewModel(IWindowManager windowManager, ISyncThingManager syncThingManager, IConfigurationProvider configurationProvider)
        {
            this.windowManager = windowManager;
            this.syncThingManager = syncThingManager;
            this.syncThingManager.StateChanged += (o, e) =>
            {
                this.syncThingState = e.NewState;
            };

            this.Folders = new BindableCollection<FolderViewModel>()
            {
                new FolderViewModel(new Folder("id", "path", null)),
            };

            //this.callback = new JavascriptCallbackObject(this.OpenFolder);

            //this.Bind(x => x.WebBrowser, (o, e) =>
            //{
            //    if (e.NewValue != null)
            //        this.InitializeBrowser(e.NewValue);
            //});

            this.SetCulture(configurationProvider.Load());
            configurationProvider.ConfigurationChanged += (o, e) => this.SetCulture(e.NewConfiguration);
        }

        private void SetCulture(Configuration configuration)
        {
            lock (this.cultureLock)
            {
                this.culture = configuration.UseComputerCulture ? Thread.CurrentThread.CurrentUICulture : null;
            }
        }

        private void OpenFolder(string folderId)
        {
            Folder folder;
            if (!this.syncThingManager.TryFetchFolderById(folderId, out folder))
                return;
            Process.Start("explorer.exe", folder.Path);
        }

        public void Start()
        {
            this.syncThingManager.StartWithErrorDialog(this.windowManager);
        }
    }
}
