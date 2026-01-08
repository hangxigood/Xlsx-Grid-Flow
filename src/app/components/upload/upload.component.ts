/**
 * Upload Component - File upload interface with drag-drop
 */

import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApiService } from '../../services/api.service';
import { StateService } from '../../services/state.service';
import { NotificationService } from '../../services/notification.service';
import { GridRow } from '../../models/grid-types';
import { fromApiGridRows } from '../../utils/data-transform.utils';

@Component({
  selector: 'app-upload',
  imports: [CommonModule],
  template: `
    <div class="bg-white rounded-lg shadow-md p-6 mb-6">
      <h2 class="text-2xl font-bold text-gray-800 mb-4">Upload Excel Template</h2>
      
      <!-- Download Example Template Section -->
      <div class="mb-6 p-4 bg-blue-50 border border-blue-200 rounded-lg">
        <div class="flex items-start gap-3 mb-3">
          <svg class="w-5 h-5 text-blue-600 mt-0.5 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"/>
          </svg>
          <div class="flex-1">
            <p class="text-sm font-medium text-blue-900 mb-1">
              Please use our example template format
            </p>
            <p class="text-xs text-blue-700">
              Download the template below to ensure your Excel file meets our specific requirements for columns, data types, and formulas.
            </p>
          </div>
        </div>
        <button
          (click)="downloadExampleTemplate($event)"
          class="inline-flex items-center gap-2 px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg transition-colors duration-200 shadow-sm"
        >
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 10v6m0 0l-3-3m3 3l3-3m2 8H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"/>
          </svg>
          Download Example Template
        </button>
      </div>

      <!-- Drag and Drop Zone -->
      <div
        (drop)="onDrop($event)"
        (dragover)="onDragOver($event)"
        (dragleave)="onDragLeave($event)"
        [class.border-blue-500]="isDragging()"
        [class.bg-blue-50]="isDragging()"
        class="border-2 border-dashed border-gray-300 rounded-lg p-8 text-center transition-all duration-200 hover:border-gray-400 cursor-pointer"
        (click)="fileInput.click()"
      >
        @if (isUploading()) {
          <div class="flex flex-col items-center">
            <svg class="animate-spin h-12 w-12 text-blue-500 mb-4" fill="none" viewBox="0 0 24 24">
              <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
              <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
            </svg>
            <p class="text-gray-600 font-medium">Uploading...</p>
          </div>
        } @else {
          <div class="flex flex-col items-center">
            <svg class="h-12 w-12 text-gray-400 mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12"/>
            </svg>
            <p class="text-lg font-medium text-gray-700 mb-2">
              Drag and drop your Excel file here
            </p>
            <p class="text-sm text-gray-500 mb-4">or click to browse</p>
            <p class="text-xs text-gray-400">Supports .xlsx files only</p>
          </div>
        }
        
        <input
          #fileInput
          type="file"
          accept=".xlsx"
          (change)="onFileSelected($event)"
          class="hidden"
        />
      </div>
    </div>
  `,
  styles: [],
})
export class UploadComponent {
  private apiService = inject(ApiService);
  private stateService = inject(StateService);
  private notificationService = inject(NotificationService);

  protected isDragging = signal(false);
  protected isUploading = signal(false);

  protected onDragOver(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging.set(true);
  }

  protected onDragLeave(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging.set(false);
  }

  protected onDrop(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging.set(false);

    const files = event.dataTransfer?.files;
    if (files && files.length > 0) {
      this.handleFile(files[0]);
    }
  }

  protected onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.handleFile(input.files[0]);
    }
  }

  private handleFile(file: File): void {
    // Validate file type
    if (!file.name.endsWith('.xlsx')) {
      this.notificationService.error('Invalid file type. Please upload an .xlsx file.');
      return;
    }

    // Validate file size (max 10MB)
    const maxSize = 10 * 1024 * 1024; // 10MB
    if (file.size > maxSize) {
      this.notificationService.error('File size exceeds 10MB limit.');
      return;
    }

    this.uploadFile(file);
  }

  private uploadFile(file: File): void {
    this.isUploading.set(true);
    this.stateService.setUploading(true);

    this.apiService.uploadTemplate(file).subscribe({
      next: (response) => {
        // Transform the nested API structure to flat GridRow structure
        const flatRowData: GridRow[] = fromApiGridRows(response.template.rowData);

        // Create the template with flattened row data
        const templateWithId = {
          id: response.sessionId,
          filename: response.template.filename,
          columnDefs: response.template.columnDefs,
          rowData: flatRowData,
          mergedCells: response.template.mergedCells
        };

        this.stateService.loadUploadedTemplate(templateWithId, response.sessionId);
        this.notificationService.success('File uploaded successfully!');
        this.isUploading.set(false);
        this.stateService.setUploading(false);
      },
      error: (error) => {
        this.notificationService.error(`Upload failed: ${error.message}`);
        this.isUploading.set(false);
        this.stateService.setUploading(false);
      },
    });
  }

  protected downloadExampleTemplate(event: Event): void {
    event.stopPropagation();

    // Create a temporary link to trigger the download
    const link = document.createElement('a');
    link.href = 'testbook.xlsx';
    link.download = 'testbook.xlsx';
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
  }
}
