using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Fluent.IO;
using LitJson;

namespace MultiBound {
    public static class Config {
        public static Path StarboundPath;
        public static Path StarboundRootPath;
        public static Path InstanceRoot;

        public static JsonData SteamCmdInfo;

        public static void Load(string redir = null) {
            Path root = Path.Current;
            if (redir != null) root = Path.Get(redir);

            JsonData cfgJson = JsonMapper.ToObject(root.Combine("multibound.config").Read());
            if (cfgJson.Has("rootPath")) { Load((string)cfgJson["rootPath"]); return; }

            StarboundPath = root.Combine((string)cfgJson["starboundPath"]); // actually the Starbound *executable* path in most instances!
            if (!StarboundPath.Exists) throw new ArgumentException("Starbound path not found! Check your multibound.config; are you sure starboundPath is set correctly?");
            StarboundRootPath = StarboundPath;
            while (StarboundRootPath.FileName.ToLower() != "starbound" || !StarboundRootPath.IsDirectory) {
                if (!StarboundRootPath.Exists) throw new ArgumentException("Starbound path not found! Check your multibound.config; are you sure starboundPath is set correctly?");
                StarboundRootPath = StarboundRootPath.Up();
            }

            InstanceRoot = root.Combine("instances");
            if (cfgJson.Has("instanceRoot")) InstanceRoot = root.Combine((string)cfgJson["instanceRoot"]);

            // todo: steamcmd
        }
    }
}
