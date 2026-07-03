# MES (Manufacturing Execution System) Server

**스마트 팩토리 운영을 위한 생산 실행 시스템(MES) 서버**

## 🏗 프로젝트 소개
본 프로젝트는 생산 현장의 실시간 데이터 흐름을 관리하고, 생산 지시부터 공정 실적 추적(Traceability), 설비 공구 관리까지 아우르는 데이터 기반의 MES 서버입니다. 기존 프론트엔드 개발 경험을 바탕으로, 제조 도메인 지식을 녹여내어 데이터의 무결성과 추적성을 확보하는 데 중점을 두었습니다.

## 🛠 Tech Stack
* **Language**: C#, .NET 8.0 (LTS)
* **Framework**: ASP.NET Core Web API
* **ORM**: Entity Framework Core (Code First)
* **Database**: SQL Server
* **Tooling**: Visual Studio, Git

## 📁 폴더 구조 (Layered Architecture)
```text
mes_server/
├── Controllers/       # API 엔드포인트 관리
├── Data/              # EF Core DbContext 및 DB 연결 설정
├── Models/            # 핵심 도메인 모델 (Product, Lot, Performance 등)
└── Services/          # 비즈니스 로직 처리