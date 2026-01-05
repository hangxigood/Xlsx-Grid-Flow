# Technical Design Document: Xlsx-Grid-Flow

## 1. Introduction
This document outlines the technical architecture and data schema for **Xlsx-Grid-Flow**, a secure, full-stack solution. The system leverages an **Angular 18+** frontend for the interactive grid (AG-Grid) and a **.NET Core (C#)** backend for stateless session management and document generation.

## 2. Core Data Models

### 2.1 Workspace Schema (`Template`)
The primary object representing the parsed Excel sheet and the current grid state.

```typescript
type DataType = 'text' | 'number' | 'date' | 'boolean' | 'formula';

type MergedCell = {
  startRow: number;
  startCol: number;
  endRow: number;
  endCol: number;
};

interface Template {
  id: string;              // Unique identifier for the session
  filename: string;        // Original .xlsx filename
  columnDefs: ColumnDef[]; // AG-Grid column configurations
  rowData: any[];          // Sheet data (array of objects). Formulas are stored as strings starting with '='
  mergedCells: MergedCell[]; // List of ranges to apply rowSpan/colSpan
}
```

### 2.2 Column Definition Schema (`ColumnDef`)
Properties used to configure AG-Grid columns, derived from the Excel header conventions.

```typescript
interface ColumnDef {
  field: string;      // Excel column reference (e.g., "A", "B", "C")
  headerName: string; // The cleaned display name (without tags)
  dataType: DataType; // The detected or specified type
  editable: boolean;  // Based on the specific rules or tags
}
```

### 2.3 Audit Trail Schema
Models used for tracking changes server-side.

```typescript
interface AuditLogEntry {
  version: number;        // The version identifier this change belongs to
  timestamp: string;      // ISO 8601
  cellReference: string;  // e.g., "B4"
  oldValue: any;
  newValue: any;
}

interface SessionState {
  sessionId: string;
  version: number;        // Incrementing revision number
  currentSnapshot: any[]; // Latest saved state
  changeLog: AuditLogEntry[];
}
```

## 3. Template Specification & Parsing Logic
Instead of complex UI forms, the system relies on a strict Excel template structure to define the application's behavior.

### 3.1 Structural Constraints
To ensure reliable parsing, uploaded `.xlsx` files must adhere to the following rules:
1. **Worksheet**: Only the **first visible worksheet** is processed. Hidden sheets or secondary sheets are ignored.
2. **Grid Origin**: The data table must start at cell **A1**.
   - **Row 1**: Strictly reserved for **Column Headers**.
   - **Row 2+**: Contains the data records.
   - *Note: Files with "Title Rows" or logos above the header row will result in parsing errors.*
3. **Empty Headers**: Columns with an empty value in Row 1 will be ignored and excluded from the grid.
4. **Unique Headers**: If duplicate header names exist (e.g., two columns named "Price"), the system will append a suffix to the field key (e.g., `price_1`) while keeping the display name intact.

### 3.2 Parsing Conventions (Type Inference)
The system parses the string values in Row 1 to determine column logic.

1. **Editable Columns**: Defined by suffixing the header with a type hint in parentheses.
   - Example: `Price (number)`, `Birthday (date)`, `Comment (text)`
2. **ReadOnly Columns**: Defined by suffixing the header with `(ReadOnly)`.
   - Example: `Total (ReadOnly)`, `Status (ReadOnly)`
3. **Implicit Logic**: 
   - If no tag is present, the system auto-detects the type from the first data row (Row 2) and defaults to `editable: true`.
   - **Empty First Row Fallback**: If the first data row is empty, the column defaults to `text`.
   - Tags are stripped from the final `headerName` displayed in the web UI.

### 3.3 Mapping Table
| Header String | Resulting DataType | Editable |
| :--- | :--- | :--- |
| `Name (text)` | `text` | `true` |
| `Quantity (number)` | `number` | `true` |
| `Due Date (date)` | `date` | `true` |
| `Total (ReadOnly)` | `text` | `false` |

## 4. Components & Logic

### 4.1 Grid Rendering (AG-Grid Integration)
- **Cell Merging**: 
  - The `mergedCells` array is processed to configure AG-Grid's `rowSpan` and `colSpan`. 
  - **Editing Logic**: Only the top-left cell of a merged range is editable. Changes to this cell logically represent the entire merged area.
- **Unified Logic**: Formatting (date strings, number alignment) and validation are applied globally based on the `dataType` property in `ColumnDef`.
- **Unsaved Changes**: The grid maintains a diff state. Cells where `currentValue !== baselineValue` are assigned a CSS class (e.g., `.cell-unsaved`) with a specific background color (e.g., amber/yellow) as defined in styles.

### 4.2 Formula Engine
- **Storage**: Formulas are extracted from Excel and preserved in `rowData` as strings beginning with `=`.
- **Execution**: A client-side formula parser (e.g., `hyperformula`) creates a dependency graph.
  - When an editable cell changes, dependent cells are automatically recalculated.
- **Display vs. Value**: The grid displays the *calculated result* by default. The *formula string* is visible only in the Metadata Inspector when the cell is selected.

### 4.3 State Management (Stateless Flow)
The application operates in-memory to maintain data privacy.
- **Save**: Submits current state to backend. Backend validates, generates a new "Snapshot" (Version N+1), and records differences.
- **Cancel**: Re-fetches the last saved Snapshot from the backend, discarding local changes.
- **History & Rollback**:
  - API provides the full list of `AuditLogEntry[]` grouped by version.
  - **Preview**: UI allows the user to click a version to load that historical data into the grid (read-only mode).
  - **Rollback**: Confirmed "Rollback" calls `/api/session/revert/{version}`. This creates a **new version** (N+1) that matches the data of the target historical version, ensuring the rollback event itself is audited.

## 5. UI/UX Specifications
- **Header Icons**: Display small icons next to `headerName` to indicate type (e.g., 📅 for date, ƒ for formula).
- **Metadata Inspector Panel**: A dedicated side panel (or detailed tooltip) displaying:
  - Cell Reference (e.g., C4)
  - Data Type
  - Raw Value / Formula
- **Protection Visibility**: Read-only columns utilize a distinct background (light gray) and a `not-allowed` cursor.

## 6. Validation & Integrity (Frontend)

### 6.1 Real-time Validation
Leverages AG-Grid's built-in validation capabilities while enforcing specific visual cues.

- **Type Checking**: Managed by AG-Grid's column definitions (e.g., `valueParser`, `suppressKeyboardEvent`).
- **Visual Feedback**:
  - **Invalid Cells**: Custom CSS class applied to cells failing validation to show a **red border**.
  - **Blocking**: Users cannot "Save" if **Critical Errors** exist (e.g., wrong data type). Non-critical warnings may allow saving with confirmation.

## 7. Backend Architecture (C# .NET)

### 7.1 Stateless Session Management
The backend utilizes `IMemoryCache` to store extraction sessions temporarily. No database is used.

- **Storage Key**: `SessionID` (GUID).
- **Expiration**: Sessions auto-expire after a set idle time (e.g., 30 minutes), purging data from RAM.

### 7.2 API Endpoints

| Method | Endpoint | Description |
| :--- | :--- | :--- |
| `POST` | `/api/template/upload` | Parses `.xlsx`, initializes Session, returns `Template` schema and `sessionId`. |
| `POST` | `/api/session/{sessionId}/save` | Accepts modified `rowData`. Calculates Diff, increments version, and appends to `changeLog`. |
| `POST` | `/api/session/{sessionId}/revert/{version}` | Reverts the session to a specific `version`. |
| `GET` | `/api/session/{sessionId}/audit` | Returns the full `AuditLogEntry[]` history for the session. |
| `GET` | `/api/session/{sessionId}/export/pdf` | Generates a PDF report of the current state and audit trail. |

## 8. Document Generation

### 8.1 PDF Export Strategy
The system generates a high-fidelity PDF server-side to ensure formatting consistency.

- **Library**: `QuestPDF`.
- **Structure**:
  1. **Cover Page**: Metadata (Filename, Export Date, Session ID).
  2. **Data Tables**: Renders the final grid state, preserving merged cells and column headers.
  3. **Audit Appendix**: A chronological table of all `AuditLogEntry` items, showing the "Life of the Data".

## 9. Project Structure

### 9.1 Frontend (Angular)
The frontend is organized to separate concerns between UI components and data communication.

`src/app/`
- **components/**
  - `grid-wrapper/`: The core editing interface.
    - `grid-wrapper.component.ts`: Initializes AG-Grid, handles cell value changes, applies validation styling.
  - `toolbar/`: Top-level controls.
    - `toolbar.component.ts`: Contains logic for "Save" and "Cancel" actions.
  - `audit-panel/`: History visualization.
    - `audit-panel.component.ts`: Displays versions list and handles Rollback actions.
  - `metadata-inspector/`: Cell details.
    - `metadata-inspector.component.ts`: Displays active cell's properties (formula, type, value).
- **services/**
  - `api.service.ts`: Facade for all HTTP requests to the C# backend.
  - `notification.service.ts`: Handles toast notifications.
  - `state.service.ts` (Optional): Manages local store of `mergedCells` and `rowData`.
- **models/**
  - `api-types.ts`: Shared interfaces (`Template`, `AuditLogEntry`, `SessionResponse`).

### 9.2 Backend (C# .NET Core)
The backend is structured as a clean Web API with service-layer isolation.

`XlsxGridFlow.API/`
- **Controllers/**: `TemplateController`, `SessionController`, `ExportController`.
- **Services/**
  - `ExcelService.cs`: Uses EPPlus. Parses headers for config conventions. Identifies merged cell ranges (`ExcelWorksheet.MergedCells`) and maps them to `MergedCell` objects.
  - `SessionService.cs`: Manages memory cache.
  - `DiffService.cs`: Compares snapshots.
  - `PdfService.cs`: Generates PDF reports.

## 10. Error Handling & Edge Cases
- **Session Expiration**: If the user attempts an action on an expired session, the API returns `404 Session Not Found`. The frontend redirects the user to the Upload page with a notification.
- **Concurrent Access**: Since sessions are stateless and ID-based, basic concurrency is handled by version checking. If `clientVersion != serverVersion` on save, the server rejects the update to prevent overwriting (Optimistic Concurrency Control).
- **Invalid File Type**: The Upload endpoint strictly validates MIME types to reject non-Excel files.
