# Backend Formula Calculation Implementation

## Overview
Implemented backend formula calculation using EPPlus to ensure audit trail integrity and validate frontend calculations.

## Key Principles

### 1. **Formula Immutability**
- Formulas are defined **once** during Excel upload
- Users **cannot edit** formula cells (they are always `editable: false`)
- Users can only edit **input cells** that formulas reference
- Formula cells automatically recalculate when their dependencies change

### 2. **Dual Calculation Strategy**

#### Frontend (HyperFormula)
- **Purpose**: Instant UX feedback
- **When**: Real-time as user types
- **Library**: HyperFormula (JavaScript)
- **Result**: Immediate visual updates in the grid

#### Backend (EPPlus)
- **Purpose**: Audit integrity and validation
- **When**: On save operation
- **Library**: EPPlus built-in formula engine
- **Result**: Server-validated calculations stored in session

### 3. **Audit Trail Design**
- **Logs ALL cell changes** including both editable cells and formula results
- **Provides complete traceability** of how data evolved over time
- **Example scenario**:
  - User changes C2 from 10 → 20 (input cell)
  - Formula E2 (=C2*D2) recalculates from 100 → 200
  - Audit log shows BOTH changes:
    - "C2: 10 → 20" (direct user action)
    - "E2: 100 → 200" (calculated result change)
- **Formula strings** (e.g., "=C2*D2") are stored in data but never logged since they don't change
- **Rationale**: Complete audit trail shows both user actions AND their downstream effects on calculated values

## Implementation Details

### Files Created/Modified

#### New Files
1. **`backend/Services/FormulaService.cs`**
   - `RecalculateFormulas()`: Rebuilds Excel worksheet, calculates formulas, returns updated data
   - `ValidateFormulas()`: Compares frontend vs backend calculations (optional security check)
   - Uses EPPlus in-memory worksheet for calculation

#### Modified Files
1. **`backend/Services/SessionService.cs`**
   - Added `FormulaService` dependency injection
   - Updated `SaveChanges()` to recalculate formulas before saving
   - Ensures stored data has server-validated formula results

2. **`backend/Services/DiffService.cs`**
   - Updated to **track ALL cell changes** including formula results
   - Logs both user edits and calculated value changes
   - Provides complete audit trail of data evolution

3. **`backend/Program.cs`**
   - Registered `FormulaService` in DI container

4. **`docs/TechnicalDesign.md`**
   - Added section 4.2.2: Backend Formula Calculation
   - Added section 4.2.3: Formula Immutability
   - Documented dual-calculation strategy

## Data Flow on Save

```
User edits cell C2: 10 → 20
    ↓
Frontend: HyperFormula recalculates E2 (=C2*D2) instantly
    ↓
User clicks Save
    ↓
Frontend sends: { rowData with C2=20, E2="=C2*D2" }
    ↓
Backend receives data
    ↓
FormulaService.RecalculateFormulas():
  - Creates in-memory Excel worksheet
  - Writes all data and formulas
  - Calls worksheet.Calculate()
  - Reads back calculated results
    ↓
DiffService.CalculateDiff():
  - Compares old vs new data
  - Logs changes to ALL cells (editable + formula)
  - Captures complete data evolution
    ↓
Audit Log: 
  - "C2: 10 → 20" (user action)
  - "E2: 100 → 200" (formula result change)
    ↓
Session snapshot updated with recalculated data
```

## Benefits

1. **Security**: Backend independently validates all calculations
2. **Complete Audit Trail**: Logs both user actions AND their downstream effects on formulas
3. **Consistency**: Both frontend and backend use Excel-compatible formula engines
4. **Traceability**: Full visibility into how data evolved, including calculated values
5. **Simplicity**: Formulas never change after upload, reducing complexity

## Testing Recommendations

1. **Upload Excel with formulas** (e.g., `=C2*D2`)
2. **Edit input cell** (e.g., C2)
3. **Verify frontend** shows updated formula result
4. **Save changes**
5. **Check audit log**: Should show BOTH C2 change AND formula result change
6. **Verify backend** stored correct calculated value

## Future Enhancements (Optional)

1. **Formula Validation**: Add `ValidateFormulas()` call in save endpoint to detect tampering
2. **Calculation Logging**: Log backend calculation time for performance monitoring
3. **Formula Error Handling**: Return specific error messages for invalid formulas
4. **Calculation Cache**: Cache formula results to avoid recalculation on every save
