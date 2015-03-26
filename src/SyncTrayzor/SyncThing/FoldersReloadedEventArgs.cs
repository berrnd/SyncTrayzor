using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing
{
    public class FoldersReloadedEventArgs : EventArgs
    {
        public IReadOnlyCollection<Folder> Folders { get; private set; }

        public FoldersReloadedEventArgs(IReadOnlyCollection<Folder> folders)
        {
            this.Folders = folders;
        }
    }
}
