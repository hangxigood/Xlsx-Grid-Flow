/**
 * Grid Wrapper Component - AG-Grid integration
 */

import { Component, inject, OnInit, Output, EventEmitter, effect, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AgGridAngular } from 'ag-grid-angular';
import {
    ColDef,
    GridApi,
    GridReadyEvent,
    CellValueChangedEvent,
    CellClickedEvent,
    ModuleRegistry,
    AllCommunityModule,
    themeAlpine,
} from 'ag-grid-community';
import { StateService } from '../../services/state.service';
import { FormulaService } from '../../services/formula.service';
import { NotificationService } from '../../services/notification.service';
import { ColumnDef, GridRow } from '../../models/grid-types';
import { SelectedCellInfo } from '../metadata-inspector/metadata-inspector.component';

// Register AG Grid modules
ModuleRegistry.registerModules([AllCommunityModule]);

@Component({
    selector: 'app-grid-wrapper',
    imports: [CommonModule, AgGridAngular],
    template: `
    <div class="bg-white rounded-lg shadow-md p-4">
      <ag-grid-angular
        [theme]="theme"
        style="width: 100%; min-height: 200px;"
        [domLayout]="'autoHeight'"
        [rowData]="rowData()"
        [columnDefs]="columnDefs()"
        [defaultColDef]="defaultColDef"
        [singleClickEdit]="true"
        [stopEditingWhenCellsLoseFocus]="true"
        [suppressRowTransform]="true"
        (gridReady)="onGridReady($event)"
        (cellValueChanged)="onCellValueChanged($event)"
        (cellClicked)="onCellClicked($event)"
      />
    </div>
  `,
    styles: [],
})
export class GridWrapperComponent implements OnInit {
    protected readonly stateService = inject(StateService);
    protected readonly formulaService = inject(FormulaService);
    protected readonly notificationService = inject(NotificationService);
    protected theme = themeAlpine;

    @Output() cellSelected = new EventEmitter<SelectedCellInfo | null>();

    private gridApi?: GridApi;
    protected rowData = this.stateService.rowData;
    protected columnDefs = signal<ColDef[]>(
        this.stateService.template().columnDefs.map((col) => this.mapToAgGridColumn(col))
    );

    protected defaultColDef: ColDef = {
        sortable: false,
        filter: false,
        resizable: true,
        editable: false, // Will be overridden by column-specific settings
    };

    constructor() {
        // Watch for template changes and update column definitions
        effect(() => {
            const template = this.stateService.template();
            this.columnDefs.set(template.columnDefs.map((col) => this.mapToAgGridColumn(col)));
        });

        // REACTIVE PATTERN: Watch for version changes (Save operations)
        // This replaces the manual 'saveCompleted' event chain.
        // When version updates, we know data was saved, so we refresh styling.
        effect(() => {
            // Register dependency on version
            const version = this.stateService.version();

            // Use untracked for the side effect to avoid loops (though safe here)
            // We check if gridApi exists because this effect runs immediately on init
            if (this.gridApi) {
                this.gridApi.refreshCells({ force: true });
            }
        });
    }

    ngOnInit(): void { }

    protected onGridReady(params: GridReadyEvent): void {
        this.gridApi = params.api;
        this.gridApi.sizeColumnsToFit();
    }

    protected onCellValueChanged(event: CellValueChangedEvent): void {
        const rowId = event.data.rowId;
        const field = event.colDef.field;
        const newValue = event.newValue;

        if (field) {
            this.stateService.updateCellValue(rowId, field, newValue);
        }

        // Refresh the cell to apply unsaved changes styling
        if (this.gridApi) {
            this.gridApi.refreshCells({ force: true });
        }
    }

    protected onCellClicked(event: CellClickedEvent): void {
        const colDef = this.stateService
            .template()
            .columnDefs.find((col) => col.field === event.colDef.field);

        if (!colDef) {
            this.cellSelected.emit(null);
            return;
        }

        // Check if this cell contains a formula
        const rowIndex = event.rowIndex! + 1; // +1 because HyperFormula row 0 is headers
        const formula = this.formulaService.getFormula(
            rowIndex,
            colDef.field,
            this.stateService.template().columnDefs
        );

        const cellInfo: SelectedCellInfo = {
            cellReference: `${event.colDef.field}${event.rowIndex! + 2}`, // +2 because row 1 is header
            dataType: formula ? 'formula' : colDef.dataType,
            value: event.value,
            formula: formula || undefined,
        };

        this.cellSelected.emit(cellInfo);
    }

    /**
     * Map our ColumnDef to AG-Grid ColDef
     */
    private mapToAgGridColumn(col: ColumnDef): ColDef {
        const agCol: ColDef = {
            field: col.field,
            headerName: col.headerName,
            editable: (params) => {
                if (!params.node) return false;
                // Only the top-left cell of a merged range is editable
                if (typeof col.editable === 'boolean' && !col.editable) return false;

                const rowIndex = params.node.rowIndex! + 2; // Convert to Excel row num
                const colIndex = this.stateService.template().columnDefs.findIndex(c => c.field === col.field);
                const excelColIndex = colIndex + 1; // Convert to 1-based for comparison

                const merge = this.getMergeRule(rowIndex, colIndex);
                if (merge && (rowIndex !== merge.startRow || excelColIndex !== merge.startCol)) {
                    return false;
                }

                return !!col.editable;
            },
            rowSpan: (params) => {
                if (!params.node) return 1;
                const rowIndex = params.node.rowIndex! + 2; // Convert to Excel row num
                const colIndex = this.stateService.template().columnDefs.findIndex(c => c.field === col.field);
                const excelColIndex = colIndex + 1; // Convert to 1-based for comparison

                const merge = this.getMergeRule(rowIndex, colIndex);
                if (merge && rowIndex === merge.startRow && excelColIndex === merge.startCol) {
                    return merge.endRow - merge.startRow + 1;
                }
                return 1;
            },
            colSpan: (params) => {
                if (!params.node) return 1;
                const rowIndex = params.node.rowIndex! + 2; // Convert to Excel row num
                const colIndex = this.stateService.template().columnDefs.findIndex(c => c.field === col.field);
                const excelColIndex = colIndex + 1; // Convert to 1-based for comparison

                const merge = this.getMergeRule(rowIndex, colIndex);
                if (merge && rowIndex === merge.startRow && excelColIndex === merge.startCol) {
                    return merge.endCol - merge.startCol + 1;
                }
                return 1;
            },
            cellClass: (params) => {
                const classes: string[] = [];
                if (!params.node) return classes;

                const rowIndex = params.node.rowIndex! + 2;
                const colIndex = this.stateService.template().columnDefs.findIndex(c => c.field === col.field);
                const excelColIndex = colIndex + 1; // Convert to 1-based for comparison

                // Readonly styling
                if (!col.editable) {
                    classes.push('cell-readonly');
                }

                // Unsaved changes styling
                if (this.isCellModified(params.data.rowId, col.field)) {
                    classes.push('cell-unsaved');
                }

                // Merged cell base styling
                const merge = this.getMergeRule(rowIndex, colIndex);
                if (merge) {
                    classes.push('cell-merged');
                    if (rowIndex === merge.startRow && excelColIndex === merge.startCol) {
                        classes.push('cell-merge-master');
                    }
                }

                return classes;
            },
        };

        // Type-specific configurations
        switch (col.dataType) {
            case 'number':
                agCol.valueParser = (params) => {
                    const value = params.newValue;
                    if (value === null || value === '') return null;
                    const num = Number(value);
                    if (isNaN(num)) {
                        this.notificationService.warning(
                            `Invalid number format: "${value}". Please enter a valid number.`
                        );
                        return params.oldValue;
                    }
                    return num;
                };
                agCol.cellStyle = { textAlign: 'right' };
                break;

            case 'date':
                agCol.cellEditor = 'agDateCellEditor';
                agCol.valueFormatter = (params) => {
                    if (!params.value) return '';
                    const date = new Date(params.value);
                    return date.toLocaleDateString();
                };
                agCol.valueParser = (params) => {
                    // Handle date input from the editor
                    if (!params.newValue) return null;
                    // AG-Grid date editor returns ISO string format
                    return params.newValue;
                };
                break;

            case 'formula':
                agCol.editable = false;
                agCol.cellStyle = { fontStyle: 'italic', color: '#6366f1' };
                agCol.valueFormatter = (params) => {
                    // Display the calculated value instead of the formula string
                    if (!params.value || !params.node) return '';

                    const rowIndex = params.node.rowIndex! + 1; // +1 because row 0 is headers in HyperFormula

                    const calculatedValue = this.formulaService.getCellCalculatedValue(
                        rowIndex,
                        col.field,
                        this.stateService.template().columnDefs
                    );

                    if (calculatedValue === null || calculatedValue === undefined) return '';

                    return String(calculatedValue);
                };
                break;

            case 'boolean':
                agCol.cellEditor = 'agCheckboxCellEditor';
                agCol.valueParser = (params) => {
                    const value = params.newValue;

                    // Handle null/empty
                    if (value === null || value === undefined || value === '') {
                        return null;
                    }

                    // Checkbox editor returns boolean, just pass it through
                    if (typeof value === 'boolean') {
                        return value;
                    }

                    // If somehow a non-boolean value comes through, reject it
                    this.notificationService.warning(
                        `Invalid boolean value. Please use the checkbox to set true/false.`
                    );
                    return params.oldValue;
                };
                agCol.cellRenderer = (params: any) => {
                    // null/undefined = empty (no value set)
                    if (params.value === null || params.value === undefined) {
                        return '';
                    }
                    // true = ✓, false = ✗
                    return params.value === true ? '✓' : '✗';
                };
                break;
        }

        return agCol;
    }

    /**
     * Get merge rule for a specific cell coordinate
     * Note: colIndex is 0-based (array index), but merged cell data uses 1-based (Excel) indexing
     */
    private getMergeRule(rowNum: number, colIndex: number) {
        // Convert 0-based colIndex to 1-based for comparison with backend data
        const excelColIndex = colIndex + 1;
        return this.stateService.template().mergedCells.find(m =>
            rowNum >= m.startRow && rowNum <= m.endRow &&
            excelColIndex >= m.startCol && excelColIndex <= m.endCol
        );
    }

    /**
     * Check if a cell has been modified (unsaved changes)
     */
    private isCellModified(rowId: number, field: string): boolean {
        const currentRows = this.stateService.rowData();
        const savedRows = this.stateService['savedRowData'](); // Access private signal for comparison

        const currentRow = currentRows.find((r) => r.rowId === rowId);
        const savedRow = savedRows.find((r) => r.rowId === rowId);

        if (!currentRow || !savedRow) return false;

        return currentRow[field] !== savedRow[field];
    }

    /**
     * Refresh all grid cells (used after save to update styling)
     */
    public refreshCells(): void {
        if (this.gridApi) {
            this.gridApi.refreshCells({ force: true });
        }
    }
}
