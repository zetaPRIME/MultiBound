using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using System.Net.Http;

using Fluent.IO;
using LitJson;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Parser.Html;

namespace MultiBound {
    public class InstanceRefreshEventArgs : EventArgs {
        public Instance selectInst { get; set; }
    }

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

        public static async void FromCollection(string url, EventHandler onComplete, EventHandler onFail, Instance updateTarget = null) {
            string id = url.Substring(url.LastIndexOf("=") + 1);

            JsonData instData;
            Path instPath;
            if (updateTarget != null) {
                instPath = updateTarget.path.Combine("instance.json");
                instData = JsonMapper.ToObject(instPath.Read());
                if (url == "") {
                    url = (string)instData["info"]["workshopLink"];
                    id = url.Substring(url.LastIndexOf("=") + 1);
                }
            }
            else {
                instPath = Config.InstanceRoot.Combine("workshop_" + id, "instance.json");
                if (instPath.Exists) instData = JsonMapper.ToObject(instPath.Read());
                else {
                    instData = JsonMapper.ToObject( // holy crepes this looks ugly
@"{
    ""info"" : { ""name"" : """", ""windowTitle"" : """" },
    ""savePath"" : ""inst:/storage/"",
    ""assetSources"" : [ ""inst:/mods"" ]
}");
                }
            }

            HtmlParser parser = new HtmlParser();
            //IBrowsingContext bc = BrowsingContext.New(Configuration.Default);
            //IDocument doc = bc.OpenAsync(url).Result;
            HttpClient client = new HttpClient();
            var request = await client.GetAsync(url);
            var response = await request.Content.ReadAsStreamAsync();
            var doc = parser.Parse(response);

            string serial = doc.DocumentElement.OuterHtml;

            //if (doc.All.Where(m => m.LocalName == "a" && m.Attributes["href"].Value == "http://steamcommunity.com/app/211820" && m.TextContent == "All").Count() == 0) return;
            var tst = doc.QuerySelectorAll("a[href=\"http://steamcommunity.com/app/211820\"]");
            if (doc.QuerySelectorAll("a[href=\"http://steamcommunity.com/app/211820\"]").Where(m => m.TextContent == "All").Count() == 0) { Gtk.Application.Invoke(onFail); return; } // make sure it's for Starbound
            if (doc.QuerySelectorAll("a[onclick=\"SubscribeCollection();\"]").Count() == 0) { Gtk.Application.Invoke(onFail); return; } // and that it's a collection

            JsonData autoMods = new JsonData();
            foreach (var item in doc.QuerySelectorAll(".collectionItemDetails > a")) {
                JsonData mod = new JsonData();
                string link = item.Attributes["href"].Value;
                mod["type"] = "workshopAuto";
                mod["id"] = link.Substring(link.LastIndexOf("=") + 1);
                mod["friendlyName"] = item.TextContent;
                autoMods.Add(mod);
            }

            foreach (JsonData item in instData["assetSources"]) {
                if (item.IsObject && (string)item["type"] == "workshopAuto") continue;
                autoMods.Add(item);
            }

            instData["assetSources"] = autoMods;

            string colName = doc.QuerySelector(".workshopItemDetailsHeader > .workshopItemTitle").TextContent;
            instData["info"]["workshopLink"] = url;
            if (!(instData["info"].Has("lockInfo") && (!instData["info"]["lockInfo"].IsBoolean || (bool)instData["info"]["lockInfo"]))) {
                instData["info"]["name"] = colName;
                instData["info"]["windowTitle"] = "Starbound - " + colName;
            }

            instPath.Write(JsonMapper.ToPrettyJson(instData));

            Instance.RefreshList();

            InstanceRefreshEventArgs e = new InstanceRefreshEventArgs();
            string fpath = instPath.Up().FullPath;
            foreach (Instance iInst in list) {
                if (iInst.path.FullPath == fpath) {
                    e.selectInst = iInst;
                    break;
                }
            }
            
            Gtk.Application.Invoke(null, e, onComplete);
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
        public bool IsWorkshop {
            get {
                return info.Has("workshopLink");
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
