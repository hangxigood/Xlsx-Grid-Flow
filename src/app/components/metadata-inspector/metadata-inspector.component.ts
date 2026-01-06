/**
 * Metadata Inspector Component - Cell details sidebar
 */

import { Component, Input, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DataType } from '../../models/grid-types';

export interface SelectedCellInfo {
  cellReference: string;
  dataType: DataType;
  value: any;
  formula?: string;
}

@Component({
  selector: 'app-metadata-inspector',
  imports: [CommonModule],
  template: `
    <div class="bg-white rounded-lg shadow-md p-4 h-fit">
      <h3 class="text-lg font-bold text-gray-800 mb-4 flex items-center gap-2">
        <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"/>
        </svg>
        Cell Inspector
      </h3>

      @if (selectedCell()) {
        <div class="space-y-3">
          <!-- Cell Reference -->
          <div>
            <label class="text-xs font-semibold text-gray-500 uppercase">Cell Reference</label>
            <p class="text-sm font-mono bg-gray-50 px-2 py-1 rounded mt-1">
              {{ selectedCell()!.cellReference }}
            </p>
          </div>

          <!-- Data Type -->
          <div>
            <label class="text-xs font-semibold text-gray-500 uppercase">Data Type</label>
            <div class="flex items-center gap-2 mt-1">
              <span class="text-sm">{{ getDataTypeIcon(selectedCell()!.dataType) }}</span>
              <span class="text-sm font-medium capitalize">{{ selectedCell()!.dataType }}</span>
            </div>
          </div>

          <!-- Value -->
          <div>
            <label class="text-xs font-semibold text-gray-500 uppercase">Value</label>
            <p class="text-sm bg-gray-50 px-2 py-1 rounded mt-1 break-words">
              {{ formatValue(selectedCell()!.value) }}
            </p>
          </div>

          <!-- Formula (if applicable) -->
          @if (selectedCell()!.formula) {
            <div>
              <label class="text-xs font-semibold text-gray-500 uppercase">Formula</label>
              <p class="text-sm font-mono bg-blue-50 px-2 py-1 rounded mt-1 break-words text-blue-700">
                {{ selectedCell()!.formula }}
              </p>
            </div>
          }
        </div>
      } @else {
        <div class="text-center py-8 text-gray-400">
          <svg class="w-12 h-12 mx-auto mb-2 opacity-50" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"/>
          </svg>
          <p class="text-sm">Select a cell to view details</p>
        </div>
      }
    </div>
  `,
  styles: [],
})
export class MetadataInspectorComponent {
  protected selectedCell = signal<SelectedCellInfo | null>(null);

  @Input()
  set cellInfo(value: SelectedCellInfo | null) {
    this.selectedCell.set(value);
  }

  protected getDataTypeIcon(dataType: DataType): string {
    switch (dataType) {
      case 'text':
        return '📝';
      case 'number':
        return '🔢';
      case 'date':
        return '📅';
      case 'boolean':
        return '✓';
      case 'formula':
        return 'ƒ';
      default:
        return '•';
    }
  }

  protected formatValue(value: any): string {
    if (value === null || value === undefined) {
      return '(empty)';
    }

    // Format numbers with proper decimals
    if (typeof value === 'number') {
      return value.toLocaleString(undefined, {
        maximumFractionDigits: 10,
        useGrouping: true
      });
    }

    // Format dates nicely
    if (value instanceof Date) {
      return value.toLocaleDateString();
    }

    // Format booleans
    if (typeof value === 'boolean') {
      return value ? 'TRUE' : 'FALSE';
    }

    return String(value);
  }
}
