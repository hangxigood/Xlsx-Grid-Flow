# Formula Implementation Guide

## Overview
This document describes the client-side formula calculation implementation using HyperFormula in the Xlsx-Grid-Flow application.

## What Was Implemented

### 1. FormulaService (`src/app/services/formula.service.ts`)
A new Angular service that wraps HyperFormula functionality:

**Key Features:**
- **Formula Initialization**: Converts grid data to HyperFormula's 2D array format
- **Automatic Recalculation**: When a cell value changes, all dependent formulas are recalculated
- **Formula Detection**: Can check if a cell contains a formula and retrieve the formula string
- **Type Safety**: Handles HyperFormula's error types and converts them to our application's data model

**Main Methods:**
- `initializeFormulas()`: Sets up HyperFormula with initial data
- `updateCell()`: Updates a cell value and triggers recalculation
- `getCalculatedData()`: Retrieves all calculated values from HyperFormula
- `getFormula()`: Gets the formula string for a specific cell
- `rebuildFormulas()`: Rebuilds the formula engine when template changes

### 2. StateService Integration
The StateService now uses FormulaService to manage formula calculations:

**Changes:**
- Formulas are initialized when loading example data or uploaded templates
- Cell updates trigger automatic recalculation of dependent formulas
- Calculated values are stored in the state instead of raw formula strings

**Flow:**
1. Template is loaded → FormulaService initializes with column definitions and row data
2. User edits a cell → FormulaService updates the cell and recalculates dependents
3. State is updated with calculated values → Grid displays the results

### 3. GridWrapper Component Updates
The grid component now integrates with FormulaService:

**Changes:**
- Injects FormulaService to detect formulas in cells
- When a cell is clicked, checks if it contains a formula
- Passes both the calculated value AND the formula string to the metadata inspector

### 4. MetadataInspector Enhancements
Improved value formatting for better user experience:

**Enhancements:**
- Numbers are formatted with locale-specific thousand separators
- Dates are displayed in readable format
- Booleans show as "TRUE" or "FALSE"
- Empty values show as "(empty)"

## How It Works

### Formula Storage and Display
Following the Technical Design specification:

1. **Storage**: Formulas are stored as strings beginning with `=` (e.g., `=C2*D2`)
2. **Execution**: HyperFormula creates a dependency graph and calculates results
3. **Display**: The grid shows the calculated result (e.g., `4999.95`)
4. **Inspector**: The formula string is visible in the Metadata Inspector when the cell is selected

### Example Flow
```
User Action: Changes Quantity (C2) from 5 to 10
   ↓
StateService.updateCellValue(2, 'C', 10)
   ↓
FormulaService.updateCell(2, 'C', 10)
   ↓
HyperFormula recalculates E2 (=C2*D2)
   ↓
FormulaService.getCalculatedData() returns updated values
   ↓
Grid refreshes showing new Total: 9999.90
```

### Real-time Dependency Updates
When you edit cell A1 that is referenced by formulas in B1, C1, and D1:
- HyperFormula automatically detects all dependencies
- All three cells (B1, C1, D1) are recalculated instantly
- The grid updates to show all new values

## Testing the Implementation

### With Example Data
The example template includes formulas in column E (Total):
- Formula: `=C*D` (Quantity × Unit Price)
- Try editing Quantity (C) or Unit Price (D)
- Watch the Total (E) update automatically

### Steps to Test
1. Start the application (it loads with example data)
2. Click on any cell in the "Total" column (E)
3. Check the Metadata Inspector - you should see:
   - **Value**: The calculated result (e.g., 4999.95)
   - **Formula**: The formula string (e.g., =C2*D2)
   - **Data Type**: formula
4. Edit a Quantity or Unit Price value
5. Observe the Total column update automatically

## Technical Details

### HyperFormula Configuration
```typescript
const config: Partial<ConfigParams> = {
    licenseKey: 'gpl-v3',  // Using GPL v3 license
    useColumnIndex: false,  // Use A1 notation (A, B, C instead of 0, 1, 2)
};
```

### Data Conversion
The service converts between two formats:

**Grid Format (Angular):**
```typescript
{ rowId: 2, A: 1, B: 'Laptop', C: 5, D: 999.99, E: '=C2*D2' }
```

**Sheet Format (HyperFormula):**
```typescript
[
  ['ID', 'Product Name', 'Quantity', 'Unit Price', 'Total'],  // Row 0: Headers
  [1, 'Laptop', 5, 999.99, '=C2*D2']                          // Row 1: Data
]
```

### Error Handling
HyperFormula can return `DetailedCellError` objects when formulas fail:
- Division by zero
- Invalid references
- Circular dependencies

The FormulaService converts these errors to `null` values to maintain type safety with our `CellValue` type.

## Future Enhancements

### Potential Improvements
1. **Error Display**: Show formula errors in the grid with specific error messages
2. **Formula Builder**: UI to help users construct formulas
3. **Advanced Functions**: Support for more Excel functions (VLOOKUP, IF, etc.)
4. **Formula Auditing**: Visual indicators showing cell dependencies
5. **Performance**: Optimize for large spreadsheets with many formulas

### Known Limitations
- Currently only supports formulas that HyperFormula can parse
- Complex Excel functions may not be supported
- No support for array formulas yet

## Dependencies
- **hyperformula**: ^2.7.1 (or latest version)
- Installed via: `npm install hyperformula`

## References
- [HyperFormula Documentation](https://hyperformula.handsontable.com/)
- [Technical Design Document](./TechnicalDesign.md) - Section 4.2: Formula Engine
