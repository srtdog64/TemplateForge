using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TemplateForge.Core
{
    /// <summary>
    /// YAML 명세를 파싱하여 프로젝트 구조를 생성
    /// </summary>
    public class YamlStructureGenerator
    {
        private readonly List<string> createdPaths;
        private readonly Dictionary<string, List<string>> structurePreview;

        public YamlStructureGenerator()
        {
            createdPaths = new List<string>();
            structurePreview = new Dictionary<string, List<string>>();
        }

        /// <summary>
        /// YAML 컨텐츠를 파싱하여 폴더 구조 미리보기 생성
        /// </summary>
        public Dictionary<string, List<string>> ParseYamlStructure(string yamlContent)
        {
            structurePreview.Clear();
            structurePreview["folders"] = new List<string>();
            structurePreview["files"] = new List<string>();

            var lines = yamlContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            string currentSection = "";
            string moduleName = "MyModule";
            
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                
                // 모듈명 추출
                if (trimmed.StartsWith("module:") || trimmed.StartsWith("composition:"))
                {
                    var name = ExtractValue(trimmed);
                    if (!string.IsNullOrEmpty(name) && name != "MODULE_NAME" && name != "APP_NAME")
                    {
                        moduleName = SanitizeName(name);
                    }
                }
                
                // 섹션 감지
                if (!trimmed.StartsWith("-") && !trimmed.StartsWith("#") && trimmed.EndsWith(":"))
                {
                    currentSection = trimmed.TrimEnd(':').ToLower();
                }
                
                // 구조 분석
                if (currentSection == "structure" || currentSection == "modules" || currentSection == "layers")
                {
                    if (trimmed.StartsWith("- name:") || trimmed.StartsWith("- "))
                    {
                        var name = ExtractValue(trimmed);
                        if (!string.IsNullOrEmpty(name))
                        {
                            structurePreview["folders"].Add($"{moduleName}/{name}");
                        }
                    }
                }
                
                // API, 이벤트, 데이터 모델 등을 파일로 변환
                if (currentSection == "api" || currentSection == "events" || currentSection == "models")
                {
                    if (trimmed.StartsWith("- name:") || trimmed.StartsWith("- method:"))
                    {
                        var name = ExtractValue(trimmed);
                        if (!string.IsNullOrEmpty(name))
                        {
                            var fileName = $"{moduleName}/{currentSection}/{name}.cs";
                            structurePreview["files"].Add(fileName);
                        }
                    }
                }
            }
            
            // 기본 구조 추가
            if (structurePreview["folders"].Count == 0)
            {
                structurePreview["folders"].Add($"{moduleName}/Core");
                structurePreview["folders"].Add($"{moduleName}/Services");
                structurePreview["folders"].Add($"{moduleName}/Models");
                structurePreview["folders"].Add($"{moduleName}/Interfaces");
            }
            
            // 기본 파일 추가
            structurePreview["files"].Add($"{moduleName}/README.md");
            structurePreview["files"].Add($"{moduleName}/module-spec.yaml");
            
            return structurePreview;
        }

        /// <summary>
        /// YAML 구조를 실제 파일 시스템에 생성
        /// </summary>
        public GenerationResult GenerateStructure(string yamlContent, string outputPath, string moduleName = null)
        {
            var result = new GenerationResult();
            createdPaths.Clear();
            
            try
            {
                // YAML 구조 파싱
                var structure = ParseYamlStructure(yamlContent);
                
                // 모듈명 추출 또는 사용자 지정 값 사용
                if (string.IsNullOrEmpty(moduleName))
                {
                    moduleName = ExtractModuleName(yamlContent);
                }
                moduleName = SanitizeName(moduleName);
                
                var basePath = Path.Combine(outputPath, moduleName);
                
                // 기본 폴더 생성
                CreateDirectory(basePath);
                result.ModuleName = moduleName;
                result.BasePath = basePath;
                
                // 폴더 구조 생성
                foreach (var folder in structure["folders"])
                {
                    var folderPath = Path.Combine(outputPath, folder);
                    CreateDirectory(folderPath);
                    result.CreatedFolders.Add(folderPath);
                }
                
                // 파일 생성
                foreach (var file in structure["files"])
                {
                    var filePath = Path.Combine(outputPath, file);
                    var dir = Path.GetDirectoryName(filePath);
                    if (!Directory.Exists(dir))
                    {
                        CreateDirectory(dir);
                    }
                    
                    // 파일 내용 생성
                    if (file.EndsWith(".yaml"))
                    {
                        // YAML 파일인 경우 원본 내용 저장
                        File.WriteAllText(filePath, yamlContent, Encoding.UTF8);
                    }
                    else if (file.EndsWith(".md"))
                    {
                        // README 생성
                        var readmeContent = GenerateReadme(moduleName, yamlContent);
                        File.WriteAllText(filePath, readmeContent, Encoding.UTF8);
                    }
                    else if (file.EndsWith(".cs"))
                    {
                        // 기본 C# 템플릿 생성
                        var csContent = GenerateCSharpTemplate(file, moduleName);
                        File.WriteAllText(filePath, csContent, Encoding.UTF8);
                    }
                    
                    result.CreatedFiles.Add(filePath);
                }
                
                result.Success = true;
                result.Message = $"Successfully generated {result.CreatedFolders.Count} folders and {result.CreatedFiles.Count} files";
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Error: {ex.Message}";
            }
            
            return result;
        }
        
        private void CreateDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                createdPaths.Add(path);
            }
        }
        
        private string ExtractValue(string line)
        {
            // "key: value" 형식에서 value 추출
            var colonIndex = line.IndexOf(':');
            if (colonIndex > 0 && colonIndex < line.Length - 1)
            {
                var value = line.Substring(colonIndex + 1).Trim();
                // 따옴표 제거
                value = value.Trim('"', '\'');
                // 리스트 항목인 경우 처리
                if (line.TrimStart().StartsWith("- "))
                {
                    value = line.Substring(line.IndexOf("- ") + 2).Trim();
                    if (value.Contains(":"))
                    {
                        value = value.Substring(value.IndexOf(":") + 1).Trim().Trim('"', '\'');
                    }
                }
                return value;
            }
            return "";
        }
        
        private string ExtractModuleName(string yamlContent)
        {
            var lines = yamlContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("module:") || trimmed.StartsWith("composition:"))
                {
                    var name = ExtractValue(trimmed);
                    if (!string.IsNullOrEmpty(name) && name != "MODULE_NAME" && name != "APP_NAME")
                    {
                        return name;
                    }
                }
            }
            return "MyModule";
        }
        
        private string SanitizeName(string name)
        {
            // 파일/폴더명으로 사용 불가능한 문자 제거
            var invalid = Path.GetInvalidFileNameChars().Concat(Path.GetInvalidPathChars()).Distinct();
            foreach (var c in invalid)
            {
                name = name.Replace(c.ToString(), "");
            }
            return name.Trim();
        }
        
        private string GenerateReadme(string moduleName, string yamlContent)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# {moduleName}");
            sb.AppendLine();
            sb.AppendLine("## Overview");
            sb.AppendLine($"This module was generated from a YAML specification.");
            sb.AppendLine();
            sb.AppendLine("## Structure");
            sb.AppendLine("```yaml");
            sb.AppendLine(yamlContent.Substring(0, Math.Min(500, yamlContent.Length)));
            if (yamlContent.Length > 500) sb.AppendLine("...");
            sb.AppendLine("```");
            sb.AppendLine();
            sb.AppendLine("## Generated");
            sb.AppendLine($"- Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"- Tool: TemplateForge");
            return sb.ToString();
        }
        
        private string GenerateCSharpTemplate(string filePath, string moduleName)
        {
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var namespaceParts = Path.GetDirectoryName(filePath)?.Replace('\\', '.').Replace('/', '.');
            
            var sb = new StringBuilder();
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System.Linq;");
            sb.AppendLine("using System.Threading.Tasks;");
            sb.AppendLine();
            sb.AppendLine($"namespace {namespaceParts}");
            sb.AppendLine("{");
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// Auto-generated from YAML specification");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    public class {fileName}");
            sb.AppendLine("    {");
            sb.AppendLine("        // TODO: Implement based on YAML specification");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            
            return sb.ToString();
        }
    }
    
    public class GenerationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string ModuleName { get; set; }
        public string BasePath { get; set; }
        public List<string> CreatedFolders { get; set; } = new List<string>();
        public List<string> CreatedFiles { get; set; } = new List<string>();
    }
}