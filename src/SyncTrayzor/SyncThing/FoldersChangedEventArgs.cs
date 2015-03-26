using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing
{
    public class FoldersChangedEventArgs : EventArgs
    {
        public IReadOnlyCollection<Folder> Folders { get; private set; }

        public FoldersChangedEventArgs(IReadOnlyCollection<Folder> folders)
        {
            this.Folders = folders;
        }
    }
}
