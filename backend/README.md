# Xlsx-Grid-Flow Backend API

This is the .NET 8.0 Web API backend for the Xlsx-Grid-Flow application.

## Overview

The backend provides a stateless, in-memory session management system for Excel template processing with:
- Excel file parsing with EPPlus
- Automatic formula and merged cell detection
- Version control and audit trail
- PDF report generation with QuestPDF
- Optimistic concurrency control

## Prerequisites

- .NET 8.0 SDK or later

## Getting Started

### Build the project
```bash
dotnet build
```

### Run the API locally
```bash
dotnet run
```

The API will start on `http://localhost:5155` by default (or check console output for actual port).

### Development with hot reload
```bash
dotnet watch run
```

## API Documentation

When running in development mode, Swagger UI is available at:
- `http://localhost:5155/swagger`

## API Endpoints

### Template Management
- **POST** `/api/template/upload` - Upload and parse Excel file

### Session Management
- **POST** `/api/session/{sessionId}/save` - Save changes and create new version
- **POST** `/api/session/{sessionId}/revert/{version}` - Revert to specific version
- **GET** `/api/session/{sessionId}/audit` - Get audit history

### Export
- **GET** `/api/session/{sessionId}/export/pdf` - Export session as PDF

## Configuration

Edit `appsettings.json` to configure:

```json
{
  "SessionSettings": {
    "ExpirationMinutes": 30,
    "MaxFileSizeMB": 10
  },
  "CorsSettings": {
    "AllowedOrigins": [
      "http://localhost:4200",
      "https://yourusername.github.io"
    ]
  }
}
```

## Project Structure

```
backend/
├── Controllers/          # API endpoints
│   ├── TemplateController.cs
│   ├── SessionController.cs
│   └── ExportController.cs
├── Services/            # Business logic
│   ├── ExcelService.cs
│   ├── SessionService.cs
│   ├── DiffService.cs
│   └── PdfService.cs
├── DTOs/               # Data transfer objects
│   ├── Requests/
│   └── Responses/
├── Models/             # Internal domain models
├── Configuration/      # Settings classes
├── Exceptions/         # Custom exceptions
└── Middleware/         # Exception handling
```

## Features

### Excel Parsing
- Supports `.xlsx` files
- Parses header conventions: `Name (text)`, `Total (ReadOnly)`
- Auto-detects data types from first row
- Preserves formulas (stored as `=FORMULA` strings)
- Extracts merged cell ranges

### Session Management
- In-memory sessions with 30-minute sliding expiration
- Version control with automatic diff calculation
- Optimistic concurrency control
- Point-in-time version reconstruction

### Audit Trail
- Tracks all cell-level changes
- Groups changes by version
- ISO 8601 timestamps
- Cell reference format (e.g., "B4")

### PDF Export
- Cover page with session metadata
- Current data table
- Complete audit trail
- Timestamped filenames

## Deployment

This API is designed to be deployed to Azure App Service. 

### Azure Deployment Steps
1. Create Azure App Service (Linux, .NET 8)
2. Configure CORS origins in `appsettings.json`
3. Deploy using:
   ```bash
   dotnet publish -c Release
   ```
4. Update frontend `environment.ts` with Azure API URL

## License

EPPlus and QuestPDF are used under their non-commercial licenses, suitable for side projects and personal use.

## Next Steps

- ✅ Core infrastructure implemented
- ✅ Excel parsing with EPPlus
- ✅ Session and diff management
- ✅ PDF generation
- ✅ API controllers
- 🔄 Integration testing with frontend
- 🔄 Deploy to Azure

