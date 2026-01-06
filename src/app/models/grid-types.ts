/**
 * Core data models for the grid interface
 */

export type DataType = 'text' | 'number' | 'date' | 'boolean' | 'formula';
export type CellValue = string | number | boolean | null;

/**
 * Represents a merged cell range in the grid
 */
export interface MergedCell {
    startRow: number;
    startCol: number;
    endRow: number;
    endCol: number;
}

/**
 * Represents a single row in the grid with dynamic columns
 */
export interface GridRow {
    rowId: number; // 1-based index matching the original Excel row number
    [key: string]: CellValue | number; // Dynamic fields matching ColumnDef.field
}

/**
 * Column definition for AG-Grid configuration
 */
export interface ColumnDef {
    field: string; // Excel column reference (e.g., "A", "B", "C")
    headerName: string; // The cleaned display name (without tags)
    dataType: DataType; // The detected or specified type
    editable: boolean; // Based on the specific rules or tags
}

/**
 * Main template schema representing the parsed Excel sheet
 */
export interface Template {
    id: string; // Unique identifier for the session (use 'example' for demo data)
    filename: string; // Original .xlsx filename (use 'Example Template.xlsx' for demo)
    columnDefs: ColumnDef[]; // AG-Grid column configurations
    rowData: GridRow[]; // Structured sheet data
    mergedCells: MergedCell[]; // List of ranges to apply rowSpan/colSpan
}
