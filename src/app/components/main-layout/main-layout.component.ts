/**
 * Main Layout Component - Root SPA container
 */

import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { UploadComponent } from '../upload/upload.component';
import { GridWrapperComponent } from '../grid-wrapper/grid-wrapper.component';
import { ToolbarComponent } from '../toolbar/toolbar.component';
import { MetadataInspectorComponent, SelectedCellInfo } from '../metadata-inspector/metadata-inspector.component';
import { AuditPanelComponent } from '../audit-panel/audit-panel.component';
import { StateService } from '../../services/state.service';

@Component({
  selector: 'app-main-layout',
  imports: [
    CommonModule,
    UploadComponent,
    GridWrapperComponent,
    ToolbarComponent,
    MetadataInspectorComponent,
    AuditPanelComponent,
  ],
  template: `
    <div class="min-h-screen bg-gray-50">
      <!-- Header -->
      <header class="bg-white shadow-sm border-b border-gray-200">
        <div class="container mx-auto px-4 py-4">
          <div class="flex items-center justify-between">
            <div>
              <h1 class="text-2xl font-bold text-gray-800">Xlsx-Grid-Flow</h1>
              <p class="text-sm text-gray-500">Excel Template Data Entry Interface</p>
            </div>
            
            <!-- Session Info -->
            @if (stateService.session()) {
              <div class="text-right">
                <p class="text-sm font-medium text-gray-700">
                  {{ stateService.template().filename }}
                </p>
                <p class="text-xs text-gray-500">
                  Version {{ stateService.version() }}
                  @if (stateService.session()) {
                    <span class="ml-2 text-green-600">● Active</span>
                  }
                </p>
              </div>
            } @else {
              <div class="text-right">
                <p class="text-sm font-medium text-gray-700">Example Mode</p>
                <p class="text-xs text-gray-500">Upload a file to start a session</p>
              </div>
            }
          </div>
        </div>
      </header>

      <!-- Main Content -->
      <main class="container mx-auto px-4 py-6">
        <!-- Grid Section -->
        <div class="grid-section">
          <!-- Toolbar -->
          <app-toolbar />

          <!-- Grid and Metadata Inspector -->
          <div class="flex items-start gap-4 mb-6">
            <div class="flex-1">
              <app-grid-wrapper (cellSelected)="onCellSelected($event)" />
            </div>
            <div class="w-80">
              <app-metadata-inspector [cellInfo]="selectedCell()" />
            </div>
          </div>

          <!-- Audit Panel -->
          <app-audit-panel />
        </div>

        <!-- Upload Section (shown when no file uploaded or in example mode) -->
        @if (showUploadSection()) {
          <div class="mt-8 pt-8 border-t border-gray-200">
            <app-upload />
          </div>
        }
      </main>

      <!-- Footer -->
      <footer class="bg-white border-t border-gray-200 mt-12">
        <div class="container mx-auto px-4 py-4">
          <p class="text-center text-sm text-gray-500">
            Xlsx-Grid-Flow - Transform Excel templates into controlled web-based data entry interfaces
          </p>
        </div>
      </footer>
    </div>
  `,
  styles: [],
})
export class MainLayoutComponent {
  protected readonly stateService = inject(StateService);
  protected selectedCell = signal<SelectedCellInfo | null>(null);
  protected showUploadSection = signal(true);

  protected onCellSelected(cellInfo: SelectedCellInfo | null): void {
    this.selectedCell.set(cellInfo);
  }
}
