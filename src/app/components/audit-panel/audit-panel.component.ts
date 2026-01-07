/**
 * Audit Panel Component - Version history and rollback interface
 */

import { Component, inject, signal, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApiService } from '../../services/api.service';
import { StateService } from '../../services/state.service';
import { NotificationService } from '../../services/notification.service';
import { VersionGroup } from '../../models/api-types';

@Component({
  selector: 'app-audit-panel',
  imports: [CommonModule],
  template: `
    <div class="bg-white rounded-lg shadow-md p-4">
      <div class="flex items-center mb-4">
        <h3 class="text-lg font-bold text-gray-800 flex items-center gap-2">
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"/>
          </svg>
          Audit History
        </h3>
      </div>

      <div class="border-t pt-4">
          @if (stateService.loadingHistory()) {
            <div class="flex justify-center py-8">
              <svg class="animate-spin h-8 w-8 text-blue-500" fill="none" viewBox="0 0 24 24">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
              </svg>
            </div>
          } @else if (auditHistory().length === 0) {
            <div class="text-center py-8 text-gray-400">
              <svg class="w-12 h-12 mx-auto mb-2 opacity-50" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"/>
              </svg>
              <p class="text-sm">No history available</p>
              <p class="text-xs mt-1">Upload a file and make changes to see version history</p>
            </div>
          } @else {
            <div class="max-h-96 overflow-y-auto space-y-3">
              @for (versionGroup of auditHistory(); track versionGroup.version) {
                <div class="border rounded-lg p-3 hover:bg-gray-50 transition-colors">
                  <div class="flex items-center justify-between mb-2">
                    <div>
                      <span class="font-semibold text-gray-800">Version {{ versionGroup.version }}</span>
                      <span class="text-xs text-gray-500 ml-2">
                        {{ formatTimestamp(versionGroup.timestamp) }}
                      </span>
                    </div>
                    <span class="text-xs bg-blue-100 text-blue-700 px-2 py-1 rounded">
                      {{ versionGroup.entries.length }} change(s)
                    </span>
                  </div>

                  <!-- Changes list -->
                  <div class="space-y-1 mb-2">
                    @for (entry of versionGroup.entries.slice(0, 3); track entry.cellReference) {
                      <div class="text-xs text-gray-600 flex items-center gap-2">
                        <span class="font-mono bg-gray-100 px-1 rounded">{{ entry.cellReference }}</span>
                        <span class="text-gray-400">→</span>
                        <span class="line-through text-red-600">{{ formatValue(entry.oldValue) }}</span>
                        <span class="text-green-600">{{ formatValue(entry.newValue) }}</span>
                      </div>
                    }
                    @if (versionGroup.entries.length > 3) {
                      <div class="text-xs text-gray-400 italic">
                        + {{ versionGroup.entries.length - 3 }} more changes
                      </div>
                    }
                  </div>

                  <!-- Action buttons -->
                  <div class="flex gap-2">
                    <button
                      (click)="onPreview(versionGroup.version)"
                      class="text-xs px-3 py-1 bg-purple-100 hover:bg-purple-200 text-purple-700 rounded transition-colors"
                    >
                      Preview
                    </button>
                    <button
                      (click)="onRollback(versionGroup.version)"
                      class="text-xs px-3 py-1 bg-orange-100 hover:bg-orange-200 text-orange-700 rounded transition-colors"
                    >
                      Rollback
                    </button>
                  </div>
                </div>
              }
            </div>
          }

      </div>
    </div>
  `,
  styles: [],
})
export class AuditPanelComponent {
  protected readonly stateService = inject(StateService);
  private readonly apiService = inject(ApiService);
  private readonly notificationService = inject(NotificationService);

  protected auditHistory = signal<VersionGroup[]>([]);

  constructor() {
    // Auto-load and auto-reload audit history when version changes or session is created
    effect(() => {
      const version = this.stateService.version();
      const sessionId = this.stateService.session();

      // Load history if we have a session and version > 0
      if (sessionId && version > 0) {
        this.loadHistory();
      }
    });
  }

  protected loadHistory(): void {
    const sessionId = this.stateService.session();
    if (!sessionId) return;

    this.stateService.setLoadingHistory(true);

    this.apiService.getAuditHistory(sessionId).subscribe({
      next: (response) => {
        this.auditHistory.set(response.history);
        this.stateService.setLoadingHistory(false);
      },
      error: (error) => {
        this.notificationService.error(`Failed to load history: ${error.message}`);
        this.stateService.setLoadingHistory(false);
      },
    });
  }

  protected onPreview(version: number): void {
    this.notificationService.info(`Preview for version ${version} - Feature coming soon`);
    // TODO: Implement preview by fetching version data and loading into grid (readonly mode)
  }

  protected onRollback(version: number): void {
    this.notificationService.info(`Rollback to version ${version} - Feature coming soon`);

    // Logic commented out for now
    /*
    const sessionId = this.stateService.session();
    if (!sessionId) return;

    // Confirmation dialog
    const confirmed = confirm(
      `Are you sure you want to rollback to version ${version}? This will create a new version.`
    );
    if (!confirmed) return;

    this.apiService.revertToVersion(sessionId, version).subscribe({
      next: (response) => {
        this.stateService.updateAfterRevert(response.newVersion, response.rowData);
        this.notificationService.success(`Rolled back to version ${version}. New version: ${response.newVersion}`);

        // Reload history to show the new version
        this.loadHistory();
      },
      error: (error) => {
        this.notificationService.error(`Rollback failed: ${error.message}`);
      },
    });
    */
  }

  protected formatTimestamp(timestamp: string): string {
    const date = new Date(timestamp);
    return date.toLocaleString();
  }

  protected formatValue(value: any): string {
    if (value === null || value === undefined) {
      return '(empty)';
    }
    return String(value);
  }
}
