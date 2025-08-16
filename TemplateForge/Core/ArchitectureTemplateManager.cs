using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TemplateForge.Core
{
    /// <summary>
    /// 아키텍처 템플릿 관리 및 YAML 연결 처리
    /// </summary>
    public class ArchitectureTemplateManager
    {
        // 템플릿 카테고리별 분류
        private readonly Dictionary<string, List<ArchitectureTemplate>> templateCategories;

        public ArchitectureTemplateManager()
        {
            templateCategories = new Dictionary<string, List<ArchitectureTemplate>>();
            InitializeTemplates();
        }

        /// <summary>
        /// 내장 아키텍처 템플릿 초기화
        /// </summary>
        private void InitializeTemplates()
        {
            // 아키텍처 루트 템플릿
            AddTemplate("아키텍처 템플릿", new ArchitectureTemplate
            {
                Name = "Architecture Root",
                Category = "🏗️ Architecture",
                Description = "전체 시스템 아키텍처 루트 (모듈, 통합, 파이프라인 참조)",
                FileName = "architecture.yaml",
                IsRoot = true,
                Template = GetArchitectureRootTemplate(),
                LinkedTemplates = new List<string>
                {
                    "modules/*.yaml",
                    "integrations/*.yaml",
                    "pipelines/*.yaml",
                    "testing/*.yaml",
                    "ops/*.yaml"
                }
            });

            // 모듈 템플릿들
            AddTemplate("모듈 템플릿", new ArchitectureTemplate
            {
                Name = "Module Spec (Mini)",
                Category = "📦 Module",
                Description = "단일 모듈 명세 - 미니 버전",
                FileName = "modules/module_mini.yaml",
                IsRoot = false,
                Template = GetModuleMiniTemplate()
            });

            AddTemplate("모듈 템플릿", new ArchitectureTemplate
            {
                Name = "Module Spec (Extended)",
                Category = "📦 Module",
                Description = "단일 모듈 명세 - 확장 버전",
                FileName = "modules/module_extended.yaml",
                IsRoot = false,
                Template = GetModuleExtendedTemplate()
            });

            // 앱 구성 템플릿
            AddTemplate("아키텍처 템플릿", new ArchitectureTemplate
            {
                Name = "App Composition (Mini)",
                Category = "🎯 Composition",
                Description = "앱 구성 및 DI 설정 - 미니 버전",
                FileName = "composition/app_mini.yaml",
                IsRoot = false,
                Template = GetAppCompositionMiniTemplate()
            });

            AddTemplate("아키텍처 템플릿", new ArchitectureTemplate
            {
                Name = "App Composition (Extended)",
                Category = "🎯 Composition",
                Description = "앱 구성 및 DI 설정 - 확장 버전",
                FileName = "composition/app_extended.yaml",
                IsRoot = false,
                Template = GetAppCompositionExtendedTemplate()
            });

            // 통합 템플릿
            AddTemplate("통합 템플릿", new ArchitectureTemplate
            {
                Name = "Integration",
                Category = "🔗 Integration",
                Description = "외부 시스템 통합 명세",
                FileName = "integrations/integration.yaml",
                IsRoot = false,
                Template = GetIntegrationTemplate()
            });

            AddTemplate("통합 템플릿", new ArchitectureTemplate
            {
                Name = "Data Pipeline",
                Category = "📊 Pipeline",
                Description = "데이터 파이프라인 명세",
                FileName = "pipelines/pipeline.yaml",
                IsRoot = false,
                Template = GetDataPipelineTemplate()
            });

            AddTemplate("통합 템플릿", new ArchitectureTemplate
            {
                Name = "Testing Strategy",
                Category = "🧪 Testing",
                Description = "테스트 전략 명세",
                FileName = "testing/test_strategy.yaml",
                IsRoot = false,
                Template = GetTestingTemplate()
            });

            AddTemplate("통합 템플릿", new ArchitectureTemplate
            {
                Name = "Monitoring",
                Category = "📈 Ops",
                Description = "모니터링 설정",
                FileName = "ops/monitoring.yaml",
                IsRoot = false,
                Template = GetMonitoringTemplate()
            });

            AddTemplate("통합 템플릿", new ArchitectureTemplate
            {
                Name = "Migration",
                Category = "🔄 Ops",
                Description = "마이그레이션 전략",
                FileName = "ops/migration.yaml",
                IsRoot = false,
                Template = GetMigrationTemplate()
            });
        }

        private void AddTemplate(string category, ArchitectureTemplate template)
        {
            if (!templateCategories.ContainsKey(category))
            {
                templateCategories[category] = new List<ArchitectureTemplate>();
            }
            templateCategories[category].Add(template);
        }

        public List<ArchitectureTemplate> GetTemplatesByCategory(string category)
        {
            return templateCategories.ContainsKey(category) 
                ? templateCategories[category] 
                : new List<ArchitectureTemplate>();
        }

        public List<string> GetCategories()
        {
            return templateCategories.Keys.ToList();
        }

        /// <summary>
        /// YAML 파일 간 참조 파싱
        /// </summary>
        public List<string> ParseLinkedFiles(string yamlContent)
        {
            var linkedFiles = new List<string>();
            var lines = yamlContent.Split('\n');

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                
                // ref: "./path/to/file.yaml" 형식 파싱
                if (trimmed.Contains("ref:"))
                {
                    var refStart = trimmed.IndexOf("\"");
                    var refEnd = trimmed.LastIndexOf("\"");
                    if (refStart >= 0 && refEnd > refStart)
                    {
                        var refPath = trimmed.Substring(refStart + 1, refEnd - refStart - 1);
                        if (refPath.StartsWith("./"))
                        {
                            refPath = refPath.Substring(2);
                        }
                        linkedFiles.Add(refPath);
                    }
                }
            }

            return linkedFiles;
        }

        /// <summary>
        /// 연결된 모든 YAML 파일 생성
        /// </summary>
        public Dictionary<string, string> GenerateLinkedStructure(string rootYaml, string projectName)
        {
            var result = new Dictionary<string, string>();
            
            // 루트 파일 추가
            result["architecture.yaml"] = rootYaml.Replace("APP_OR_GAME_NAME", projectName)
                                                  .Replace("OWNER_NAME", "Team")
                                                  .Replace("YYYY-MM-DD", DateTime.Now.ToString("yyyy-MM-dd"));

            // 연결된 파일들 파싱
            var linkedFiles = ParseLinkedFiles(rootYaml);
            
            foreach (var file in linkedFiles)
            {
                // 파일 경로에서 타입 추출
                if (file.StartsWith("modules/"))
                {
                    result[file] = GetModuleMiniTemplate()
                        .Replace("MODULE_NAME", Path.GetFileNameWithoutExtension(file))
                        .Replace("MODULE_ID", Path.GetFileNameWithoutExtension(file).ToUpper());
                }
                else if (file.StartsWith("integrations/"))
                {
                    result[file] = GetIntegrationTemplate()
                        .Replace("INTEGRATION_NAME", Path.GetFileNameWithoutExtension(file));
                }
                else if (file.StartsWith("pipelines/"))
                {
                    result[file] = GetDataPipelineTemplate()
                        .Replace("PIPELINE_NAME", Path.GetFileNameWithoutExtension(file));
                }
                else if (file.StartsWith("testing/"))
                {
                    result[file] = GetTestingTemplate()
                        .Replace("TEST_SUITE_NAME", Path.GetFileNameWithoutExtension(file));
                }
                else if (file.StartsWith("ops/"))
                {
                    if (file.Contains("monitoring"))
                    {
                        result[file] = GetMonitoringTemplate()
                            .Replace("MONITORING_NAME", Path.GetFileNameWithoutExtension(file));
                    }
                    else if (file.Contains("migration"))
                    {
                        result[file] = GetMigrationTemplate()
                            .Replace("MIGRATION_NAME", Path.GetFileNameWithoutExtension(file));
                    }
                }
            }

            return result;
        }

        // 템플릿 내용들
        private string GetArchitectureRootTemplate()
        {
            return @"# === architecture.yaml (GENERIC ROOT) ===
meta:
  version: 1
  owner: ""OWNER_NAME""
  lastUpdated: ""YYYY-MM-DD""

product:
  name: ""APP_OR_GAME_NAME""
  targets: [ ""Windows"", ""Linux"" ]

guardrails:
  llmRules:
    - ""Use method-based getters/setters""
    - ""Curly braces for all if/for/while""
    - ""No main-thread-only APIs off main thread""
    - ""Hot path: zero alloc""

contracts:
  events:
    - name: ""SystemStarted""
      version: 1
      schema: { timestamp: long }
      delivery: ""at-least-once""
      ordering: ""global""

composition:
  modules:
    - id: ""CoreModule""
      ref: ""./modules/core_module.yaml""
    - id: ""DataModule""
      ref: ""./modules/data_module.yaml""
  integrations:
    - id: ""ApiIntegration""
      ref: ""./integrations/api_integration.yaml""
  pipelines:
    - id: ""DataPipeline""
      ref: ""./pipelines/data_pipeline.yaml""
  testing:
    - id: ""TestSuite""
      ref: ""./testing/test_suite.yaml""
  ops:
    monitoring:
      - id: ""SystemMonitoring""
        ref: ""./ops/monitoring.yaml""
    migrations:
      - id: ""V1Migration""
        ref: ""./ops/migration_v1.yaml""

constraints:
  perf:
    mainThreadBudgetMs: 0.2
    frameAllocsBytes: 0
  memory:
    peakBytes: ""100MB""
  compliance:
    loggingPII: false
    deterministic: true";
        }

        private string GetModuleMiniTemplate()
        {
            return @"# === modules/MODULE_ID.yaml (MINI) ===
module: ""MODULE_NAME""
goal: ""Module responsibility and purpose""

context:
  summary: ""Core functionality summary""
  assumptions:
    - ""Assumption about environment""
    - ""Assumption about dependencies""

invariants:
  - ""State must always be valid""
  - ""Events must be idempotent""

api:
  interfaces:
    - name: ""IModuleService""
      methods:
        - name: ""ProcessData""
          in:  { data: object, options: object }
          out: { success: bool, result: object }
          effects: [ ""Emitted(DataProcessed)"" ]

events:
  - name: ""DataProcessed""
    data: { id: string, timestamp: long }

constraints:
  perf: { mainThreadBudgetMs: 0.2 }
  memory: { allocBytesPerCall: 0 }
  threading: ""MainThread""

tests:
  - ""All invariants are maintained""
  - ""Zero allocation on hot path""";
        }

        private string GetModuleExtendedTemplate()
        {
            return @"# === modules/MODULE_ID.yaml (EXTENDED) ===
meta:
  version: 1
  owner: ""OWNER_NAME""
  lastUpdated: ""YYYY-MM-DD""

module: ""MODULE_NAME""
goal: ""Comprehensive module specification""

context:
  summary: ""Core domain and responsibility""
  scope:
    in:  [ ""Data processing"", ""Event handling"" ]
    out: [ ""UI rendering"", ""Network communication"" ]
  assumptions:
    - ""Running on main thread""
    - ""Has access to shared memory pool""
  dependencies:
    - ""CoreFramework""
    - ""EventBus""

# ... (더 많은 상세 내용)";
        }

        private string GetAppCompositionMiniTemplate()
        {
            return @"# === composition/APP_NAME.mini.yaml ===
composition: ""APP_NAME""
goal: ""Application wiring and DI configuration""

modules:
  registry:
    - name: ""CoreModule""
      interface: ""ICoreService""
      impl: ""CoreServiceImpl""
      lifecycle: ""Singleton""

bindings:
  implementation:
    ICoreService: ""CoreServiceImpl""

entryPoints:
  - name: ""Main""
    onStart: [ ""CoreModule.Init()"" ]
    onStop:  [ ""CoreModule.Dispose()"" ]

eventFlows:
  - from: ""SystemStarted""
    to: [ ""CoreModule.HandleStart"" ]

threading:
  model: ""MainThread""

constraints:
  perf: { frameBudgetMs: 0.2 }
  memory: { steadyStateBytes: ""50MB"" }";
        }

        private string GetAppCompositionExtendedTemplate()
        {
            return @"# === composition/APP_NAME.extended.yaml ===
meta:
  version: 1
  owner: ""OWNER_NAME""
  lastUpdated: ""YYYY-MM-DD""

composition: ""APP_NAME""
goal: ""Complete application composition with all configurations""

# ... (확장된 상세 내용)";
        }

        private string GetIntegrationTemplate()
        {
            return @"# === integrations/INTEGRATION_ID.yaml ===
integration: ""INTEGRATION_NAME""
goal: ""Safe integration with external systems""

externalSystems:
  - name: ""APIService""
    type: ""REST""
    endpoint: ""https://api.example.com""
    rateLimits: { requestsPerSecond: 100 }

adapters:
  outbound:
    - name: ""APIAdapter""
      from: ""IAPIService.Call""
      to: ""POST /api/endpoint""
      retry: { maxAttempts: 3 }

resilience:
  patterns:
    - name: ""CircuitBreaker""
      failureThreshold: 5

monitoring:
  metrics: [ ""integration.latency"", ""error.rate"" ]";
        }

        private string GetDataPipelineTemplate()
        {
            return @"# === pipelines/PIPELINE_ID.yaml ===
pipeline: ""PIPELINE_NAME""
goal: ""Data transformation and processing flow""

sources:
  - name: ""DataSource""
    type: ""database""
    schema: { format: ""json"" }

stages:
  - name: ""Extract""
    operations: [ ""ReadFromSource"" ]
  - name: ""Transform""
    operations: [ ""Normalize"", ""Validate"" ]
  - name: ""Load""
    operations: [ ""WriteToDestination"" ]

destinations:
  - name: ""DataStore""
    type: ""database""

quality:
  checks:
    - { name: ""Completeness"", threshold: 0.99 }

performance:
  throughput: ""1000/sec""
  latency: ""< 100ms""";
        }

        private string GetTestingTemplate()
        {
            return @"# === testing/TEST_SUITE_ID.yaml ===
testStrategy: ""TEST_SUITE_NAME""
goal: ""Comprehensive test coverage""

levels:
  unit:
    coverage: ""80%""
    frameworks: [ ""NUnit"" ]
  integration:
    scenarios: [ ""Happy path"", ""Error handling"" ]
  e2e:
    userJourneys: [ ""Complete workflow"" ]

automation:
  ci:
    pipeline: ""GitHub Actions""
    stages: [ ""build"", ""test"", ""deploy"" ]

quality:
  gates:
    - { metric: ""coverage"", threshold: ""80%"" }";
        }

        private string GetMonitoringTemplate()
        {
            return @"# === ops/MONITORING_ID.yaml ===
monitoring: ""MONITORING_NAME""
goal: ""System observability""

metrics:
  business:  [ ""transactions.count"", ""revenue.total"" ]
  technical: [ ""api.latency"", ""error.rate"" ]

alerts:
  - name: ""HighErrorRate""
    condition: ""error_rate > 5%""
    severity: ""critical""

dashboards:
  - name: ""System Overview""
    widgets: [ ""RequestRate"", ""ErrorRate"", ""Latency"" ]

observability:
  golden_signals:
    latency:    ""p99 < 100ms""
    traffic:    ""availability > 99.9%""
    errors:     ""error_rate < 1%""
    saturation: ""cpu < 80%""";
        }

        private string GetMigrationTemplate()
        {
            return @"# === ops/MIGRATION_ID.yaml ===
migration: ""MIGRATION_NAME""
goal: ""Safe upgrade from v1 to v2""

from:
  version: ""1.0.0""
  database: { type: ""SQL"", version: ""1.0"" }

to:
  version: ""2.0.0""
  database: { type: ""SQL"", version: ""2.0"" }
  changes:
    - { type: ""add_column"", table: ""users"", column: ""created_at"" }

strategy:
  approach: ""blue_green""
  phases: [ ""PREPARE"", ""MIGRATE"", ""VALIDATE"", ""CLEANUP"" ]

rollback:
  strategy: ""checkpoint_based""

validation:
  checks: [ ""DATA_INTEGRITY"", ""PERFORMANCE"" ]";
        }
    }

    /// <summary>
    /// 아키텍처 템플릿 정보
    /// </summary>
    public class ArchitectureTemplate
    {
        public string Name { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public string FileName { get; set; }
        public string Template { get; set; }
        public bool IsRoot { get; set; }
        public List<string> LinkedTemplates { get; set; } = new List<string>();
    }
}
