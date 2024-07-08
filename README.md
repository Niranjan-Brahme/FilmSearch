# Film Search Application

This project is a Film Search application that allows users to search for films, filter results, and view film details. The application is built using ASP.NET, jQuery, and Bootstrap.

## Features

- Search for films with an autocomplete feature
- Filter films by duration, rating, and release date
- Display search results with pagination
- Responsive design with modern UI elements

## Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download) installed
- [GitHub Desktop](https://desktop.github.com/) installed
- A [GitHub](https://github.com/) account

## Setup and Installation

### Step 1: Clone the Repository

1. Open GitHub Desktop.
2. Click on "File" in the menu and select "Clone repository".
3. In the pop-up window, choose the "URL" tab.
4. Enter the repository URL: `https://github.com/yourusername/film-search.git`
5. Click "Clone".

### Step 2: Open the Project in Visual Studio

1. Open Visual Studio.
2. Click on "File" > "Open" > "Project/Solution".
3. Navigate to the cloned repository folder and open the `.sln` file.

### Step 3: Run the Application

1. In Visual Studio, click the "Run" button or press `F5` to start the application.
2. Open your web browser and go to `https://localhost:5001` to view the application.

## Implementation Details

### Autocomplete Feature

- Uses jQuery UI Autocomplete to provide suggestions as the user types in the search box.
- Retrieves suggestions from the server via AJAX requests.

### Filtering Results

- Users can filter search results by duration, rating, and release date using dropdowns and date pickers.
- Filters are applied in real-time as the user changes the filter values.

### Pagination

- Results are displayed in a paginated format, allowing users to navigate through pages of results.
- Pagination controls include "Previous" and "Next" buttons and a dropdown to select the number of results per page.

## Custom Styling

- The application uses Bootstrap for responsive design and modern UI components.
- Custom CSS styles are applied for specific elements like the search box, filter section, and result panels.

### CSS for Rounded Autocomplete Box

```css
.ui-autocomplete {
    border-radius: 15px;
    box-shadow: 0 5px 15px rgba(0, 0, 0, 0.1);
    overflow: hidden;
}
