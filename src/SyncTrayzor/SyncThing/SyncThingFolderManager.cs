using SyncTrayzor.SyncThing.Api;
using SyncTrayzor.SyncThing.EventWatcher;
using SyncTrayzor.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing
{
    public interface ISyncThingFolderManager
    {
        bool TryFetchById(string folderId, out Folder folder);
        IReadOnlyCollection<Folder> FetchAll();

        event EventHandler<FoldersReloadedEventArgs> FoldersReloaded;
        event EventHandler<FolderChangedEventArgs> FolderChanged;
        event EventHandler<FolderSyncStateChangeEventArgs> SyncStateChanged;
    }

    public class SyncThingFolderManager : ISyncThingFolderManager
    {
        private readonly SynchronizedEventDispatcher eventDispatcher;
        private readonly ISyncThingApiClient apiClient;
        private readonly ISyncThingEventWatcher eventWatcher;

        public event EventHandler<FoldersReloadedEventArgs> FoldersReloaded;
        public event EventHandler<FolderChangedEventArgs> FolderChanged;
        public event EventHandler<FolderSyncStateChangeEventArgs> SyncStateChanged;

        // Folders is a ConcurrentDictionary, which suffices for most access
        // However, it is sometimes set outright (in the case of an initial load or refresh), so we need this lock
        // to create a memory barrier. The lock is only used when setting/fetching the field, not when accessing the
        // Folders dictionary itself.
        private readonly object foldersLock = new object();
        private ConcurrentDictionary<string, Folder> _folders = new ConcurrentDictionary<string, Folder>();
        private ConcurrentDictionary<string, Folder> folders
        {
            get { lock (this.foldersLock) { return this._folders; } }
            set { lock (this.foldersLock) { this._folders = value; } }
        }

        public SyncThingFolderManager(ISyncThingApiClient apiClient, ISyncThingEventWatcher eventWatcher)
        {
            this.eventDispatcher = new SynchronizedEventDispatcher(this);
            this.apiClient = apiClient;
            this.eventWatcher = eventWatcher;

            this.eventWatcher.SyncStateChanged += (o, e) => this.OnSyncStateChanged(e);
            this.eventWatcher.ItemStarted += (o, e) => this.ItemStarted(e.Folder, e.Item);
            this.eventWatcher.ItemFinished += (o, e) => this.ItemFinished(e.Folder, e.Item);
            this.eventWatcher.LocalIndexUpdated += (o, e) => this.LocalIndexUpdated(e.Folder);
        }

        public bool TryFetchById(string folderId, out Folder folder)
        {
            return this.folders.TryGetValue(folderId, out folder);
        }

        public IReadOnlyCollection<Folder> FetchAll()
        {
            return new List<Folder>(this.folders.Values).AsReadOnly();
        }

        public async Task ReloadIgnoresAsync(string folderId)
        {
            Folder folder;
            if (!this.folders.TryGetValue(folderId, out folder))
                return;

            var ignores = await this.apiClient.FetchIgnoresAsync(folderId);
            folder.Ignores = new FolderIgnores(ignores.IgnorePatterns, ignores.RegexPatterns);
        }

        public async Task LoadFoldersAsync(Config config, SystemInfoResponse systemInfo)
        {
            var folderConstructionTasks = config.Folders.Select(async configFolder =>
            {
                var expandedPath = configFolder.Path;
                if (expandedPath.StartsWith("~"))
                    expandedPath = Path.Combine(systemInfo.Tilde, expandedPath.Substring(1).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

                var folder = new Folder(configFolder.ID, configFolder.Path, expandedPath);
                await Task.WhenAll(this.LocalFolderIgnoresAsync(folder), this.RefreshFolderAsync(folder));
                return folder;
            });

            var folders = await Task.WhenAll(folderConstructionTasks);
            this.folders = new ConcurrentDictionary<string, Folder>(folders.Select(x => new KeyValuePair<string, Folder>(x.FolderId, x)));

            this.OnFoldersReloaded(this.FetchAll());
        }

        private void ItemStarted(string folderId, string item)
        {
            Folder folder;
            if (!this.folders.TryGetValue(folderId, out folder))
                return; // Don't know about it

            // This isn't worthy (currently) of a FolderChanged event
            folder.AddSyncingPath(item);
        }

        private void ItemFinished(string folderId, string item)
        {
            Folder folder;
            if (!this.folders.TryGetValue(folderId, out folder))
                return; // Don't know about it

            // This isn't worthy (currently) of a FolderChanged event
            folder.RemoveSyncingPath(item);
        }

        private async void LocalIndexUpdated(string folderId)
        {
            Folder folder;
            if (!this.folders.TryGetValue(folderId, out folder))
                return; // Don't know about it

            await this.RefreshFolderAsync(folder);

            this.OnFolderChanged(folder);
        }

        private async Task LocalFolderIgnoresAsync(Folder folder)
        {
            var ignores = await this.apiClient.FetchIgnoresAsync(folder.FolderId);
            folder.Ignores = new FolderIgnores(ignores.IgnorePatterns, ignores.RegexPatterns);
        }

        private async Task RefreshFolderAsync(Folder folder)
        {
            var folderModel = await this.apiClient.FetchFolderModelAsync(folder.FolderId);

            folder.GlobalState = new FolderState(folderModel.GlobalBytes, folderModel.GlobalDeleted, folderModel.GlobalFiles);
            folder.LocalState = new FolderState(folderModel.LocalBytes, folderModel.LocalDeleted, folderModel.LocalFiles);
        }

        private void OnFoldersReloaded(IReadOnlyCollection<Folder> folders)
        {
            this.eventDispatcher.Raise(this.FoldersReloaded, new FoldersReloadedEventArgs(folders));

            foreach (var folder in folders)
            {
                this.OnFolderChanged(folder);
            }
        }

        private void OnFolderChanged(Folder folder)
        {
            this.eventDispatcher.Raise(this.FolderChanged, new FolderChangedEventArgs(folder));
        }

        private void OnSyncStateChanged(SyncStateChangedEventArgs e)
        {
            Folder folder;
            if (!this.folders.TryGetValue(e.FolderId, out folder))
                return; // We don't know about this folder

            folder.SyncState = e.SyncState;

            this.eventDispatcher.Raise(this.SyncStateChanged, new FolderSyncStateChangeEventArgs(folder, e.PrevSyncState, e.SyncState));
        }
    }
}
