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

            try {
                Config.Load();
                Instance.RefreshList();

                new MainWindow();

                Application.Run();
            }
            catch (Exception e) {
                MessageDialog md = new MessageDialog(new Window(""), DialogFlags.DestroyWithParent, MessageType.Question, ButtonsType.Close,
                    "Caught " + e.GetType().Name + ":\n\n"+e.Message
                );
                md.Icon = new Gdk.Pixbuf(System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("MultiBound.MultiBound-icon.ico"));
                md.Title = "MultiBound - Error Encountered";
                md.Run();
                md.Destroy();
            }
        }

        ScrolledWindow scrollField;
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
            scrollField = new ScrolledWindow();
            scrollField.Add(instList);
            vert.Add(scrollField);

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
                string selPath = (instListStore.GetValue(selIter, 0) as Instance).path.FileName;

                Instance.RefreshList();

                instListStore.Clear();
                TreeIter nSelIter; instListStore.GetIterFirst(out nSelIter);
                foreach (Instance inst in Instance.list) {
                    var iter = instListStore.AppendValues(inst);
                    if (inst.path.FileName == selPath) nSelIter = iter;
                }
                instList.Selection.SelectIter(nSelIter);
                instList.ScrollToCell(instListStore.GetPath(nSelIter), instList.Columns[0], false, 0, 0);
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
                    TreeIter nSelIter; instListStore.GetIterFirst(out nSelIter);
                    foreach (Instance inst in Instance.list) {
                        var iter = instListStore.AppendValues(inst);
                        if (inst == ire.selectInst) nSelIter = iter;
                    }
                    instList.Selection.SelectIter(nSelIter);
                    instList.ScrollToCell(instListStore.GetPath(nSelIter), instList.Columns[0], false, 0, 0);
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
                TreeIter nSelIter; instListStore.GetIterFirst(out nSelIter);
                foreach (Instance inst in Instance.list) {
                    var iter = instListStore.AppendValues(inst);
                    if (inst == ire.selectInst) nSelIter = iter;
                }
                instList.Selection.SelectIter(nSelIter);
                instList.ScrollToCell(instListStore.GetPath(nSelIter), instList.Columns[0], false, 0, 0);
                UpdateButtonBar();
            }, (sender, e) => {
                // fail
                Window.Cursor = new Gdk.Cursor(Gdk.CursorType.Arrow);
                btnWorkshopRefresh.Sensitive = true;
                SystemSounds.Asterisk.Play(); // signal failure
            }, selInst);
        }

        protected override void OnDestroyed() {
            Application.Quit();
        }
    }
}
