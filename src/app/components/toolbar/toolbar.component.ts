/**
 * Toolbar Component - Action buttons for grid operations
 */

import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { StateService } from '../../services/state.service';
import { ApiService } from '../../services/api.service';
import { NotificationService } from '../../services/notification.service';

@Component({
  selector: 'app-toolbar',
  imports: [CommonModule],
  template: `
    <div class="bg-white rounded-lg shadow-md p-4 mb-4">
      <div class="flex items-center justify-between flex-wrap gap-4">
        <!-- Left side: Save/Cancel buttons -->
        <div class="flex items-center gap-3">
          <button
            (click)="onSave()"
            [disabled]="!stateService.hasUnsavedChanges() || stateService.saving() || !stateService.session()"
            class="px-4 py-2 bg-blue-600 hover:bg-blue-700 disabled:bg-gray-300 disabled:cursor-not-allowed text-white rounded-lg font-medium transition-colors duration-200 flex items-center gap-2"
          >
            @if (stateService.saving()) {
              <svg class="animate-spin h-4 w-4" fill="none" viewBox="0 0 24 24">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
              </svg>
              Saving...
            } @else {
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7H5a2 2 0 00-2 2v9a2 2 0 002 2h14a2 2 0 002-2V9a2 2 0 00-2-2h-3m-1 4l-3 3m0 0l-3-3m3 3V4"/>
              </svg>
              Save
            }
          </button>

          <button
            (click)="onCancel()"
            [disabled]="!stateService.hasUnsavedChanges() || stateService.saving()"
            class="px-4 py-2 bg-gray-200 hover:bg-gray-300 disabled:bg-gray-100 disabled:cursor-not-allowed text-gray-700 rounded-lg font-medium transition-colors duration-200 flex items-center gap-2"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>
            </svg>
            Cancel
          </button>

          <!-- Unsaved changes indicator -->
          @if (stateService.hasUnsavedChanges()) {
            <span class="text-sm text-amber-600 font-medium flex items-center gap-1">
              <svg class="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
                <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7 4a1 1 0 11-2 0 1 1 0 012 0zm-1-9a1 1 0 00-1 1v4a1 1 0 102 0V6a1 1 0 00-1-1z" clip-rule="evenodd"/>
              </svg>
              {{ stateService.unsavedChangesCount() }} unsaved change(s)
            </span>
          }
        </div>

        <!-- Right side: Export and History buttons -->
        <div class="flex items-center gap-3">
          <button
            (click)="onViewHistory()"
            [disabled]="!stateService.session()"
            class="px-4 py-2 bg-purple-100 hover:bg-purple-200 disabled:bg-gray-100 disabled:cursor-not-allowed text-purple-700 rounded-lg font-medium transition-colors duration-200 flex items-center gap-2"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"/>
            </svg>
            View History
          </button>

          <button
            (click)="onExportPdf()"
            [disabled]="!stateService.session() || stateService.exporting()"
            class="px-4 py-2 bg-green-600 hover:bg-green-700 disabled:bg-gray-300 disabled:cursor-not-allowed text-white rounded-lg font-medium transition-colors duration-200 flex items-center gap-2"
          >
            @if (stateService.exporting()) {
              <svg class="animate-spin h-4 w-4" fill="none" viewBox="0 0 24 24">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
              </svg>
              Exporting...
            } @else {
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 10v6m0 0l-3-3m3 3l3-3m2 8H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"/>
              </svg>
              Export PDF
            }
          </button>
        </div>
      </div>
    </div>
  `,
  styles: [],
})
export class ToolbarComponent {
  protected readonly stateService = inject(StateService);
  private readonly apiService = inject(ApiService);
  private readonly notificationService = inject(NotificationService);

  protected onSave(): void {
    const sessionId = this.stateService.session();
    if (!sessionId) {
      this.notificationService.error('No active session. Please upload a file first.');
      return;
    }

    this.stateService.setSaving(true);

    const saveRequest = {
      rowData: this.stateService.rowData(),
      clientVersion: this.stateService.version(),
    };

    this.apiService.saveChanges(sessionId, saveRequest).subscribe({
      next: (response) => {
        this.stateService.markAsSaved(response.newVersion);
        this.notificationService.success(`Changes saved successfully! Version ${response.newVersion}`);
        this.stateService.setSaving(false);
      },
      error: (error) => {
        this.notificationService.error(`Save failed: ${error.message}`);
        this.stateService.setSaving(false);
      },
    });
  }

  protected onCancel(): void {
    this.stateService.revertToSaved();
    this.notificationService.info('Changes discarded. Reverted to last saved state.');
  }

  protected onViewHistory(): void {
    // This will be handled by emitting an event or using a shared service
    // For now, just show a notification
    this.notificationService.info('History panel will open when audit panel is integrated.');
  }

  protected onExportPdf(): void {
    const sessionId = this.stateService.session();
    if (!sessionId) {
      this.notificationService.error('No active session. Please upload a file first.');
      return;
    }

    this.stateService.setExporting(true);

    this.apiService.exportPdf(sessionId).subscribe({
      next: (blob) => {
        // Create download link
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `report-${new Date().toISOString()}.pdf`;
        link.click();
        window.URL.revokeObjectURL(url);

        this.notificationService.success('PDF exported successfully!');
        this.stateService.setExporting(false);
      },
      error: (error) => {
        this.notificationService.error(`Export failed: ${error.message}`);
        this.stateService.setExporting(false);
      },
    });
  }
}
