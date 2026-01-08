# Xlsx-Grid-Flow

[![Frontend Deploy](https://img.shields.io/github/deployments/hangxigood/Xlsx-Grid-Flow/github-pages?label=Frontend&logo=github)](https://hangxigood.github.io/Xlsx-Grid-Flow/)
[![Backend](https://img.shields.io/badge/Backend-Azure-0078D4?logo=microsoftazure)](https://azure.microsoft.com/)

A secure, full-stack solution for transforming Excel templates into controlled web-based data entry interfaces with automated audit trails and professional PDF report generation.

**Live Demo:** [https://hangxigood.github.io/Xlsx-Grid-Flow/](https://hangxigood.github.io/Xlsx-Grid-Flow/)

---

## 🚀 Features

### Dynamic Excel Architecture
- **Structure Parsing**: Automatically extracts metadata, logic, and layouts from `.xlsx` files.
- **Merged Cell Support**: Preserves complex cell merging from Excel to the web grid.
- **Type Intelligence**: Detects and enforces Excel-defined data types (text, numbers, dates) with real-time validation.
- **Formula Engine**: Dual-layer calculation (Client-side interactive + Server-side validation) for maximum accuracy.

### Advanced Data Control
- **Role-Based Access**: Automatically identifies and locks read-only or formula columns defined in the template.
- **Real-Time Validation**: Immediate feedback with toast notifications and automatic reversion of invalid inputs.
- **Audit Trails**: Detailed history tracking (Old Value vs. New Value) for every modification.
- **Time Travel**: Browse historical versions and rollback the entire session to any previous state.

### Enterprise Outputs
- **PDF Export**: Generates high-fidelity PDF reports combining the final data grid with a chronological audit appendix.
- **Stateless Security**: "Privacy by Design"—all data is processed in-memory and purged upon session expiry. No database persistence.

---

## 🛠 Tech Stack

- **Frontend**: Angular 21+, TailwindCSS 4, AG-Grid 35
- **Backend**: .NET 8.0 Web API (C#)
- **Deployment**: 
  - Frontend: GitHub Pages
  - Backend: Azure App Service

## 🏗 Architecture

The system employs a **Stateless Transformation Layer** to ensure security and performance:
1. **Upload**: Users upload a standard Excel, which serves as the "configuration."
2. **Parse**: Backend extracts the schema, formulas, and data types.
3. **Interact**: Frontend provides a reactive, validation-rich interface.
4. **Audit**: Every save creates an immutable version snapshot in server memory.
5. **Export**: The final state and audit history are compiled into a comprehensive PDF.

For a deeper dive, read the [Technical Design Document](docs/TechnicalDesign.md).

---

## 🏃‍♂️ Quick Start

### Frontend
1. Install dependencies:
   ```bash
   npm install
   ```
2. Run the development server:
   ```bash
   ng serve
   ```
   Access the app at `http://localhost:4200/`.

### Backend
1. Navigate to the backend folder:
   ```bash
   cd backend
   ```
2. Run the .NET API:
   ```bash
   dotnet run
   ```
   The API will be available at `http://localhost:5000` (Swagger at `/swagger`).

## 📂 Project Structure

- **`/src`**: Angular frontend (Components, Services, Reactive Signals)
- **`/backend`**: .NET Web API (EPPlus, Audit Logic, PDF Service)
- **`/docs`**: Project documentation (PRD, Technical Design)

## 📄 Documentation

- [Product Requirements (PRD)](docs/PRD.md) - Detailed feature specifications.
- [Technical Design](docs/TechnicalDesign.md) - Architecture, data models, and implementation details.
