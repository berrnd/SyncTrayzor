using Stylet;
using SyncTrayzor.SyncThing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.Pages
{
    public class FolderViewModel : PropertyChangedBase
    {
        public string FolderId { get; set; }
        public string FolderPath { get; set; }

        public FolderViewModel(Folder folder)
        {
            this.FolderId = folder.FolderId;
            this.FolderPath = folder.Path;
        }
    }
}
