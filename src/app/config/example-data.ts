/**
 * Example template data for initial application load
 * Demonstrates all key features: editable/readonly columns, formulas, validation, merged cells
 */

import { Template } from '../models/grid-types';

export const EXAMPLE_TEMPLATE: Template = {
    id: 'example',
    filename: 'testbook.xlsx',
    columnDefs: [
        {
            field: 'A',
            headerName: 'Part',
            dataType: 'text',
            editable: false,
        },
        {
            field: 'B',
            headerName: '-',
            dataType: 'text',
            editable: false,
        },
        {
            field: 'C',
            headerName: 'Quantity Used',
            dataType: 'number',
            editable: true,
        },
        {
            field: 'D',
            headerName: 'Quantity Unused',
            dataType: 'number',
            editable: true,
        },
        {
            field: 'E',
            headerName: 'Total',
            dataType: 'formula',
            editable: false,
        },
        {
            field: 'F',
            headerName: 'Accept/Reject',
            dataType: 'boolean',
            editable: true,
        },
        {
            field: 'G',
            headerName: 'Date',
            dataType: 'date',
            editable: true,
        },
    ],
    rowData: [
        {
            rowId: 2,
            A: '1234-AB-01',
            B: null,
            C: null,
            D: null,
            E: '=C2+D2',
            F: null,
            G: null,
        },
        {
            rowId: 3,
            A: null,
            B: null,
            C: null,
            D: null,
            E: '=C3+D3',
            F: null,
            G: null,
        },
        {
            rowId: 4,
            A: 'OR',
            B: '2345-PT-01',
            C: null,
            D: null,
            E: '=C4+D4',
            F: null,
            G: null,
        },
        {
            rowId: 5,
            A: null,
            B: '112233-1-1',
            C: null,
            D: null,
            E: '=C5+D5',
            F: null,
            G: null,
        },
        {
            rowId: 6,
            A: 'OR',
            B: '3456-PT-01',
            C: null,
            D: null,
            E: '=C6+D6',
            F: null,
            G: null,
        },
        {
            rowId: 7,
            A: null,
            B: '5678-PT-01',
            C: null,
            D: null,
            E: '=C7+D7',
            F: null,
            G: null,
        },
    ],
    mergedCells: [
        {
            startRow: 2,
            startCol: 1,
            endRow: 3,
            endCol: 2,
        },
        {
            startRow: 4,
            startCol: 1,
            endRow: 5,
            endCol: 1,
        },
        {
            startRow: 6,
            startCol: 1,
            endRow: 7,
            endCol: 1,
        },
    ],
};
