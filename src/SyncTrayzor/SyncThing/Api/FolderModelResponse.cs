using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing.Api
{
    public class FolderModelResponse
    {
        [JsonProperty("globalBytes")]
        public long GlobalBytes { get; set; }

        [JsonProperty("globalDeleted")]
        public long GlobalDeleted { get; set; }

        [JsonProperty("globalFiles")]
        public long GlobalFiles { get; set; }

        [JsonProperty("localBytes")]
        public long LocalBytes { get; set; }

        [JsonProperty("localDeleted")]
        public long LocalDeleted { get; set; }

        [JsonProperty("localFiles")]
        public long LocalFiles { get; set; }

        [JsonProperty("needBytes")]
        public long NeedBytes { get; set; }

        [JsonProperty("needFiles")]
        public long NeedFiles { get; set; }

        [JsonProperty("ignorePatterns")]
        public bool HasIgnorePatterns { get; set; }

        [JsonProperty("inSyncBytes")]
        public long InSyncBytes { get; set; }

        [JsonProperty("inSyncFiles")]
        public long InSyncFiles { get; set; }

        [JsonProperty("invalid")]
        public string Invalid { get; set; }

        // TODO This needs to be an enumeration
        [JsonProperty("state")]
        public string State { get; set; }

        [JsonProperty("stateChanged")]
        public DateTime StateChanged { get; set; }

        [JsonProperty("version")]
        public long Version { get; set; }

        public override string ToString()
        {
            return String.Format("<FolderModel globalBytes={0} globalDeleted={1} globalFiles={2} localBytes={3} localDeleted={4} localFiles={5} " +
                    "needBytes={6} needFiles={7} ignorePatterns={8} inSyncBytes={9} inSyncFiles={10} invalid={11} state={12} stateChanged={13} version={14}>",
                    this.GlobalBytes, this.GlobalDeleted, this.GlobalFiles, this.LocalBytes, this.LocalDeleted, this.LocalFiles,
                    this.NeedBytes, this.NeedFiles, this.HasIgnorePatterns, this.InSyncBytes, this.InSyncFiles, this.Invalid, this.State, this.StateChanged, this.Version);
        }
    }
}
