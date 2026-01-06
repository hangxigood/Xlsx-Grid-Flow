/**
 * Upload Component - File upload interface with drag-drop
 */

import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApiService } from '../../services/api.service';
import { StateService } from '../../services/state.service';
import { NotificationService } from '../../services/notification.service';

@Component({
    selector: 'app-upload',
    imports: [CommonModule],
    template: `
    <div class="bg-white rounded-lg shadow-md p-6 mb-6">
      <h2 class="text-2xl font-bold text-gray-800 mb-4">Upload Excel Template</h2>
      
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

      <!-- Download Example Template Button -->
      <div class="mt-4 flex justify-center">
        <button
          (click)="downloadExampleTemplate($event)"
          class="inline-flex items-center gap-2 px-4 py-2 bg-gray-100 hover:bg-gray-200 text-gray-700 rounded-lg transition-colors duration-200"
        >
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 10v6m0 0l-3-3m3 3l3-3m2 8H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"/>
          </svg>
          Download Example Template
        </button>
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
                const templateWithId = { ...response.template, id: response.sessionId };
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

        // For now, show a notification that this feature requires a static file
        // In production, this would download a pre-made example.xlsx file from /public
        this.notificationService.info('Example template download will be available once the static file is added to /public folder.');
    }
}
