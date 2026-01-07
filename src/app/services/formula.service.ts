/**
 * Formula Service - Client-side formula calculation using HyperFormula
 */

import { Injectable } from '@angular/core';
import { HyperFormula, ConfigParams } from 'hyperformula';
import { GridRow, ColumnDef } from '../models/grid-types';

@Injectable({
    providedIn: 'root',
})
export class FormulaService {
    private hfInstance: HyperFormula | null = null;
    private originalRowData: GridRow[] = []; // Store original row data for rowId mapping

    /**
     * Initialize HyperFormula with grid data
     */
    initializeFormulas(columnDefs: ColumnDef[], rowData: GridRow[]): void {
        // Store original row data for rowId mapping
        this.originalRowData = rowData;

        // Configuration for HyperFormula
        const config: Partial<ConfigParams> = {
            licenseKey: 'gpl-v3', // Use GPL v3 license
            useColumnIndex: false, // Use A1 notation
        };

        // Convert grid data to 2D array format that HyperFormula expects
        const sheetData = this.convertToSheetData(columnDefs, rowData);

        // Initialize HyperFormula instance
        this.hfInstance = HyperFormula.buildFromArray(sheetData, config);
    }

    /**
     * Update a cell value and recalculate dependent formulas
     * @param rowId The rowId from the GridRow (not the array index!)
     */
    updateCell(rowId: number, columnField: string, value: any, columnDefs: ColumnDef[]): void {
        if (!this.hfInstance) {
            console.warn('HyperFormula not initialized');
            return;
        }

        // Find the array index of the row with this rowId
        const arrayIndex = this.originalRowData.findIndex(row => row.rowId === rowId);
        if (arrayIndex === -1) {
            console.warn(`Row with rowId ${rowId} not found`);
            return;
        }

        // Convert column field (e.g., "A", "B") to column index
        const colIndex = columnDefs.findIndex(col => col.field === columnField);

        if (colIndex === -1) {
            console.warn(`Column ${columnField} not found`);
            return;
        }

        // Update the cell in HyperFormula
        // arrayIndex is 0-based, but HyperFormula row 0 is headers, so add 1
        const hfRow = arrayIndex + 1;
        this.hfInstance.setCellContents(
            { sheet: 0, col: colIndex, row: hfRow },
            value
        );
    }

    /**
     * Get calculated values for all cells
     * For formula cells, returns the formula string (e.g., "=C2*D2")
     * For regular cells, returns the calculated value
     */
    getCalculatedData(columnDefs: ColumnDef[], originalRowData: GridRow[]): GridRow[] {
        if (!this.hfInstance) {
            console.warn('HyperFormula not initialized');
            return [];
        }

        const calculatedRows: GridRow[] = [];

        // Iterate through original rows to preserve rowIds
        originalRowData.forEach((originalRow, index) => {
            const rowIdx = index + 1; // +1 because row 0 is headers in HyperFormula
            const row: GridRow = { rowId: originalRow.rowId }; // Preserve original rowId

            columnDefs.forEach((colDef, colIdx) => {
                // First, check if this cell contains a formula
                const formula = this.hfInstance!.getCellFormula({
                    sheet: 0,
                    col: colIdx,
                    row: rowIdx,
                });

                if (formula) {
                    // This is a formula cell - store the formula string
                    row[colDef.field] = formula;
                } else {
                    // This is a regular cell - get the calculated value
                    const cellValue = this.hfInstance!.getCellValue({
                        sheet: 0,
                        col: colIdx,
                        row: rowIdx,
                    });

                    // Convert HyperFormula's CellValue to our CellValue type
                    // HyperFormula can return DetailedCellError objects, which we convert to null
                    if (cellValue && typeof cellValue === 'object' && 'type' in cellValue) {
                        // This is an error object, store as null
                        row[colDef.field] = null;
                    } else {
                        // Store the calculated value (string, number, boolean, or null)
                        row[colDef.field] = cellValue as string | number | boolean | null;
                    }
                }
            });

            calculatedRows.push(row);
        });

        return calculatedRows;
    }

    /**
     * Get the formula string for a specific cell
     */
    getFormula(rowIndex: number, columnField: string, columnDefs: ColumnDef[]): string | null {
        if (!this.hfInstance) {
            return null;
        }

        const colIndex = columnDefs.findIndex(col => col.field === columnField);
        if (colIndex === -1) {
            return null;
        }

        const formula = this.hfInstance.getCellFormula({
            sheet: 0,
            col: colIndex,
            row: rowIndex,
        });

        return formula || null;
    }

    /**
     * Check if a cell contains a formula
     */
    isFormula(rowIndex: number, columnField: string, columnDefs: ColumnDef[]): boolean {
        return this.getFormula(rowIndex, columnField, columnDefs) !== null;
    }

    /**
     * Get the calculated value for a specific cell
     */
    getCellCalculatedValue(rowIndex: number, columnField: string, columnDefs: ColumnDef[]): any {
        if (!this.hfInstance) {
            return null;
        }

        const colIndex = columnDefs.findIndex(col => col.field === columnField);
        if (colIndex === -1) {
            return null;
        }

        const cellValue = this.hfInstance.getCellValue({
            sheet: 0,
            col: colIndex,
            row: rowIndex,
        });

        // Handle error objects
        if (cellValue && typeof cellValue === 'object' && 'type' in cellValue) {
            return null;
        }

        return cellValue;
    }

    /**
     * Destroy the HyperFormula instance
     */
    destroy(): void {
        if (this.hfInstance) {
            this.hfInstance.destroy();
            this.hfInstance = null;
        }
    }

    /**
     * Convert grid data to 2D array format for HyperFormula
     */
    private convertToSheetData(columnDefs: ColumnDef[], rowData: GridRow[]): any[][] {
        const sheetData: any[][] = [];

        // First row: headers
        const headerRow = columnDefs.map(col => col.headerName);
        sheetData.push(headerRow);

        // Data rows
        rowData.forEach(row => {
            const dataRow: any[] = [];
            columnDefs.forEach(colDef => {
                const cellValue = row[colDef.field];

                // If the value is a string starting with '=', it's a formula
                if (typeof cellValue === 'string' && cellValue.startsWith('=')) {
                    dataRow.push(cellValue);
                } else {
                    dataRow.push(cellValue ?? null);
                }
            });
            sheetData.push(dataRow);
        });

        return sheetData;
    }

    /**
     * Rebuild formulas when template changes
     */
    rebuildFormulas(columnDefs: ColumnDef[], rowData: GridRow[]): void {
        this.destroy();
        this.initializeFormulas(columnDefs, rowData);
    }
}
