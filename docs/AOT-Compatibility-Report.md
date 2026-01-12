# Native AOT Compatibility Report
## Xlsx-Grid-Flow Dependencies Analysis

---

## 📊 **Current Dependencies**

| Package | Version | AOT Status | Notes | Recommended Action |
|---------|---------|------------|-------|-------------------|
| **EPPlus** | 7.5.2 | ⚠️ **Unknown** | Uses reflection for Excel parsing | Test with `dotnet publish /p:PublishAot=true` |
| **QuestPDF** | 2024.12.3 | ⚠️ **Unknown** | May use dynamic code generation | Test or replace with iText7 |
| **System.Text.Json** | Built-in | ✅ **Compatible** | Requires source generators | Add `JsonSerializerContext` |
| **Azure.Storage.Blobs** | 12.22.0 | ✅ **Compatible** | Fully AOT-compatible | No changes needed |
| **Swashbuckle** | 6.4.0 | ❌ **Incompatible** | Uses reflection heavily | Replace with Scalar or remove |

---

## 🔍 **Testing Strategy**

### **Step 1: Build with AOT Warnings**

```bash
cd backend
dotnet publish -c Release /p:PublishAot=true 2>&1 | tee aot-warnings.txt
```

Look for these warning codes:
- **IL2026**: Using member with RequiresUnreferencedCode
- **IL2087**: Unrecognized reflection pattern
- **IL3050**: Using member with RequiresDynamicCode

### **Step 2: Analyze Warnings**

```bash
# Count warnings by type
grep "IL2026" aot-warnings.txt | wc -l
grep "IL3050" aot-warnings.txt | wc -l

# Find which libraries cause warnings
grep "IL2026" aot-warnings.txt | grep -o "in .*\.dll" | sort | uniq -c
```

### **Step 3: Test Runtime Behavior**

Even if it compiles, test these scenarios:
- [ ] Upload Excel file with formulas
- [ ] Parse merged cells
- [ ] Generate PDF with audit trail
- [ ] Serialize/deserialize complex objects

---

## 🔄 **Alternative Libraries (If Needed)**

### **Excel Parsing Alternatives**

| Library | AOT Support | License | Notes |
|---------|-------------|---------|-------|
| **ClosedXML** | ⚠️ Unknown | MIT | More modern than EPPlus |
| **NPOI** | ⚠️ Unknown | Apache 2.0 | Java POI port |
| **ExcelDataReader** | ✅ Likely | MIT | Read-only, lightweight |
| **DocumentFormat.OpenXml** | ✅ Yes | MIT | Microsoft official, verbose API |

**Recommendation**: Try **DocumentFormat.OpenXml** first (Microsoft official, likely AOT-compatible)

```bash
dotnet add package DocumentFormat.OpenXml --version 3.0.0
```

### **PDF Generation Alternatives**

| Library | AOT Support | License | Notes |
|---------|-------------|---------|-------|
| **iText7** | ✅ Likely | AGPL/Commercial | Industry standard |
| **PdfSharp** | ⚠️ Unknown | MIT | Simpler API |
| **Syncfusion** | ✅ Yes | Commercial | Expensive but guaranteed AOT |
| **QuestPDF** | ⚠️ Unknown | MIT | Current choice |

**Recommendation**: Try **iText7** if QuestPDF fails

```bash
dotnet add package itext7 --version 8.0.0
```

---

## 🛠️ **Workaround: Hybrid Approach**

If some libraries don't support AOT, use a **hybrid architecture**:

### **Option A: Separate Non-AOT Function**

```
✅ AOT Functions (fast cold start):
- UploadTemplate
- SaveSession
- GetAudit
- RevertSession

❌ Standard .NET Function (slower cold start, but works):
- ExportPdf (uses QuestPDF)
```

**Implementation:**

```xml
<!-- backend-functions-aot/XlsxGridFlow.Functions.csproj -->
<PublishAot>true</PublishAot>
<!-- All functions except PDF -->

<!-- backend-functions-pdf/XlsxGridFlow.PdfFunction.csproj -->
<!-- No PublishAot - standard .NET -->
<!-- Only PDF generation function -->
```

Deploy both:
```bash
# Deploy AOT functions
cd backend-functions-aot
func azure functionapp publish xlsx-grid-flow-functions

# Deploy PDF function separately
cd ../backend-functions-pdf
func azure functionapp publish xlsx-grid-flow-pdf-function
```

### **Option B: Move PDF to Durable Function**

Use Azure Durable Functions for long-running PDF generation:

```csharp
[Function("ExportPdf_Orchestrator")]
public async Task<string> RunOrchestrator(
    [OrchestrationTrigger] IDurableOrchestrationContext context)
{
    var sessionId = context.GetInput<string>();
    
    // This can run on standard .NET runtime
    var pdfUrl = await context.CallActivityAsync<string>(
        "GeneratePdfActivity", sessionId);
    
    return pdfUrl;
}
```

---

## 📋 **Testing Checklist**

### **Phase 1: Compilation Test**
- [ ] Run `dotnet publish /p:PublishAot=true`
- [ ] Check for IL2026, IL2087, IL3050 warnings
- [ ] Verify output binary size (<25MB target)
- [ ] Check trimming warnings

### **Phase 2: Runtime Test**
- [ ] Test Excel upload with simple file
- [ ] Test Excel upload with formulas
- [ ] Test Excel upload with merged cells
- [ ] Test session save/load
- [ ] Test PDF generation
- [ ] Test JSON serialization/deserialization

### **Phase 3: Performance Test**
- [ ] Measure cold start time (target: <500ms)
- [ ] Measure memory usage (target: <50MB)
- [ ] Measure response time (target: <200ms)
- [ ] Load test with 100 concurrent requests

### **Phase 4: Edge Cases**
- [ ] Large Excel files (>5MB)
- [ ] Complex formulas (nested, cross-sheet)
- [ ] Special characters in cell values
- [ ] Empty cells and null values
- [ ] Date formatting edge cases

---

## 🎯 **Decision Matrix**

| Scenario | Recommendation |
|----------|----------------|
| **All libraries work with AOT** | ✅ Full Native AOT deployment |
| **EPPlus fails, QuestPDF works** | Replace EPPlus with DocumentFormat.OpenXml |
| **QuestPDF fails, EPPlus works** | Replace QuestPDF with iText7 |
| **Both fail** | Hybrid approach (AOT + Standard .NET) |
| **Too many issues** | Standard .NET Functions (still 90% benefits) |

---

## 📊 **Expected Results**

### **Best Case (Full AOT)**
- Cold start: 200-500ms
- Memory: 30-50MB
- Binary size: 15-25MB
- Cost: $0-2/month

### **Hybrid Case (Mostly AOT)**
- Cold start: 300-800ms (PDF slower)
- Memory: 40-60MB
- Binary size: 20-30MB
- Cost: $1-5/month

### **Fallback Case (Standard .NET)**
- Cold start: 1-2 seconds
- Memory: 80-120MB
- Binary size: 60-80MB
- Cost: $3-10/month

**All cases are better than current App Service ($13-70/month)!**

---

## 🚀 **Next Steps**

1. **Test Current Setup**
   ```bash
   cd backend
   dotnet add package Microsoft.DotNet.ILCompiler -v 9.0.0
   dotnet publish -c Release /p:PublishAot=true
   ```

2. **Review Warnings**
   - If <10 warnings: Probably safe to proceed
   - If 10-50 warnings: Need investigation
   - If >50 warnings: Consider alternatives

3. **Create Test Plan**
   - Document all Excel features you use
   - Create test files covering edge cases
   - Set up automated testing

4. **Implement & Deploy**
   - Start with non-production environment
   - Monitor cold start times
   - Gradually migrate traffic

---

**Status**: Ready for testing
**Last Updated**: 2026-01-09
**Owner**: @hangxigood
