/**
 * Example template data for initial application load
 * Demonstrates all key features: editable/readonly columns, formulas, validation, merged cells
 */

import { Template } from '../models/grid-types';

export const EXAMPLE_TEMPLATE: Template = {
    id: 'example',
    filename: 'Example Template.xlsx',
    columnDefs: [
        {
            field: 'A',
            headerName: 'ID',
            dataType: 'number',
            editable: false, // ReadOnly
        },
        {
            field: 'B',
            headerName: 'Product Name',
            dataType: 'text',
            editable: true,
        },
        {
            field: 'C',
            headerName: 'Quantity',
            dataType: 'number',
            editable: true,
        },
        {
            field: 'D',
            headerName: 'Unit Price',
            dataType: 'number',
            editable: true,
        },
        {
            field: 'E',
            headerName: 'Total',
            dataType: 'formula',
            editable: false, // Formula cells are readonly
        },
        {
            field: 'F',
            headerName: 'Order Date',
            dataType: 'date',
            editable: true,
        },
        {
            field: 'G',
            headerName: 'Status',
            dataType: 'text',
            editable: true,
        },
    ],
    rowData: [
        {
            rowId: 2,
            A: 1,
            B: 'Laptop',
            C: 5,
            D: 999.99,
            E: '=C2*D2', // Formula: Quantity * Unit Price
            F: '2026-01-01',
            G: 'Shipped',
        },
        {
            rowId: 3,
            A: 2,
            B: 'Mouse',
            C: 25,
            D: 29.99,
            E: '=C3*D3',
            F: '2026-01-02',
            G: 'Shipped',
        },
        {
            rowId: 4,
            A: 3,
            B: 'Keyboard',
            C: 15,
            D: 79.99,
            E: '=C4*D4',
            F: '2026-01-03',
            G: 'Delivered',
        },
        {
            rowId: 5,
            A: 4,
            B: 'Monitor',
            C: 8,
            D: 299.99,
            E: '=C5*D5',
            F: '2026-01-04',
            G: 'Pending',
        },
        {
            rowId: 6,
            A: 5,
            B: 'Headphones',
            C: 30,
            D: 49.99,
            E: '=C6*D6',
            F: '2026-01-05',
            G: 'Shipped',
        },
    ],
    mergedCells: [
        {
            startRow: 2, // Laptop (rowId 2) status
            endRow: 3,   // Mouse (rowId 3) status
            startCol: 6, // Column G (Status)
            endCol: 6,   // Column G (Status)
        },
    ],
};
