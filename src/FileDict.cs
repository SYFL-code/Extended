using Newtonsoft.Json;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtensionLib
{
    /// <summary>
    /// 轻量级文件字典，无锁，实时保存
    /// </summary>
    public class FileDict
    {
        private Dictionary<string, string> _data = new ();
        private readonly string _path;

        public FileDict(string path)
        {
            _path = path;
            Reload();
        }

        public bool TryGetValue(string key, out string value)
        {
            return _data.TryGetValue(key, out value);
        }

        public string this[string key]
        {
            get => _data[key];
            set
            {
                if (value == null)
                    _data.Remove(key);
                else
                    _data[key] = value;
                Save();
            }
        }

        public Dictionary<string, string> GetAll()
        {
            return _data;
        }

        public void Reload()
        {
            if (File.Exists(_path))
            {
                string json = File.ReadAllText(_path);
                _data = JsonConvert.DeserializeObject<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
            }
            else
            {
                _data = new Dictionary<string, string>();
            }
        }

        private void Save()
        {
            File.WriteAllText(_path, JsonConvert.SerializeObject(_data, Formatting.Indented));
        }
    }
}
