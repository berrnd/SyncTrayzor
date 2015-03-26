using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing.EventWatcher
{
    public class LocalIndexUpdatedEventArgs : EventArgs
    {
        public string Flags { get; private set; }
        public DateTime Modified { get; private set; }
        public string Name { get; private set; }
        public string Folder { get; private set; }
        public long Size { get; private set; }

        public LocalIndexUpdatedEventArgs(string flags, DateTime modified, string name, string folder, long size)
        {
            this.Flags = flags;
            this.Modified = modified;
            this.Name = name;
            this.Folder = folder;
            this.Size = size;
        }
    }
}
