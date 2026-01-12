/**
 * Unit tests for CoordinateMapper utility
 * Run with: ng test
 */

import { CoordinateMapper, RowIndexCache } from './coordinate-mapper.utils';
import { GridRow, ColumnDef, DataType } from '../models/grid-types';

describe('CoordinateMapper', () => {

    // Sample test data
    const columnDefs: ColumnDef[] = [
        { field: 'A', headerName: 'Name', dataType: DataType.Text, editable: true },
        { field: 'B', headerName: 'Price', dataType: DataType.Number, editable: true },
        { field: 'C', headerName: 'Total', dataType: DataType.Formula, editable: false }
    ];

    const rowData: GridRow[] = [
        { rowId: 2, A: 'Item 1', B: 100, C: '=B2*2' },
        { rowId: 4, A: 'Item 2', B: 200, C: '=B4*2' },  // Note: rowId 3 is missing (sparse data)
        { rowId: 5, A: 'Item 3', B: 300, C: '=B5*2' }
    ];

    describe('Column Letter ↔ Index Conversions', () => {
        it('should convert column letters to indices', () => {
            expect(CoordinateMapper.columnLetterToIndex('A')).toBe(0);
            expect(CoordinateMapper.columnLetterToIndex('B')).toBe(1);
            expect(CoordinateMapper.columnLetterToIndex('Z')).toBe(25);
            expect(CoordinateMapper.columnLetterToIndex('AA')).toBe(26);
            expect(CoordinateMapper.columnLetterToIndex('AB')).toBe(27);
        });

        it('should convert indices to column letters', () => {
            expect(CoordinateMapper.indexToColumnLetter(0)).toBe('A');
            expect(CoordinateMapper.indexToColumnLetter(1)).toBe('B');
            expect(CoordinateMapper.indexToColumnLetter(25)).toBe('Z');
            expect(CoordinateMapper.indexToColumnLetter(26)).toBe('AA');
            expect(CoordinateMapper.indexToColumnLetter(27)).toBe('AB');
        });

        it('should round-trip column conversions', () => {
            const letters = ['A', 'B', 'Z', 'AA', 'AB', 'AZ', 'BA'];
            letters.forEach(letter => {
                const index = CoordinateMapper.columnLetterToIndex(letter);
                const result = CoordinateMapper.indexToColumnLetter(index);
                expect(result).toBe(letter);
            });
        });
    });

    describe('Cell Reference Parsing', () => {
        it('should parse valid cell references', () => {
            const coord = CoordinateMapper.parseCellReference('B4');
            expect(coord).toEqual({ row: 4, column: 'B' });
        });

        it('should parse multi-letter column references', () => {
            const coord = CoordinateMapper.parseCellReference('AA10');
            expect(coord).toEqual({ row: 10, column: 'AA' });
        });

        it('should return null for invalid references', () => {
            expect(CoordinateMapper.parseCellReference('4B')).toBeNull();
            expect(CoordinateMapper.parseCellReference('B')).toBeNull();
            expect(CoordinateMapper.parseCellReference('4')).toBeNull();
            expect(CoordinateMapper.parseCellReference('')).toBeNull();
        });

        it('should format cell references', () => {
            expect(CoordinateMapper.formatCellReference({ row: 4, column: 'B' })).toBe('B4');
            expect(CoordinateMapper.formatCellReference({ row: 10, column: 'AA' })).toBe('AA10');
        });
    });

    describe('Excel ↔ HyperFormula Conversions', () => {
        it('should convert Excel to HyperFormula coordinates', () => {
            const hf = CoordinateMapper.excelToHyperFormula({ row: 4, column: 'B' });
            expect(hf).toEqual({ sheet: 0, row: 3, col: 1 });
        });

        it('should convert HyperFormula to Excel coordinates', () => {
            const excel = CoordinateMapper.hyperFormulaToExcel({ sheet: 0, row: 3, col: 1 });
            expect(excel).toEqual({ row: 4, column: 'B' });
        });

        it('should round-trip Excel ↔ HyperFormula conversions', () => {
            const original = { row: 10, column: 'AA' };
            const hf = CoordinateMapper.excelToHyperFormula(original);
            const result = CoordinateMapper.hyperFormulaToExcel(hf);
            expect(result).toEqual(original);
        });
    });

    describe('AG-Grid ↔ HyperFormula Conversions', () => {
        it('should convert AG-Grid to HyperFormula coordinates', () => {
            // rowData[1] has rowId: 4
            const hf = CoordinateMapper.agGridToHyperFormula(
                { rowId: 4, field: 'B' },
                rowData,
                columnDefs
            );
            expect(hf).toEqual({ sheet: 0, row: 2, col: 1 });
            // row: 2 because arrayIndex=1, +1 for header = 2
        });

        it('should handle sparse rowIds correctly', () => {
            // rowData[0] has rowId: 2
            const hf1 = CoordinateMapper.agGridToHyperFormula(
                { rowId: 2, field: 'A' },
                rowData,
                columnDefs
            );
            expect(hf1).toEqual({ sheet: 0, row: 1, col: 0 });

            // rowData[2] has rowId: 5
            const hf2 = CoordinateMapper.agGridToHyperFormula(
                { rowId: 5, field: 'C' },
                rowData,
                columnDefs
            );
            expect(hf2).toEqual({ sheet: 0, row: 3, col: 2 });
        });

        it('should return null for invalid rowId', () => {
            const hf = CoordinateMapper.agGridToHyperFormula(
                { rowId: 999, field: 'A' },
                rowData,
                columnDefs
            );
            expect(hf).toBeNull();
        });

        it('should return null for invalid field', () => {
            const hf = CoordinateMapper.agGridToHyperFormula(
                { rowId: 2, field: 'Z' },
                rowData,
                columnDefs
            );
            expect(hf).toBeNull();
        });

        it('should convert HyperFormula to AG-Grid coordinates', () => {
            const agGrid = CoordinateMapper.hyperFormulaToAGGrid(
                { sheet: 0, row: 2, col: 1 },
                rowData,
                columnDefs
            );
            expect(agGrid).toEqual({ rowId: 4, field: 'B' });
        });

        it('should return null for out of bounds HyperFormula coordinates', () => {
            const agGrid = CoordinateMapper.hyperFormulaToAGGrid(
                { sheet: 0, row: 999, col: 0 },
                rowData,
                columnDefs
            );
            expect(agGrid).toBeNull();
        });
    });

    describe('Validation Helpers', () => {
        it('should validate existing rowId', () => {
            expect(() => {
                CoordinateMapper.validateRowId(2, rowData);
            }).not.toThrow();
        });

        it('should throw for invalid rowId', () => {
            expect(() => {
                CoordinateMapper.validateRowId(999, rowData);
            }).toThrow('Invalid rowId 999');
        });

        it('should validate existing field', () => {
            expect(() => {
                CoordinateMapper.validateField('A', columnDefs);
            }).not.toThrow();
        });

        it('should throw for invalid field', () => {
            expect(() => {
                CoordinateMapper.validateField('Z', columnDefs);
            }).toThrow('Invalid field Z');
        });
    });
});

describe('RowIndexCache', () => {
    const rowData: GridRow[] = [
        { rowId: 2, A: 'Item 1' },
        { rowId: 4, A: 'Item 2' },
        { rowId: 5, A: 'Item 3' }
    ];

    const columnDefs: ColumnDef[] = [
        { field: 'A', headerName: 'Name', dataType: DataType.Text, editable: true }
    ];

    it('should cache row indices correctly', () => {
        const cache = new RowIndexCache();
        cache.rebuild(rowData);

        expect(cache.getArrayIndex(2)).toBe(0);
        expect(cache.getArrayIndex(4)).toBe(1);
        expect(cache.getArrayIndex(5)).toBe(2);
        expect(cache.getArrayIndex(999)).toBe(-1);
    });

    it('should convert AG-Grid to HyperFormula using cache', () => {
        const cache = new RowIndexCache();
        cache.rebuild(rowData);

        const hf = cache.agGridToHyperFormula(
            { rowId: 4, field: 'A' },
            columnDefs
        );

        expect(hf).toEqual({ sheet: 0, row: 2, col: 0 });
    });

    it('should clear cache', () => {
        const cache = new RowIndexCache();
        cache.rebuild(rowData);

        expect(cache.getArrayIndex(2)).toBe(0);

        cache.clear();

        expect(cache.getArrayIndex(2)).toBe(-1);
    });

    it('should rebuild cache with new data', () => {
        const cache = new RowIndexCache();
        cache.rebuild(rowData);

        expect(cache.getArrayIndex(2)).toBe(0);

        const newRowData: GridRow[] = [
            { rowId: 10, A: 'New Item' }
        ];

        cache.rebuild(newRowData);

        expect(cache.getArrayIndex(2)).toBe(-1);
        expect(cache.getArrayIndex(10)).toBe(0);
    });
});
