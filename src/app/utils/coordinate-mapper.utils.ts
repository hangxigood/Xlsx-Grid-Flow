/**
 * Coordinate Mapper Utility
 * 
 * Centralizes all coordinate system conversions between:
 * - Excel (1-based rows, letter columns)
 * - AG-Grid (array-based with persistent rowId)
 * - HyperFormula (0-based 2D array)
 */

import { GridRow, ColumnDef } from '../models/grid-types';

export interface ExcelCoordinate {
    row: number;      // 1-based (Row 1 = headers, Row 2 = first data)
    column: string;   // Letter-based (A, B, C, ..., Z, AA, AB, ...)
}

export interface HyperFormulaCoordinate {
    sheet: number;    // Always 0 for single-sheet
    row: number;      // 0-based (Row 0 = headers, Row 1 = first data)
    col: number;      // 0-based (0 = A, 1 = B, 2 = C, ...)
}

export interface AGGridCoordinate {
    rowId: number;    // Persistent identifier matching Excel row number
    field: string;    // Column field name (A, B, C, ...)
}

/**
 * Centralized coordinate mapping service
 */
export class CoordinateMapper {

    // ============================================================================
    // Column Letter ↔ Index Conversions
    // ============================================================================

    /**
     * Converts Excel column letter to 0-based index
     * @example columnLetterToIndex("A") → 0
     * @example columnLetterToIndex("B") → 1
     * @example columnLetterToIndex("AA") → 26
     */
    static columnLetterToIndex(letter: string): number {
        let index = 0;
        for (let i = 0; i < letter.length; i++) {
            index *= 26;
            index += (letter.charCodeAt(i) - 'A'.charCodeAt(0) + 1);
        }
        return index - 1; // Convert to 0-based
    }

    /**
     * Converts 0-based column index to Excel letter
     * @example indexToColumnLetter(0) → "A"
     * @example indexToColumnLetter(1) → "B"
     * @example indexToColumnLetter(26) → "AA"
     */
    static indexToColumnLetter(index: number): string {
        let letter = '';
        let num = index + 1; // Convert to 1-based for calculation

        while (num > 0) {
            const modulo = (num - 1) % 26;
            letter = String.fromCharCode('A'.charCodeAt(0) + modulo) + letter;
            num = Math.floor((num - modulo) / 26);
        }

        return letter;
    }

    // ============================================================================
    // Excel ↔ HyperFormula Conversions
    // ============================================================================

    /**
     * Converts Excel coordinates to HyperFormula coordinates
     * @example excelToHyperFormula({ row: 4, column: "B" }) 
     *          → { sheet: 0, row: 3, col: 1 }
     */
    static excelToHyperFormula(excel: ExcelCoordinate): HyperFormulaCoordinate {
        return {
            sheet: 0,
            row: excel.row - 1,  // Excel row 4 → HF row 3
            col: this.columnLetterToIndex(excel.column)
        };
    }

    /**
     * Converts HyperFormula coordinates to Excel coordinates
     * @example hyperFormulaToExcel({ sheet: 0, row: 3, col: 1 })
     *          → { row: 4, column: "B" }
     */
    static hyperFormulaToExcel(hf: HyperFormulaCoordinate): ExcelCoordinate {
        return {
            row: hf.row + 1,  // HF row 3 → Excel row 4
            column: this.indexToColumnLetter(hf.col)
        };
    }

    // ============================================================================
    // Excel ↔ AG-Grid Conversions
    // ============================================================================

    /**
     * Converts Excel coordinates to AG-Grid coordinates
     * @example excelToAGGrid({ row: 4, column: "B" })
     *          → { rowId: 4, field: "B" }
     */
    static excelToAGGrid(excel: ExcelCoordinate): AGGridCoordinate {
        return {
            rowId: excel.row,      // Direct mapping
            field: excel.column    // Direct mapping
        };
    }

    /**
     * Converts AG-Grid coordinates to Excel coordinates
     * @example agGridToExcel({ rowId: 4, field: "B" })
     *          → { row: 4, column: "B" }
     */
    static agGridToExcel(agGrid: AGGridCoordinate): ExcelCoordinate {
        return {
            row: agGrid.rowId,     // Direct mapping
            column: agGrid.field   // Direct mapping
        };
    }

    // ============================================================================
    // AG-Grid ↔ HyperFormula Conversions (Requires Row Data Context)
    // ============================================================================

    /**
     * Converts AG-Grid coordinates to HyperFormula coordinates
     * Requires row data to find array index for the given rowId
     * 
     * @param agGrid AG-Grid coordinate with rowId and field
     * @param rowData Current row data array
     * @param columnDefs Column definitions for field lookup
     * @returns HyperFormula coordinate or null if rowId not found
     * 
     * @example 
     * // If rowData[2] has rowId: 4
     * agGridToHyperFormula({ rowId: 4, field: "B" }, rowData, columnDefs)
     * → { sheet: 0, row: 3, col: 1 }
     * // row: 3 because arrayIndex=2, +1 for header = 3
     */
    static agGridToHyperFormula(
        agGrid: AGGridCoordinate,
        rowData: GridRow[],
        columnDefs: ColumnDef[]
    ): HyperFormulaCoordinate | null {
        // Find array index for this rowId
        const arrayIndex = rowData.findIndex(row => row.rowId === agGrid.rowId);

        if (arrayIndex === -1) {
            console.warn(`Row with rowId ${agGrid.rowId} not found in data`);
            return null;
        }

        // Find column index for this field
        const colIndex = columnDefs.findIndex(col => col.field === agGrid.field);

        if (colIndex === -1) {
            console.warn(`Column with field ${agGrid.field} not found in definitions`);
            return null;
        }

        return {
            sheet: 0,
            row: arrayIndex + 1,  // +1 because HyperFormula row 0 is headers
            col: colIndex
        };
    }

    /**
     * Converts HyperFormula coordinates to AG-Grid coordinates
     * Requires row data to map array index back to rowId
     * 
     * @param hf HyperFormula coordinate
     * @param rowData Current row data array
     * @param columnDefs Column definitions for field lookup
     * @returns AG-Grid coordinate or null if index out of bounds
     * 
     * @example
     * // If rowData[2] has rowId: 4
     * hyperFormulaToAGGrid({ sheet: 0, row: 3, col: 1 }, rowData, columnDefs)
     * → { rowId: 4, field: "B" }
     */
    static hyperFormulaToAGGrid(
        hf: HyperFormulaCoordinate,
        rowData: GridRow[],
        columnDefs: ColumnDef[]
    ): AGGridCoordinate | null {
        // Convert HyperFormula row to array index
        const arrayIndex = hf.row - 1;  // -1 because HF row 0 is headers

        if (arrayIndex < 0 || arrayIndex >= rowData.length) {
            console.warn(`HyperFormula row ${hf.row} out of bounds`);
            return null;
        }

        // Get rowId from the row at this array index
        const rowId = rowData[arrayIndex].rowId;

        // Get field from column definitions
        if (hf.col < 0 || hf.col >= columnDefs.length) {
            console.warn(`HyperFormula column ${hf.col} out of bounds`);
            return null;
        }

        const field = columnDefs[hf.col].field;

        return {
            rowId,
            field
        };
    }

    // ============================================================================
    // Cell Reference String Parsing
    // ============================================================================

    /**
     * Parses Excel cell reference string (e.g., "B4") to coordinates
     * @example parseCellReference("B4") → { row: 4, column: "B" }
     * @example parseCellReference("AA10") → { row: 10, column: "AA" }
     */
    static parseCellReference(cellRef: string): ExcelCoordinate | null {
        const match = cellRef.match(/^([A-Z]+)(\d+)$/);

        if (!match) {
            console.warn(`Invalid cell reference: ${cellRef}`);
            return null;
        }

        return {
            column: match[1],
            row: parseInt(match[2], 10)
        };
    }

    /**
     * Formats coordinates as Excel cell reference string
     * @example formatCellReference({ row: 4, column: "B" }) → "B4"
     */
    static formatCellReference(excel: ExcelCoordinate): string {
        return `${excel.column}${excel.row}`;
    }

    // ============================================================================
    // Validation Helpers
    // ============================================================================

    /**
     * Validates that a rowId exists in the row data
     * @throws Error if rowId not found
     */
    static validateRowId(rowId: number, rowData: GridRow[]): void {
        const exists = rowData.some(row => row.rowId === rowId);
        if (!exists) {
            throw new Error(`Invalid rowId ${rowId} - not found in row data`);
        }
    }

    /**
     * Validates that a field exists in column definitions
     * @throws Error if field not found
     */
    static validateField(field: string, columnDefs: ColumnDef[]): void {
        const exists = columnDefs.some(col => col.field === field);
        if (!exists) {
            throw new Error(`Invalid field ${field} - not found in column definitions`);
        }
    }
}

/**
 * Performance-optimized row index cache
 * Use this when performing many rowId lookups in a tight loop
 */
export class RowIndexCache {
    private cache = new Map<number, number>();

    /**
     * Rebuild the cache from current row data
     * Call this whenever row data changes (upload, revert, etc.)
     */
    rebuild(rowData: GridRow[]): void {
        this.cache.clear();
        rowData.forEach((row, index) => {
            this.cache.set(row.rowId, index);
        });
    }

    /**
     * Get array index for a given rowId (O(1) lookup)
     * @returns Array index or -1 if not found
     */
    getArrayIndex(rowId: number): number {
        return this.cache.get(rowId) ?? -1;
    }

    /**
     * Convert AG-Grid coordinate to HyperFormula using cached indices
     * Much faster than CoordinateMapper.agGridToHyperFormula for bulk operations
     */
    agGridToHyperFormula(
        agGrid: AGGridCoordinate,
        columnDefs: ColumnDef[]
    ): HyperFormulaCoordinate | null {
        const arrayIndex = this.getArrayIndex(agGrid.rowId);

        if (arrayIndex === -1) {
            return null;
        }

        const colIndex = columnDefs.findIndex(col => col.field === agGrid.field);

        if (colIndex === -1) {
            return null;
        }

        return {
            sheet: 0,
            row: arrayIndex + 1,
            col: colIndex
        };
    }

    /**
     * Clear the cache
     */
    clear(): void {
        this.cache.clear();
    }
}
