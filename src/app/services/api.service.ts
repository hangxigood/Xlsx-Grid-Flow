/**
 * API Service - HTTP communication layer with backend
 */

import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import {
    UploadResponse,
    SaveRequest,
    SaveResponse,
    RevertResponse,
    AuditHistoryResponse,
    ErrorResponse,
} from '../models/api-types';

@Injectable({
    providedIn: 'root',
})
export class ApiService {
    private readonly baseUrl = '/api';

    constructor(private http: HttpClient) { }

    /**
     * Upload Excel file and initialize session
     */
    uploadTemplate(file: File): Observable<UploadResponse> {
        const formData = new FormData();
        formData.append('file', file);

        return this.http
            .post<UploadResponse>(`${this.baseUrl}/template/upload`, formData)
            .pipe(catchError(this.handleError));
    }

    /**
     * Save grid changes and generate new version
     */
    saveChanges(sessionId: string, request: SaveRequest): Observable<SaveResponse> {
        return this.http
            .post<SaveResponse>(`${this.baseUrl}/session/${sessionId}/save`, request, {
                headers: new HttpHeaders({ 'Content-Type': 'application/json' }),
            })
            .pipe(catchError(this.handleError));
    }

    /**
     * Revert session to a previous version
     */
    revertToVersion(sessionId: string, version: number): Observable<RevertResponse> {
        return this.http
            .post<RevertResponse>(`${this.baseUrl}/session/${sessionId}/revert/${version}`, {})
            .pipe(catchError(this.handleError));
    }

    /**
     * Fetch full audit history for session
     */
    getAuditHistory(sessionId: string): Observable<AuditHistoryResponse> {
        return this.http
            .get<AuditHistoryResponse>(`${this.baseUrl}/session/${sessionId}/audit`)
            .pipe(catchError(this.handleError));
    }

    /**
     * Download PDF report
     */
    exportPdf(sessionId: string): Observable<Blob> {
        return this.http
            .get(`${this.baseUrl}/session/${sessionId}/export/pdf`, {
                responseType: 'blob',
            })
            .pipe(catchError(this.handleError));
    }

    /**
     * Handle HTTP errors
     */
    private handleError(error: any): Observable<never> {
        let errorMessage = 'An unknown error occurred';

        if (error.error instanceof ErrorEvent) {
            // Client-side error
            errorMessage = `Error: ${error.error.message}`;
        } else {
            // Server-side error
            const errorResponse = error.error as ErrorResponse;
            if (errorResponse && errorResponse.message) {
                errorMessage = errorResponse.message;
            } else {
                errorMessage = `Error Code: ${error.status}\nMessage: ${error.message}`;
            }
        }

        console.error('API Error:', errorMessage, error);
        return throwError(() => new Error(errorMessage));
    }
}
