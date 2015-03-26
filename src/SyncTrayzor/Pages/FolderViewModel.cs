using Stylet;
using SyncTrayzor.SyncThing;
using SyncTrayzor.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.Pages
{
    public class FolderViewModel : PropertyChangedBase
    {
        public BindableCollection<DisplayLine> DisplayLines { get; private set; }
        public string FolderId { get; private set; }

        private DisplayLine folderPath = new DisplayLine() { Title = "Folder Path" };
        private DisplayLine globalState = new DisplayLine() { Title = "Global State" };

        public FolderViewModel(Folder folder)
        {
            this.FolderId = folder.FolderId;
            this.folderPath.Value = folder.Path;
            this.globalState.Value = "34 items, ~85.5MiB";

            this.DisplayLines = new BindableCollection<DisplayLine>()
            {
                this.folderPath,
                this.globalState,
            };
        }
    }
}
