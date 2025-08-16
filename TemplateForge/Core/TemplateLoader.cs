using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;

namespace TemplateForge.Core
{
    public sealed class TemplateLoader
    {
        private readonly List<TemplateInfo> builtInTemplates;
        private readonly List<TemplateInfo> importedTemplates;

        public TemplateLoader()
        {
            this.builtInTemplates = new List<TemplateInfo>();
            this.importedTemplates = new List<TemplateInfo>();
            this.loadBuiltInTemplates();
        }

        public IReadOnlyList<TemplateInfo> getAvailableTemplates()
        {
            return this.getAllTemplates();
        }

        public IReadOnlyList<TemplateInfo> getAllTemplates()
        {
            var allTemplates = new List<TemplateInfo>();
            allTemplates.AddRange(this.builtInTemplates);
            allTemplates.AddRange(this.importedTemplates);

            return new ReadOnlyCollection<TemplateInfo>(
                allTemplates
                    .OrderBy(t => t.getCategory(), StringComparer.Ordinal)
                    .ThenBy(t => t.getName(), StringComparer.Ordinal)
                    .ToList()
            );
        }

        public string loadTemplate(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("fileName is null or empty.");
            }
            var info = this.builtInTemplates.FirstOrDefault(t => string.Equals(t.FileName, fileName, StringComparison.OrdinalIgnoreCase))
                       ?? this.importedTemplates.FirstOrDefault(t => string.Equals(t.FileName, fileName, StringComparison.OrdinalIgnoreCase));

            if (info == null)
            {
                throw new FileNotFoundException($"Template not found: {fileName}");
            }
            return this.loadTemplate(info);
        }

        public string loadTemplate(TemplateInfo templateInfo)
        {
            if (templateInfo == null)
            {
                throw new ArgumentNullException(nameof(templateInfo));
            }

            // FilePath가 있으면 파일에서 직접 읽기
            if (!string.IsNullOrEmpty(templateInfo.FilePath) && File.Exists(templateInfo.FilePath))
            {
                return File.ReadAllText(templateInfo.FilePath, System.Text.Encoding.UTF8);
            }
            else if (templateInfo.IsBuiltIn)
            {
                return this.loadBuiltInTemplate(templateInfo.FileName);
            }
            else
            {
                throw new FileNotFoundException($"Template file not found: {templateInfo.FileName}");
            }
        }

        public void importTemplate(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"템플릿 파일을 찾을 수 없습니다: {filePath}");
            }

            var fileNameNoExt = Path.GetFileNameWithoutExtension(filePath);
            var templateInfo = this.createImportedTemplateInfo(fileNameNoExt, filePath);

            var existing = this.importedTemplates.FirstOrDefault(t =>
                t.FileName.Equals(templateInfo.FileName, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                this.importedTemplates.Remove(existing);
            }

            this.importedTemplates.Add(templateInfo);
        }

        public void removeImportedTemplate(TemplateInfo templateInfo)
        {
            if (templateInfo == null) { return; }
            this.importedTemplates.Remove(templateInfo);
        }

        private void loadBuiltInTemplates()
        {
            // 기본 내장 템플릿들만 제공
            this.builtInTemplates.Add(new TemplateInfo("빈 프로젝트", "📄 Basic", "한국어", "empty-project", null, true, true, "새 프로젝트를 시작하기 위한 빈 템플릿"));
            this.builtInTemplates.Add(new TemplateInfo("모듈 명세", "📋 Module", "한국어", "module-spec", null, true, true, "단일 모듈의 API, 이벤트, 제약사항 정의"));
            this.builtInTemplates.Add(new TemplateInfo("앱 구성", "🏢 App", "한국어", "app-composition", null, true, true, "전역 DI, 수명주기, 이벤트 라우팅 설정"));
            this.builtInTemplates.Add(new TemplateInfo("마이크로서비스", "🔗 Service", "한국어", "microservice", null, true, true, "마이크로서비스 아키텍처 템플릿"));
            this.builtInTemplates.Add(new TemplateInfo("데이터 파이프라인", "📊 Data", "한국어", "data-pipeline", null, true, true, "데이터 처리 파이프라인 구조"));
        }

        private string loadBuiltInTemplate(string fileName)
        {
            // 기본 템플릿 내용 반환
            switch (fileName)
            {
                case "empty-project":
                    return @"# 프로젝트 명세
module: MyProject
goal: ""프로젝트 목표""

structure:
  - name: Core
  - name: Services
  - name: Models

api:
  - name: Example
    method: GET
    path: /api/example

events:
  - name: ExampleEvent
    payload: object
";
                
                case "module-spec":
                    return @"# 모듈 명세
module: MODULE_NAME
goal: ""모듈의 목표와 책임""

structure:
  - name: Core
    description: ""핵심 비즈니스 로직""
  - name: Models
    description: ""데이터 모델""
  - name: Services
    description: ""서비스 레이어""
  - name: Interfaces
    description: ""인터페이스 정의""

api:
  - name: CreateItem
    method: POST
    path: /api/items
  - name: GetItem
    method: GET
    path: /api/items/{id}

events:
  - name: ItemCreated
    payload: ItemData
  - name: ItemUpdated
    payload: ItemData

constraints:
  performance:
    responseTime: ""< 100ms""
  memory:
    maxHeap: ""256MB""
";
                
                case "app-composition":
                    return @"composition: APP_NAME
goal: ""애플리케이션 구성 및 DI 설정""

modules:
  - name: AuthModule
    interface: IAuthService
    lifecycle: Singleton
  - name: DataModule
    interface: IDataService
    lifecycle: Scoped
  - name: CacheModule
    interface: ICacheService
    lifecycle: Singleton

bindings:
  IAuthService: AuthService
  IDataService: DataService
  ICacheService: MemoryCacheService

eventFlows:
  - from: UserLoggedIn
    to: [""AuditModule.Log"", ""NotificationModule.Send""]

threading:
  model: Workers
  workerCount: 4
";
                
                case "microservice":
                    return @"# 마이크로서비스 명세
service: SERVICE_NAME
goal: ""서비스의 목표와 책임""

structure:
  - name: API
    description: ""REST API 엔드포인트""
  - name: Domain
    description: ""도메인 로직""
  - name: Infrastructure
    description: ""인프라 레이어""
  - name: Application
    description: ""애플리케이션 서비스""

api:
  - name: HealthCheck
    method: GET
    path: /health
  - name: GetStatus
    method: GET
    path: /api/status

messaging:
  publishes:
    - event: ServiceStarted
    - event: ServiceStopped
  subscribes:
    - event: ConfigurationChanged
    - event: SystemShutdown

database:
  type: PostgreSQL
  migrations: true
";
                
                case "data-pipeline":
                    return @"# 데이터 파이프라인 명세
pipeline: PIPELINE_NAME
goal: ""데이터 처리 파이프라인""

stages:
  - name: Extract
    type: DataExtractor
    source: Database
  - name: Transform
    type: DataTransformer
    operations:
      - filter
      - map
      - aggregate
  - name: Load
    type: DataLoader
    destination: DataWarehouse

scheduling:
  frequency: ""0 */6 * * *""
  retryPolicy:
    maxRetries: 3
    backoff: exponential

monitoring:
  metrics:
    - processed_records
    - error_rate
    - processing_time
";
                
                default:
                    return @"# 새 프로젝트
module: NewProject
goal: ""프로젝트 설명""

structure:
  - name: src
  - name: tests
  - name: docs
";
            }
        }

        private TemplateInfo createImportedTemplateInfo(string fileNameNoExt, string filePath)
        {
            var category = this.detectCategory(fileNameNoExt);
            var isKorean = fileNameNoExt.ToLower().EndsWith("-ko")
                           || fileNameNoExt.Contains("한글")
                           || fileNameNoExt.ToLower().Contains("kr");

            var language = isKorean ? "한국어" : "English";
            var displayName = this.formatDisplayName(fileNameNoExt, isKorean);
            var categoryIcon = this.getCategoryIcon(category);

            return new TemplateInfo(
                name: displayName,
                category: categoryIcon + " " + category,
                language: language,
                fileName: Path.GetFileName(filePath),
                filePath: filePath,
                isKorean: isKorean,
                isBuiltIn: false,
                description: this.generateDescription(fileNameNoExt, isKorean)
            );
        }

        private string detectCategory(string fileNameNoExt)
        {
            var lower = fileNameNoExt.ToLower();
            if (lower.Contains("module")) { return "Module"; }
            if (lower.Contains("app") || lower.Contains("composition")) { return "App Composition"; }
            if (lower.Contains("system")) { return "System"; }
            if (lower.Contains("integration")) { return "Integration"; }
            if (lower.Contains("data") || lower.Contains("pipeline")) { return "Data Pipeline"; }
            if (lower.Contains("migration")) { return "Migration"; }
            if (lower.Contains("monitoring")) { return "Monitoring"; }
            if (lower.Contains("test")) { return "Testing"; }

            return "Custom";
        }

        private string getCategoryIcon(string category)
        {
            switch (category)
            {
                case "Module": return "📋";
                case "App Composition": return "🏢";
                case "System": return "🖥️";
                case "Integration": return "🔗";
                case "Data Pipeline": return "📊";
                case "Migration": return "🔄";
                case "Monitoring": return "📈";
                case "Testing": return "🧪";
                default: return "📄";
            }
        }

        private string formatDisplayName(string fileNameNoExt, bool isKorean)
        {
            var name = fileNameNoExt
                .Replace("-template", "")
                .Replace("template-", "")
                .Replace("-ko", "")
                .Replace("-en", "")
                .Replace("-", " ")
                .Replace("_", " ");

            if (!string.IsNullOrEmpty(name))
            {
                name = char.ToUpper(name[0], CultureInfo.InvariantCulture) + name.Substring(1);
            }

            return string.IsNullOrEmpty(name) ? "Imported Template" : name;
        }

        private string generateDescription(string fileNameNoExt, bool isKorean)
        {
            var lower = fileNameNoExt.ToLower();
            if (lower.Contains("module"))
            {
                return isKorean ? "모듈 명세 템플릿" : "Module specification template";
            }
            if (lower.Contains("app") || lower.Contains("composition"))
            {
                return isKorean ? "앱 구성 템플릿" : "Application composition template";
            }
            return isKorean ? "임포트된 템플릿" : "Imported template";
        }
    }

    public sealed class TemplateInfo
    {
        // WPF 바인딩을 위해서는 프로퍼티 유지 + 메서드형 getter도 병행
        public string Name { get; set; }
        public string Category { get; set; }
        public string Language { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public bool IsKorean { get; set; }
        public bool IsBuiltIn { get; set; }
        public string Description { get; set; }

        public TemplateInfo() { }

        public TemplateInfo(string name, string category, string language, string fileName, string filePath, bool isKorean, bool isBuiltIn, string description)
        {
            this.Name = name;
            this.Category = category;
            this.Language = language;
            this.FileName = fileName;
            this.FilePath = filePath;
            this.IsKorean = isKorean;
            this.IsBuiltIn = isBuiltIn;
            this.Description = description;
        }

        public string getName() { return this.Name; }
        public string getCategory() { return this.Category; }
        public string getLanguage() { return this.Language; }
        public string getFileName() { return this.FileName; }
        public string getFilePath() { return this.FilePath; }
        public bool getIsKorean() { return this.IsKorean; }
        public bool getIsBuiltIn() { return this.IsBuiltIn; }
        public string getDescription() { return this.Description; }

        public string getDisplayText() { return $"{this.Name} ({this.Language})"; }

        public string getTooltipText()
        {
            var source = this.IsBuiltIn ? "내장" : "임포트";
            return $"{this.Description}\n\n소스: {source}\n파일: {this.FileName}";
        }
    }
}
