#!/usr/bin/env python3
"""
TemplateForge FastMCP Server
YAML 명세 기반 폴더 구조 생성 서비스
"""
import os
import sys
import yaml
import json
import argparse
from pathlib import Path
from typing import Dict, List, Any
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
import uvicorn

app = FastAPI(title="TemplateForge FastMCP", version="1.0.0")

class GenerateProjectRequest(BaseModel):
    yaml_content: str
    output_path: str

class ValidateYamlRequest(BaseModel):
    yaml_content: str

class PreviewStructureRequest(BaseModel):
    yaml_content: str

class ProjectStructure(BaseModel):
    folders: List[str]
    files: List[str]
    module_name: str

@app.get("/health")
def health_check():
    """헬스체크 엔드포인트"""
    return {"status": "ok", "service": "TemplateForge FastMCP"}

@app.post("/api/generate")
def generate_project(request: GenerateProjectRequest):
    """YAML 명세 기반 프로젝트 폴더 생성"""
    try:
        # YAML 파싱
        spec = yaml.safe_load(request.yaml_content)
        
        # 프로젝트 구조 분석
        structure = analyze_yaml_structure(spec)
        
        # 실제 폴더 생성
        created_paths = create_folder_structure(structure, request.output_path)
        
        return {
            "success": True,
            "module_name": structure.module_name,
            "created_folders": created_paths,
            "structure": structure.dict()
        }
    except Exception as e:
        raise HTTPException(status_code=400, detail=str(e))

@app.post("/api/preview")
def preview_structure(request: PreviewStructureRequest):
    """생성될 폴더 구조 미리보기"""
    try:
        spec = yaml.safe_load(request.yaml_content)
        structure = analyze_yaml_structure(spec)
        
        return {
            "success": True,
            "structure": structure.dict()
        }
    except Exception as e:
        raise HTTPException(status_code=400, detail=str(e))

@app.post("/api/validate")
def validate_yaml(request: ValidateYamlRequest):
    """YAML 명세 검증"""
    try:
        spec = yaml.safe_load(request.yaml_content)
        errors = validate_spec(spec)
        
        return {
            "valid": len(errors) == 0,
            "errors": errors
        }
    except yaml.YAMLError as e:
        return {
            "valid": False,
            "errors": [f"YAML 구문 오류: {str(e)}"]
        }

def analyze_yaml_structure(spec: Dict[str, Any]) -> ProjectStructure:
    """YAML 명세를 분석해서 폴더 구조 결정"""
    folders = []
    files = []
    
    # 모듈명 추출
    module_name = spec.get('module', 'UnknownModule')
    
    # 기본 폴더 구조
    folders.extend([
        'src',
        'tests', 
        'docs'
    ])
    
    # API 인터페이스가 있으면 Interfaces 폴더
    if spec.get('api', {}).get('interfaces'):
        folders.append('src/Interfaces')
    
    # 이벤트가 있으면 Events 폴더
    if spec.get('events'):
        folders.append('src/Events')
    
    # 데이터 모델이 있으면 Models 폴더
    if spec.get('dataModels'):
        folders.append('src/Models')
        if spec.get('dataModels', {}).get('dtos'):
            folders.append('src/Models/Dtos')
        if spec.get('dataModels', {}).get('entities'):
            folders.append('src/Models/Entities')
    
    # 아키텍처 레이어가 정의되어 있으면
    if spec.get('architecture', {}).get('layers'):
        for layer in spec['architecture']['layers']:
            layer_name = layer.get('name', '').replace(' ', '')
            if layer_name:
                folders.append(f'src/{layer_name}')
    
    # 통합이 있으면 Integration 폴더
    if spec.get('integration'):
        folders.append('src/Integration')
    
    # 모니터링이 있으면 Monitoring 폴더
    if spec.get('monitoring'):
        folders.append('src/Monitoring')
    
    # 기본 파일들
    files.extend([
        f'{module_name}.yaml',  # 원본 명세 파일
        'README.md',
        'docs/Architecture.md'
    ])
    
    return ProjectStructure(
        folders=sorted(list(set(folders))),
        files=files,
        module_name=module_name
    )

def create_folder_structure(structure: ProjectStructure, base_path: str) -> List[str]:
    """실제 폴더/파일 생성"""
    created_paths = []
    
    # 모듈 루트 폴더 생성
    module_root = Path(base_path) / structure.module_name
    module_root.mkdir(parents=True, exist_ok=True)
    created_paths.append(str(module_root))
    
    # 폴더들 생성
    for folder in structure.folders:
        folder_path = module_root / folder
        folder_path.mkdir(parents=True, exist_ok=True)
        created_paths.append(str(folder_path))
    
    # 기본 파일들 생성
    for file in structure.files:
        file_path = module_root / file
        file_path.parent.mkdir(parents=True, exist_ok=True)
        
        if not file_path.exists():
            if file.endswith('.md'):
                file_path.write_text(f"# {structure.module_name}\n\n생성일: {__import__('datetime').datetime.now().strftime('%Y-%m-%d %H:%M:%S')}\n", encoding='utf-8')
            elif file.endswith('.yaml'):
                file_path.write_text("# 원본 YAML 명세 파일\n", encoding='utf-8')
            else:
                file_path.touch()
            
            created_paths.append(str(file_path))
    
    return created_paths

def validate_spec(spec: Dict[str, Any]) -> List[str]:
    """YAML 명세 검증"""
    errors = []
    
    # 필수 필드 체크
    if 'module' not in spec:
        errors.append("'module' 필드가 필요합니다")
    
    if 'goal' not in spec:
        errors.append("'goal' 필드가 필요합니다")
    
    # 모듈명 검증
    if 'module' in spec:
        module_name = spec['module']
        if not isinstance(module_name, str) or not module_name.strip():
            errors.append("'module' 필드는 비어있지 않은 문자열이어야 합니다")
    
    return errors

def main():
    parser = argparse.ArgumentParser(description="TemplateForge FastMCP Server")
    parser.add_argument("--port", type=int, default=6060, help="Server port")
    parser.add_argument("--host", default="127.0.0.1", help="Server host")
    
    args = parser.parse_args()
    
    print(f"Starting TemplateForge FastMCP Server on {args.host}:{args.port}")
    uvicorn.run(app, host=args.host, port=args.port, log_level="info")

if __name__ == "__main__":
    main()
