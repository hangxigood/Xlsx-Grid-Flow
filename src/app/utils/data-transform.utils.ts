/**
 * Data Transformation Utilities
 * Handles conversion between frontend (flat) and backend (nested) data structures
 */

import { GridRow } from '../models/grid-types';
import { ApiGridRow } from '../models/api-types';

/**
 * Transform flat GridRow structure to nested ApiGridRow structure
 * Frontend uses: { rowId: 1, A: "value", B: 123 }
 * Backend expects: { rowId: 1, cells: { A: "value", B: 123 } }
 */
export function toApiGridRows(rows: GridRow[]): ApiGridRow[] {
    return rows.map(row => {
        const { rowId, ...cells } = row;
        return {
            rowId,
            cells
        };
    });
}

/**
 * Transform nested ApiGridRow structure to flat GridRow structure
 * Backend returns: { rowId: 1, cells: { A: "value", B: 123 } }
 * Frontend uses: { rowId: 1, A: "value", B: 123 }
 */
export function fromApiGridRows(apiRows: ApiGridRow[]): GridRow[] {
    return apiRows.map(apiRow => {
        const flatRow: GridRow = { rowId: apiRow.rowId };
        // Flatten the cells object into the row
        Object.keys(apiRow.cells).forEach(field => {
            flatRow[field] = apiRow.cells[field];
        });
        return flatRow;
    });
}
