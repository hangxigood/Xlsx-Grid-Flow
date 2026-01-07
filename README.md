# Xlsx-Grid-Flow

A secure, full-stack solution for transforming Excel templates into controlled web-based data entry interfaces with automated audit trails and PDF report generation.

## Architecture

- **Frontend**: Angular 21 application deployed to GitHub Pages
- **Backend**: .NET 8.0 Web API deployed to Azure
- **Design**: Stateless, in-memory processing for maximum data privacy

## Project Structure

- `/src` - Angular frontend application
- `/backend` - .NET Web API backend
- `/docs` - Product requirements and technical design documentation

## Quick Start

### Frontend Development

To start a local development server, run:

```bash
ng serve
```

Once the server is running, open your browser and navigate to `http://localhost:4200/`. The application will automatically reload whenever you modify any of the source files.

## Code scaffolding

Angular CLI includes powerful code scaffolding tools. To generate a new component, run:

```bash
ng generate component component-name
```

For a complete list of available schematics (such as `components`, `directives`, or `pipes`), run:

```bash
ng generate --help
```

## Building

To build the project run:

```bash
ng build
```

This will compile your project and store the build artifacts in the `dist/` directory. By default, the production build optimizes your application for performance and speed.

### Backend Development

To run the backend API locally:

```bash
cd backend
dotnet run
```

The API will be available at `http://localhost:5000` with Swagger documentation at `http://localhost:5000/swagger`.

For more details, see [backend/README.md](backend/README.md).

## Running unit tests

To execute unit tests with the [Vitest](https://vitest.dev/) test runner, use the following command:

```bash
ng test
```

## Running end-to-end tests

For end-to-end (e2e) testing, run:

```bash
ng e2e
```

Angular CLI does not come with an end-to-end testing framework by default. You can choose one that suits your needs.

## Additional Resources

For more information on using the Angular CLI, including detailed command references, visit the [Angular CLI Overview and Command Reference](https://angular.dev/tools/cli) page.
