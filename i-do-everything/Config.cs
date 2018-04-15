using System;
using System.Collections.Generic;

namespace I.Do.Everything
{
    public class Config
    {
        private Dictionary<string, string> store = new Dictionary<string, string>();

        public string this[string key]
        {
            get
            {
                var k = $"{Environment.MachineName}.{key}".ToLower();
                return store.ContainsKey(k) ? store[k] : store[key.ToLower()]; // fall back to non-machine-specific as necessary
            }
            set { store.Add(key.ToLower(), value); }
        }

        public bool ContainsKey(string key)
        {
            return store.ContainsKey(key.ToLower());
        }
    }
}
