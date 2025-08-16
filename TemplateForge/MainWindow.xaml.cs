using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
        private readonly TemplateLoader templateLoader;
        private readonly YamlStructureGenerator structureGenerator;
        private bool isUpdatingFromTemplate = false;

        public MainWindow()
        {
            InitializeComponent();
            this.templateLoader = new TemplateLoader();
            this.structureGenerator = new YamlStructureGenerator();
            
            this.Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 템플릿 목록 로드
            loadTemplates();
            
            // 상태 표시 (Standalone Mode)
            this.StatusIndicator.Fill = new SolidColorBrush(Colors.Orange);
            this.StatusText.Text = "Standalone Mode";
        }

        private void loadTemplates()
        {
            try
            {
                var templates = this.templateLoader.getAvailableTemplates();
                this.TemplateListBox.ItemsSource = templates;
                // DisplayMemberPath 제거 - DataTemplate 사용

                if (templates.Any())
                {
                    this.TemplateListBox.SelectedIndex = 0;
                }
                
                // 템플릿 카운트 업데이트
                if (this.TemplateCountText != null)
                {
                    this.TemplateCountText.Text = $"Templates: {templates.Count} loaded";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"템플릿 로드 오류: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void TemplateListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.TemplateListBox.SelectedItem is TemplateInfo selectedTemplate)
            {
                this.isUpdatingFromTemplate = true;
                try
                {
                    var templateContent = this.templateLoader.loadTemplate(selectedTemplate);
                    this.YamlEditor.Text = templateContent;
                    this.updateModuleName(templateContent);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"템플릿 로드 오류: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                finally
                {
                    this.isUpdatingFromTemplate = false;
                }
            }
        }

        private void updateModuleName(string yamlContent)
        {
            // 간단한 YAML 파싱으로 모듈명 추출
            var lines = yamlContent.Split('\n');
            foreach (var line in lines)
            {
                if (line.Trim().StartsWith("module:"))
                {
                    var moduleName = line.Split(':')[1].Trim().Trim('"');
                    if (!string.IsNullOrEmpty(moduleName) && moduleName != "모듈명" && moduleName != "MODULE_NAME")
                    {
                        this.ModuleNameTextBox.Text = moduleName;
                        break;
                    }
                }
            }
        }

        private async void Generate_Click(object sender, RoutedEventArgs e)
        {
            // 로컬 생성 모드만 사용
            GenerateLocal();
        }

        private void GenerateLocal()
        {
            try
            {
                this.GenerateButton.IsEnabled = false;
                this.GenerateButton.Content = "생성 중...";

                var yamlContent = this.YamlEditor.Text;
                var outputPath = this.OutputPathTextBox.Text;
                var moduleName = this.ModuleNameTextBox.Text;
                
                if (string.IsNullOrEmpty(outputPath))
                {
                    MessageBox.Show("출력 경로를 선택해주세요.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                // YAML에서 모듈명 교체
                yamlContent = replaceModuleName(yamlContent, moduleName);
                
                // 로컬 생성기 사용
                var result = structureGenerator.GenerateStructure(yamlContent, outputPath, moduleName);
                
                if (result.Success)
                {
                    MessageBox.Show(
                        $"프로젝트가 성공적으로 생성되었습니다!\n\n" +
                        $"모듈: {result.ModuleName}\n" +
                        $"생성된 폴더: {result.CreatedFolders.Count}개\n" +
                        $"생성된 파일: {result.CreatedFiles.Count}개\n" +
                        $"위치: {result.BasePath}",
                        "Success",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    // 생성된 폴더 열기
                    if (Directory.Exists(result.BasePath))
                    {
                        System.Diagnostics.Process.Start("explorer.exe", result.BasePath);
                    }
                    
                    // 파일 카운트 업데이트
                    if (this.FileCountText != null)
                    {
                        this.FileCountText.Text = $"Files: {result.CreatedFiles.Count} generated";
                    }
                }
                else
                {
                    MessageBox.Show($"프로젝트 생성 실패: {result.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"프로젝트 생성 오류: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                this.GenerateButton.IsEnabled = true;
                this.GenerateButton.Content = "🚀 Generate";
            }
        }

        private async void Validate_Click(object sender, RoutedEventArgs e)
        {
            ValidateLocal();
        }

        private void ValidateLocal()
        {
            try
            {
                var yamlContent = replaceModuleName(this.YamlEditor.Text, this.ModuleNameTextBox.Text);
                
                // 기본 YAML 검증
                var lines = yamlContent.Split('\n');
                var errors = new List<string>();
                bool hasModule = false;
                bool hasStructure = false;
                
                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    if (trimmed.StartsWith("module:") || trimmed.StartsWith("composition:"))
                    {
                        hasModule = true;
                    }
                    if (trimmed.StartsWith("structure:") || trimmed.StartsWith("modules:") || 
                        trimmed.StartsWith("api:") || trimmed.StartsWith("events:"))
                    {
                        hasStructure = true;
                    }
                }
                
                if (!hasModule)
                {
                    errors.Add("모듈 또는 컴포지션 이름이 없습니다.");
                }
                if (!hasStructure)
                {
                    errors.Add("구조 정의가 없습니다.");
                }
                
                if (errors.Count == 0)
                {
                    this.ValidationResults.Text = "✅ YAML 검증 성공!\n\n유효한 명세입니다.";
                    this.ValidationResults.Foreground = new SolidColorBrush(Colors.Green);
                }
                else
                {
                    this.ValidationResults.Text = "❌ YAML 검증 실패:\n\n" + string.Join("\n", errors);
                    this.ValidationResults.Foreground = new SolidColorBrush(Colors.Red);
                }
            }
            catch (Exception ex)
            {
                this.ValidationResults.Text = $"❌ 검증 오류: {ex.Message}";
                this.ValidationResults.Foreground = new SolidColorBrush(Colors.Red);
            }
        }

        private async void Preview_Click(object sender, RoutedEventArgs e)
        {
            await updatePreview();
        }

        private async void YamlEditor_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (this.isUpdatingFromTemplate)
            {
                return;
            }

            // 실시간 프리뷰 업데이트
            await updatePreview();
        }

        private async Task updatePreview()
        {
            try
            {
                var yamlContent = this.replaceModuleName(this.YamlEditor.Text, this.ModuleNameTextBox.Text);
                
                // 로컬 미리보기
                var structure = structureGenerator.ParseYamlStructure(yamlContent);
                var moduleName = this.ModuleNameTextBox.Text;
                if (string.IsNullOrEmpty(moduleName)) moduleName = "MyModule";
                
                var preview = $"📁 {moduleName}/\n";
                foreach (var folder in structure["folders"]) 
                { 
                    preview += $"├── 📁 {folder}/\n"; 
                }
                foreach (var file in structure["files"]) 
                { 
                    preview += $"├── 📄 {file}\n"; 
                }
                
                this.GeneratedFiles.Text = preview;
                this.GeneratedFiles.Foreground = new SolidColorBrush(Colors.Black);
            }
            catch (Exception ex)
            {
                this.GeneratedFiles.Text = $"미리보기 오류: {ex.Message}";
                this.GeneratedFiles.Foreground = new SolidColorBrush(Colors.Red);
            }
        }

        private string replaceModuleName(string yamlContent, string newModuleName)
        {
            // 간단한 모듈명 교체
            return yamlContent
                .Replace("module: 모듈명", $"module: {newModuleName}")
                .Replace("module: MODULE_NAME", $"module: {newModuleName}");
        }

        private void BrowseOutputPath_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.Description = "출력 폴더를 선택하세요.";
                dialog.ShowNewFolderButton = true;
                var result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
                {
                    this.OutputPathTextBox.Text = dialog.SelectedPath;
                }
            }
        }

        private void NewProject_Click(object sender, RoutedEventArgs e)
        {
            // 템플릿이 선택되었는지 확인
            if (this.TemplateListBox.SelectedItem == null)
            {
                MessageBox.Show("템플릿을 먼저 선택해주세요.", "Info", 
                               MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            // 선택된 템플릿 로드
            var selectedTemplate = this.TemplateListBox.SelectedItem as TemplateInfo;
            if (selectedTemplate != null)
            {
                try
                {
                    var templateContent = this.templateLoader.loadTemplate(selectedTemplate);
                    this.YamlEditor.Text = templateContent;
                    this.updateModuleName(templateContent);
                    
                    // 모듈명 초기화
                    this.ModuleNameTextBox.Text = "MyNewProject";
                    
                    MessageBox.Show($"템플릿 '{selectedTemplate.Name}'을(를) 불러왔습니다.\n모듈명과 내용을 수정 후 Generate 버튼을 클릭하세요.", 
                                   "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"템플릿 로드 오류: {ex.Message}", "Error", 
                                   MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        
        private void LoadYaml_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Filter = "YAML files (*.yaml;*.yml)|*.yaml;*.yml|All files (*.*)|*.*";
            dialog.Title = "YAML 파일 불러오기";
            
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var yamlContent = File.ReadAllText(dialog.FileName, System.Text.Encoding.UTF8);
                    this.YamlEditor.Text = yamlContent;
                    this.updateModuleName(yamlContent);
                    
                    // 파일명을 모듈명으로 사용
                    var fileName = Path.GetFileNameWithoutExtension(dialog.FileName);
                    if (!string.IsNullOrEmpty(fileName))
                    {
                        this.ModuleNameTextBox.Text = fileName;
                    }
                    
                    MessageBox.Show($"파일 '{Path.GetFileName(dialog.FileName)}'을(를) 불러왔습니다.", 
                                   "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"YAML 파일 로드 오류: {ex.Message}", "Error", 
                                   MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        
        private void ImportTemplate_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Filter = "YAML files (*.yaml;*.yml)|*.yaml;*.yml|All files (*.*)|*.*";
            dialog.Title = "Import YAML Template";
            
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    this.templateLoader.importTemplate(dialog.FileName);
                    loadTemplates();
                    MessageBox.Show($"템플릿이 성공적으로 임포트되었습니다: {Path.GetFileName(dialog.FileName)}", 
                                   "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"템플릿 임포트 오류: {ex.Message}", "Error", 
                                   MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        
        // 이제 RefreshTemplate_Click은 필요없음 (템플릿은 내장된 것만 사용)
        private void RefreshTemplate_Click(object sender, RoutedEventArgs e)
        {
            // 템플릿 목록 새로고침 (필요시)
            loadTemplates();
        }
        
        private void Diagram_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Mermaid 다이어그램 생성
                var yamlContent = this.YamlEditor.Text;
                var diagram = GenerateMermaidDiagram(yamlContent);
                
                var html = $@"<!DOCTYPE html>
<html>
<head>
    <script src='https://cdn.jsdelivr.net/npm/mermaid/dist/mermaid.min.js'></script>
    <script>mermaid.initialize({{startOnLoad:true}});</script>
</head>
<body>
    <div class='mermaid'>
    {diagram}
    </div>
</body>
</html>";
                
                this.DiagramWebBrowser.NavigateToString(html);
                this.ResultTabControl.SelectedIndex = 0; // Diagram 탭으로 전환
            }
            catch (Exception ex)
            {
                MessageBox.Show($"다이어그램 생성 오류: {ex.Message}", "Error", 
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void DiagramWebBrowser_LoadCompleted(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            // 웹브라우저 로드 완료 이벤트
        }
        
        private string GenerateMermaidDiagram(string yamlContent)
        {
            var lines = yamlContent.Split('\n');
            var moduleName = "Module";
            var components = new List<string>();
            
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("module:") || trimmed.StartsWith("composition:"))
                {
                    var name = ExtractValue(trimmed);
                    if (!string.IsNullOrEmpty(name) && name != "MODULE_NAME")
                        moduleName = name;
                }
                else if (trimmed.StartsWith("- name:"))
                {
                    var name = ExtractValue(trimmed);
                    if (!string.IsNullOrEmpty(name))
                        components.Add(name);
                }
            }
            
            var diagram = "graph TD\n";
            diagram += $"    {moduleName}[{moduleName}]\n";
            foreach (var comp in components)
            {
                diagram += $"    {moduleName} --> {comp}[{comp}]\n";
            }
            
            return diagram;
        }
        
        private string ExtractValue(string line)
        {
            var colonIndex = line.IndexOf(':');
            if (colonIndex > 0 && colonIndex < line.Length - 1)
            {
                return line.Substring(colonIndex + 1).Trim().Trim('"', '\'');
            }
            return "";
        }
    }
}
