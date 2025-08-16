# TemplateForge

독립적인 YAML 기반 프로젝트 구조 생성 도구

## 개요

TemplateForge는 YAML 명세를 기반으로 프로젝트 폴더 구조를 자동으로 생성하는 독립 실행형 Windows 애플리케이션입니다.

## 주요 기능

### 1. 프로젝트 생성 방식
- **🆕 새로 생성**: 내장 템플릿을 선택하여 새 프로젝트 시작
- **📂 불러오기**: 기존 YAML 파일을 불러와서 구조 생성

### 2. 내장 템플릿
- 빈 프로젝트 - 기본 구조만 있는 템플릿
- 모듈 명세 - API와 이벤트가 포함된 모듈 템플릿
- 앱 구성 - DI와 수명주기 관리 템플릿
- 마이크로서비스 - 서비스 아키텍처 템플릿
- 데이터 파이프라인 - ETL 구조 템플릿

### 3. YAML 편집 기능
- 실시간 YAML 편집기
- 구문 검증
- 실시간 폴더 구조 미리보기
- 모듈명 자동 추출 및 교체

### 4. 자동 생성 항목
- 📁 폴더 계층 구조
- 📄 README.md (프로젝트 설명)
- 📄 module-spec.yaml (원본 YAML 보존)
- 📄 기본 C# 코드 템플릿 (.cs 파일)

## 설치 및 실행

### 시스템 요구사항
- Windows 10 이상
- .NET Framework 4.7.2 이상

### 실행 방법

#### 방법 1: 빌드하여 실행
```bash
# 빌드
build.bat

# 실행
run.bat
```

#### 방법 2: Visual Studio에서 실행
1. `TemplateForge.sln` 열기
2. F5 키로 디버그 실행

## 사용 가이드

### 새 프로젝트 생성
1. **새로 생성** 버튼 클릭
2. 왼쪽 패널에서 템플릿 선택
3. 중앙 편집기에서 YAML 수정
4. 모듈명 입력
5. 출력 경로 선택
6. **Generate** 버튼 클릭

### 기존 YAML 불러오기
1. **불러오기** 버튼 클릭
2. YAML 파일 선택
3. 필요시 내용 수정
4. **Generate** 버튼 클릭

## YAML 구조 예시

```yaml
module: MyProject
goal: "프로젝트 목표 설명"

structure:
  - name: Core
    description: "핵심 비즈니스 로직"
  - name: Services
    description: "서비스 레이어"
  - name: Models
    description: "데이터 모델"

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
```

## 생성되는 폴더 구조

```
MyProject/
├── Core/
├── Services/
├── Models/
├── api/
│   ├── CreateItem.cs
│   └── GetItem.cs
├── events/
│   ├── ItemCreated.cs
│   └── ItemUpdated.cs
├── README.md
└── module-spec.yaml
```

## 주요 기능 설명

### 템플릿 시스템
- 5가지 기본 템플릿 제공
- 각 템플릿은 특정 아키텍처 패턴에 최적화
- 템플릿 선택 후 자유롭게 수정 가능

### 구조 생성 엔진
- YAML 파싱을 통한 구조 분석
- 자동 폴더/파일 생성
- 기본 코드 템플릿 생성

### 검증 시스템
- YAML 구문 검증
- 필수 항목 체크
- 실시간 오류 표시

## 확장 가능성

### 커스텀 YAML 구조
- `structure:` - 폴더 정의
- `api:` - API 엔드포인트 파일 생성
- `events:` - 이벤트 핸들러 파일 생성
- `models:` - 데이터 모델 파일 생성
- `services:` - 서비스 클래스 파일 생성

### 지원 언어
- 현재: C# 코드 템플릿 생성
- 추후: 다른 언어 템플릿 추가 가능

## 문제 해결

### 빌드 오류
- .NET Framework 4.7.2 설치 확인
- Visual Studio 2019 이상 설치 확인

### 생성 실패
- 출력 경로 쓰기 권한 확인
- 모듈명에 특수 문자 제거
- YAML 구문 검증

## 개발자 정보

이 프로그램은 독립적인 오픈소스 프로젝트입니다.

## 라이선스

MIT License

## 기여 방법

1. Fork the repository
2. Create your feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request

## 변경 이력

### v1.0.0
- 초기 릴리스
- 5가지 기본 템플릿
- YAML 기반 구조 생성
- 독립 실행형 애플리케이션