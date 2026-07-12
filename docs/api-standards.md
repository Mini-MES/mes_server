# API Standards & Auth Guide 🔑

이 문서는 프론트엔드 개발자 및 타 시스템과의 유기적인 협업을 위해 정의한 API 응답 표준, 공통 에러 핸들링, 그리고 JWT 인증 체계 가이드입니다.

---

## 🔒 JWT 인증 및 인가 (Authentication & Authorization)

본 서버의 모든 API(로그인/회원가입 제외)는 **[Authorize]** 속성을 통해 보호받고 있습니다. 클라이언트 애플리케이션은 반드시 요청 헤더에 JWT 토큰을 실어 보내야 합니다.

### 1. 토큰 발급 (로그인)
* **Endpoint**: `POST /api/User/login`
* **Request Body**:
  ```json
  {
    "userId": "operator1",
    "password": "Password123!"
  }
  ```
* **Response Body**:
  ```json
  {
    "message": "로그인에 성공했습니다.",
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
  }
  ```

### 2. 보호된 API 요청 헤더 규격
토큰을 발급받은 후, 모든 API 요청의 HTTP Header에 아래와 같이 `Authorization` 필드를 추가해야 합니다.
```http
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```
* **주의**: `Bearer` 와 토큰 문자열 사이에 반드시 한 칸의 공백(Space)이 있어야 합니다.

---

## 📤 공통 API 응답 규격 (Common Response Format)

본 프로젝트는 클라이언트가 응답을 일관되게 파싱할 수 있도록 공통 JSON 응답 포맷을 권장 및 구현하고 있습니다.

### 1. 데이터가 반환되는 성공 응답 (200 OK / 201 Created)
성공 메시지(`message`)와 함께 실제 페이로드(`data`)가 반환됩니다.
```json
{
  "message": "생산 지시가 성공적으로 생성되었습니다.",
  "data": {
    "orderID": 1024,
    "productID": "PROD-A",
    "targetQty": 500,
    "status": "Created",
    "lotID": "ABCD01",
    "orderDate": "2026-07-12T14:57:44"
  }
}
```

### 2. 상태 변경성 성공 응답 (200 OK)
단순 상태 변경이나 삭제 요청의 경우 `data` 필드 없이 결과 메시지만 전달합니다.
```json
{
  "message": "생산 지시가 성공적으로 완료되었습니다."
}
```

---

## ❌ 예외 및 에러 핸들링 (Error Handling)

요청 처리 중 비즈니스 룰 위반이나 데이터 미존재 등의 예외가 발생할 경우 아래와 같은 규격으로 에러를 응답합니다.

### 1. 리소스를 찾을 수 없는 경우 (404 Not Found)
* **발생 원인**: 잘못된 ID를 사용하여 단일 조회나 삭제를 시도할 때 발생합니다.
* **응답**:
  ```http
  HTTP/1.1 404 Not Found
  Content-Type: text/plain
  ```
  ```text
  생산 지시를 찾을 수 없습니다.
  ```

### 2. 비즈니스 룰 위반 (400 Bad Request / 500 Internal Server Error)
* **발생 원인**: 
  * 이미 진행중이거나 완료된 생산 지시를 수정/삭제하려고 할 때 (`InvalidOperationException`)
  * 재고 부족 시 원자재 차감을 시도할 때 (`InvalidOperationException`)
  * 보류(HOLD) 상태의 Lot을 다음 공정으로 이동하려 할 때
* **동작**: 시스템 내부적으로 예외(`Exception`)가 던져지며, 트랜잭션이 시작된 공정 이동(MoveProcess) 등의 API는 자동으로 롤백(Rollback) 처리됩니다.

---

## 📝 개발자 참고사항 (Swagger 사용법)
1. 로컬 환경에서 서버 기동 시 `http://localhost:<PORT>/swagger` 경로를 통해 인터랙티브 API 명세 및 테스트 툴을 제공합니다.
2. 우측 상단의 **[Authorize]** 버튼을 클릭하고 `Bearer [토큰값]` 형식으로 로그인 시 발급받은 토큰을 등록하면 Swagger 상에서 인증이 필요한 API를 즉시 테스트해볼 수 있습니다.
