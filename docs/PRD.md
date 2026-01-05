# Product Requirements Document (PRD)
## Xlsx-Grid-Flow

---

## 1. Project Purpose

To provide a secure, full-stack (C# & Angular) solution that transforms Excel templates into a controlled, browser-based data entry interface. The system focuses on **Data Integrity** by generating an automated **Audit Trail** and professional PDF reports without requiring a persistent database.

---

## 2. Scope of Requirements

### 2.1 Dynamic Template Extraction

- **Structure Parsing**: The system must accept `.xlsx` files and extract row/column metadata to generate a web-based grid layout.

- **Logic Mapping**: Detect and preserve Excel-defined data types (numbers, dates, text) and cell-to-cell formulas.

- **UI Hardening**: Automatically lock cells containing formulas while enabling input for data-entry fields.

### 2.2 Controlled Data Entry Interface

- **High-Performance Grid**: Render the extracted schema into a responsive, interactive table.

- **Real-time Validation**: Implement visual indicators for data that violates pre-defined ranges or types (e.g., highlighting out-of-spec values in red).

- **Formula Synchronization**: Re-calculate dependent values instantly upon any user input within the browser.

### 2.3 Stateless Audit Trail (In-Memory)

- **Baseline Versioning**: Upon initial upload, the system must store a "Version 0" snapshot in the server's memory.

- **Difference Calculation (Diffing)**: Every time a user saves their progress, the system must compare the new data against the previous version to identify changes.

- **Change Logging**: Automatically capture the timestamp, field location, old value, and new value for every modification.

- **History Retrieval**: Provide a dedicated view or panel for users to fetch and review the accumulated change logs.

### 2.4 Document Generation

- **Data-Audit Merge**: Combine the current grid state and the historical audit logs into a single structured report.

- **Professional PDF Export**: Generate a non-editable PDF document featuring:
  - The final data table
  - A chronological Audit Trail appendix
  - Document metadata (original filename, export time)

---

## 3. Non-Functional Requirements

- **Privacy by Design**: No data shall be persisted in a database; all session data resides in volatile memory and is purged upon session expiry.

- **High Fidelity**: The web-based grid must accurately reflect the visual and logical intent of the source Excel file.

- **Audit Transparency**: All changes must be traceable from the moment of upload to the moment of export.

---

**Document Version**: 3.0  
**Last Updated**: 2026-01-05  
**Status**: Active Development