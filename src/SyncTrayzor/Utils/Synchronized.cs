using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.Utils
{
    public class Synchronized<T>
    {
        private readonly object lockObject = new object();
        public T UnlockedValue { get; set; }
        public T Value
        {
            get { lock (this.lockObject) { return this.UnlockedValue; } }
            set { lock (this.lockObject) { this.UnlockedValue = value; } }
        }
    }
}
