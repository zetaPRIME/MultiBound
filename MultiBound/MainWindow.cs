using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Gtk;

using Fluent.IO;
using LitJson;

namespace MultiBound {
    public class MainWindow : Window {
        const string VERSION_LABEL = "alpha v0.01";

        static void Main(string[] args) {
            Application.Init ();

            Config.Load();
            Instance.RefreshList();

            new MainWindow ();

            Application.Run();
        }

        TreeView instList;
        ListStore instListStore;

        public MainWindow() : base("MultiBound") {

            Title = "MultiBound (" + VERSION_LABEL + ")";
            //string[] asdf = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceNames();
            this.Icon = new Gdk.Pixbuf(System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("MultiBound.MultiBound-icon.ico"));
            
            var vert = new VBox();
            Add(vert);

            instListStore = new ListStore(typeof(Instance));
            instList = new TreeView();
            
            instList.Model = instListStore;

            var col = new TreeViewColumn();
            col.Title = "Profile";

            CellRendererText cr = new CellRendererText();
            col.PackStart(cr, true);
            col.SetCellDataFunc(cr, (TreeViewColumn column, CellRenderer cell, ITreeModel model, TreeIter iter) => {
                Instance inst = (Instance)model.GetValue(iter, 0);
                CellRendererText ct = cell as CellRendererText;
                ct.Text = inst.Name;
                //ct.FontDesc = Pango.FontDescription.FromString("Segoe UI 10");
            });

            instList.AppendColumn(col);
            vert.Add(instList);

            instList.HeadersVisible = false;
            instList.CursorChanged += instList_CursorChanged;
            instList.ButtonPressEvent += (obj, args) => {
                if (args.Event.Type == Gdk.EventType.TwoButtonPress) LaunchSelected();
            };
            
            Button btn = new Button();
            btn.Label = "Launch!";
            vert.PackEnd(btn, false, false, 0);
            foreach (Instance inst in Instance.list) {
                instListStore.AppendValues(inst);
            }
            
            //btn.SetSizeRequest(100, 32);

            btn.Clicked += (sender, e) => LaunchSelected();

            this.SetDefaultSize(400, 300);
            ShowAll();
        }

        void instList_CursorChanged(object sender, EventArgs e) {
            //
        }

        void LaunchSelected() {
            TreeIter iter;
            instList.Selection.GetSelected(out iter);
            Hide();
            (instListStore.GetValue(iter, 0) as Instance).Launch();
            Show();
        }

        void LaunchOld() {
            JsonData jBuild = new JsonData();
            jBuild.SetJsonType(JsonType.Object);
            jBuild["storageDirectory"] = "..\\storage\\";
            var dconf = jBuild["defaultConfiguration"] = new JsonData();
            dconf.SetJsonType(JsonType.Object);
            dconf["gameServerBind"] = dconf["queryServerBind"] = dconf["rconServerBind"] = "*";

            List<string> dirs = new List<string>();
            dirs.Add("../assets/");

            JsonData assetDirs = jBuild["assetDirectories"] = new JsonData();
            assetDirs.SetJsonType(JsonType.Array);
            assetDirs.Add("..\\assets\\");

            Path wsp = Config.StarboundRootPath.Combine("../../workshop/content/211820/");
            foreach (var p in wsp.Directories()) {
                if (p.FileName.StartsWith("_")) continue; // ignore _whatever
                assetDirs.Add(p.FullPath);
            }
            

            string sj = JsonMapper.ToPrettyJson(jBuild);

            Path cfg = Config.StarboundPath.Up().Combine("mbinit.config");
            cfg.Write(sj);

            this.Hide();

            Process sb = new Process();
            sb.StartInfo.WorkingDirectory = Config.StarboundPath.Up().FullPath;
            sb.StartInfo.FileName = Config.StarboundPath.FullPath;
            //sb.StartInfo.Arguments = "-bootconfig \"" + cfg.FullPath + "\"";
            sb.StartInfo.Arguments = "-bootconfig mbinit.config";
            sb.Start();
            sb.WaitForExit();

            this.Show();
        }

        protected override void OnDestroyed() {
            Application.Quit();
        }
    }
}
