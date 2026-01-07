# API Contract: Xlsx-Grid-Flow

This document defines the interface between the **Angular Frontend** and the **.NET Core Backend**.

---

## 1. General Specifications

- **Base URL**: `/api`
- **Content-Type**: `application/json` (except for file upload and export)
- **Date Format**: ISO 8601 (`YYYY-MM-DDTHH:mm:ss.sssZ`)
- **Statelessness**: No authentication required for this version; sessions are identified by a `sessionId` (GUID) returned upon file upload.
- **Session Expiration**: Sessions auto-expire after 30 minutes of inactivity.

---

## 2. Common Data Models (DTOs)

### 2.1 Basic Types
```json
// DataType
"text" | "number" | "date" | "boolean" | "formula"

// CellValue
string | number | boolean | null

// Note on Formulas:
// When dataType is "formula", the CellValue is the raw formula string beginning with "=".
// The client is responsible for evaluation using its internal formula engine.
```

### 2.2 Shared Objects
#### `ColumnDefDto`
```json
{
  "field": "string",      // e.g., "A", "B", "C"
  "headerName": "string", // Display name
  "dataType": "DataType",
  "editable": "boolean"
}
```

#### `GridRowDto`
```json
{
  "rowId": "number",
  "cells": {
    "A": "CellValue", // e.g., "Laptop"
    "B": "CellValue", // e.g., 1
    "[key: string]": "CellValue"
  }
}
```

#### `MergedCellDto`
```json
{
  "startRow": "number",
  "startCol": "number",
  "endRow": "number",
  "endCol": "number"
}

// Note on Merged Cells:
// Only the top-left cell of a merged range (startRow, startCol) is editable
// and contains the value for the entire merged area.
```

#### `AuditLogEntryDto`
```json
{
  "version": "number",
  "timestamp": "string", // ISO 8601
  "cellReference": "string", // e.g., "B4"
  "oldValue": "CellValue",
  "newValue": "CellValue"
}
```

---

## 3. Endpoints

### 3.1 Template Management

#### `POST /template/upload`
Initializes a session by parsing an Excel file.

- **Request**: `multipart/form-data`
  - `file`: .xlsx file
- **Response**: `201 Created`
  ```json
  {
    "sessionId": "uuid",
    "expiresAt": "string", // ISO 8601
    "template": {
      "filename": "string",
      "columnDefs": ["ColumnDefDto"],
      "rowData": ["GridRowDto"],
      "mergedCells": ["MergedCellDto"]
    }
  }
  ```
- **Error Codes**:
  - `400`: `INVALID_FILE_TYPE`, `EMPTY_FILE`, `PARSING_ERROR`

---

### 3.2 Session Management

#### `POST /session/{sessionId}/save`
Saves changes made to the grid and generates a new version.

- **Request Body**:
  ```json
  {
    "rowData": ["GridRowDto"],
    "clientVersion": "number" // Current version known by client. Prevents overwrites (Optimistic Concurrency Control).
  }
  ```
- **Response**: `200 OK`
  ```json
  {
    "newVersion": "number",
    "timestamp": "string",
    "auditEntries": ["AuditLogEntryDto"] // Only the changes introduced in this version
  }
  ```
- **Error Codes**:
  - `404`: `SESSION_NOT_FOUND`
  - `409`: `CONCURRENCY_CONFLICT` (if clientVersion != current server version)
  - `400`: `VALIDATION_FAILED`

#### `POST /session/{sessionId}/revert/{version}`
Reverts the session data to a previous version number.

- **Request Parameters**:
  - `version`: Target version number to revert to.
- **Response**: `200 OK`
  ```json
  {
    "newVersion": "number",
    "rowData": ["GridRowDto"],
    "auditEntries": ["AuditLogEntryDto"] // Entries representing only the state change of this revert
  }
  ```
- **Error Codes**:
  - `404`: `SESSION_NOT_FOUND`, `VERSION_NOT_FOUND`

---

### 3.3 Audit & Reporting

#### `GET /session/{sessionId}/audit`
Retrieves the full change history for the session.

- **Response**: `200 OK`
  ```json
  {
    "sessionId": "uuid",
    "history": [
      {
        "version": "number",
        "timestamp": "string",
        "entries": ["AuditLogEntryDto"]
      }
    ]
  }
  ```
- **Error Codes**:
  - `404`: `SESSION_NOT_FOUND`

#### `GET /session/{sessionId}/export/pdf`
Generates a PDF report containing the grid and audit trail.

- **Response**: `200 OK`
  - `Content-Type`: `application/pdf`
  - `Content-Disposition`: `attachment; filename="report-timestamp.pdf"`
  - **Payload Structure**:
    1. **Cover Page**: Session metadata (filename, export date, sessionId).
    2. **Data Tables**: Current grid state rendering, preserving merged cells.
    3. **Audit Appendix**: Full chronological log of all changes.
- **Error Codes**:
  - `404`: `SESSION_NOT_FOUND`

---

## 4. Error Response Format

All error responses follow this structure:

```json
{
  "errorCode": "string",
  "message": "string",
  "details": {} // Optional object with specific validation errors
}
```

### Table of Error Codes
| HTTP Status | Error Code | Description |
| :--- | :--- | :--- |
| 400 | `INVALID_FILE_TYPE` | Uploaded file is not a valid .xlsx file. |
| 400 | `PARSING_ERROR` | Failed to parse Excel structure due to constraints (e.g., missing Row 1). |
| 400 | `VALIDATION_FAILED` | Data submitted in `save` fails type or business rules. |
| 404 | `SESSION_NOT_FOUND` | Session expired or ID is invalid. |
| 409 | `CONCURRENCY_CONFLICT` | Server version has advanced since client's last fetch. |
| 500 | `SERVER_ERROR` | Unexpected server-side failure. |
