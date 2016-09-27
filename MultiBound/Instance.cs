using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Fluent.IO;
using LitJson;

namespace MultiBound {
    public class Instance {
        public static List<Instance> list = new List<Instance>();
        public static void RefreshList() {
            list.Clear();
            Config.InstanceRoot.Directories().ForEach((p) => {
                if (!p.Combine("instance.json").Exists) return; // skip
                list.Add(Load(p));
            });

            list = list.OrderBy(i => i.Name).ToList(); // todo: ThenBy
        }

        public static Instance Load(Path root) {
            Instance inst = new Instance();
            inst.path = root;

            JsonData data = JsonMapper.ToObject(root.Combine("instance.json").Read());
            inst.info = data["info"];

            return inst;
        }

        private Instance() { }

        public Path path;
        public JsonData info;

        public string Name { get { return (string)info["name"]; } }
        public string WindowTitle {
            get {
                if (info.Has("windowTitle")) return (string)info["windowTitle"];
                return Name;
            }
        }

        public string EvalPath(string pathIn) {
            int cpos = pathIn.IndexOf(':');
            if (cpos == -1) return pathIn;

            string spec = pathIn.Substring(0, cpos);
            pathIn = pathIn.Substring(cpos + 1);
            if (pathIn.StartsWith("/") || pathIn.StartsWith("\\")) pathIn = pathIn.Substring(1); // let's not send to drive root

            if (spec == "sb") return Config.StarboundRootPath.Combine(pathIn).FullPath;
            if (spec == "inst") return path.Combine(pathIn).FullPath;

            return pathIn;
        }

        public void Launch() {
            JsonData data = JsonMapper.ToObject(path.Combine("instance.json").Read());
            info = data["info"]; // might as well refresh

            JsonData initCfg = new JsonData();
            initCfg.SetJsonType(JsonType.Object);
            var dconf = initCfg["defaultConfiguration"] = new JsonData();
            dconf.SetJsonType(JsonType.Object);
            dconf["gameServerBind"] = dconf["queryServerBind"] = dconf["rconServerBind"] = "*";

            JsonData assetDirs = initCfg["assetDirectories"] = new JsonData();

            assetDirs.SetJsonType(JsonType.Array);
            assetDirs.Add("../assets/");

            Path workshopRoot = Config.StarboundRootPath.Combine("../../workshop/content/211820/");

            if (data.Has("assetSources")) {
                JsonData assetSources = data["assetSources"];
                foreach (JsonData src in assetSources) {
                    if (src.IsString) {
                        assetDirs.Add(EvalPath((string)src)); // TODO: process with sb:, inst: markers
                        continue;
                    }

                    string type = "mod";
                    if (src.Has("type")) type = (string)src["type"];

                    switch (type) {
                        case "mod": {
                            // TODO: IMPLEMENT THIS MORE
                            if (src.Has("workshopId")) {
                                assetDirs.Add(workshopRoot.Combine((string)src["workshopId"]).FullPath);
                            }
                        } break;

                        case "workshopAuto": {
                            if (src.Has("id")) assetDirs.Add(workshopRoot.Combine((string)src["id"]).FullPath);
                        } break;

                        case "workshop": {
                            Dictionary<string, bool> blacklist = new Dictionary<string, bool>();
                            if (src.Has("blacklist")) foreach (JsonData entry in src["blacklist"]) blacklist[(string)entry] = true;

                            foreach (var p in workshopRoot.Directories()) {
                                if (p.FileName.StartsWith("_")) continue; // ignore _whatever
                                if (blacklist.ContainsKey(p.FileName)) continue; // ignore blacklisted items
                                assetDirs.Add(p.FullPath);
                            }
                        } break;

                        default: break; // unrecognized
                    }
                }
            }

            // and set window title
            {
                JsonData wtpatch = new JsonData();
                wtpatch.SetJsonType(JsonType.Array);
                JsonData patchEntry = new JsonData();
                patchEntry["op"] = "replace";
                patchEntry["path"] = "/windowTitle";
                patchEntry["value"] = WindowTitle;
                wtpatch.Add(patchEntry);

                path.Combine(".autopatch").Delete(true).Combine(".autopatch/assets/client.config.patch").Write(JsonMapper.ToJson(wtpatch));
                assetDirs.Add(EvalPath("inst:/.autopatch/"));
            }

            string storageDir = "inst:/storage/";
            if (data.Has("savePath")) storageDir = (string)(data["savePath"]);
            initCfg["storageDirectory"] = EvalPath(storageDir);

            Path outCfg = Config.StarboundPath.Up().Combine("mbinit.config");
            outCfg.Write(JsonMapper.ToPrettyJson(initCfg));

            Process sb = new Process();
            sb.StartInfo.WorkingDirectory = Config.StarboundPath.Up().FullPath;
            sb.StartInfo.FileName = Config.StarboundPath.FullPath;
            sb.StartInfo.Arguments = "-bootconfig mbinit.config";
            sb.Start();
            sb.WaitForExit();

            // cleanup
            path.Combine(".autopatch").Delete(true);
            outCfg.Delete();
        }
    }
}
