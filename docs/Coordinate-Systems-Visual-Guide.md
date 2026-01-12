# Coordinate Systems Visual Reference

## Three Coordinate Systems

```
┌─────────────────────────────────────────────────────────────────────┐
│                        EXCEL COORDINATE SYSTEM                       │
│                           (1-based rows)                             │
├─────────────────────────────────────────────────────────────────────┤
│                                                                       │
│     Row 1:  │  A  │  B  │  C  │  D  │  ← Headers                    │
│     Row 2:  │ ... │ ... │ ... │ ... │  ← First data row             │
│     Row 3:  │ ... │ ... │ ... │ ... │                               │
│     Row 4:  │ ... │ B4  │ ... │ ... │  ← Cell "B4"                  │
│     Row 5:  │ ... │ ... │ ... │ ... │                               │
│                                                                       │
│  Cell Reference: "B4" = Column B, Row 4                              │
│  Used by: Excel files, Audit logs, User interface                    │
└─────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│                      AG-GRID COORDINATE SYSTEM                       │
│                    (Array with persistent rowId)                     │
├─────────────────────────────────────────────────────────────────────┤
│                                                                       │
│  rowData[0] = { rowId: 2, A: "...", B: "...", C: "..." }            │
│  rowData[1] = { rowId: 4, A: "...", B: "...", C: "..." }  ← Sparse! │
│  rowData[2] = { rowId: 5, A: "...", B: "...", C: "..." }            │
│                                                                       │
│  ⚠️  Array index ≠ rowId                                             │
│  ⚠️  rowData[1].rowId = 4 (not 3!)                                   │
│                                                                       │
│  Cell Reference: { rowId: 4, field: "B" }                            │
│  Used by: StateService, Grid events, API requests                    │
└─────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│                   HYPERFORMULA COORDINATE SYSTEM                     │
│                          (0-based 2D array)                          │
├─────────────────────────────────────────────────────────────────────┤
│                                                                       │
│  sheetData[0] = ["Name", "Price", "Qty", "Total"]  ← Headers        │
│  sheetData[1] = ["...",  "...",   "...",  "..."]   ← First data     │
│  sheetData[2] = ["...",  "...",   "...",  "..."]                    │
│  sheetData[3] = ["...",  "...",   "...",  "..."]   ← Cell at [3][1] │
│  sheetData[4] = ["...",  "...",   "...",  "..."]                    │
│                                                                       │
│  Cell Reference: { sheet: 0, row: 3, col: 1 }                        │
│  Used by: FormulaService internal calculations                       │
└─────────────────────────────────────────────────────────────────────┘
```

## Coordinate Mapping Examples

### Example: Cell "B4" in all three systems

```
Excel:          Row 4, Column "B"
                ↓
AG-Grid:        { rowId: 4, field: "B" }
                ↓ (if rowData[1].rowId = 4)
HyperFormula:   { sheet: 0, row: 2, col: 1 }
                          ↑         ↑
                  arrayIndex=1,   field="B"
                  +1 for header   →colIndex=1
```

### Mapping Flow Diagram

```
┌──────────────────────────────────────────────────────────────────┐
│                    User edits cell "B4"                           │
└────────────────────────┬─────────────────────────────────────────┘
                         ↓
        ┌────────────────────────────────────────┐
        │  AG-Grid Event                         │
        │  { data: { rowId: 4 },                 │
        │    colDef: { field: "B" },             │
        │    newValue: 20 }                      │
        └────────────────┬───────────────────────┘
                         ↓
        ┌────────────────────────────────────────┐
        │  StateService                          │
        │  updateCellValue(4, "B", 20)           │
        └────────────────┬───────────────────────┘
                         ↓
        ┌────────────────────────────────────────┐
        │  CoordinateMapper.agGridToHyperFormula │
        │  Input:  { rowId: 4, field: "B" }      │
        │  Steps:                                │
        │    1. Find arrayIndex for rowId=4      │
        │       → arrayIndex = 1                 │
        │    2. Map to HF row: 1 + 1 = 2         │
        │    3. Find colIndex for field="B"      │
        │       → colIndex = 1                   │
        │  Output: { sheet: 0, row: 2, col: 1 }  │
        └────────────────┬───────────────────────┘
                         ↓
        ┌────────────────────────────────────────┐
        │  HyperFormula                          │
        │  setCellContents(                      │
        │    { sheet: 0, row: 2, col: 1 },       │
        │    20                                  │
        │  )                                     │
        │  → Recalculates dependent formulas     │
        └────────────────┬───────────────────────┘
                         ↓
        ┌────────────────────────────────────────┐
        │  Backend Save                          │
        │  { rowId: 4, cells: { B: 20 } }        │
        └────────────────┬───────────────────────┘
                         ↓
        ┌────────────────────────────────────────┐
        │  CoordinateMapper.FormatCellReference  │
        │  Input:  { rowId: 4, field: "B" }      │
        │  Output: "B4"                          │
        └────────────────┬───────────────────────┘
                         ↓
        ┌────────────────────────────────────────┐
        │  Audit Log Entry                       │
        │  { cellReference: "B4",                │
        │    oldValue: 10,                       │
        │    newValue: 20 }                      │
        └────────────────────────────────────────┘
```

## Common Pitfalls to Avoid

### ❌ DON'T: Assume array index equals rowId
```typescript
// WRONG!
const row = rowData[rowId];  // This will fail!
```

### ✅ DO: Use rowId to find the row
```typescript
// CORRECT
const row = rowData.find(r => r.rowId === rowId);
```

### ❌ DON'T: Manually calculate coordinates
```typescript
// WRONG - Error prone!
const hfRow = rowData.findIndex(r => r.rowId === rowId) + 1;
const hfCol = columnDefs.findIndex(c => c.field === field);
```

### ✅ DO: Use CoordinateMapper
```typescript
// CORRECT - Centralized and validated
const hfCoord = CoordinateMapper.agGridToHyperFormula(
    { rowId, field },
    rowData,
    columnDefs
);
```

### ❌ DON'T: Mix coordinate systems
```typescript
// WRONG - Mixing Excel row with HyperFormula column
worksheet.Cells[excelRow, hfCol];  // Inconsistent!
```

### ✅ DO: Convert consistently
```typescript
// CORRECT - Use proper conversion
const epplus = CoordinateMapper.GridToEPPlus(
    new GridCoordinate(rowId, field)
);
worksheet.Cells[epplus.Row, epplus.Column];
```

## Quick Reference Table

| System | Row Numbering | Column Format | Example Cell | Notes |
|--------|---------------|---------------|--------------|-------|
| **Excel** | 1-based (Row 1 = headers) | Letters (A, B, C...) | "B4" | User-facing format |
| **AG-Grid** | `rowId` property | Field name (A, B, C...) | `{ rowId: 4, field: "B" }` | Array index ≠ rowId |
| **HyperFormula** | 0-based (Row 0 = headers) | 0-based index | `{ sheet: 0, row: 3, col: 1 }` | Internal only |

## Performance Tips

### Use RowIndexCache for bulk operations
```typescript
// Processing 1000 cells
const cache = new RowIndexCache();
cache.rebuild(rowData);

for (const cell of cells) {
    // O(1) lookup instead of O(n)
    const hfCoord = cache.agGridToHyperFormula(
        { rowId: cell.rowId, field: cell.field },
        columnDefs
    );
    // Process...
}
```

### Rebuild cache when data changes
```typescript
// After upload, revert, or any data change
cache.rebuild(newRowData);
```
