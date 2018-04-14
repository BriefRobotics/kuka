using System;
using System.Collections.Generic;

namespace I.Do.Everything
{
    public class Config
    {
        private Dictionary<string, string> store = new Dictionary<string, string>();

        public string this[string key]
        {
            get { return store[key.ToLower()]; }
            set { store.Add(key.ToLower(), value); }
        }

        public string MachineSpecific(string key)
        {
            var k = $"{Environment.MachineName}.{key}".ToLower();
            if (store.ContainsKey(k))
            {
                return store[k];
            }
            else
            {
                return this[key]; // fall back to non-machine-specific
            }
        }
    }
}
