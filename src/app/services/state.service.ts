/**
 * State Service - Application state management
 */

import { Injectable, signal, computed, inject } from '@angular/core';
import { Template, GridRow } from '../models/grid-types';
import { EXAMPLE_TEMPLATE } from '../config/example-data';
import { FormulaService } from './formula.service';

@Injectable({
    providedIn: 'root',
})
export class StateService {
    // Core state signals
    private currentTemplate = signal<Template>(EXAMPLE_TEMPLATE);
    private sessionId = signal<string | null>(null);
    private currentVersion = signal<number>(0);
    private savedRowData = signal<GridRow[]>(EXAMPLE_TEMPLATE.rowData);
    private currentRowData = signal<GridRow[]>(EXAMPLE_TEMPLATE.rowData);

    // Loading states
    private isUploading = signal<boolean>(false);
    private isSaving = signal<boolean>(false);
    private isExporting = signal<boolean>(false);
    private isLoadingHistory = signal<boolean>(false);

    // Computed signals
    readonly template = this.currentTemplate.asReadonly();
    readonly session = this.sessionId.asReadonly();
    readonly version = this.currentVersion.asReadonly();
    readonly rowData = this.currentRowData.asReadonly();

    readonly uploading = this.isUploading.asReadonly();
    readonly saving = this.isSaving.asReadonly();
    readonly exporting = this.isExporting.asReadonly();
    readonly loadingHistory = this.isLoadingHistory.asReadonly();

    // Computed: Check if there are unsaved changes
    readonly hasUnsavedChanges = computed(() => {
        const current = this.currentRowData();
        const saved = this.savedRowData();
        return JSON.stringify(current) !== JSON.stringify(saved);
    });

    // Computed: Count of unsaved changes
    readonly unsavedChangesCount = computed(() => {
        const current = this.currentRowData();
        const saved = this.savedRowData();
        let count = 0;

        for (let i = 0; i < current.length; i++) {
            const currentRow = current[i];
            const savedRow = saved[i];

            if (!savedRow) {
                count++;
                continue;
            }

            for (const key in currentRow) {
                if (key !== 'rowId' && currentRow[key] !== savedRow[key]) {
                    count++;
                }
            }
        }

        return count;
    });

    constructor() {
        // Initialize with example data
        this.loadExampleData();
    }

    private readonly formulaService = inject(FormulaService);

    /**
     * Load example data (initial state)
     */
    loadExampleData(): void {
        this.currentTemplate.set(EXAMPLE_TEMPLATE);
        this.sessionId.set(null);
        this.currentVersion.set(0);

        // Initialize formulas with example data
        this.formulaService.initializeFormulas(EXAMPLE_TEMPLATE.columnDefs, EXAMPLE_TEMPLATE.rowData);

        // Get calculated values (formulas will be evaluated)
        const calculatedData = this.formulaService.getCalculatedData(
            EXAMPLE_TEMPLATE.columnDefs,
            EXAMPLE_TEMPLATE.rowData.length
        );

        this.savedRowData.set(JSON.parse(JSON.stringify(calculatedData)));
        this.currentRowData.set(JSON.parse(JSON.stringify(calculatedData)));
    }

    /**
     * Load uploaded template data
     */
    loadUploadedTemplate(template: Template, sessionId: string): void {
        this.currentTemplate.set(template);
        this.sessionId.set(sessionId);
        this.currentVersion.set(0);

        // Initialize formulas with uploaded data
        this.formulaService.rebuildFormulas(template.columnDefs, template.rowData);

        // Get calculated values
        const calculatedData = this.formulaService.getCalculatedData(
            template.columnDefs,
            template.rowData.length
        );

        this.savedRowData.set(JSON.parse(JSON.stringify(calculatedData)));
        this.currentRowData.set(JSON.parse(JSON.stringify(calculatedData)));
    }

    /**
     * Update a single cell value
     */
    updateCellValue(rowId: number, field: string, value: any): void {
        const template = this.currentTemplate();

        // Update the cell in HyperFormula (this will trigger recalculation)
        // rowId is 1-based, but we need 0-based for the data array
        this.formulaService.updateCell(rowId, field, value, template.columnDefs);

        // Get the recalculated data
        const calculatedData = this.formulaService.getCalculatedData(
            template.columnDefs,
            this.currentRowData().length
        );

        this.currentRowData.set(calculatedData);
    }

    /**
     * Update entire row data (used after grid edits)
     */
    updateRowData(rowData: GridRow[]): void {
        this.currentRowData.set(JSON.parse(JSON.stringify(rowData)));
    }

    /**
     * Mark current state as saved (after successful save operation)
     */
    markAsSaved(newVersion: number): void {
        this.currentVersion.set(newVersion);
        this.savedRowData.set(JSON.parse(JSON.stringify(this.currentRowData())));
    }

    /**
     * Revert to last saved state (cancel changes)
     */
    revertToSaved(): void {
        const template = this.currentTemplate();
        const savedData = this.savedRowData();

        // Rebuild the HyperFormula instance with the saved data
        // This ensures that any cell edits in the formula engine are also reverted
        this.formulaService.rebuildFormulas(template.columnDefs, savedData);

        // Use a deep copy of the saved data directly
        // We don't call getCalculatedData() here because savedData already has the correct structure
        this.currentRowData.set(JSON.parse(JSON.stringify(savedData)));
    }

    /**
     * Reset to example data
     */
    resetToExample(): void {
        this.loadExampleData();
    }

    /**
     * Load historical version data (for preview)
     */
    loadHistoricalData(rowData: GridRow[]): void {
        this.currentRowData.set(JSON.parse(JSON.stringify(rowData)));
    }

    /**
     * Update session after revert operation
     */
    updateAfterRevert(newVersion: number, rowData: GridRow[]): void {
        this.currentVersion.set(newVersion);
        this.savedRowData.set(JSON.parse(JSON.stringify(rowData)));
        this.currentRowData.set(JSON.parse(JSON.stringify(rowData)));
    }

    // Loading state setters
    setUploading(value: boolean): void {
        this.isUploading.set(value);
    }

    setSaving(value: boolean): void {
        this.isSaving.set(value);
    }

    setExporting(value: boolean): void {
        this.isExporting.set(value);
    }

    setLoadingHistory(value: boolean): void {
        this.isLoadingHistory.set(value);
    }
}
