# Product Requirements Document (PRD)
## Xlsx-Grid-Flow

---

## 1. Project Purpose

To provide a secure, full-stack (C# & Angular) solution that transforms Excel templates into a controlled, browser-based data entry interface. The system focuses on **Data Integrity** by generating an automated **Audit Trail** and professional PDF reports without requiring a persistent database.

### 1.1 Application Architecture

- **Single Page Application (SPA)**: The frontend is built as a single-page Angular application with two main sections:
  - **Upload Section**: Allows users to upload `.xlsx` files and provides a downloadable example Excel template
  - **Grid Section**: Displays an interactive AG-Grid interface with example/default data on initial load
  
- **Styling Framework**: The application uses **TailwindCSS** for all UI styling, providing a modern, utility-first approach to design and ensuring consistent, responsive layouts across all components.

---

## 2. Scope of Requirements

### 2.1 Dynamic Template Extraction

- **Structure Parsing**: The system must accept `.xlsx` files and extract row/column metadata to generate a web-based grid layout.

- **Merged Cell Support**: Record and preserve merged cell information from the source Excel file, including:
  - Merged cell ranges (start/end row and column)
  - Display merged cells correctly in the web-based grid
  - Maintain merged cell structure during data entry and export

- **Logic Mapping**: Detect and preserve Excel-defined data types (numbers, dates, text) and cell-to-cell formulas.

- **UI Hardening**: Automatically lock cells containing formulas while enabling input for data-entry fields.

- **Column Editability Configuration**: Support column-level editability rules defined in the source Excel file:
  - Identify which columns are editable vs. ineditable based on Excel metadata
  - Enforce these rules in the web interface to prevent unauthorized modifications
  - Provide visual indicators to distinguish editable from ineditable columns

### 2.2 Controlled Data Entry Interface

- **Default Example Data**: On initial application load, the grid must display pre-configured example data to demonstrate the interface capabilities and allow users to explore features immediately without uploading a file.

- **High-Performance Grid**: Render the extracted schema into a responsive, interactive table using AG-Grid.

- **Real-time Validation**: Implement visual indicators for data that violates pre-defined ranges or types (e.g., highlighting out-of-spec values in red).

- **Formula Synchronization**: Re-calculate dependent values instantly upon any user input within the browser.

- **Cell Metadata Inspector**: When a user selects a cell, display:
  - The cell's data type (text, number, date, boolean, etc.)
  - The formula definition (if the cell contains a formula)
  - This information should be shown in a dedicated panel or tooltip for easy reference

- **Data Manipulation Controls**: Provide user-friendly controls for managing changes:
  - **Save**: Commit current changes and create a new version snapshot
  - **Cancel**: Discard all unsaved changes and revert to the last saved state
  - **History & Rollback**:
    - Users can view a list of all historical versions (Audit Logs)
    - Selecting a version previews the grid state at that point in time and displays the specific changes (old value vs. new value) for that version
    - Users can confirm to "Rollback" the current session to that specific version
  - Clear visual feedback on the current save state and available actions

- **Unsaved Changes Visualization**: Provide clear visual indicators for modified data:
  - Cells with unsaved changes must be highlighted with a distinct color (e.g., light yellow or amber background)
  - This visual differentiation helps users quickly identify which cells have been modified since the last save
  - The highlighting should be removed once changes are saved or cancelled
  - Ensure the color scheme maintains accessibility standards and doesn't interfere with validation indicators

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

- **Modern UI/UX**: The application must use TailwindCSS for styling to ensure:
  - Responsive design across all device sizes
  - Consistent visual language and component styling
  - Fast development iteration with utility-first CSS
  - Professional, modern appearance that enhances user experience

---

**Document Version**: 5.0  
**Last Updated**: 2026-01-05  
**Status**: Active Development