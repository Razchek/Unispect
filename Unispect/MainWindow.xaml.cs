using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using MenuItem = System.Windows.Controls.MenuItem;


namespace Unispect
{
    public sealed partial class MainWindow
    {
        private readonly System.Timers.Timer _typingSearchTimer = new System.Timers.Timer(500);
        private List<TypeDefWrapper> TypeDefinitions => _inspector?.TypeDefinitions;
        private List<TypeDefWrapper> TypeDefinitionsDb { get; set; }


        private void Inspector_ProgressChanged(object sender, float e)
        {
            PbMain.IsIndeterminate = false;
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

            CbDropType.DataContext = this;
            CbDropType.ItemsSource = DropTypes;

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

                CbDropType.SelectedIndex = _settings.TryGetValue("DropTypeIndex", out var dropTypeIndex)
                    ? int.Parse(dropTypeIndex)
                    : 0;

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
                _settings.AddOrUpdate("DropTypeIndex", CbDropType.SelectedIndex.ToString());

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

        private void BtnDumpOffsets_OnClick(object sender, RoutedEventArgs e)
        {
            StartTypeDump();
        }

        private void BtnMoreClick(object sender, RoutedEventArgs e)
        {
            var cm = new ContextMenu();

            var mi = new MenuItem { Header = "Load Type DB" };
            mi.Click += LoadTypeDbClick;

            cm.Items.Add(mi);
            cm.IsOpen = true;
        }

        private async void LoadTypeDbClick(object sender, RoutedEventArgs e)
        {
            ToggleProcessingButtons(false);

            var ofd = new OpenFileDialog
            {
                Filter = "Unispect TypeDef DB|*.utd;*.gz|All files|*.*"
            };
            var dialogResult = ofd.ShowDialog(this);

            if (dialogResult == true)
            {
                try
                {
                    Log.Add("Loading type definition database");

                    PbMain.IsIndeterminate = true;
                    if (!PbMain.IsVisible) PbMain.FadeIn();

                    await Task.Run(() =>
                    {
                        Thread.Sleep(100);
                        TypeDefinitionsDb = ofd.FileName.ToLower().EndsWith(".gz")
                            ? Serializer.LoadCompressed<List<TypeDefWrapper>>(ofd.FileName)
                            : Serializer.Load<List<TypeDefWrapper>>(ofd.FileName);
                    });

                    PbMain.IsIndeterminate = false;
                    PbMain.Value = 1;
                    TvMainView.DataContext = this;
                    TvMainView.ItemsSource = TypeDefinitionsDb;

                    if (!BtnShowInspector.IsVisible)
                    {
                        BtnShowInspector.FadeIn(200);
                        BtnSaveToFile.FadeIn(200);
                    }

                    Log.Add("Done");
                }
                catch (Exception ex)
                {
                    Log.Exception(
                        "Could not load the type definition database, " +
                        "perhaps it's from a different version of Unispect.",
                        ex);
                }
            }

            ToggleProcessingButtons(true);
        }

        private async void StartTypeDump()
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

            ToggleProcessingButtons(false);

            _inspector = new Inspector();
            _inspector.ProgressChanged += Inspector_ProgressChanged;

            var progressWatcher = new System.Timers.Timer(200);
            var lastProgressValue = 0d;
            progressWatcher.Start();
            progressWatcher.Elapsed += (sender, args) =>
            {
                try
                {
                    Dispatcher.Invoke(() =>
                    {
                        // Precision comparison should be fine here, but we'll do this just in case
                        const double epsilon = double.Epsilon;
                        if (Math.Abs(PbMain.Value - lastProgressValue) < epsilon &&
                            Math.Abs(PbMain.Value - 1) > epsilon &&
                            Math.Abs(PbMain.Value) > epsilon)
                        {
                            PbMain.IsIndeterminate = true;
                            return;
                        }

                        lastProgressValue = PbMain.Value;
                    });
                }
                catch
                {
                    // 
                }
            };

            PbMain.FadeIn(200);

            var fileName = _dumpToFile ? TxOutputFile.Text : "";
            var processHandle = TxProcessHandle.Text;
            var moduleToDump = TxInspectorTarget.Text;

            TypeDefinitionsDb = null;
            var exceptionOccurred = false;
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

            ToggleProcessingButtons(true);

            if (exceptionOccurred)
            {
                PbMain.Value = 0;
                PbMain.FadeOut();
                return;
            }

            TvMainView.DataContext = this;
            TvMainView.ItemsSource = TypeDefinitions;

            //PbMain.FadeOut(1000);
            if (!BtnShowInspector.IsVisible)
            {
                BtnShowInspector.FadeIn(200);
                BtnSaveToFile.FadeIn(200);
            }
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
            TxSearchBox.Text = "";
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
            ToggleProcessingButtons(false);

            var fileName = TxOutputFile.Text;
            var procHandle = TxProcessHandle.Text;
            var modToDump = TxInspectorTarget.Text;
            Log.Add($"Dumping definitions and offsets to file \"{fileName}\"");

            await Task.Run(() =>
            {
                // If tdlToDump is null then the method will try to dump from the inspector type definition list
                // TypeDefinitionsDb will be null if: a) nothing has been loaded, or b) StartTypeDump has executed successfully
                if (_inspector == null) _inspector = new Inspector();
                _inspector.DumpToFile(fileName, tdlToDump: TypeDefinitionsDb);

                PbMain.Dispatcher.Invoke(() => { PbMain.IsIndeterminate = true; });

                // Only save the database if it's a fresh dump
                if (TypeDefinitionsDb == null)
                {
                    _inspector.SaveTypeDefDb(procHandle, modToDump);
                    Log.Add("Done");
                }
            });

            PbMain.IsIndeterminate = false;
            ToggleProcessingButtons(true);

        }

        private void ToggleProcessingButtons(bool enabled)
        {
            BtnDumpOffsets.IsEnabled = enabled;
            BtnMore.IsEnabled = enabled;
            BtnSaveToFile.IsEnabled = enabled;
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
            try
            {
                TypeDefWrapper fTypeDef;
                var context = (FieldDefWrapper)((System.Windows.Documents.Hyperlink)routedEventArgs.Source).DataContext;
                if (MemoryProxy.Instance == null)
                {
                    var curTypeDefList = TypeDefinitionsDb ?? TypeDefinitions;
                    fTypeDef = curTypeDefList.Find(tdw => tdw.FullName == context.FieldType.Replace("[]", ""));
                }
                else
                {
                    if (LbFields.ItemsSource == null) LbFields.Items.Clear();

                    // This requires remote memory access
                    // Using Field.FieldTypeDefinition requires significantly extra time and local memory during propogation
                    // Currently the only usage would be here, so it is not used for now.
                    fTypeDef = context.InnerDefinition.GetFieldType();
                }

                TypePropertiesFlyout.IsOpen = true;
                if (fTypeDef == null)
                {
                    TbTypeName.Text = context.FieldType;
                    LbFields.ItemsSource = null;
                    TbOffset.Text = $"Offset: 0x{context.Offset:X4}";
                    var t = Type.GetType(context.FieldType);
                    if (t == null)
                        t = Type.GetType("System." + context.FieldType);
                    if (t != null)
                        LbFields.Items.Add($"Basic type length: {System.Runtime.InteropServices.Marshal.SizeOf(t)}");
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
                TbTypeName.Text = $"Unable to retrieve.{Environment.NewLine}{ex.Message}";
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
                var results = TypeDefinitionsDb == null
                    ? TypeDefinitions.FindAll(MatchCondition)
                    : TypeDefinitionsDb.FindAll(MatchCondition);

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
                TvMainView.ItemsSource = TypeDefinitionsDb ?? _inspector.TypeDefinitions;
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

            var cm = new ContextMenu();
            cm.Items.Add(new MenuItem() { Header = "Test" });
            cm.IsOpen = true;

            // todo refactor and then move to the proper location (menuItem.Click event in a task)
            var suffix = $"[0x{context.Offset:X4}] {context.Name}";
            var chains = new List<string>();

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

        private Point _dragStartPoint;
        private bool _isDragging = false;
        private bool _canDrag = false;
        private void TvMainView_OnPreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (!_canDrag)
                return;

            if (e.LeftButton == MouseButtonState.Pressed || e.RightButton == MouseButtonState.Pressed && !_isDragging)
            {
                var position = e.GetPosition(null);
                if (Math.Abs(position.X - _dragStartPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(position.Y - _dragStartPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    StartDrag(e);
                }
            }
        }

        private void TvMainView_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (IsMouseOverScrollbar(sender, e.GetPosition(sender as IInputElement)))
            {
                _canDrag = false;
            }
            else
            {
                _canDrag = true;
                _dragStartPoint = e.GetPosition(null);
            }
        }

        private static bool IsMouseOverScrollbar(object sender, Point mousePosition)
        {
            if (!(sender is Visual visual))
                return false;

            var hit = VisualTreeHelper.HitTest(visual, mousePosition);
            if (hit == null)
                return false;

            var dObj = hit.VisualHit;
            while (dObj != null)
            {
                switch (dObj)
                {
                    case ScrollBar _:
                        return true;
                    case Visual _:
                    case Visual3D _:
                        dObj = VisualTreeHelper.GetParent(dObj);
                        break;
                    default:
                        dObj = LogicalTreeHelper.GetParent(dObj);
                        break;
                }
            }

            return false;
        }

        private ObservableCollection<string> DropTypes { get; } = new ObservableCollection<string>
        {
            "Text tree",
            "C# struct (IntPtr)",
            "C# struct (ulong)"
        };

        private void StartDrag(MouseEventArgs e)
        {
            _isDragging = true;

            var item = this.TvMainView.SelectedItem;
            if (item != null)
            {
                //https://docs.microsoft.com/en-us/dotnet/api/system.windows.dataformats?view=netcore-3.1
                var data = new DataObject();
                //data.SetData(DataFormats.Serializable, item);
                switch (item)
                {
                    case TypeDefWrapper typeDef:
                        var outputType = CbDropType.SelectedIndex;
                        var dataStr = "";
                        switch (outputType)
                        {
                            case 0:
                                dataStr = typeDef.ToTreeString();
                                break;
                            case 1:
                                dataStr = typeDef.ToCSharpString("IntPtr");
                                break;
                            case 2:
                                dataStr = typeDef.ToCSharpString();
                                break;
                        }

                        data.SetData(DataFormats.Text, dataStr);
                        break;

                    case FieldDefWrapper fieldDef:
                        data.SetData(DataFormats.Text,
                            $"{fieldDef.Parent.FullName}->{fieldDef.Name} // Offset: 0x{fieldDef.Offset:X4} (Type: {fieldDef.FieldType})" +
                            $"{Environment.NewLine}");
                        break;

                    default:
                        data.SetData(DataFormats.Text, item.ToString());
                        break;
                }

                var dragDropEffects = DragDropEffects.Move | DragDropEffects.Copy;

                if (e.RightButton == MouseButtonState.Pressed)
                    dragDropEffects = DragDropEffects.All;

                DragDrop.DoDragDrop(TvMainView, data, dragDropEffects);
            }

            _isDragging = false;
        }
    }
}
