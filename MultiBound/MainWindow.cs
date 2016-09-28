using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Media;
using Gtk;

using Fluent.IO;
using LitJson;

namespace MultiBound {
    public class MainWindow : Window {
        const string VERSION_LABEL = "alpha v0.03";

        static void Main(string[] args) {
            Application.Init ();

            Config.Load();
            Instance.RefreshList();

            new MainWindow ();

            Application.Run();
        }

        TreeView instList;
        ListStore instListStore;

        Button btnWorkshopRefresh;

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

            var bottomBar = new HBox();
            vert.PackEnd(bottomBar, false, false, 0);
            
            Button btn = new Button();
            btn.Label = "Launch!";
            bottomBar.PackStart(btn, true, true, 0);

            btnWorkshopRefresh = new Button();
            btnWorkshopRefresh.Label = "Refresh";
            bottomBar.PackEnd(btnWorkshopRefresh, false, false, 0);
            

            foreach (Instance inst in Instance.list) {
                instListStore.AppendValues(inst);
            }

            
            //btn.SetSizeRequest(100, 32);

            btn.Clicked += (sender, e) => LaunchSelected();
            btnWorkshopRefresh.Clicked += (sender, e) => RefreshCollection();

            this.KeyPressEvent += OnKeyPress;

            this.SetDefaultSize(400, 300);
            ShowAll();
            UpdateButtonBar();
        }

        void OnKeyPress(object sender, KeyPressEventArgs e) {
            if (e.Event.Key == Gdk.Key.v && e.Event.State == Gdk.ModifierType.ControlMask) {
                // ctrl+v
                this.GetClipboard(Gdk.Selection.Clipboard).RequestText(OnPaste);
            }
            else if (e.Event.Key == Gdk.Key.r && e.Event.State == Gdk.ModifierType.ControlMask) {
                // ctrl+r, refresh list
                TreeIter selIter;
                instList.Selection.GetSelected(out selIter);
                string selPath = (instListStore.GetValue(selIter, 0) as Instance).path.FullPath;

                Instance.RefreshList();

                instListStore.Clear();
                foreach (Instance inst in Instance.list) {
                    var iter = instListStore.AppendValues(inst);
                    if (inst.path.FullPath == selPath) instList.Selection.SelectIter(iter);
                }
                if (instList.Selection.CountSelectedRows() == 0) { // empty selection, just select the first thing
                    TreeIter iter;
                    instListStore.GetIterFirst(out iter);
                    instList.Selection.SelectIter(iter);
                }
                UpdateButtonBar();

                SystemSounds.Asterisk.Play(); // signal that it at least did something
            }
        }

        void OnPaste(Clipboard clipboard, string text) {
            /*MessageDialog md = new MessageDialog(this, DialogFlags.DestroyWithParent, MessageType.Question, ButtonsType.YesNo, "Paste?");
            md.Run();
            md.Destroy();*/

            if (!btnWorkshopRefresh.Sensitive) SystemSounds.Hand.Play(); // signal disabled
            // example http://steamcommunity.com/sharedfiles/filedetails/?id=738053345
            else if (text.Contains("steamcommunity.com/sharedfiles/filedetails/?id=")) {
                text = "http://" + text.Substring(text.LastIndexOf("steamcommunity.com")); // conform to being link
                Window.Cursor = new Gdk.Cursor(Gdk.CursorType.Watch);
                btnWorkshopRefresh.Sensitive = false; // disable button
                
                Instance.FromCollection(text, (sender, e) => {
                    // success
                    Window.Cursor = new Gdk.Cursor(Gdk.CursorType.Arrow);
                    btnWorkshopRefresh.Sensitive = true;
                    var ire = (InstanceRefreshEventArgs)e;

                    instListStore.Clear();
                    foreach (Instance inst in Instance.list) {
                        var iter = instListStore.AppendValues(inst);
                        if (inst == ire.selectInst) instList.Selection.SelectIter(iter);
                    }
                    UpdateButtonBar();
                }, (sender, e) => {
                    // fail
                    Window.Cursor = new Gdk.Cursor(Gdk.CursorType.Arrow);
                    btnWorkshopRefresh.Sensitive = true;
                    SystemSounds.Asterisk.Play(); // signal failure
                });
            }
            else {
                SystemSounds.Asterisk.Play(); // signal invalid link
            }
        }

        void instList_CursorChanged(object sender, EventArgs e) {
            UpdateButtonBar();
        }

        void UpdateButtonBar() {
            TreeIter iter;
            instList.Selection.GetSelected(out iter);
            Instance inst = (instListStore.GetValue(iter, 0) as Instance);
            btnWorkshopRefresh.Visible = inst != null && inst.IsWorkshop;
        }

        void LaunchSelected() {
            TreeIter iter;
            instList.Selection.GetSelected(out iter);
            Hide();
            (instListStore.GetValue(iter, 0) as Instance).Launch();
            Show();
        }

        void RefreshCollection() {
            TreeIter selIter;
            instList.Selection.GetSelected(out selIter);
            Instance selInst = (instListStore.GetValue(selIter, 0) as Instance);

            Window.Cursor = new Gdk.Cursor(Gdk.CursorType.Watch);
            btnWorkshopRefresh.Sensitive = false; // disable button
            Instance.FromCollection("", (sender, e) => {
                // success
                Window.Cursor = new Gdk.Cursor(Gdk.CursorType.Arrow);
                btnWorkshopRefresh.Sensitive = true;
                var ire = (InstanceRefreshEventArgs)e;

                instListStore.Clear();
                foreach (Instance inst in Instance.list) {
                    var iter = instListStore.AppendValues(inst);
                    if (inst == ire.selectInst) instList.Selection.SelectIter(iter);
                }
                UpdateButtonBar();
            }, (sender, e) => {
                // fail
                Window.Cursor = new Gdk.Cursor(Gdk.CursorType.Arrow);
                btnWorkshopRefresh.Sensitive = true;
                SystemSounds.Asterisk.Play(); // signal failure
            }, selInst);
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
