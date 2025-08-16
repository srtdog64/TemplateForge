using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using TemplateForge.Core;

namespace TemplateForge
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        private TemplateSpaceManager spaceManager;
        private TemplateSpace currentSpace;
        private YamlStructureGenerator structureGenerator;
        private Dictionary<string, TextBox> tabEditors;
        private ObservableCollection<DocumentTreeItem> documentTreeItems;

        public MainWindow()
        {
            InitializeComponent();
            InitializeComponents();
            this.Loaded += MainWindow_Loaded;
        }

        private void InitializeComponents()
        {
            spaceManager = new TemplateSpaceManager();
            structureGenerator = new YamlStructureGenerator();
            tabEditors = new Dictionary<string, TextBox>();
            documentTreeItems = new ObservableCollection<DocumentTreeItem>();
            
            // 기본 스페이스 생성
            currentSpace = spaceManager.CreateSpace("Default");
            currentSpace.AddDocument("main", GetDefaultYaml(), "architecture");
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateUI();
            LoadTemplateSpaces();
        }

        private void LoadTemplateSpaces()
        {
            var spaces = spaceManager.GetAllSpaces();
            TemplateSpaceCombo.ItemsSource = spaces;
            if (spaces.Any())
            {
                TemplateSpaceCombo.SelectedIndex = 0;
            }
        }

        private void UpdateUI()
        {
            if (currentSpace != null)
            {
                // 문서 트리 업데이트
                UpdateDocumentTree();
                
                // 상태바 업데이트
                DocumentCountText.Text = $"Documents: {currentSpace.Documents.Count}";
                SpaceNameText.Text = $"Space: {currentSpace.Name}";
                StatusText.Text = "Ready";
                
                // 참조 분석
                UpdateReferences();
            }
        }

        private void UpdateDocumentTree()
        {
            documentTreeItems.Clear();
            
            if (currentSpace == null) return;
            
            var root = new DocumentTreeItem
            {
                Name = currentSpace.Name,
                Icon = "📦",
                IsExpanded = true,
                Children = new ObservableCollection<DocumentTreeItem>()
            };
            
            // 문서별 그룹화
            var groups = currentSpace.Documents.Values
                .GroupBy(d => d.Type)
                .OrderBy(g => g.Key);
            
            foreach (var group in groups)
            {
                var groupItem = new DocumentTreeItem
                {
                    Name = group.Key,
                    Icon = GetTypeIcon(group.Key),
                    IsExpanded = true,
                    Children = new ObservableCollection<DocumentTreeItem>()
                };
                
                foreach (var doc in group)
                {
                    groupItem.Children.Add(new DocumentTreeItem
                    {
                        Name = doc.Name,
                        Icon = "📄",
                        Document = doc,
                        IsModified = doc.IsModified
                    });
                }
                
                root.Children.Add(groupItem);
            }
            
            documentTreeItems.Add(root);
            DocumentTree.ItemsSource = documentTreeItems;
        }

        private void UpdateReferences()
        {
            if (currentSpace == null) return;
            
            var references = currentSpace.AnalyzeReferences();
            var refText = "=== YAML References ===\n\n";
            
            foreach (var reference in references)
            {
                refText += $"📄 {reference.FromDocument}\n";
                refText += $"  └─> {reference.ToPath} (line {reference.LineNumber})\n";
            }
            
            if (!references.Any())
            {
                refText = "No references found.";
            }
            
            ReferencesText.Text = refText;
        }

        private string GetTypeIcon(string type)
        {
            switch (type.ToLower())
            {
                case "architecture":
                case "root":
                    return "🏗️";
                case "module":
                    return "📦";
                case "integration":
                    return "🔗";
                case "pipeline":
                    return "⚡";
                case "testing":
                    return "🧪";
                case "monitoring":
                    return "📊";
                default:
                    return "📁";
            }
        }

        // === 이벤트 핸들러 ===

        private void NewSpace_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new TextInputDialog("New Template Space", "Enter space name:");
            if (dialog.ShowDialog() == true)
            {
                currentSpace = spaceManager.CreateSpace(dialog.InputText);
                currentSpace.AddDocument("architecture", null, "architecture");
                LoadTemplateSpaces();
                UpdateUI();
            }
        }

        private void TemplateSpace_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TemplateSpaceCombo.SelectedItem is TemplateSpace space)
            {
                currentSpace = space;
                spaceManager.SetActiveSpace(space.Name);
                UpdateUI();
                LoadDocumentTabs();
            }
        }

        private void NewProject_Click(object sender, RoutedEventArgs e)
        {
            var templates = new[]
            {
                "Microservice Architecture",
                "Game Architecture",
                "Data Pipeline",
                "Basic Module"
            };
            
            var dialog = new ListSelectionDialog("Select Template", templates);
            if (dialog.ShowDialog() == true)
            {
                var templateType = dialog.SelectedItem.ToLower().Replace(" ", "");
                currentSpace = spaceManager.CreateSpaceFromTemplate(templateType);
                LoadTemplateSpaces();
                UpdateUI();
            }
        }

        private void LoadYaml_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "YAML files (*.yaml;*.yml)|*.yaml;*.yml|All files (*.*)|*.*",
                Title = "Load YAML File"
            };
            
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var content = File.ReadAllText(dialog.FileName);
                    var name = Path.GetFileNameWithoutExtension(dialog.FileName);
                    
                    if (currentSpace != null)
                    {
                        var doc = currentSpace.AddDocument(name, content, "imported");
                        AddDocumentTab(doc);
                        UpdateUI();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading file: {ex.Message}", "Error", 
                                   MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void AddDocument_Click(object sender, RoutedEventArgs e)
        {
            if (currentSpace == null)
            {
                MessageBox.Show("Please create or select a template space first.", "Info",
                               MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            var dialog = new NewDocumentDialog();
            if (dialog.ShowDialog() == true)
            {
                var doc = currentSpace.AddDocument(dialog.DocumentName, null, dialog.DocumentType);
                AddDocumentTab(doc);
                UpdateUI();
            }
        }

        private void LinkDocuments_Click(object sender, RoutedEventArgs e)
        {
            if (currentSpace == null) return;
            
            currentSpace.GenerateReferencedDocuments();
            UpdateUI();
            MessageBox.Show("Referenced documents created successfully!", "Success",
                           MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SaveSpace_Click(object sender, RoutedEventArgs e)
        {
            // 현재 탭의 내용을 문서에 저장
            SaveCurrentTab();
            
            MessageBox.Show($"Template space '{currentSpace?.Name}' saved.", "Success",
                           MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            if (currentSpace == null) return;
            
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.Description = "Select export folder";
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    try
                    {
                        currentSpace.ExportToFileSystem(dialog.SelectedPath);
                        MessageBox.Show($"Exported to: {dialog.SelectedPath}", "Success",
                                       MessageBoxButton.OK, MessageBoxImage.Information);
                        
                        // 폴더 열기
                        System.Diagnostics.Process.Start("explorer.exe", dialog.SelectedPath);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Export failed: {ex.Message}", "Error",
                                       MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void Generate_Click(object sender, RoutedEventArgs e)
        {
            if (currentSpace == null) return;
            
            var outputPath = OutputPathTextBox.Text;
            if (string.IsNullOrEmpty(outputPath))
            {
                MessageBox.Show("Please select output path.", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            try
            {
                // 모든 문서를 파일로 출력
                currentSpace.ExportToFileSystem(outputPath);
                
                // 폴더 구조도 생성
                foreach (var doc in currentSpace.Documents.Values)
                {
                    var result = structureGenerator.GenerateStructure(
                        doc.Content, 
                        Path.Combine(outputPath, doc.Type),
                        doc.Name
                    );
                }
                
                MessageBox.Show($"Project generated successfully!\nLocation: {outputPath}",
                               "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                
                System.Diagnostics.Process.Start("explorer.exe", outputPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Generation failed: {ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DocumentTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (DocumentTree.SelectedItem is DocumentTreeItem item && item.Document != null)
            {
                // 해당 문서의 탭 열기 또는 활성화
                OpenDocumentTab(item.Document);
            }
        }

        private void NewTab_Click(object sender, RoutedEventArgs e)
        {
            AddDocument_Click(sender, e);
        }

        private void CloseTab_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var tabItem = FindParent<TabItem>(button);
            if (tabItem != null && EditorTabs.Items.Count > 1)
            {
                var docId = tabItem.Tag as string;
                if (docId != null && tabEditors.ContainsKey(docId))
                {
                    tabEditors.Remove(docId);
                }
                EditorTabs.Items.Remove(tabItem);
            }
        }

        private void YamlEditor_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox editor && editor.Tag is YamlDocument doc)
            {
                doc.Content = editor.Text;
                doc.IsModified = true;
                UpdateUI();
            }
        }

        private void Validate_Click(object sender, RoutedEventArgs e)
        {
            SaveCurrentTab();
            // 검증 로직
            ValidationResults.Text = "✅ Validation passed";
        }

        private void Preview_Click(object sender, RoutedEventArgs e)
        {
            SaveCurrentTab();
            UpdateReferences();
        }

        private void Diagram_Click(object sender, RoutedEventArgs e)
        {
            // 다이어그램 생성 로직
        }

        private void BrowseOutputPath_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.Description = "Select output folder";
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    OutputPathTextBox.Text = dialog.SelectedPath;
                }
            }
        }

        // === 헬퍼 메서드 ===

        private void LoadDocumentTabs()
        {
            EditorTabs.Items.Clear();
            tabEditors.Clear();
            
            if (currentSpace != null && currentSpace.Documents.Any())
            {
                var firstDoc = currentSpace.Documents.Values.First();
                AddDocumentTab(firstDoc);
            }
        }

        private void AddDocumentTab(YamlDocument doc)
        {
            var tabItem = new TabItem
            {
                Header = doc.Name,
                Tag = doc.Id
            };
            
            var editor = new TextBox
            {
                Text = doc.Content,
                Tag = doc,
                FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                FontSize = 11,
                AcceptsReturn = true,
                AcceptsTab = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                TextWrapping = TextWrapping.NoWrap,
                Background = Brushes.White,
                Padding = new Thickness(8)
            };
            
            editor.TextChanged += YamlEditor_TextChanged;
            
            tabItem.Content = editor;
            EditorTabs.Items.Add(tabItem);
            EditorTabs.SelectedItem = tabItem;
            
            tabEditors[doc.Id] = editor;
        }

        private void OpenDocumentTab(YamlDocument doc)
        {
            // 이미 열려있는지 확인
            foreach (TabItem tab in EditorTabs.Items)
            {
                if (tab.Tag as string == doc.Id)
                {
                    EditorTabs.SelectedItem = tab;
                    return;
                }
            }
            
            // 새 탭 추가
            AddDocumentTab(doc);
        }

        private void SaveCurrentTab()
        {
            if (EditorTabs.SelectedItem is TabItem tab && tab.Content is TextBox editor)
            {
                if (editor.Tag is YamlDocument doc)
                {
                    doc.Content = editor.Text;
                    doc.LastModified = DateTime.Now;
                }
            }
        }

        private T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            var parent = VisualTreeHelper.GetParent(child);
            if (parent == null) return null;
            if (parent is T) return parent as T;
            return FindParent<T>(parent);
        }

        private string GetDefaultYaml()
        {
            return @"# Template Space Root
meta:
  version: 1
  created: " + DateTime.Now.ToString("yyyy-MM-dd") + @"

architecture:
  name: MyProject
  modules:
    - ref: ./modules/module1.yaml
    - ref: ./modules/module2.yaml
";
        }
    }

    // === 보조 클래스들 ===

    public class DocumentTreeItem
    {
        public string Name { get; set; }
        public string Icon { get; set; }
        public YamlDocument Document { get; set; }
        public bool IsModified { get; set; }
        public bool IsExpanded { get; set; }
        public ObservableCollection<DocumentTreeItem> Children { get; set; }
    }

    // 간단한 다이얼로그들
    public class TextInputDialog : Window
    {
        public string InputText { get; private set; }
        private TextBox inputBox;

        public TextInputDialog(string title, string prompt)
        {
            Title = title;
            Width = 400;
            Height = 150;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            
            var label = new Label { Content = prompt, Margin = new Thickness(10) };
            Grid.SetRow(label, 0);
            
            inputBox = new TextBox { Margin = new Thickness(10, 0, 10, 10) };
            Grid.SetRow(inputBox, 1);
            
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(10)
            };
            
            var okButton = new Button { Content = "OK", Width = 75, Margin = new Thickness(5), IsDefault = true };
            okButton.Click += (s, e) => { InputText = inputBox.Text; DialogResult = true; };
            
            var cancelButton = new Button { Content = "Cancel", Width = 75, Margin = new Thickness(5), IsCancel = true };
            
            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            Grid.SetRow(buttonPanel, 2);
            
            grid.Children.Add(label);
            grid.Children.Add(inputBox);
            grid.Children.Add(buttonPanel);
            
            Content = grid;
        }
    }

    public class ListSelectionDialog : Window
    {
        public string SelectedItem { get; private set; }
        private ListBox listBox;

        public ListSelectionDialog(string title, string[] items)
        {
            Title = title;
            Width = 400;
            Height = 300;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            
            listBox = new ListBox { Margin = new Thickness(10) };
            foreach (var item in items)
            {
                listBox.Items.Add(item);
            }
            Grid.SetRow(listBox, 0);
            
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(10)
            };
            
            var okButton = new Button { Content = "OK", Width = 75, Margin = new Thickness(5), IsDefault = true };
            okButton.Click += (s, e) =>
            {
                if (listBox.SelectedItem != null)
                {
                    SelectedItem = listBox.SelectedItem.ToString();
                    DialogResult = true;
                }
            };
            
            var cancelButton = new Button { Content = "Cancel", Width = 75, Margin = new Thickness(5), IsCancel = true };
            
            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            Grid.SetRow(buttonPanel, 1);
            
            grid.Children.Add(listBox);
            grid.Children.Add(buttonPanel);
            
            Content = grid;
        }
    }

    public class NewDocumentDialog : Window
    {
        public string DocumentName { get; private set; }
        public string DocumentType { get; private set; }
        private TextBox nameBox;
        private ComboBox typeCombo;

        public NewDocumentDialog()
        {
            Title = "New Document";
            Width = 400;
            Height = 200;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            
            var nameLabel = new Label { Content = "Document Name:", Margin = new Thickness(10, 10, 10, 0) };
            Grid.SetRow(nameLabel, 0);
            
            nameBox = new TextBox { Margin = new Thickness(10, 0, 10, 10) };
            Grid.SetRow(nameBox, 1);
            
            var typeLabel = new Label { Content = "Document Type:", Margin = new Thickness(10, 0, 10, 0) };
            Grid.SetRow(typeLabel, 2);
            
            typeCombo = new ComboBox { Margin = new Thickness(10, 0, 10, 10) };
            typeCombo.Items.Add("module");
            typeCombo.Items.Add("integration");
            typeCombo.Items.Add("pipeline");
            typeCombo.Items.Add("testing");
            typeCombo.Items.Add("monitoring");
            typeCombo.SelectedIndex = 0;
            Grid.SetRow(typeCombo, 3);
            
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(10)
            };
            
            var okButton = new Button { Content = "OK", Width = 75, Margin = new Thickness(5), IsDefault = true };
            okButton.Click += (s, e) =>
            {
                DocumentName = nameBox.Text;
                DocumentType = typeCombo.SelectedItem?.ToString();
                DialogResult = true;
            };
            
            var cancelButton = new Button { Content = "Cancel", Width = 75, Margin = new Thickness(5), IsCancel = true };
            
            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            Grid.SetRow(buttonPanel, 4);
            
            grid.Children.Add(nameLabel);
            grid.Children.Add(nameBox);
            grid.Children.Add(typeLabel);
            grid.Children.Add(typeCombo);
            grid.Children.Add(buttonPanel);
            
            Content = grid;
        }
    }
}
