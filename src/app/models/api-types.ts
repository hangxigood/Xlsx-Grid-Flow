/**
 * API request and response DTOs
 */

import { ColumnDef, GridRow, MergedCell, CellValue } from './grid-types';

/**
 * Row data structure from the backend API (with nested cells)
 */
export interface ApiGridRow {
    rowId: number;
    cells: Record<string, CellValue>; // e.g., { "A": "Laptop", "B": 1, ... }
}

/**
 * Response from template upload endpoint
 */
export interface UploadResponse {
    sessionId: string;
    expiresAt: string; // ISO 8601
    template: {
        filename: string;
        columnDefs: ColumnDef[];
        rowData: ApiGridRow[]; // Backend returns nested structure
        mergedCells: MergedCell[];
    };
}

/**
 * Request for saving grid changes
 */
export interface SaveRequest {
    rowData: GridRow[];
    clientVersion: number; // Current version known by client (Optimistic Concurrency Control)
}

/**
 * Response from save operation
 */
export interface SaveResponse {
    newVersion: number;
    timestamp: string; // ISO 8601
    auditEntries: AuditLogEntryDto[]; // Only the changes introduced in this version
}

/**
 * Response from revert operation
 */
export interface RevertResponse {
    newVersion: number;
    rowData: GridRow[];
    auditEntries: AuditLogEntryDto[]; // Entries representing only the state change of this revert
}

/**
 * Single audit log entry
 */
export interface AuditLogEntryDto {
    version: number;
    timestamp: string; // ISO 8601
    cellReference: string; // e.g., "B4"
    oldValue: string | number | boolean | null;
    newValue: string | number | boolean | null;
}

/**
 * Response from audit history endpoint
 */
export interface AuditHistoryResponse {
    sessionId: string;
    history: VersionGroup[];
}

/**
 * Grouped audit entries by version
 */
export interface VersionGroup {
    version: number;
    timestamp: string;
    entries: AuditLogEntryDto[];
}

/**
 * Standard error response format
 */
export interface ErrorResponse {
    errorCode: string;
    message: string;
    details?: Record<string, unknown>; // Optional object with specific validation errors
}
