using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing
{
    public class FolderChangedEventArgs : EventArgs
    {
        public Folder Folder { get; private set; }

        public FolderChangedEventArgs(Folder folder)
        {
            this.Folder = folder;
        }
    }
}
