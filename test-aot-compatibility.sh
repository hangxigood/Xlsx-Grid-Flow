#!/bin/bash

# Native AOT Compatibility Test Script
# This script tests if your current dependencies support Native AOT compilation

set -e

echo "🔍 Native AOT Compatibility Test for Xlsx-Grid-Flow"
echo "=================================================="
echo ""

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Check if .NET 9 is installed
echo "📦 Checking .NET SDK version..."
if ! command -v dotnet &> /dev/null; then
    echo -e "${RED}❌ .NET SDK not found${NC}"
    echo "Install from: https://dotnet.microsoft.com/download/dotnet/10.0"
    exit 1
fi

DOTNET_VERSION=$(dotnet --version)
echo -e "${GREEN}✅ .NET SDK $DOTNET_VERSION found${NC}"
echo ""

# Check if we're in the right directory
if [ ! -f "backend/XlsxGridFlow.Api.csproj" ]; then
    echo -e "${RED}❌ Please run this script from the project root${NC}"
    exit 1
fi

# Create test directory
TEST_DIR="backend-aot-test"
echo "📁 Creating test project in $TEST_DIR..."
rm -rf $TEST_DIR
mkdir -p $TEST_DIR
cd $TEST_DIR

# Create test project with same dependencies
echo "🔧 Creating test project with Native AOT enabled..."
cat > XlsxGridFlow.AotTest.csproj << 'EOF'
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <OutputType>Exe</OutputType>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    
    <!-- Enable Native AOT -->
    <PublishAot>true</PublishAot>
    <InvariantGlobalization>true</InvariantGlobalization>
    <StripSymbols>true</StripSymbols>
  </PropertyGroup>

  <ItemGroup>
    <!-- Your current dependencies -->
    <PackageReference Include="EPPlus" Version="7.5.2" />
    <PackageReference Include="QuestPDF" Version="2024.12.3" />
    <PackageReference Include="Azure.Storage.Blobs" Version="12.22.0" />
  </ItemGroup>

</Project>
EOF

# Create minimal Program.cs
cat > Program.cs << 'EOF'
using OfficeOpenXml;
using QuestPDF.Fluent;
using Azure.Storage.Blobs;

Console.WriteLine("Testing Native AOT compatibility...");

// Test EPPlus
ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
using var package = new ExcelPackage();
var worksheet = package.Workbook.Worksheets.Add("Test");
worksheet.Cells["A1"].Value = "Hello AOT";
Console.WriteLine("✅ EPPlus basic test passed");

// Test QuestPDF
try 
{
    var document = QuestPDF.Fluent.Document.Create(container =>
    {
        container.Page(page =>
        {
            page.Content().Text("Hello AOT");
        });
    });
    Console.WriteLine("✅ QuestPDF basic test passed");
}
catch (Exception ex)
{
    Console.WriteLine($"⚠️  QuestPDF test failed: {ex.Message}");
}

// Test Azure Blob Storage
try
{
    var blobClient = new BlobServiceClient("UseDevelopmentStorage=true");
    Console.WriteLine("✅ Azure.Storage.Blobs basic test passed");
}
catch (Exception ex)
{
    Console.WriteLine($"⚠️  Azure.Storage.Blobs test: {ex.Message}");
}

Console.WriteLine("\n🎉 All basic tests completed!");
EOF

echo ""
echo "🔨 Building with Native AOT..."
echo "This may take 2-5 minutes on first run..."
echo ""

# Attempt to publish with AOT
if dotnet publish -c Release /p:PublishAot=true > build.log 2>&1; then
    echo -e "${GREEN}✅ Native AOT compilation SUCCEEDED!${NC}"
    echo ""
    
    # Check binary size
    if [ -f "bin/Release/net10.0/linux-x64/publish/XlsxGridFlow.AotTest" ]; then
        BINARY_SIZE=$(du -h bin/Release/net10.0/linux-x64/publish/XlsxGridFlow.AotTest | cut -f1)
        echo "📦 Binary size: $BINARY_SIZE"
    elif [ -f "bin/Release/net10.0/osx-arm64/publish/XlsxGridFlow.AotTest" ]; then
        BINARY_SIZE=$(du -h bin/Release/net10.0/osx-arm64/publish/XlsxGridFlow.AotTest | cut -f1)
        echo "📦 Binary size: $BINARY_SIZE"
    fi
    
    # Count warnings
    WARNING_COUNT=$(grep -c "warning IL" build.log || true)
    if [ $WARNING_COUNT -eq 0 ]; then
        echo -e "${GREEN}✅ No AOT warnings!${NC}"
    elif [ $WARNING_COUNT -lt 10 ]; then
        echo -e "${YELLOW}⚠️  $WARNING_COUNT AOT warnings (acceptable)${NC}"
    else
        echo -e "${RED}⚠️  $WARNING_COUNT AOT warnings (needs investigation)${NC}"
    fi
    
    echo ""
    echo "📊 Detailed warnings:"
    grep "warning IL" build.log | head -20 || echo "No warnings found"
    
else
    echo -e "${RED}❌ Native AOT compilation FAILED${NC}"
    echo ""
    echo "📋 Error details:"
    tail -50 build.log
    echo ""
    echo -e "${YELLOW}💡 This means some dependencies are not AOT-compatible${NC}"
    echo "   Consider using the hybrid approach (see AOT-Compatibility-Report.md)"
fi

echo ""
echo "📄 Full build log saved to: $TEST_DIR/build.log"
echo ""

# Analyze specific libraries
echo "🔍 Analyzing library compatibility..."
echo ""

if grep -q "EPPlus" build.log; then
    if grep -q "warning.*EPPlus" build.log; then
        echo -e "${YELLOW}⚠️  EPPlus: Has AOT warnings${NC}"
        echo "   Consider: DocumentFormat.OpenXml as alternative"
    else
        echo -e "${GREEN}✅ EPPlus: No obvious issues${NC}"
    fi
fi

if grep -q "QuestPDF" build.log; then
    if grep -q "warning.*QuestPDF" build.log; then
        echo -e "${YELLOW}⚠️  QuestPDF: Has AOT warnings${NC}"
        echo "   Consider: iText7 as alternative"
    else
        echo -e "${GREEN}✅ QuestPDF: No obvious issues${NC}"
    fi
fi

echo ""
echo "✅ Test complete!"
echo ""
echo "📚 Next steps:"
echo "1. Review build.log for detailed warnings"
echo "2. Check docs/AOT-Compatibility-Report.md for alternatives"
echo "3. If successful, proceed with docs/NativeAOT-Migration-Plan.md"
echo ""

cd ..
