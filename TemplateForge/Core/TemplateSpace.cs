using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TemplateForge.Core
{
    /// <summary>
    /// 템플릿스페이스 - 연관된 YAML 파일들의 집합을 관리
    /// </summary>
    public class TemplateSpace
    {
        public string Name { get; set; }
        public string RootPath { get; set; }
        public Dictionary<string, YamlDocument> Documents { get; set; }
        public YamlDocument ActiveDocument { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ModifiedAt { get; set; }

        public TemplateSpace(string name)
        {
            Name = name;
            Documents = new Dictionary<string, YamlDocument>();
            CreatedAt = DateTime.Now;
            ModifiedAt = DateTime.Now;
        }

        /// <summary>
        /// 새 문서 추가
        /// </summary>
        public YamlDocument AddDocument(string name, string content = null, string type = "module")
        {
            var doc = new YamlDocument
            {
                Id = Guid.NewGuid().ToString(),
                Name = name,
                Type = type,
                Content = content ?? GetDefaultContent(type),
                FilePath = $"{name}.yaml",
                IsModified = true
            };

            Documents[doc.Id] = doc;
            ModifiedAt = DateTime.Now;
            return doc;
        }

        /// <summary>
        /// YAML 참조 분석 (ref: 패턴 찾기)
        /// </summary>
        public List<YamlReference> AnalyzeReferences()
        {
            var references = new List<YamlReference>();
            var refPattern = @"ref:\s*[""']?([./\w-]+\.yaml)[""']?";

            foreach (var doc in Documents.Values)
            {
                var matches = Regex.Matches(doc.Content, refPattern);
                foreach (Match match in matches)
                {
                    references.Add(new YamlReference
                    {
                        FromDocument = doc.Name,
                        ToPath = match.Groups[1].Value,
                        LineNumber = GetLineNumber(doc.Content, match.Index)
                    });
                }
            }

            return references;
        }

        /// <summary>
        /// 연결된 문서 자동 생성
        /// </summary>
        public void GenerateReferencedDocuments()
        {
            var references = AnalyzeReferences();
            
            foreach (var reference in references)
            {
                // 이미 존재하는지 확인
                var exists = Documents.Values.Any(d => 
                    d.FilePath == reference.ToPath || 
                    d.Name == Path.GetFileNameWithoutExtension(reference.ToPath));
                
                if (!exists)
                {
                    var docName = Path.GetFileNameWithoutExtension(reference.ToPath);
                    var docType = DetectDocumentType(reference.ToPath);
                    AddDocument(docName, null, docType);
                }
            }
        }

        /// <summary>
        /// 전체 구조를 파일 시스템으로 출력
        /// </summary>
        public void ExportToFileSystem(string basePath)
        {
            foreach (var doc in Documents.Values)
            {
                var fullPath = Path.Combine(basePath, doc.FilePath);
                var directory = Path.GetDirectoryName(fullPath);
                
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                File.WriteAllText(fullPath, doc.Content, Encoding.UTF8);
            }
            
            // 메타데이터 저장
            var metaPath = Path.Combine(basePath, ".templatespace");
            var metadata = new
            {
                Name = Name,
                CreatedAt = CreatedAt,
                ModifiedAt = ModifiedAt,
                Documents = Documents.Values.Select(d => new
                {
                    d.Id,
                    d.Name,
                    d.Type,
                    d.FilePath
                })
            };
            
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(metadata, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(metaPath, json, Encoding.UTF8);
        }

        private string GetDefaultContent(string type)
        {
            switch (type.ToLower())
            {
                case "root":
                case "architecture":
                    return @"# Architecture Root
meta:
  version: 1
  owner: ""OWNER_NAME""
  lastUpdated: """ + DateTime.Now.ToString("yyyy-MM-dd") + @"""

product:
  name: ""APP_NAME""
  targets: [ ""platform"" ]

composition:
  modules:
    - id: ""module1""
      ref: ""./modules/module1.yaml""
  integrations:
    - id: ""integration1""
      ref: ""./integrations/integration1.yaml""

constraints:
  perf:
    mainThreadBudgetMs: 0.2
";

                case "module":
                    return @"# Module Specification
module: ""MODULE_NAME""
goal: ""Module purpose and responsibility""

context:
  summary: ""Brief description""
  assumptions:
    - ""Assumption 1""

api:
  interfaces:
    - name: ""IModuleInterface""
      methods:
        - name: ""MethodName""
          in: { param1: string }
          out: { result: bool }

events:
  - name: ""ModuleEvent""
    data: { field1: string }

constraints:
  perf: { mainThreadBudgetMs: 0.2 }
  memory: { allocBytesPerCall: 0 }
";

                case "integration":
                    return @"# Integration Specification
integration: ""INTEGRATION_NAME""
goal: ""Safe integration with external systems""

externalSystems:
  - name: ""SYSTEM_NAME""
    type: ""REST""
    endpoint: ""https://api.example.com""

adapters:
  inbound:
    - name: ""InboundAdapter""
      from: ""HTTP""
      to: ""Internal.Method""
  outbound:
    - name: ""OutboundAdapter""
      from: ""Internal.Method""
      to: ""RemoteAPI""

resilience:
  patterns:
    - name: ""CircuitBreaker""
      failureThreshold: 5
";

                case "pipeline":
                    return @"# Data Pipeline
pipeline: ""PIPELINE_NAME""
goal: ""Data transformation and processing""

sources:
  - name: ""Source""
    type: ""database""

stages:
  - name: ""Extract""
    operations: [ ""ReadFromSource"" ]
  - name: ""Transform""
    operations: [ ""Normalize"", ""Enrich"" ]
  - name: ""Load""
    operations: [ ""WriteToDestination"" ]

destinations:
  - name: ""Destination""
    type: ""datawarehouse""
";

                default:
                    return @"# YAML Document
# Type: " + type + @"
# Created: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + @"

name: """ + type + @"""
content:
  - item1
  - item2
";
            }
        }

        private string DetectDocumentType(string path)
        {
            var lower = path.ToLower();
            if (lower.Contains("module")) return "module";
            if (lower.Contains("integration")) return "integration";
            if (lower.Contains("pipeline")) return "pipeline";
            if (lower.Contains("test")) return "testing";
            if (lower.Contains("monitor")) return "monitoring";
            if (lower.Contains("migration")) return "migration";
            if (lower.Contains("architecture") || lower.Contains("root")) return "architecture";
            return "generic";
        }

        private int GetLineNumber(string text, int charIndex)
        {
            var lineNumber = 1;
            for (int i = 0; i < charIndex && i < text.Length; i++)
            {
                if (text[i] == '\n') lineNumber++;
            }
            return lineNumber;
        }
    }

    /// <summary>
    /// YAML 문서
    /// </summary>
    public class YamlDocument
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Content { get; set; }
        public string FilePath { get; set; }
        public bool IsModified { get; set; }
        public DateTime LastModified { get; set; }

        public YamlDocument()
        {
            LastModified = DateTime.Now;
        }
    }

    /// <summary>
    /// YAML 참조
    /// </summary>
    public class YamlReference
    {
        public string FromDocument { get; set; }
        public string ToPath { get; set; }
        public int LineNumber { get; set; }
    }

    /// <summary>
    /// 템플릿스페이스 관리자
    /// </summary>
    public class TemplateSpaceManager
    {
        private Dictionary<string, TemplateSpace> spaces;
        private TemplateSpace activeSpace;

        public TemplateSpaceManager()
        {
            spaces = new Dictionary<string, TemplateSpace>();
        }

        public TemplateSpace CreateSpace(string name)
        {
            var space = new TemplateSpace(name);
            spaces[name] = space;
            activeSpace = space;
            return space;
        }

        public TemplateSpace GetActiveSpace()
        {
            return activeSpace;
        }

        public void SetActiveSpace(string name)
        {
            if (spaces.ContainsKey(name))
            {
                activeSpace = spaces[name];
            }
        }

        public List<TemplateSpace> GetAllSpaces()
        {
            return spaces.Values.ToList();
        }

        /// <summary>
        /// 템플릿에서 새 스페이스 생성
        /// </summary>
        public TemplateSpace CreateSpaceFromTemplate(string templateType)
        {
            var space = CreateSpace($"Project_{DateTime.Now:yyyyMMdd_HHmmss}");
            
            switch (templateType)
            {
                case "microservice":
                    // 마이크로서비스 구조 생성
                    var root = space.AddDocument("architecture", null, "architecture");
                    space.AddDocument("auth-module", null, "module");
                    space.AddDocument("data-module", null, "module");
                    space.AddDocument("api-integration", null, "integration");
                    space.AddDocument("data-pipeline", null, "pipeline");
                    space.GenerateReferencedDocuments();
                    break;
                    
                case "game":
                    // 게임 구조 생성
                    space.AddDocument("game-architecture", null, "architecture");
                    space.AddDocument("player-module", null, "module");
                    space.AddDocument("inventory-module", null, "module");
                    space.AddDocument("combat-module", null, "module");
                    space.GenerateReferencedDocuments();
                    break;
                    
                default:
                    // 기본 구조
                    space.AddDocument("architecture", null, "architecture");
                    space.AddDocument("module1", null, "module");
                    break;
            }
            
            return space;
        }
    }
}
