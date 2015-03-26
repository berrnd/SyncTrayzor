using SyncTrayzor.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing
{
    public enum FolderSyncState
    {
        Syncing,
        Idle,
    }

    public class FolderIgnores
    {
        public IReadOnlyList<string> IgnorePatterns { get; private set; }
        public IReadOnlyList<Regex> IncludeRegex { get; private set; }
        public IReadOnlyList<Regex> ExcludeRegex { get; private set; }

        public FolderIgnores(List<string> ignores, List<string> patterns)
        {
            this.IgnorePatterns = ignores;
            var includeRegex = new List<Regex>();
            var excludeRegex = new List<Regex>();

            foreach (var pattern in patterns)
            {
                if (pattern.StartsWith("(?exclude)"))
                    excludeRegex.Add(new Regex(pattern.Substring("(?exclude)".Length)));
                else
                    includeRegex.Add(new Regex(pattern));
            }

            this.IncludeRegex = includeRegex.AsReadOnly();
            this.ExcludeRegex = excludeRegex.AsReadOnly();
        }
    }

    public class FolderState
    {
        public long Bytes { get; private set; }
        public long Deleted { get; private set; }
        public long Files { get; private set; }

        public FolderState(long bytes, long deleted, long files)
        {
            this.Bytes = bytes;
            this.Deleted = deleted;
            this.Files = files;
        }
    }

    public class Folder
    {
        public string FolderId { get; private set; }
        public string Path { get; private set; }
        public string ExpandedPath { get; private set; }

        private readonly Synchronized<FolderSyncState> _syncState = new Synchronized<FolderSyncState>();
        public FolderSyncState SyncState
        {
            get { return this._syncState.Value; }
            set { this._syncState.Value = value; }
        }

        private readonly object syncingPathsLock = new object();
        private HashSet<string> syncingPaths { get; set; }

        private readonly Synchronized<FolderIgnores> _ignores = new Synchronized<FolderIgnores>();
        public FolderIgnores Ignores
        {
            get { return this._ignores.Value; }
            set { this._ignores.Value = value; }
        }

        private readonly Synchronized<FolderState> _globalState = new Synchronized<FolderState>();
        public FolderState GlobalState
        {
            get { return this._globalState.Value; }
            set { this._globalState.Value = value; }
        }

        private readonly Synchronized<FolderState> _localState = new Synchronized<FolderState>();
        public FolderState LocalState
        {
            get { return this._localState.Value; }
            set { this._localState.Value = value; }
        }

        public Folder(string folderId, string path, string expandedPath)
        {
            this.FolderId = folderId;
            this.Path = path;
            this.ExpandedPath = expandedPath;
            this._syncState.UnlockedValue = FolderSyncState.Idle;
            this.syncingPaths = new HashSet<string>();
        }

        public bool IsSyncingPath(string path)
        {
            lock (this.syncingPathsLock)
            {
                return this.syncingPaths.Contains(path);
            }
        }

        public void AddSyncingPath(string path)
        {
            lock (this.syncingPathsLock)
            {
                this.syncingPaths.Add(path);
            }
        }

        public void RemoveSyncingPath(string path)
        {
            lock (this.syncingPathsLock)
            {
                this.syncingPaths.Remove(path);
            }
        }
    }
}
