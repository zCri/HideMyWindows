using HideMyWindows.App.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HideMyWindows.App.Services.ConfigProvider
{
    public interface IConfigProvider
    {
        public string ConfigPath { get; protected set; } 
        public Config? Config { get; protected set; }

        public void Reload(string path);

        public void Reload()
        {
            Reload(ConfigPath);
        }

        public void Load(string path) {
            if (Config is null)
                Reload(path);
        }

        public void Load()
        {
            Load(ConfigPath);
        }

        public void Save(string path);

        public void Save()
        {
            Save(ConfigPath);
        }
    }
}
