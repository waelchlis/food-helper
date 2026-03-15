# Angular Material Sample App

This project is an Angular application with Google Material Design library pre-installed and a sample page showcasing various Material components.

## Generated with Angular CLI version 21.2.1

### What's Included
- **Angular 21.2.1** - Latest version of the Angular framework
- **Angular Material 21.2.1** - Pre-configured Material Design components
- **Sample Components:**
  - Material Toolbar
  - Material Cards (3 variants)
  - Material Buttons (multiple styles)
  - Material Form Fields (input and select)
  - Material Data Table
  - Material Lists
  - Material Icons
  - Material Divider

## Development server

To start a local development server, run:

```bash
ng serve
```

Once the server is running, open your browser and navigate to `http://localhost:4200/`. The application will automatically reload whenever you modify any of the source files.

## Sample Page

The application displays a sample page at the root path with the following Material components:
- Header toolbar with title
- Welcome section with descriptive text
- 3-column card grid with Material cards
- Button showcase with various button styles and FAB buttons
- Form elements (text input and select dropdown)
- Data table with sample user data
- Material list with icons

## Customization

### Adding New Components
To generate new components, run:

```bash
ng generate component component-name
```

### Building for Production
To build the project for production, run:

```bash
npm run build
```

The build artifacts will be stored in the `dist/` directory.

## Project Structure

```
src/
├── app/
│   ├── app.ts                 # Root component
│   ├── app.html               # Root template
│   ├── app.scss               # Root styles
│   ├── sample-page/           # Sample page component
│   │   ├── sample-page.ts     # Component logic
│   │   ├── sample-page.html   # Component template
│   │   └── sample-page.scss   # Component styles
│   └── app.routes.ts          # Routing configuration
├── styles.scss                # Global styles
└── main.ts                    # Application entry point
```

## Material Components Used

- **MatToolbarModule** - Header toolbar
- **MatCardModule** - Content cards
- **MatButtonModule** - Interactive buttons
- **MatFormFieldModule** - Form field containers
- **MatInputModule** - Text input fields
- **MatSelectModule** - Dropdown selection
- **MatTableModule** - Data tables
- **MatListModule** - List component
- **MatIconModule** - Material icons
- **MatDividerModule** - Visual divider

## Further Help

For more information on Angular CLI, visit the [Angular CLI Overview and Command Reference](https://angular.dev/tools/cli) page or explore the [Angular official documentation](https://angular.dev)
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
