using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MenuItem = System.Windows.Controls.MenuItem;


namespace Unispect
{
    public sealed partial class MainWindow
    {
        private readonly System.Timers.Timer _typingSearchTimer = new System.Timers.Timer(500);
        private List<TypeDefWrapper> TypeDefinitions => _inspector?.TypeDefinitions;

        private void Inspector_ProgressChanged(object sender, float e)
        {
            PbMain.Value = e;
        }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            Loaded += MainWindow_Loaded;
            _typingSearchTimer.Elapsed += TypingSearchTimer_Elapsed;

            Closing += MainWindow_Closing;
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveSettings();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Log.LogMessageAdded += (o, args) =>
            {
                TxLog.Dispatcher.Invoke(() =>
                {
                    TxLog.Text = Log.LogText;
                    TxLog.ScrollToEnd();
                });
            };

            //TvMainView.SelectedItemChanged += (o, args) => { /* Todo: maybe set up the context to be used by other functions for the selected item here */ };

            // Assign our default memory accessor (it may be overridden in the next line)
            _memoryProxyType = typeof(BasicMemory);

            LoadSettings();
        }

        private void TreeViewItem_RequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
        {
            if (_mSuppressRequestBringIntoView)
                return;

            e.Handled = true;

            _mSuppressRequestBringIntoView = true;

            if (sender is TreeViewItem tvi)
            {
                var newTargetRect = new Rect(-1000, 0, tvi.ActualWidth + 1000, tvi.ActualHeight);
                tvi.BringIntoView(newTargetRect);
            }

            _mSuppressRequestBringIntoView = false;
        }
        private bool _mSuppressRequestBringIntoView;

        private Settings _settings;
        private static readonly string SettingsPath = Directory.GetCurrentDirectory() + "\\unispect.settings";

        private void LoadSettings()
        {
            try
            {
                Log.Add("Loading settings ...");
                _settings = Serializer.Load<Settings>(SettingsPath);

                if (_settings.AreEmpty)
                {
                    _settings = new Settings();
                    return;
                }

                // Todo maybe create a proper settings class, or just enum the property names
                if (_settings.TryGetValue("MemoryProxy", out var memProxyName))
                {
                    var plugins = LoadPlugins();
                    foreach (var pluginType in plugins.Where(pluginType => pluginType.FullName == memProxyName))
                    {
                        _memoryProxyType = pluginType;

                        var asmName = pluginType.Assembly.GetName();
                        Log.Add(
                            $"Using the plugin: '{asmName.Name}:{pluginType.FullName} (v{asmName.Version})' for memory access");
                    }
                }

                if (_settings.TryGetValue("ProcessHandle", out var procHandle))
                    TxProcessHandle.Text = procHandle;

                if (_settings.TryGetValue("TargetModule", out var targetModule))
                    TxInspectorTarget.Text = targetModule;

                if (_settings.TryGetValue("OutputPath", out var outputPath))
                    TxOutputFile.Text = outputPath;
            }
            catch (Exception ex)
            {
                Log.Exception("Couldn't read the settings file.", ex);
            }
        }

        private void SaveSettings(string specificSetting = "", string value = "")
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(specificSetting))
                {
                    _settings.AddOrUpdate(specificSetting, value);
                    return;
                }

                _settings.AddOrUpdate("MemoryProxy", _memoryProxyType.FullName);
                _settings.AddOrUpdate("ProcessHandle", TxProcessHandle.Text);
                _settings.AddOrUpdate("TargetModule", TxInspectorTarget.Text);
                _settings.AddOrUpdate("OutputPath", TxOutputFile.Text);

                Serializer.Save(SettingsPath, _settings);

                Log.Add("Settings saved.");
            }
            catch (Exception ex)
            {
                Log.Exception("Unable to save settings.", ex);
            }
        }

        private Inspector _inspector;
        private Type _memoryProxyType;

        private async void BtnDumpOffsets_Click(object sender, RoutedEventArgs e)
        {
            if (_memoryProxyType == typeof(BasicMemory))
            {
                var shouldContinue = await Utilities.MessageBox(
                    $"You are using native user-mode API.{Environment.NewLine}" +
                    "Are you sure you wish to continue?",
                    messageDialogStyle: MessageDialogStyle.AffirmativeAndNegative,
                    metroDialogSettings: new MetroDialogSettings
                    {
                        AnimateHide = false,
                        AnimateShow = false
                    });

                if (shouldContinue != MessageDialogResult.Affirmative)
                {
                    Log.Add("Operation cancelled");
                    return;
                }

                if (TxProcessHandle.Text.ToLower().EndsWith(".exe"))
                {
                    TxProcessHandle.Text = TxProcessHandle.Text.Substring(0, TxProcessHandle.Text.Length - 4);
                    Log.Add("Removed '.exe' from the proces handle (BasicMemory requires this to be omitted)");
                }
            }

            BtnDumpOffsets.IsEnabled = false;
            _inspector = new Inspector();
            _inspector.ProgressChanged += Inspector_ProgressChanged;

            PbMain.FadeIn(200);

            var fileName = _dumpToFile ? TxOutputFile.Text : "";
            var processHandle = TxProcessHandle.Text;
            var moduleToDump = TxInspectorTarget.Text;

            bool exceptionOccurred = false;
            await Task.Run(() =>
            {
                try
                {
                    Thread.Sleep(200); // Wait for the progress bar.. because it looks nice.

                    CacheStore.Clear();

                    _inspector.DumpTypes(
                        fileName,
                        _memoryProxyType,
                        processHandle: processHandle,
                        moduleToDump: moduleToDump);
                }
                catch (Exception ex)
                {
                    exceptionOccurred = true;
                    Log.Exception(null, ex);
                }
            });

            BtnDumpOffsets.IsEnabled = true;
            if (exceptionOccurred)
                return;

            TvMainView.DataContext = _inspector;
            TvMainView.ItemsSource = _inspector.TypeDefinitions;

            //PbMain.FadeOut(1000);
            BtnShowInspector.FadeIn(200);
            BtnSaveToFile.FadeIn(200);
        }

        private void BtnOpenGithub_OnClick(object sender, RoutedEventArgs e)
        {
            Utilities.LaunchUrl(Utilities.GithubLink);
        }

        private async void BtnCreateAsm_Click(object sender, RoutedEventArgs e)
        {
            await Utilities.MessageBox("Not implemented yet");
        }

        private void BtnSystemMenuClick(object sender, RoutedEventArgs e)
        {
            Utilities.ShowSystemMenu(this);
        }

        private void BtnShowInspector_OnClick(object sender, RoutedEventArgs e)
        {
            TypeInspectorFlyout.IsOpen = true;
        }


        private bool _isFlyoutOpen;

        public bool IsFlyoutOpen
        {
            get => _isFlyoutOpen;
            set
            {
                _isFlyoutOpen = value;
                OnFlyoutOpenChanged(value);
            }
        }

        public void OnFlyoutOpenChanged(bool isOpen)
        {
            this.ResizeFromTo(new Size(Width, Height),
                isOpen ? new Size(Width, 550) : new Size(Width, 350), 120);

            if (!isOpen)
            {
                TypePropertiesFlyout.IsOpen = false;
            }
        }

        private void BtnBrowseClick(object sender, RoutedEventArgs e)
        {
            var sfd = new SaveFileDialog
            {
                Title = "Unispect - Choose a location to save your file",
                Filter = "Text files|*.txt|All files|*.*",
                DefaultExt = "txt"
            };

            if (sfd.ShowDialog(this) != true)
                return;

            TxOutputFile.Text = sfd.FileName;
        }

        private bool _dumpToFile = true;

        private void CkDumpToFile_OnChecked(object sender, RoutedEventArgs e)
        {
            _dumpToFile = CkDumpToFile.IsChecked == true;
        }

        private async void BtnDumpToFile_OnClick(object sender, RoutedEventArgs e)
        {
            var fileName = TxOutputFile.Text;
            Log.Add($"Dumping definitions and offsets to file \"{fileName}\"");
            await Task.Run(() => { _inspector.DumpToFile(fileName); });
        }

        #region TypeInfo

        private void TreeViewParentClicked(object sender, RoutedEventArgs routedEventArgs)
        {
            TypePropertiesFlyout.IsOpen = true;
            if (LbFields.ItemsSource == null) LbFields.Items.Clear();
            try
            {
                var context =
                    (TypeDefWrapper)((System.Windows.Documents.Hyperlink)routedEventArgs.Source).DataContext;

                // We clicked the parent class, so we want to view that info2
                var typeDef = context.Parent;

                TbTypeName.Text = $"Base Type: {typeDef.Name}";
                TbOffset.Text = $"Parent: {typeDef.ParentName} ";
                LbFields.DataContext = typeDef;
                LbFields.ItemsSource = typeDef.Fields;

            }
            catch (Exception ex)
            {
                TbTypeName.Text = "Unable to retrieve.";
                Log.Exception(null, ex);
            }

        }

        private void TreeViewFieldTypeClicked(object sender, RoutedEventArgs routedEventArgs)
        {
            TypePropertiesFlyout.IsOpen = true;
            if (LbFields.ItemsSource == null) LbFields.Items.Clear();
            try
            {
                var context =
                    (FieldDefWrapper)((System.Windows.Documents.Hyperlink)routedEventArgs.Source).DataContext;
                var fTypeDef = (TypeDefWrapper)context.InnerDefinition.GetFieldType();
                if (fTypeDef == null)
                {
                    TbTypeName.Text = context.FieldType;
                    LbFields.ItemsSource = null;

                    var t = Type.GetType(context.FieldType);
                    if (t == null)
                    {
                        t = Type.GetType("System." + context.FieldType);
                    }
                    if (t != null)
                    {
                        LbFields.Items.Add($"Basic type length: {System.Runtime.InteropServices.Marshal.SizeOf(t)}");
                    }
                }
                else
                {
                    TbTypeName.Text = $"Base Type: {fTypeDef.Name}";
                    TbOffset.Text = $"Offset: 0x{context.Offset:X4}";
                    LbFields.DataContext = fTypeDef;
                    LbFields.ItemsSource = fTypeDef.Fields;
                }

            }
            catch (Exception ex)
            {
                TbTypeName.Text = "Unable to retrieve.";
                Log.Exception(null, ex);
            }

        }
        #endregion


        #region Inspector search
        private readonly List<TypeDefWrapper> _tempView = new List<TypeDefWrapper>();
        private string _searchFor = "";

        public bool SsIncludeFields { get; set; }
        public bool SsIncludeFieldTypes { get; set; }
        public bool SsIncludeParent { get; set; }
        public bool SsIncludeExtends { get; set; }

        private async void SearchTypes()
        {
            bool MatchCondition(TypeDefWrapper tdw)
            {
                var searchForLower = _searchFor.ToLower();

                if (tdw.FullName.ToLower().Contains(searchForLower))
                    return true;

                if (SsIncludeFields)
                    if (tdw.Fields.Any(field => field.Name.ToLower().Contains(searchForLower)))
                        return true;

                if (SsIncludeFieldTypes)
                    if (tdw.Fields.Any(field => field.FieldType?.ToLower().Contains(searchForLower) == true))
                        return true;

                if (SsIncludeParent)
                    if (tdw.ParentName?.ToLower().Contains(searchForLower) == true)
                        return true;

                if (SsIncludeExtends)
                    if (tdw.Interfaces.Any(iface => iface.Name.ToLower().Contains(searchForLower)))
                        return true;

                return false;
            }

            await Task.Run(() =>
            {
                var results = TypeDefinitions.FindAll(MatchCondition);

                _tempView.Clear();
                _tempView.AddRange(results);
            });

            Dispatcher.Invoke(() =>
            {
                TvMainView.ItemsSource = null; // hacky way to force the refresh
                                               // todo maybe change lists to observable collections
                TvMainView.ItemsSource = _tempView;
            });
        }

        private void BtnClearTextClick(object sender, RoutedEventArgs e)
        {
            TxSearchBox.Clear();
        }

        private void TxSearchBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            RestartSearch();
        }

        private void SearchToggleChanged(object sender, RoutedEventArgs e)
        {
            RestartSearch();
        }

        private void RestartSearch()
        {
            if (_typingSearchTimer.Enabled) _typingSearchTimer.Stop();

            _searchFor = TxSearchBox.Text;

            if (string.IsNullOrWhiteSpace(TxSearchBox.Text))
                TvMainView.ItemsSource = _inspector.TypeDefinitions;
            else
                _typingSearchTimer.Start();
        }

        private void TypingSearchTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            SearchTypes();
            _typingSearchTimer.Stop();
        }
        #endregion

        private async void BtnLoadPluginClick(object sender, RoutedEventArgs e)
        {

            var cm = new ContextMenu { IsOpen = true };

            cm.Items.Add(new MenuItem { Header = "Loading ...", IsEnabled = false });

            List<Type> pluginList = null;
            await Task.Run(() =>
            {
                pluginList = LoadPlugins();
            });

            cm.Items.RemoveAt(0);

            foreach (var p in pluginList)
            {
                var mi = new MenuItem();
                mi.Click += (o, args) =>
                {
                    _memoryProxyType = p;
                    if (p == typeof(BasicMemory))
                    {
                        Log.Add("Using Unispect's default memory access implementation");
                        return;
                    }

                    var asmName = p.Assembly.GetName();
                    Log.Add($"Using the plugin: '{asmName.Name}:{p.FullName} (v{asmName.Version})' for memory access");
                };

                mi.Header = p.Name;
                cm.Items.Add(mi);
            }
        }

        public List<Type> LoadPlugins()
        {
            // We will reload the list of assemblies each time just in case there are changes.
            var retList = new List<Type>();

            var pluginPath = Directory.GetCurrentDirectory() + "\\Plugins\\";
            if (!Directory.Exists(pluginPath))
                Directory.CreateDirectory(pluginPath);

            //Log.Add($"Searching for plug-ins in: {pluginPath}");

            // We will add our BasicMemory class here as an option as well.
            retList.Add(typeof(BasicMemory));

            // Search all sub directories, just in case the user decides to group their library with it's potential additional resources.
            foreach (var fileName in Directory.GetFiles(pluginPath, "*.dll", SearchOption.AllDirectories))
            {
                try
                {
                    var assembly = Assembly.LoadFrom(fileName);

                    // We will grab the first type found marked with our custom attribute.
                    var targetClass = assembly.GetTypes().FirstOrDefault(type =>
                        type.GetCustomAttributes(typeof(UnispectPluginAttribute), true).Length > 0);

                    if (targetClass == null)
                    {
                        Log.Warn(
                            $"{Path.GetFileName(fileName)} does not contain any classes with the UnispectPlugin attribute.");
                        continue;
                    }

                    retList.Add(targetClass);
                }
                catch (ReflectionTypeLoadException ex)
                {
                    Log.Exception($"{Path.GetFileName(fileName)}: The plugin definitions were not implemented correctly.", ex);
                }
                catch (Exception ex)
                {
                    Log.Exception($"Error loading: {Path.GetFileName(fileName)}.", ex);
                }
            }

            return retList;
        }


        public class OffsetChainInfo
        {
            public OffsetChainInfo(int offset, FieldDefWrapper field, TypeDefWrapper fieldParent)
            {
                FieldOffset = offset;
                Field = field;
                FieldParent = fieldParent;
            }
            public int FieldOffset;
            public FieldDefWrapper Field;
            public TypeDefWrapper FieldParent;
        }

        private void UIElement_OnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            return;
            // Todo implement offset chain viewer
            dynamic source = e.Source;
            var context = (FieldDefWrapper)source.DataContext;

            ContextMenu cm = new ContextMenu();
            cm.Items.Add(new MenuItem() { Header = "Test" });
            cm.IsOpen = true;

            // todo refactor and then move to the proper location (menuItem.Click event in a task)
            var suffix = $"[0x{context.Offset:X4}] {context.Name}";
            List<string> chains = new List<string>();

            // Todo maybe make this recursive with a depth setting
            foreach (var r in GetReferences(context.Parent))
            {
                var subrefs = GetReferences(r.FieldParent);
                foreach (var sr in subrefs)
                {
                    chains.Add($"{sr.FieldParent.FullName}.{sr.Field.Name} -> [{sr.FieldOffset:X4}] {r.Field.Name} -> [{r.FieldOffset:X4}] " +
                               $"{context.Parent.FullName} -> [{context.Offset:X4}] {context.Name}");
                }
            }
        }

        public List<OffsetChainInfo> GetReferences(TypeDefWrapper targetDef)
        {
            var retList = new List<OffsetChainInfo>();

            foreach (var tdw in TypeDefinitions)
            {
                foreach (var fdw in tdw.Fields)
                {
                    if (fdw.FieldType == targetDef.FullName)
                    {
                        retList.Add(new OffsetChainInfo(fdw.Offset, fdw, tdw));
                    }
                }
            }

            return retList;
        }
    }
}
